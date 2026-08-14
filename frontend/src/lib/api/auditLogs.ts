import { apiGet, buildQueryString } from "./client";
import type { PagedResult } from "@/types/common";
import type { AuditLogListItem, GetAuditLogsQuery } from "@/types/auditLogs";

export function getAuditLogs(query: GetAuditLogsQuery) {
  return apiGet<PagedResult<AuditLogListItem>>(`/api/admin/audit-logs${buildQueryString(query)}`);
}
