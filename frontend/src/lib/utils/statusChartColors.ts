import { CaseStatus, VerificationDecisionResult } from "@/types/enums";

/**
 * Per the dataviz skill's collision rule: when a bar's meaning IS good/bad (a
 * status), it wears status/semantic tokens, not the generic categorical palette —
 * each bar is directly labeled by the axis, so color is a reinforcing cue, not the
 * only channel.
 */
export function caseStatusChartColor(status: string): string {
  switch (status) {
    case CaseStatus.Resolved:
    case CaseStatus.Closed:
      return "var(--success)";
    case CaseStatus.InProgress:
      return "var(--warning)";
    case CaseStatus.UnderReview:
    case CaseStatus.Assigned:
      return "var(--primary)";
    case CaseStatus.Rejected:
    case CaseStatus.Duplicate:
      return "var(--destructive)";
    default:
      return "var(--muted-foreground)";
  }
}

export function verificationOutcomeChartColor(result: string): string {
  switch (result) {
    case VerificationDecisionResult.Approved:
      return "var(--success)";
    case VerificationDecisionResult.ClarificationRequested:
      return "var(--warning)";
    case VerificationDecisionResult.Escalated:
      return "var(--warning)";
    case VerificationDecisionResult.Rejected:
    case VerificationDecisionResult.MarkedDuplicate:
      return "var(--destructive)";
    default:
      return "var(--muted-foreground)";
  }
}
