"use client";

import { useState } from "react";
import { useQuery } from "@tanstack/react-query";
import { AlertTriangle, Download } from "lucide-react";
import { toast } from "sonner";
import { PageHeader } from "@/components/layout/PageHeader";
import { DateRangeControl } from "@/components/dashboard/DateRangeControl";
import { ChartCard } from "@/components/dashboard/ChartCard";
import { MetricCard } from "@/components/dashboard/MetricCard";
import { ReportVolumeChart } from "@/components/dashboard/charts/ReportVolumeChart";
import { CategoryDistributionChart } from "@/components/dashboard/charts/CategoryDistributionChart";
import { StatusDistributionChart } from "@/components/dashboard/charts/StatusDistributionChart";
import { VerificationOutcomeChart } from "@/components/dashboard/charts/VerificationOutcomeChart";
import { Alert, AlertDescription, AlertTitle } from "@/components/ui/alert";
import { Skeleton } from "@/components/ui/skeleton";
import { Button } from "@/components/ui/button";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table";
import { getAnalytics, downloadAnalyticsCsv } from "@/lib/api/analytics";
import { ApiError } from "@/lib/api/client";
import { formatHours } from "@/lib/utils/format";
import { defaultDateRange } from "@/lib/utils/dateRange";

export default function AnalyticsPage() {
  const [range, setRange] = useState(defaultDateRange());
  const [isExporting, setIsExporting] = useState(false);

  const { data, isLoading, isError, error } = useQuery({
    queryKey: ["analytics", range.from, range.to],
    queryFn: () => getAnalytics({ from: range.from, to: range.to }),
  });

  const handleExport = async () => {
    setIsExporting(true);
    try {
      await downloadAnalyticsCsv({ from: range.from, to: range.to });
    } catch {
      toast.error("Couldn't export analytics as CSV.");
    } finally {
      setIsExporting(false);
    }
  };

  return (
    <div className="flex flex-col gap-6">
      <PageHeader
        title="Analytics"
        description="Report volume, distribution, and team performance for a chosen date range."
        actions={
          <div className="flex flex-wrap items-center gap-2">
            <DateRangeControl value={range} onChange={setRange} />
            <Button size="sm" variant="outline" disabled={isExporting || !data} onClick={handleExport}>
              <Download />
              Export CSV
            </Button>
          </div>
        }
      />

      {isError && (
        <Alert variant="destructive">
          <AlertTriangle className="size-4" />
          <AlertTitle>Couldn&apos;t load analytics</AlertTitle>
          <AlertDescription>
            {error instanceof ApiError ? error.message : "An unexpected error occurred. Please try again."}
          </AlertDescription>
        </Alert>
      )}

      {isLoading && (
        <div className="flex flex-col gap-4">
          <div className="grid grid-cols-2 gap-3 sm:grid-cols-4">
            {Array.from({ length: 4 }).map((_, i) => (
              <Skeleton key={i} className="h-24 rounded-lg" />
            ))}
          </div>
          <Skeleton className="h-64 rounded-lg" />
        </div>
      )}

      {data && (
        <>
          <div className="grid grid-cols-2 gap-3 sm:grid-cols-4">
            <MetricCard label="Total reports received" value={data.metrics.totalReportsReceived} />
            <MetricCard label="Resolved" value={data.metrics.resolved} />
            <MetricCard
              label="Avg. verification time"
              value={data.metrics.averageVerificationTimeHours ?? 0}
              formatValue={() => formatHours(data.metrics.averageVerificationTimeHours)}
            />
            <MetricCard
              label="Avg. resolution time"
              value={data.metrics.averageResolutionTimeHours ?? 0}
              formatValue={() => formatHours(data.metrics.averageResolutionTimeHours)}
            />
          </div>

          <div className="grid grid-cols-1 gap-4 lg:grid-cols-3">
            <ChartCard title="Report volume over time" className="lg:col-span-2">
              <ReportVolumeChart data={data.reportVolumeOverTime} />
            </ChartCard>
            <ChartCard title="Category distribution">
              <CategoryDistributionChart data={data.categoryDistribution} />
            </ChartCard>
          </div>

          <div className="grid grid-cols-1 gap-4 lg:grid-cols-2">
            <ChartCard title="Status distribution">
              <StatusDistributionChart data={data.statusDistribution} />
            </ChartCard>
            <ChartCard title="Verification outcomes">
              <VerificationOutcomeChart data={data.verificationOutcomeDistribution} />
            </ChartCard>
          </div>

          <div className="grid grid-cols-1 gap-4 lg:grid-cols-2">
            <ChartCard title="Assignment workload" description="Open assignments and reports resolved in range">
              {data.assignmentWorkload.length === 0 ? (
                <p className="py-6 text-center text-sm text-muted-foreground">No assignments in range.</p>
              ) : (
                <Table>
                  <TableHeader>
                    <TableRow>
                      <TableHead>Administrator</TableHead>
                      <TableHead className="text-right">Open</TableHead>
                      <TableHead className="text-right">Resolved in range</TableHead>
                    </TableRow>
                  </TableHeader>
                  <TableBody>
                    {data.assignmentWorkload.map((w) => (
                      <TableRow key={w.adminId}>
                        <TableCell className="font-medium">{w.adminName}</TableCell>
                        <TableCell className="text-right text-muted-foreground">{w.openAssignedCount}</TableCell>
                        <TableCell className="text-right text-muted-foreground">{w.resolvedInRangeCount}</TableCell>
                      </TableRow>
                    ))}
                  </TableBody>
                </Table>
              )}
            </ChartCard>

            <ChartCard title="Response time by category" description="Average time to resolution">
              {data.resolutionTimeByCategory.length === 0 ? (
                <p className="py-6 text-center text-sm text-muted-foreground">No resolved reports in range.</p>
              ) : (
                <Table>
                  <TableHeader>
                    <TableRow>
                      <TableHead>Category</TableHead>
                      <TableHead className="text-right">Avg. resolution time</TableHead>
                    </TableRow>
                  </TableHeader>
                  <TableBody>
                    {data.resolutionTimeByCategory.map((c) => (
                      <TableRow key={c.categoryName}>
                        <TableCell className="font-medium">{c.categoryName}</TableCell>
                        <TableCell className="text-right text-muted-foreground">
                          {formatHours(c.averageResolutionTimeHours)}
                        </TableCell>
                      </TableRow>
                    ))}
                  </TableBody>
                </Table>
              )}
            </ChartCard>
          </div>
        </>
      )}
    </div>
  );
}
