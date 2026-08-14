import type { CaseStatus, VerificationDecisionResult, VerificationStatus } from "./enums";

export interface ReporterListItem {
  id: string;
  maskedContactReference: string;
  verificationStatus: VerificationStatus;
  isRestricted: boolean;
  consentAt: string | null;
  reportCount: number;
  createdAt: string;
}

export interface ReporterReportSummary {
  id: string;
  caseReference: string;
  categoryName: string;
  caseStatus: CaseStatus;
  verificationStatus: VerificationStatus;
  createdAt: string;
}

export interface ReporterVerificationEvent {
  id: string;
  incidentReportId: string;
  caseReference: string;
  result: VerificationDecisionResult;
  notes: string | null;
  createdAt: string;
}

export interface ReporterDetail {
  id: string;
  maskedContactReference: string;
  verificationStatus: VerificationStatus;
  isRestricted: boolean;
  consentAt: string | null;
  createdAt: string;
  reports: ReporterReportSummary[];
  verificationHistory: ReporterVerificationEvent[];
}

export interface GetReportersQuery {
  page?: number;
  pageSize?: number;
  search?: string;
  verificationStatus?: VerificationStatus;
  isRestricted?: boolean;
}
