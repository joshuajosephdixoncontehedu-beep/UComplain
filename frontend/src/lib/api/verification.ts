import { apiGet, apiPost } from "./client";
import type { IncidentReportDetail } from "@/types/reports";
import type { VerificationDecisionRequest, VerificationQueueResponse } from "@/types/verification";

export function getVerificationQueue() {
  return apiGet<VerificationQueueResponse>("/api/admin/verification-queue");
}

export function submitVerificationDecision(reportId: string, request: VerificationDecisionRequest) {
  return apiPost<IncidentReportDetail>(`/api/admin/reports/${reportId}/verification-decision`, request);
}
