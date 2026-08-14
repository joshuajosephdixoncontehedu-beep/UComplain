import { apiGet, apiPost, buildQueryString } from "./client";
import type { PagedResult } from "@/types/common";
import type { GetReportersQuery, ReporterDetail, ReporterListItem } from "@/types/reporters";

export function getReporters(query: GetReportersQuery) {
  return apiGet<PagedResult<ReporterListItem>>(`/api/admin/users${buildQueryString(query)}`);
}

export function getReporter(id: string) {
  return apiGet<ReporterDetail>(`/api/admin/users/${id}`);
}

export function restrictReporter(id: string) {
  return apiPost<ReporterDetail>(`/api/admin/users/${id}/restrict`);
}

export function unrestrictReporter(id: string) {
  return apiPost<ReporterDetail>(`/api/admin/users/${id}/unrestrict`);
}
