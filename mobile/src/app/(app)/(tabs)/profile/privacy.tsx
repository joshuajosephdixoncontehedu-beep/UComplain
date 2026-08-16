import { Ionicons } from '@expo/vector-icons';
import { useEffect, useState } from 'react';
import { ActivityIndicator, Linking, Pressable, ScrollView, Text, View } from 'react-native';

import { BackButton } from '@/components/ui/back-button';
import { MenuRow } from '@/components/ui/menu-row';
import { ToggleRow } from '@/components/ui/toggle-row';
import { useReporterAuth } from '@/components/auth/reporter-auth-context';
import { AccountDeletionRequest, DataExportRequest, meApi, ReporterPrivacySetting } from '@/lib/api/me';

const WHAT_WE_STORE = [
  'Your email, stored as an encrypted reference',
  'Location pins attached to your reports',
  'Everything is deleted after 24 months',
];

function formatDate(iso: string) {
  return new Date(iso).toLocaleDateString('en-GB', { day: '2-digit', month: 'short', year: 'numeric' });
}

const EXPORT_SUBTITLE: Record<DataExportRequest['status'], string> = {
  Pending: 'Preparing your export…',
  Processing: 'Preparing your export…',
  Completed: 'Ready — tap to open',
  Failed: 'Export failed — tap to try again',
};

/** Figma "20 Privacy & data" (node 18:165). */
export default function PrivacyAndData() {
  const { authorizedRequest } = useReporterAuth();

  const [privacy, setPrivacy] = useState<ReporterPrivacySetting | null>(null);
  useEffect(() => {
    meApi
      .getPrivacy(authorizedRequest)
      .then(setPrivacy)
      .catch(() => undefined);
    // Fetch once on mount.
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  const updateToggle = async (patch: Partial<Pick<ReporterPrivacySetting, 'usePreciseLocation' | 'showOnPublicMap' | 'allowResponderContact'>>) => {
    if (!privacy) return;
    const next = { ...privacy, ...patch };
    setPrivacy(next);
    try {
      const saved = await meApi.updatePrivacy(authorizedRequest, {
        usePreciseLocation: next.usePreciseLocation,
        showOnPublicMap: next.showOnPublicMap,
        allowResponderContact: next.allowResponderContact,
      });
      setPrivacy(saved);
    } catch {
      setPrivacy(privacy);
    }
  };

  const [dataExport, setDataExport] = useState<DataExportRequest | null>(null);
  useEffect(() => {
    meApi
      .getLatestDataExport(authorizedRequest)
      .then(setDataExport)
      .catch(() => undefined);
    // Fetch once on mount; 404 (no export requested yet) is expected and left as null.
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  const onDataExportPress = async () => {
    if (dataExport?.status === 'Completed' && dataExport.downloadUrl) {
      Linking.openURL(dataExport.downloadUrl);
      return;
    }
    if (dataExport?.status === 'Pending' || dataExport?.status === 'Processing') return;
    const created = await meApi.requestDataExport(authorizedRequest);
    setDataExport(created);
  };

  const [deletion, setDeletion] = useState<AccountDeletionRequest | null>(null);
  const [confirmingDelete, setConfirmingDelete] = useState(false);
  const [deleting, setDeleting] = useState(false);

  const onConfirmDelete = async () => {
    setDeleting(true);
    try {
      const req = await meApi.requestAccountDeletion(authorizedRequest);
      setDeletion(req);
      setConfirmingDelete(false);
    } finally {
      setDeleting(false);
    }
  };

  const onCancelDeletion = async () => {
    const req = await meApi.cancelAccountDeletion(authorizedRequest);
    setDeletion(req);
  };

  return (
    <ScrollView className="flex-1 bg-canvas" contentContainerClassName="pb-10">
      <View className="mt-[64px] flex-row items-center gap-5 px-5">
        <BackButton />
        <Text className="text-body-lg font-semibold text-ink">Privacy & data</Text>
      </View>

      <View className="mx-5 mt-9 gap-4 rounded-card border border-border bg-surface p-4">
        <View className="flex-row items-center gap-2.5">
          <Ionicons name="shield-checkmark-outline" size={18} color="#0F172A" />
          <Text className="text-body-lg font-semibold text-ink">What we store</Text>
        </View>
        {WHAT_WE_STORE.map((line) => (
          <View key={line} className="flex-row items-start gap-2.5">
            <Ionicons name="checkmark-outline" size={15} color="#64748B" style={{ marginTop: 2 }} />
            <Text className="flex-1 text-body-sm text-secondary">{line}</Text>
          </View>
        ))}
      </View>

      <Text className="mb-2 mt-9 px-5 text-eyebrow uppercase tracking-wide text-muted">Your choices</Text>
      {!privacy ? (
        <View className="mx-5 items-center rounded-card border border-border bg-surface py-8">
          <ActivityIndicator color="#1D4ED8" />
        </View>
      ) : (
        <View className="mx-5 rounded-card border border-border bg-surface">
          <ToggleRow
            title="Precise location"
            subtitle="Attach an exact pin instead of an area"
            value={privacy.usePreciseLocation}
            onValueChange={(v) => updateToggle({ usePreciseLocation: v })}
          />
          <View className="h-px bg-border" />
          <ToggleRow
            title="Show on public map"
            subtitle="Only after a report is verified"
            value={privacy.showOnPublicMap}
            onValueChange={(v) => updateToggle({ showOnPublicMap: v })}
          />
          <View className="h-px bg-border" />
          <ToggleRow
            title="Allow responder contact"
            subtitle="Officers may reach you via a masked address"
            value={privacy.allowResponderContact}
            onValueChange={(v) => updateToggle({ allowResponderContact: v })}
          />
        </View>
      )}

      <Text className="mb-2 mt-9 px-5 text-eyebrow uppercase tracking-wide text-muted">Manage your data</Text>
      <View className="mx-5 rounded-card border border-border bg-surface">
        <MenuRow
          icon="download-outline"
          title="Download my data"
          subtitle={dataExport ? EXPORT_SUBTITLE[dataExport.status] : 'A copy of your reports and account'}
          onPress={onDataExportPress}
        />
        <View className="h-px bg-border" />
        {deletion && deletion.status === 'Pending' ? (
          <View className="gap-3 px-3.5 py-3.5">
            <View className="flex-row items-center gap-3.5">
              <View className="h-[34px] w-[34px] items-center justify-center rounded-full bg-status-critical-tint">
                <Ionicons name="trash-outline" size={17} color="#B91C1C" />
              </View>
              <View className="flex-1">
                <Text className="text-body-lg font-semibold text-status-critical">Account deletion scheduled</Text>
                <Text className="mt-0.5 text-body-sm text-muted">Scheduled for {formatDate(deletion.scheduledForAt)}</Text>
              </View>
            </View>
            <Pressable onPress={onCancelDeletion} className="h-11 items-center justify-center rounded-input border border-border">
              <Text className="text-body-sm font-semibold text-ink">Cancel deletion</Text>
            </Pressable>
          </View>
        ) : confirmingDelete ? (
          <View className="gap-3 px-3.5 py-3.5">
            <Text className="text-body-sm text-secondary">
              This schedules your account for deletion. You can cancel any time before the scheduled date.
            </Text>
            <View className="flex-row gap-3">
              <Pressable onPress={() => setConfirmingDelete(false)} className="h-11 flex-1 items-center justify-center rounded-input border border-border">
                <Text className="text-body-sm font-semibold text-ink">Cancel</Text>
              </Pressable>
              <Pressable
                onPress={onConfirmDelete}
                disabled={deleting}
                className="h-11 flex-1 items-center justify-center rounded-input bg-status-critical disabled:opacity-50">
                <Text className="text-body-sm font-semibold text-surface">{deleting ? 'Confirming…' : 'Confirm deletion'}</Text>
              </Pressable>
            </View>
          </View>
        ) : (
          <MenuRow icon="trash-outline" title="Delete my account" subtitle="Removes your data permanently" tone="danger" onPress={() => setConfirmingDelete(true)} />
        )}
      </View>
    </ScrollView>
  );
}
