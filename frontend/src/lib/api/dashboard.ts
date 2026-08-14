import { apiGet, buildQueryString } from "./client";
import type { DashboardResponse } from "@/types/dashboard";

export interface DashboardDateRange {
  from?: string;
  to?: string;
}

export function getDashboard(range: DashboardDateRange) {
  return apiGet<DashboardResponse>(`/api/admin/dashboard${buildQueryString(range)}`);
}
