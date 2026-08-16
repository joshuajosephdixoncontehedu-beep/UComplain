import { CaseStatus, ReportListBucket, VerificationStatus } from '@/lib/api/reports';

export type StatusProjection = { badgeLabel: string; trackerStage: number | null; progressPercent: number; bucket: ReportListBucket };

/**
 * Faithful TypeScript port of backend/.../MobileReports/ReportStatusProjection.cs.
 * MobileReportListItemDto carries these fields pre-computed, but MobileReportDetailDto
 * only has the raw VerificationStatus/CaseStatus — so the detail screen (and its
 * timeline) needs this client-side. Keep in sync with the C# source if that changes.
 */
export function projectReportStatus(verificationStatus: VerificationStatus, caseStatus: CaseStatus): StatusProjection {
  if (caseStatus === 'Withdrawn') {
    return { badgeLabel: 'Withdrawn', trackerStage: null, progressPercent: 0, bucket: 'NotListed' };
  }

  switch (verificationStatus) {
    case 'Pending':
      return { badgeLabel: 'Awaiting verification', trackerStage: 1, progressPercent: 15, bucket: 'Active' };
    case 'NeedsClarification':
      return { badgeLabel: 'Needs clarification', trackerStage: 1, progressPercent: 25, bucket: 'Active' };
    case 'FlaggedAbuse':
      return { badgeLabel: 'Under review', trackerStage: 1, progressPercent: 25, bucket: 'Active' };
    case 'Rejected':
      return { badgeLabel: 'Rejected', trackerStage: null, progressPercent: 0, bucket: 'Rejected' };
    case 'SuspectedDuplicate':
      return { badgeLabel: 'Duplicate', trackerStage: null, progressPercent: 0, bucket: 'Rejected' };
    case 'Verified':
      return projectVerified(caseStatus);
    default:
      return { badgeLabel: verificationStatus, trackerStage: null, progressPercent: 0, bucket: 'Active' };
  }
}

function projectVerified(caseStatus: CaseStatus): StatusProjection {
  switch (caseStatus) {
    case 'UnderReview':
      return { badgeLabel: 'Verified', trackerStage: 3, progressPercent: 60, bucket: 'Active' };
    case 'Assigned':
      return { badgeLabel: 'Assigned', trackerStage: 3, progressPercent: 70, bucket: 'Active' };
    case 'InProgress':
      return { badgeLabel: 'In progress', trackerStage: 4, progressPercent: 85, bucket: 'Active' };
    case 'Resolved':
      return { badgeLabel: 'Resolved', trackerStage: 5, progressPercent: 100, bucket: 'Resolved' };
    case 'Closed':
      return { badgeLabel: 'Closed', trackerStage: 5, progressPercent: 100, bucket: 'Resolved' };
    case 'Rejected':
      return { badgeLabel: 'Rejected', trackerStage: null, progressPercent: 0, bucket: 'Rejected' };
    case 'Duplicate':
      return { badgeLabel: 'Duplicate', trackerStage: null, progressPercent: 0, bucket: 'Rejected' };
    default:
      return { badgeLabel: 'Verified', trackerStage: 2, progressPercent: 40, bucket: 'Active' };
  }
}

const CASE_STATUS_LABEL: Record<CaseStatus, string> = {
  VerificationPending: 'Submitted',
  UnderReview: 'Verified as genuine',
  Assigned: 'Assigned to a responder',
  InProgress: 'Work in progress',
  Resolved: 'Resolved',
  Closed: 'Closed',
  Rejected: 'Rejected',
  Duplicate: 'Marked as duplicate',
  Withdrawn: 'Withdrawn',
};

export function caseStatusLabel(status: CaseStatus): string {
  return CASE_STATUS_LABEL[status];
}
