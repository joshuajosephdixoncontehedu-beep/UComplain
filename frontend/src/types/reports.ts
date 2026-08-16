import type { CaseStatus, IncidentPriority, MediaType, SourceChannel, VerificationDecisionResult, VerificationMethod, VerificationStatus } from "./enums";

export interface IncidentReportListItem {
  id: string;
  caseReference: string;
  categoryName: string;
  locationDescription: string;
  createdAt: string;
  priority: IncidentPriority;
  verificationStatus: VerificationStatus;
  caseStatus: CaseStatus;
  assignedAdminId: string | null;
  assignedAdminName: string | null;
}

export interface VerificationEventItem {
  id: string;
  verificationMethod: VerificationMethod;
  result: VerificationDecisionResult;
  attemptNumber: number;
  notes: string | null;
  performedByAdminId: string | null;
  performedByAdminName: string | null;
  createdAt: string;
}

export interface StatusHistoryItem {
  id: string;
  previousStatus: CaseStatus;
  newStatus: CaseStatus;
  changedByAdminId: string;
  changedByAdminName: string;
  notes: string | null;
  createdAt: string;
}

export interface InternalNoteItem {
  id: string;
  content: string;
  createdByAdminId: string;
  createdByAdminName: string;
  createdAt: string;
  updatedAt: string;
}

export interface ReportAssignmentItem {
  id: string;
  adminUserId: string;
  adminUserName: string;
  assignedByAdminId: string;
  assignedByAdminName: string;
  assignedAt: string;
  unassignedAt: string | null;
}

export interface AuditLogEntryItem {
  id: string;
  adminUserId: string | null;
  adminUserName: string | null;
  action: string;
  previousValueJson: string | null;
  newValueJson: string | null;
  createdAt: string;
}

export interface MediaAttachmentItem {
  id: string;
  fileName: string;
  mediaType: MediaType;
  mimeType: string;
  fileSizeBytes: number;
  sortOrder: number;
  uploadedAt: string;
}

export interface SignedUrlResponse {
  url: string;
  expiresAt: string;
}

export interface IncidentReportDetail {
  id: string;
  caseReference: string;
  reporterId: string;
  reporterMaskedContact: string;
  reporterIsRestricted: boolean;
  categoryId: string;
  categoryName: string;
  sourceChannel: SourceChannel;
  description: string;
  incidentOccurredAt: string;
  locationDescription: string;
  latitude: number | null;
  longitude: number | null;
  mediaReference: string | null;
  verificationStatus: VerificationStatus;
  caseStatus: CaseStatus;
  priority: IncidentPriority;
  assignedAdminId: string | null;
  assignedAdminName: string | null;
  resolutionSummary: string | null;
  createdAt: string;
  updatedAt: string;
  closedAt: string | null;
  verificationHistory: VerificationEventItem[];
  statusHistory: StatusHistoryItem[];
  notes: InternalNoteItem[];
  assignments: ReportAssignmentItem[];
  auditTrail: AuditLogEntryItem[];
  mediaAttachments: MediaAttachmentItem[];
}

export interface GetReportsQuery {
  page?: number;
  pageSize?: number;
  search?: string;
  categoryId?: string;
  priority?: IncidentPriority;
  caseStatus?: CaseStatus;
  assignedAdminId?: string;
  location?: string;
  from?: string;
  to?: string;
  sortBy?: string;
  sortDir?: "asc" | "desc";
}

export interface UpdateReportRequest {
  categoryId: string;
  priority: IncidentPriority;
  locationDescription: string;
  description: string;
}

export interface AssignReportRequest {
  adminUserId: string;
}

export interface AddNoteRequest {
  content: string;
}

export interface ChangeStatusRequest {
  newStatus: CaseStatus;
  notes?: string | null;
}
