import type { AuditLogEntryItem } from "./reports";
import type { CaseStatus, IncidentPriority } from "./enums";

export interface DashboardMetrics {
  totalReportsReceived: number;
  awaitingVerification: number;
  verifiedAwaitingReview: number;
  inProgress: number;
  resolved: number;
  rejectedDuplicateOrFlagged: number;
  averageVerificationTimeHours: number | null;
  averageResolutionTimeHours: number | null;
}

export interface NamedCount {
  name: string;
  count: number;
}

export interface TimeSeriesPoint {
  date: string;
  count: number;
}

export interface VerificationQueueSnapshot {
  pending: number;
  needsClarification: number;
  suspectedDuplicate: number;
  flaggedAbuse: number;
  rejected: number;
}

export interface PriorityReportItem {
  id: string;
  caseReference: string;
  categoryName: string;
  locationDescription: string;
  priority: IncidentPriority;
  caseStatus: CaseStatus;
  assignedAdminName: string | null;
  createdAt: string;
}

export interface DashboardResponse {
  from: string;
  to: string;
  current: DashboardMetrics;
  previous: DashboardMetrics;
  reportVolumeOverTime: TimeSeriesPoint[];
  categoryDistribution: NamedCount[];
  statusDistribution: NamedCount[];
  verificationOutcomeDistribution: NamedCount[];
  topHotspots: NamedCount[];
  priorityReports: PriorityReportItem[];
  recentActivity: AuditLogEntryItem[];
  verificationQueueSnapshot: VerificationQueueSnapshot;
}
