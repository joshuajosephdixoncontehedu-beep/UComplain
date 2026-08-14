import Link from "next/link";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table";
import { StatusBadge } from "@/components/ui/status-badge";
import { caseStatusTone, priorityTone } from "@/lib/utils/statusStyles";
import { formatRelativeTime } from "@/lib/utils/format";
import type { PriorityReportItem } from "@/types/dashboard";

export function PriorityReportsTable({ data }: { data: PriorityReportItem[] }) {
  if (data.length === 0) {
    return <p className="py-6 text-center text-sm text-muted-foreground">No open high-priority reports right now.</p>;
  }

  return (
    <div className="overflow-x-auto">
      <Table>
        <TableHeader>
          <TableRow>
            <TableHead>Case</TableHead>
            <TableHead>Category</TableHead>
            <TableHead>Location</TableHead>
            <TableHead>Priority</TableHead>
            <TableHead>Status</TableHead>
            <TableHead>Assigned</TableHead>
            <TableHead className="text-right">Received</TableHead>
          </TableRow>
        </TableHeader>
        <TableBody>
          {data.map((report) => (
            <TableRow key={report.id}>
              <TableCell>
                <Link href={`/reports/${report.id}`} className="font-medium text-primary hover:underline">
                  {report.caseReference}
                </Link>
              </TableCell>
              <TableCell className="text-muted-foreground">{report.categoryName}</TableCell>
              <TableCell className="max-w-40 truncate text-muted-foreground">{report.locationDescription}</TableCell>
              <TableCell>
                <StatusBadge value={report.priority} tone={priorityTone(report.priority)} />
              </TableCell>
              <TableCell>
                <StatusBadge value={report.caseStatus} tone={caseStatusTone(report.caseStatus)} />
              </TableCell>
              <TableCell className="text-muted-foreground">{report.assignedAdminName ?? "Unassigned"}</TableCell>
              <TableCell className="text-right text-muted-foreground">{formatRelativeTime(report.createdAt)}</TableCell>
            </TableRow>
          ))}
        </TableBody>
      </Table>
    </div>
  );
}
