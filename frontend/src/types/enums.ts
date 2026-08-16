export const AdminRole = {
  SuperAdmin: "SuperAdmin",
  IncidentManager: "IncidentManager",
  Reviewer: "Reviewer",
  ReadOnlyAnalyst: "ReadOnlyAnalyst",
} as const;
export type AdminRole = (typeof AdminRole)[keyof typeof AdminRole];

export const VerificationStatus = {
  Pending: "Pending",
  Verified: "Verified",
  NeedsClarification: "NeedsClarification",
  SuspectedDuplicate: "SuspectedDuplicate",
  FlaggedAbuse: "FlaggedAbuse",
  Rejected: "Rejected",
} as const;
export type VerificationStatus = (typeof VerificationStatus)[keyof typeof VerificationStatus];

export const CaseStatus = {
  VerificationPending: "VerificationPending",
  UnderReview: "UnderReview",
  Assigned: "Assigned",
  InProgress: "InProgress",
  Resolved: "Resolved",
  Closed: "Closed",
  Rejected: "Rejected",
  Duplicate: "Duplicate",
} as const;
export type CaseStatus = (typeof CaseStatus)[keyof typeof CaseStatus];

export const IncidentPriority = {
  Low: "Low",
  Medium: "Medium",
  High: "High",
  Critical: "Critical",
} as const;
export type IncidentPriority = (typeof IncidentPriority)[keyof typeof IncidentPriority];

export const SourceChannel = {
  WhatsApp: "WhatsApp",
} as const;
export type SourceChannel = (typeof SourceChannel)[keyof typeof SourceChannel];

export const VerificationMethod = {
  AdminReview: "AdminReview",
  AutomatedDuplicateCheck: "AutomatedDuplicateCheck",
  ReporterClarification: "ReporterClarification",
} as const;
export type VerificationMethod = (typeof VerificationMethod)[keyof typeof VerificationMethod];

export const VerificationDecisionResult = {
  Approved: "Approved",
  Rejected: "Rejected",
  ClarificationRequested: "ClarificationRequested",
  MarkedDuplicate: "MarkedDuplicate",
  Escalated: "Escalated",
} as const;
export type VerificationDecisionResult =
  (typeof VerificationDecisionResult)[keyof typeof VerificationDecisionResult];

export const MediaType = {
  Image: "Image",
  Video: "Video",
  Audio: "Audio",
  Document: "Document",
} as const;
export type MediaType = (typeof MediaType)[keyof typeof MediaType];

export const VerificationDecisionAction = {
  Approve: "Approve",
  Reject: "Reject",
  RequestClarification: "RequestClarification",
  MarkDuplicate: "MarkDuplicate",
  Escalate: "Escalate",
} as const;
export type VerificationDecisionAction =
  (typeof VerificationDecisionAction)[keyof typeof VerificationDecisionAction];
