import { Ionicons } from '@expo/vector-icons';
import { createContext, ReactNode, useCallback, useContext, useMemo, useState } from 'react';

import { useReporterAuth } from '@/components/auth/reporter-auth-context';
import { MediaAttachment, reportsApi } from '@/lib/api/reports';

export type Severity = 'low' | 'medium' | 'high' | 'critical';
const SEVERITY_TO_PRIORITY: Record<Severity, 'Low' | 'Medium' | 'High' | 'Critical'> = {
  low: 'Low',
  medium: 'Medium',
  high: 'High',
  critical: 'Critical',
};

export type ReportCategory = {
  /** MobileCategoryDto.Id (Guid) — required to submit the report. */
  id: string;
  slug?: string | null;
  label: string;
  icon: keyof typeof Ionicons.glyphMap;
};

export type ReportDraft = {
  category?: ReportCategory;
  description: string;
  incidentDate: Date;
  severity?: Severity;
  locationLabel?: string;
  locationSubtitle?: string;
  latitude?: number;
  longitude?: number;
  landmark: string;
  photos: MediaAttachment[];
  voiceNote?: MediaAttachment;
};

const EMPTY_DRAFT: ReportDraft = {
  description: '',
  incidentDate: new Date(),
  landmark: '',
  photos: [],
};

type ReportDraftContextValue = {
  draft: ReportDraft;
  draftId: string | null;
  syncing: boolean;
  syncError: string | null;
  /** Local file URI of the just-recorded voice note, for in-app playback — never sent to the server. */
  voiceNoteLocalUri?: string;
  update: (patch: Partial<ReportDraft>) => void;
  /**
   * Creates the server draft if needed, then PATCHes it (full-replace) with
   * the current local state merged with `patch`. Call on each step's
   * Continue — pass the step's just-changed fields as `patch` rather than
   * calling `update()` first, since `update()`'s setState hasn't landed yet
   * by the time a same-tick `sync()` would read `draft`.
   */
  sync: (patch?: Partial<ReportDraft>) => Promise<void>;
  uploadPhoto: (file: { uri: string; name: string; mimeType: string }) => Promise<void>;
  removePhoto: (attachmentId: string) => Promise<void>;
  uploadVoiceNote: (file: { uri: string; name: string; mimeType: string }) => Promise<void>;
  removeVoiceNote: () => Promise<void>;
  /** POST /reports/drafts/{id}/submit — returns the real created report. */
  submit: () => ReturnType<typeof reportsApi.submitDraft>;
  reset: () => void;
};

const ReportDraftContext = createContext<ReportDraftContextValue | null>(null);

/**
 * Client-side wizard state, synced to a real backend ReportDraft (Phase 3 of
 * docs/mobile-client-backend-extension.md: PATCH /reports/drafts/{id}, full-
 * replace semantics — every sync() call sends the complete current state, not
 * a partial diff).
 */
export function ReportDraftProvider({ children }: { children: ReactNode }) {
  const { authorizedRequest } = useReporterAuth();
  const [draft, setDraft] = useState<ReportDraft>(EMPTY_DRAFT);
  const [draftId, setDraftId] = useState<string | null>(null);
  const [syncing, setSyncing] = useState(false);
  const [syncError, setSyncError] = useState<string | null>(null);
  const [voiceNoteLocalUri, setVoiceNoteLocalUri] = useState<string | undefined>();

  const update = useCallback((patch: Partial<ReportDraft>) => setDraft((d) => ({ ...d, ...patch })), []);
  const reset = useCallback(() => {
    setDraft(EMPTY_DRAFT);
    setDraftId(null);
    setVoiceNoteLocalUri(undefined);
  }, []);

  const sync = useCallback(
    async (patch?: Partial<ReportDraft>) => {
      const next = patch ? { ...draft, ...patch } : draft;
      if (patch) setDraft(next);

      setSyncError(null);
      setSyncing(true);
      try {
        let id = draftId;
        if (!id) {
          const created = await reportsApi.createDraft(authorizedRequest);
          id = created.id;
          setDraftId(id);
        }
        await reportsApi.updateDraft(authorizedRequest, id, {
          categoryId: next.category?.id ?? null,
          description: next.description || null,
          incidentOccurredAt: next.incidentDate.toISOString(),
          initialPrioritySignal: next.severity ? SEVERITY_TO_PRIORITY[next.severity] : null,
          locationDescription: next.locationLabel ?? null,
          latitude: next.latitude ?? null,
          longitude: next.longitude ?? null,
          landmark: next.landmark || null,
        });
      } catch (err) {
        setSyncError(err instanceof Error ? err.message : 'Could not save this step. Please try again.');
        throw err;
      } finally {
        setSyncing(false);
      }
    },
    [draftId, draft, authorizedRequest],
  );

  const ensureDraftId = useCallback(async () => {
    if (draftId) return draftId;
    const created = await reportsApi.createDraft(authorizedRequest);
    setDraftId(created.id);
    return created.id;
  }, [draftId, authorizedRequest]);

  const uploadPhoto = useCallback(
    async (file: { uri: string; name: string; mimeType: string }) => {
      const id = await ensureDraftId();
      const form = new FormData();
      form.append('files', { uri: file.uri, name: file.name, type: file.mimeType } as unknown as Blob);
      const [attachment] = await reportsApi.uploadDraftAttachments(authorizedRequest, id, form);
      if (attachment) update({ photos: [...draft.photos, attachment] });
    },
    [ensureDraftId, authorizedRequest, draft.photos, update],
  );

  const removePhoto = useCallback(
    async (attachmentId: string) => {
      if (!draftId) return;
      await reportsApi.deleteDraftAttachment(authorizedRequest, draftId, attachmentId);
      update({ photos: draft.photos.filter((p) => p.id !== attachmentId) });
    },
    [draftId, authorizedRequest, draft.photos, update],
  );

  const uploadVoiceNote = useCallback(
    async (file: { uri: string; name: string; mimeType: string }) => {
      const id = await ensureDraftId();
      const form = new FormData();
      form.append('files', { uri: file.uri, name: file.name, type: file.mimeType } as unknown as Blob);
      const [attachment] = await reportsApi.uploadDraftAttachments(authorizedRequest, id, form);
      if (attachment) {
        update({ voiceNote: attachment });
        setVoiceNoteLocalUri(file.uri);
      }
    },
    [ensureDraftId, authorizedRequest, update],
  );

  const removeVoiceNote = useCallback(async () => {
    if (!draftId || !draft.voiceNote) return;
    await reportsApi.deleteDraftAttachment(authorizedRequest, draftId, draft.voiceNote.id);
    update({ voiceNote: undefined });
    setVoiceNoteLocalUri(undefined);
  }, [draftId, draft.voiceNote, authorizedRequest, update]);

  const submit = useCallback(() => {
    if (!draftId) throw new Error('submit called with no draft');
    return reportsApi.submitDraft(authorizedRequest, draftId, true);
  }, [draftId, authorizedRequest]);

  const value = useMemo(
    () => ({
      draft,
      draftId,
      syncing,
      syncError,
      voiceNoteLocalUri,
      update,
      sync,
      uploadPhoto,
      removePhoto,
      uploadVoiceNote,
      removeVoiceNote,
      submit,
      reset,
    }),
    [
      draft,
      draftId,
      syncing,
      syncError,
      voiceNoteLocalUri,
      update,
      sync,
      uploadPhoto,
      removePhoto,
      uploadVoiceNote,
      removeVoiceNote,
      submit,
      reset,
    ],
  );

  return <ReportDraftContext.Provider value={value}>{children}</ReportDraftContext.Provider>;
}

export function useReportDraft() {
  const ctx = useContext(ReportDraftContext);
  if (!ctx) throw new Error('useReportDraft must be used within a ReportDraftProvider');
  return ctx;
}
