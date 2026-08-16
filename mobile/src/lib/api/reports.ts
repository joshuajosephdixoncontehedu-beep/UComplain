import { AuthorizedRequestOptions } from '@/components/auth/reporter-auth-context';

// Mirrors backend/.../MobileReports/Dtos/*.cs and Domain/Enums/*.cs (camelCase on the wire).

export type IncidentPriority = 'Low' | 'Medium' | 'High' | 'Critical';
export type VerificationStatus = 'Pending' | 'Verified' | 'NeedsClarification' | 'SuspectedDuplicate' | 'FlaggedAbuse' | 'Rejected';
export type CaseStatus = 'VerificationPending' | 'UnderReview' | 'Assigned' | 'InProgress' | 'Resolved' | 'Closed' | 'Rejected' | 'Duplicate' | 'Withdrawn';
export type ReportListBucket = 'Active' | 'Resolved' | 'Rejected' | 'NotListed';
export type MediaType = 'Image' | 'Video' | 'Audio' | 'Document';

export type MediaAttachment = {
  id: string;
  fileName: string;
  mediaType: MediaType;
  mimeType: string;
  fileSizeBytes: number;
  sortOrder: number;
  uploadedAt: string;
};

export type MobileReportListItem = {
  id: string;
  caseReference: string;
  categoryName: string;
  createdAt: string;
  priority: IncidentPriority;
  verificationStatus: VerificationStatus;
  caseStatus: CaseStatus;
  attachmentCount: number;
  statusBadge: string;
  trackerStage: number | null;
  progressPercent: number;
  bucket: ReportListBucket;
};

export type MobileReportStatusHistory = { previousStatus: CaseStatus; newStatus: CaseStatus; createdAt: string };

export type MobileReportDetail = {
  id: string;
  caseReference: string;
  categoryId: string;
  categoryName: string;
  sourceChannel: 'WhatsApp' | 'MobileApp';
  description: string;
  incidentOccurredAt: string;
  locationDescription: string;
  latitude: number | null;
  longitude: number | null;
  landmark: string | null;
  verificationStatus: VerificationStatus;
  caseStatus: CaseStatus;
  priority: IncidentPriority;
  resolutionSummary: string | null;
  createdAt: string;
  updatedAt: string;
  closedAt: string | null;
  withdrawnAt: string | null;
  withdrawalReason: string | null;
  statusHistory: MobileReportStatusHistory[];
  attachments: MediaAttachment[];
};

export type ReportCounts = { active: number; resolved: number; rejected: number; total: number };

export type Draft = {
  id: string;
  categoryId: string | null;
  categoryName: string | null;
  description: string | null;
  incidentOccurredAt: string | null;
  initialPrioritySignal: IncidentPriority | null;
  locationDescription: string | null;
  latitude: number | null;
  longitude: number | null;
  landmark: string | null;
  submittedReportId: string | null;
  createdAt: string;
  updatedAt: string;
  attachments: MediaAttachment[];
};

export type UpdateDraftInput = {
  categoryId?: string | null;
  description?: string | null;
  incidentOccurredAt?: string | null;
  initialPrioritySignal?: IncidentPriority | null;
  locationDescription?: string | null;
  latitude?: number | null;
  longitude?: number | null;
  landmark?: string | null;
};

export type ClarificationResponse = { id: string; message: string; attachmentId: string | null; respondedAt: string };
export type ClarificationRequest = {
  id: string;
  message: string;
  requestedAt: string;
  dueAt: string;
  resolvedAt: string | null;
  autoClosedAt: string | null;
  responses: ClarificationResponse[];
};

export type ReportInformation = { id: string; message: string; attachmentId: string | null; createdAt: string };

type AuthorizedRequest = <T>(path: string, options?: AuthorizedRequestOptions) => Promise<T>;

export const reportsApi = {
  // --- Draft-based wizard ---
  createDraft: (req: AuthorizedRequest) => req<Draft>('api/mobile/reports/drafts', { method: 'POST' }),

  updateDraft: (req: AuthorizedRequest, id: string, input: UpdateDraftInput) =>
    req<Draft>(`api/mobile/reports/drafts/${id}`, { method: 'PATCH', body: input }),

  uploadDraftAttachments: (req: AuthorizedRequest, id: string, form: FormData) =>
    req<MediaAttachment[]>(`api/mobile/reports/drafts/${id}/attachments`, { method: 'POST', form }),

  deleteDraftAttachment: (req: AuthorizedRequest, draftId: string, attachmentId: string) =>
    req<void>(`api/mobile/reports/drafts/${draftId}/attachments/${attachmentId}`, { method: 'DELETE' }),

  submitDraft: (req: AuthorizedRequest, id: string, truthDeclarationAccepted: boolean) =>
    req<MobileReportDetail>(`api/mobile/reports/drafts/${id}/submit`, { method: 'POST', body: { truthDeclarationAccepted } }),

  // --- Reading own reports ---
  getMyReports: (req: AuthorizedRequest, query: { page?: number; pageSize?: number; status?: ReportListBucket }) =>
    req<{ items: MobileReportListItem[]; total: number; page: number; pageSize: number }>('api/mobile/reports', { query }),

  getCounts: (req: AuthorizedRequest) => req<ReportCounts>('api/mobile/reports/counts'),

  getById: (req: AuthorizedRequest, id: string) => req<MobileReportDetail>(`api/mobile/reports/${id}`),

  getTimeline: (req: AuthorizedRequest, id: string) => req<MobileReportStatusHistory[]>(`api/mobile/reports/${id}/timeline`),

  withdraw: (req: AuthorizedRequest, id: string, reason: string) =>
    req<MobileReportDetail>(`api/mobile/reports/${id}/withdraw`, { method: 'POST', body: { reason } }),

  // --- Reporter-initiated info / clarification replies ---
  getInformation: (req: AuthorizedRequest, id: string) => req<ReportInformation[]>(`api/mobile/reports/${id}/information`),

  addInformation: (req: AuthorizedRequest, id: string, message: string, attachmentId?: string) =>
    req<ReportInformation>(`api/mobile/reports/${id}/information`, { method: 'POST', body: { message, attachmentId: attachmentId ?? null } }),

  getClarifications: (req: AuthorizedRequest, id: string) => req<ClarificationRequest[]>(`api/mobile/reports/${id}/clarifications`),
};
