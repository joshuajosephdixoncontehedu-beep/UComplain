import { apiGet, apiPatch, apiPost, buildQueryString } from "./client";
import type { PagedResult } from "@/types/common";
import type {
  AddNoteRequest,
  AssignReportRequest,
  ChangeStatusRequest,
  GetReportsQuery,
  IncidentReportDetail,
  IncidentReportListItem,
  UpdateReportRequest,
} from "@/types/reports";

export function getReports(query: GetReportsQuery) {
  return apiGet<PagedResult<IncidentReportListItem>>(`/api/admin/reports${buildQueryString(query)}`);
}

export function getReport(id: string) {
  return apiGet<IncidentReportDetail>(`/api/admin/reports/${id}`);
}

export function updateReport(id: string, request: UpdateReportRequest) {
  return apiPatch<IncidentReportDetail>(`/api/admin/reports/${id}`, request);
}

export function assignReport(id: string, request: AssignReportRequest) {
  return apiPost<IncidentReportDetail>(`/api/admin/reports/${id}/assign`, request);
}

export function addReportNote(id: string, request: AddNoteRequest) {
  return apiPost<IncidentReportDetail>(`/api/admin/reports/${id}/notes`, request);
}

export function changeReportStatus(id: string, request: ChangeStatusRequest) {
  return apiPost<IncidentReportDetail>(`/api/admin/reports/${id}/status`, request);
}
