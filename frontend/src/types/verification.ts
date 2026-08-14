import type { IncidentPriority, VerificationDecisionAction, VerificationStatus } from "./enums";

export interface VerificationQueueItem {
  id: string;
  caseReference: string;
  categoryName: string;
  locationDescription: string;
  priority: IncidentPriority;
  verificationStatus: VerificationStatus;
  categorySlaHours: number;
  createdAt: string;
  attemptCount: number;
  reporterMaskedContact: string;
}

export interface VerificationQueueResponse {
  pending: VerificationQueueItem[];
  needsClarification: VerificationQueueItem[];
  suspectedDuplicate: VerificationQueueItem[];
  flaggedAbuse: VerificationQueueItem[];
  rejected: VerificationQueueItem[];
}

export interface VerificationDecisionRequest {
  action: VerificationDecisionAction;
  reason?: string | null;
}
