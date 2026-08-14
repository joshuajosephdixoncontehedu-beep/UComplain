import { apiGet, buildQueryString } from "./client";
import type { AnalyticsResponse } from "@/types/analytics";

export interface AnalyticsDateRange {
  from?: string;
  to?: string;
}

export function getAnalytics(range: AnalyticsDateRange) {
  return apiGet<AnalyticsResponse>(`/api/admin/analytics${buildQueryString(range)}`);
}

export function getAnalyticsCsvExportUrl(range: AnalyticsDateRange, apiBaseUrl: string): string {
  return `${apiBaseUrl}/api/admin/analytics${buildQueryString({ ...range, format: "csv" })}`;
}
