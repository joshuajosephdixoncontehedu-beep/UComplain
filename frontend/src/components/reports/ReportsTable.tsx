"use client";

import Link from "next/link";
import { ArrowDown, ArrowUp, ArrowUpDown } from "lucide-react";
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table";
import { Checkbox } from "@/components/ui/checkbox";
import { StatusBadge } from "@/components/ui/status-badge";
import { caseStatusTone, priorityTone } from "@/lib/utils/statusStyles";
import { formatDateTime } from "@/lib/utils/format";
import type { IncidentReportListItem } from "@/types/reports";

interface SortState {
  sortBy: string;
  sortDir: "asc" | "desc";
}

interface ReportsTableProps {
  items: IncidentReportListItem[];
  sort: SortState;
  onSortChange: (sort: SortState) => void;
  selectable: boolean;
  selectedIds: Set<string>;
  onSelectionChange: (ids: Set<string>) => void;
}

const columns: { key: string; label: string; sortable: boolean }[] = [
  { key: "caseReference", label: "Case", sortable: false },
  { key: "category", label: "Category", sortable: false },
  { key: "location", label: "Location", sortable: true },
  { key: "priority", label: "Priority", sortable: true },
  { key: "caseStatus", label: "Status", sortable: true },
  { key: "assignedAdminName", label: "Assigned to", sortable: false },
  { key: "createdAt", label: "Reported", sortable: true },
];

export function ReportsTable({
  items,
  sort,
  onSortChange,
  selectable,
  selectedIds,
  onSelectionChange,
}: ReportsTableProps) {
  const allSelected = items.length > 0 && items.every((r) => selectedIds.has(r.id));

  const toggleAll = () => {
    if (allSelected) {
      onSelectionChange(new Set());
    } else {
      onSelectionChange(new Set(items.map((r) => r.id)));
    }
  };

  const toggleOne = (id: string) => {
    const next = new Set(selectedIds);
    if (next.has(id)) next.delete(id);
    else next.add(id);
    onSelectionChange(next);
  };

  const toggleSort = (key: string) => {
    if (sort.sortBy === key) {
      onSortChange({ sortBy: key, sortDir: sort.sortDir === "asc" ? "desc" : "asc" });
    } else {
      onSortChange({ sortBy: key, sortDir: "asc" });
    }
  };

  if (items.length === 0) {
    return (
      <div className="rounded-lg border border-border bg-card py-12 text-center text-sm text-muted-foreground">
        No reports match these filters.
      </div>
    );
  }

  return (
    <div className="rounded-lg border border-border bg-card">
      <Table>
        <TableHeader>
          <TableRow>
            {selectable && (
              <TableHead className="w-8">
                <Checkbox checked={allSelected} onCheckedChange={toggleAll} aria-label="Select all reports" />
              </TableHead>
            )}
            {columns.map((col) => (
              <TableHead key={col.key}>
                {col.sortable ? (
                  <button
                    type="button"
                    onClick={() => toggleSort(col.key)}
                    className="inline-flex items-center gap-1 hover:text-foreground"
                  >
                    {col.label}
                    {sort.sortBy === col.key ? (
                      sort.sortDir === "asc" ? (
                        <ArrowUp className="size-3" />
                      ) : (
                        <ArrowDown className="size-3" />
                      )
                    ) : (
                      <ArrowUpDown className="size-3 text-muted-foreground/50" />
                    )}
                  </button>
                ) : (
                  col.label
                )}
              </TableHead>
            ))}
          </TableRow>
        </TableHeader>
        <TableBody>
          {items.map((report) => (
            <TableRow key={report.id} data-state={selectedIds.has(report.id) ? "selected" : undefined}>
              {selectable && (
                <TableCell onClick={(e) => e.stopPropagation()}>
                  <Checkbox
                    checked={selectedIds.has(report.id)}
                    onCheckedChange={() => toggleOne(report.id)}
                    aria-label={`Select ${report.caseReference}`}
                  />
                </TableCell>
              )}
              <TableCell>
                <Link href={`/reports/${report.id}`} className="font-medium text-primary hover:underline">
                  {report.caseReference}
                </Link>
              </TableCell>
              <TableCell>{report.categoryName}</TableCell>
              <TableCell className="max-w-48 truncate">{report.locationDescription}</TableCell>
              <TableCell><StatusBadge value={report.priority} tone={priorityTone(report.priority)} /></TableCell>
              <TableCell><StatusBadge value={report.caseStatus} tone={caseStatusTone(report.caseStatus)} /></TableCell>
              <TableCell>{report.assignedAdminName ?? <span className="text-muted-foreground">Unassigned</span>}</TableCell>
              <TableCell className="text-muted-foreground">{formatDateTime(report.createdAt)}</TableCell>
            </TableRow>
          ))}
        </TableBody>
      </Table>
    </div>
  );
}
