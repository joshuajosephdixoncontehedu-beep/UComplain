export interface AuditLogListItem {
  id: string;
  adminUserId: string | null;
  adminUserName: string | null;
  action: string;
  entityType: string;
  entityId: string;
  previousValueJson: string | null;
  newValueJson: string | null;
  ipAddress: string | null;
  userAgent: string | null;
  createdAt: string;
}

export interface GetAuditLogsQuery {
  page?: number;
  pageSize?: number;
  adminUserId?: string;
  action?: string;
  entityType?: string;
  from?: string;
  to?: string;
}
