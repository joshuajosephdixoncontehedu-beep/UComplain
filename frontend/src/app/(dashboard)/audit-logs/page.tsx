"use client";

import { useState } from "react";
import { useQuery } from "@tanstack/react-query";
import { AlertTriangle, ShieldOff } from "lucide-react";
import { PageHeader } from "@/components/layout/PageHeader";
import { Alert, AlertDescription, AlertTitle } from "@/components/ui/alert";
import { Skeleton } from "@/components/ui/skeleton";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table";
import { ReportsPagination } from "@/components/reports/ReportsPagination";
import { AuditLogDetailDialog } from "@/components/audit-logs/AuditLogDetailDialog";
import { useAuth } from "@/lib/auth/AuthProvider";
import { isSuperAdmin } from "@/lib/auth/permissions";
import { getAuditLogs } from "@/lib/api/auditLogs";
import { getAdministrators } from "@/lib/api/administrators";
import { ApiError } from "@/lib/api/client";
import { humanizePascalCase } from "@/lib/utils/statusStyles";
import { formatDateTime } from "@/lib/utils/format";
import type { AuditLogListItem } from "@/types/auditLogs";

const PAGE_SIZE = 25;
const ALL = "all";

export default function AuditLogsPage() {
  const { admin: currentAdmin } = useAuth();
  const canView = currentAdmin ? isSuperAdmin(currentAdmin.role) : false;

  const [adminUserId, setAdminUserId] = useState<string | undefined>(undefined);
  const [entityType, setEntityType] = useState("");
  const [page, setPage] = useState(1);
  const [selectedEntry, setSelectedEntry] = useState<AuditLogListItem | null>(null);

  const query = { adminUserId, entityType: entityType || undefined, page, pageSize: PAGE_SIZE };
  const { data, isLoading, isError, error } = useQuery({
    queryKey: ["audit-logs", query],
    queryFn: () => getAuditLogs(query),
    enabled: canView,
  });
  const { data: administrators } = useQuery({
    queryKey: ["administrators"],
    queryFn: getAdministrators,
    enabled: canView,
  });

  if (currentAdmin && !canView) {
    return (
      <div className="flex flex-col gap-6">
        <PageHeader title="Audit Logs" description="A record of every administrator action." />
        <Alert>
          <ShieldOff className="size-4" />
          <AlertTitle>Access restricted</AlertTitle>
          <AlertDescription>Only Super Admins can view the audit log.</AlertDescription>
        </Alert>
      </div>
    );
  }

  return (
    <div className="flex flex-col gap-6">
      <PageHeader title="Audit Logs" description="A read-only record of every administrator action." />

      <div className="grid grid-cols-1 gap-3 rounded-lg border border-border bg-card p-4 sm:grid-cols-2">
        <div className="flex flex-col gap-1.5">
          <Label>Administrator</Label>
          <Select
            value={adminUserId ?? ALL}
            onValueChange={(value) => {
              setAdminUserId(!value || value === ALL ? undefined : value);
              setPage(1);
            }}
          >
            <SelectTrigger className="w-full"><SelectValue placeholder="Anyone" /></SelectTrigger>
            <SelectContent>
              <SelectItem value={ALL}>Anyone</SelectItem>
              {(administrators ?? []).map((a) => (
                <SelectItem key={a.id} value={a.id}>{a.fullName}</SelectItem>
              ))}
            </SelectContent>
          </Select>
        </div>

        <div className="flex flex-col gap-1.5">
          <Label htmlFor="entity-type">Entity type</Label>
          <Input
            id="entity-type"
            placeholder="e.g. IncidentReport, AdminUser…"
            value={entityType}
            onChange={(e) => {
              setEntityType(e.target.value);
              setPage(1);
            }}
          />
        </div>
      </div>

      {isError && (
        <Alert variant="destructive">
          <AlertTriangle className="size-4" />
          <AlertTitle>Couldn&apos;t load audit logs</AlertTitle>
          <AlertDescription>
            {error instanceof ApiError ? error.message : "An unexpected error occurred. Please try again."}
          </AlertDescription>
        </Alert>
      )}

      {isLoading && <Skeleton className="h-64 rounded-lg" />}

      {data && (
        <>
          <div className="rounded-lg border border-border bg-card">
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>Timestamp</TableHead>
                  <TableHead>Administrator</TableHead>
                  <TableHead>Action</TableHead>
                  <TableHead>Entity</TableHead>
                  <TableHead>IP address</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {data.items.map((entry) => (
                  <TableRow
                    key={entry.id}
                    className="cursor-pointer"
                    onClick={() => setSelectedEntry(entry)}
                  >
                    <TableCell className="text-muted-foreground">{formatDateTime(entry.createdAt)}</TableCell>
                    <TableCell>{entry.adminUserName ?? "System"}</TableCell>
                    <TableCell className="font-medium">{humanizePascalCase(entry.action)}</TableCell>
                    <TableCell className="text-muted-foreground">{entry.entityType}</TableCell>
                    <TableCell className="text-muted-foreground">{entry.ipAddress ?? "—"}</TableCell>
                  </TableRow>
                ))}
                {data.items.length === 0 && (
                  <TableRow>
                    <TableCell colSpan={5} className="py-8 text-center text-sm text-muted-foreground">
                      No audit log entries match these filters.
                    </TableCell>
                  </TableRow>
                )}
              </TableBody>
            </Table>
          </div>
          <ReportsPagination page={page} pageSize={PAGE_SIZE} total={data.total} onPageChange={setPage} />
        </>
      )}

      <AuditLogDetailDialog entry={selectedEntry} onOpenChange={(open) => !open && setSelectedEntry(null)} />
    </div>
  );
}
