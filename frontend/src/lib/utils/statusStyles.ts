import { CaseStatus, IncidentPriority, VerificationDecisionResult, VerificationStatus } from "@/types/enums";

/**
 * Consistent badge coloring for every status/priority value across the app — one
 * source of truth so the same status always reads the same way on every page.
 * Semantic colors only: green = resolved, amber = needs attention, red = critical/
 * rejected, blue = active/in-progress, slate = neutral/inactive.
 */
export const badgeToneClasses = {
  slate: "bg-muted text-muted-foreground border-transparent",
  blue: "bg-primary/10 text-primary border-transparent",
  green: "bg-success/10 text-success border-transparent",
  amber: "bg-warning/10 text-warning border-transparent",
  red: "bg-destructive/10 text-destructive border-transparent",
} as const;

export type BadgeTone = keyof typeof badgeToneClasses;

export function caseStatusTone(status: CaseStatus): BadgeTone {
  switch (status) {
    case CaseStatus.VerificationPending:
      return "slate";
    case CaseStatus.UnderReview:
    case CaseStatus.Assigned:
      return "blue";
    case CaseStatus.InProgress:
      return "amber";
    case CaseStatus.Resolved:
      return "green";
    case CaseStatus.Closed:
      return "slate";
    case CaseStatus.Rejected:
      return "red";
    case CaseStatus.Duplicate:
      return "slate";
  }
}

export function verificationStatusTone(status: VerificationStatus): BadgeTone {
  switch (status) {
    case VerificationStatus.Pending:
      return "amber";
    case VerificationStatus.Verified:
      return "green";
    case VerificationStatus.NeedsClarification:
      return "amber";
    case VerificationStatus.SuspectedDuplicate:
      return "slate";
    case VerificationStatus.FlaggedAbuse:
      return "red";
    case VerificationStatus.Rejected:
      return "red";
  }
}

export function priorityTone(priority: IncidentPriority): BadgeTone {
  switch (priority) {
    case IncidentPriority.Low:
      return "slate";
    case IncidentPriority.Medium:
      return "blue";
    case IncidentPriority.High:
      return "amber";
    case IncidentPriority.Critical:
      return "red";
  }
}

export function verificationResultTone(result: VerificationDecisionResult): BadgeTone {
  switch (result) {
    case VerificationDecisionResult.Approved:
      return "green";
    case VerificationDecisionResult.ClarificationRequested:
    case VerificationDecisionResult.Escalated:
      return "amber";
    case VerificationDecisionResult.Rejected:
    case VerificationDecisionResult.MarkedDuplicate:
      return "red";
  }
}

const statusLabels: Record<string, string> = {
  VerificationPending: "Verification Pending",
  UnderReview: "Under Review",
  InProgress: "In Progress",
  NeedsClarification: "Needs Clarification",
  SuspectedDuplicate: "Suspected Duplicate",
  FlaggedAbuse: "Flagged Abuse",
  ClarificationRequested: "Clarification Requested",
  MarkedDuplicate: "Marked Duplicate",
};

/** "UnderReview" -> "Under Review"; falls back to the raw value for already-spaced names. */
export function humanizeEnumValue(value: string): string {
  return statusLabels[value] ?? value;
}

/** "ReportStatusChanged" -> "Report Status Changed" — for audit log action names. */
export function humanizePascalCase(value: string): string {
  return value.replace(/([a-z])([A-Z])/g, "$1 $2").replace(/([A-Z])([A-Z][a-z])/g, "$1 $2");
}
