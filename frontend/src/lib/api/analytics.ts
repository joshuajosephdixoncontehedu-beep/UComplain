import { apiGet, buildQueryString } from "./client";
import { getAccessToken } from "@/lib/auth/tokenStore";
import type { AnalyticsResponse } from "@/types/analytics";

const API_BASE_URL = process.env.NEXT_PUBLIC_API_BASE_URL ?? "http://localhost:5058";

export interface AnalyticsDateRange {
  from?: string;
  to?: string;
}

export function getAnalytics(range: AnalyticsDateRange) {
  return apiGet<AnalyticsResponse>(`/api/admin/analytics${buildQueryString(range)}`);
}

/**
 * Downloads the CSV export directly rather than linking to it: the endpoint requires
 * the JWT bearer token, which only lives in memory (see tokenStore.ts) and can't be
 * attached to a plain <a href> click, so this fetches it with the header and saves
 * the response as a blob instead.
 */
export async function downloadAnalyticsCsv(range: AnalyticsDateRange): Promise<void> {
  const accessToken = getAccessToken();
  const response = await fetch(
    `${API_BASE_URL}/api/admin/analytics${buildQueryString({ ...range, format: "csv" })}`,
    { headers: accessToken ? { Authorization: `Bearer ${accessToken}` } : {} },
  );
  if (!response.ok) {
    throw new Error("Couldn't export analytics as CSV.");
  }
  const blob = await response.blob();
  const url = URL.createObjectURL(blob);
  const link = document.createElement("a");
  link.href = url;
  link.download = `cirs-analytics-${range.from ?? "all"}-to-${range.to ?? "now"}.csv`;
  document.body.appendChild(link);
  link.click();
  link.remove();
  URL.revokeObjectURL(url);
}
