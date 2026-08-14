"use client";

import { Suspense, useState } from "react";
import { useRouter, useSearchParams } from "next/navigation";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { AlertTriangle } from "lucide-react";
import { toast } from "sonner";
import { PageHeader } from "@/components/layout/PageHeader";
import { Alert, AlertDescription, AlertTitle } from "@/components/ui/alert";
import { Skeleton } from "@/components/ui/skeleton";
import { ReportsFilterBar, type ReportFilters } from "@/components/reports/ReportsFilterBar";
import { ReportsTable } from "@/components/reports/ReportsTable";
import { ReportsPagination } from "@/components/reports/ReportsPagination";
import { BulkAssignBar } from "@/components/reports/BulkAssignBar";
import { AssignAdminDialog } from "@/components/reports/AssignAdminDialog";
import { useAuth } from "@/lib/auth/AuthProvider";
import { isManagerOrAbove } from "@/lib/auth/permissions";
import { getReports, assignReport } from "@/lib/api/reports";
import { getCategories } from "@/lib/api/categories";
import { getAdministrators } from "@/lib/api/administrators";
import { ApiError } from "@/lib/api/client";
import type { CaseStatus, IncidentPriority } from "@/types/enums";

const PAGE_SIZE = 20;

function ReportsPageContent() {
  const { admin } = useAuth();
  const router = useRouter();
  const searchParams = useSearchParams();
  const queryClient = useQueryClient();
  const canBulkAssign = admin ? isManagerOrAbove(admin.role) : false;

  const [filters, setFilters] = useState<ReportFilters>(() => ({
    search: searchParams.get("search") ?? undefined,
    categoryId: searchParams.get("categoryId") ?? undefined,
    priority: (searchParams.get("priority") as IncidentPriority) ?? undefined,
    caseStatus: (searchParams.get("caseStatus") as CaseStatus) ?? undefined,
    assignedAdminId: searchParams.get("assignedAdminId") ?? undefined,
    location: searchParams.get("location") ?? undefined,
    from: searchParams.get("from") ?? undefined,
    to: searchParams.get("to") ?? undefined,
  }));
  const [page, setPage] = useState(1);
  const [sort, setSort] = useState<{ sortBy: string; sortDir: "asc" | "desc" }>({
    sortBy: "createdAt",
    sortDir: "desc",
  });
  const [selectedIds, setSelectedIds] = useState<Set<string>>(new Set());
  const [assignDialogOpen, setAssignDialogOpen] = useState(false);

  const applyFilters = (next: ReportFilters) => {
    setFilters(next);
    setPage(1);
    setSelectedIds(new Set());
    router.replace(`/reports${buildShareableQuery(next)}`, { scroll: false });
  };

  const query = { ...filters, page, pageSize: PAGE_SIZE, sortBy: sort.sortBy, sortDir: sort.sortDir };

  const { data, isLoading, isError, error } = useQuery({
    queryKey: ["reports", query],
    queryFn: () => getReports(query),
  });

  const { data: categories } = useQuery({ queryKey: ["categories"], queryFn: getCategories });
  const { data: administrators } = useQuery({ queryKey: ["administrators"], queryFn: getAdministrators });

  const bulkAssignMutation = useMutation({
    mutationFn: async (adminUserId: string) => {
      const results = await Promise.allSettled(
        [...selectedIds].map((id) => assignReport(id, { adminUserId })),
      );
      const failed = results.filter((r) => r.status === "rejected").length;
      return { total: results.length, failed };
    },
    onSuccess: ({ total, failed }) => {
      if (failed === 0) {
        toast.success(`Assigned ${total} report${total === 1 ? "" : "s"}.`);
      } else {
        toast.warning(`Assigned ${total - failed} of ${total} reports — ${failed} failed.`);
      }
      setSelectedIds(new Set());
      setAssignDialogOpen(false);
      queryClient.invalidateQueries({ queryKey: ["reports"] });
    },
    onError: () => toast.error("Couldn't assign the selected reports."),
  });

  return (
    <div className="flex flex-col gap-6">
      <PageHeader title="Reports" description="Verified incident reports in the operational queue." />

      <ReportsFilterBar
        filters={filters}
        onChange={applyFilters}
        categories={categories ?? []}
        administrators={administrators ?? []}
      />

      {canBulkAssign && (
        <BulkAssignBar
          selectedCount={selectedIds.size}
          onClear={() => setSelectedIds(new Set())}
          onAssign={() => setAssignDialogOpen(true)}
        />
      )}

      {isError && (
        <Alert variant="destructive">
          <AlertTriangle className="size-4" />
          <AlertTitle>Couldn&apos;t load reports</AlertTitle>
          <AlertDescription>
            {error instanceof ApiError ? error.message : "An unexpected error occurred. Please try again."}
          </AlertDescription>
        </Alert>
      )}

      {isLoading && (
        <div className="flex flex-col gap-2">
          {Array.from({ length: 6 }).map((_, i) => (
            <Skeleton key={i} className="h-11 rounded-lg" />
          ))}
        </div>
      )}

      {data && (
        <>
          <ReportsTable
            items={data.items}
            sort={sort}
            onSortChange={(next) => {
              setSort(next);
              setPage(1);
            }}
            selectable={canBulkAssign}
            selectedIds={selectedIds}
            onSelectionChange={setSelectedIds}
          />
          <ReportsPagination page={page} pageSize={PAGE_SIZE} total={data.total} onPageChange={setPage} />
        </>
      )}

      <AssignAdminDialog
        open={assignDialogOpen}
        onOpenChange={setAssignDialogOpen}
        title="Assign selected reports"
        description={`Assign ${selectedIds.size} selected report${selectedIds.size === 1 ? "" : "s"} to an administrator.`}
        administrators={administrators ?? []}
        isSubmitting={bulkAssignMutation.isPending}
        onConfirm={(adminUserId) => bulkAssignMutation.mutate(adminUserId)}
      />
    </div>
  );
}

function buildShareableQuery(filters: ReportFilters): string {
  const params = new URLSearchParams();
  for (const [key, value] of Object.entries(filters)) {
    if (value) params.set(key, String(value));
  }
  const qs = params.toString();
  return qs ? `?${qs}` : "";
}

export default function ReportsPage() {
  return (
    <Suspense fallback={<Skeleton className="h-96 rounded-lg" />}>
      <ReportsPageContent />
    </Suspense>
  );
}
