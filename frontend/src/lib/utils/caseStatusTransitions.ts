import { CaseStatus } from "@/types/enums";

/**
 * Mirrors IncidentReportService.AllowedTransitions on the backend exactly, so the UI
 * only offers buttons for transitions the API will actually accept. The backend is
 * still the source of truth and re-validates on every request — this is UX only.
 * Statuses absent from this map (VerificationPending, Closed, Rejected, Duplicate)
 * have no manual next step; verification decisions and reopening are separate flows.
 */
const AllowedTransitions: Partial<Record<CaseStatus, CaseStatus[]>> = {
  [CaseStatus.UnderReview]: [CaseStatus.Assigned, CaseStatus.InProgress],
  [CaseStatus.Assigned]: [CaseStatus.InProgress, CaseStatus.UnderReview],
  [CaseStatus.InProgress]: [CaseStatus.Resolved, CaseStatus.Assigned],
  [CaseStatus.Resolved]: [CaseStatus.Closed, CaseStatus.InProgress],
};

export function getAllowedNextStatuses(current: CaseStatus): CaseStatus[] {
  return AllowedTransitions[current] ?? [];
}
