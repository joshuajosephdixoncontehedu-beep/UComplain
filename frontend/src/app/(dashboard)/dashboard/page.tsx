"use client";

import { useState } from "react";
import { useQuery } from "@tanstack/react-query";
import {
  Activity,
  AlertTriangle,
  CalendarClock,
  CheckCircle2,
  Clock,
  FileText,
  ShieldCheck,
  Timer,
  XCircle,
} from "lucide-react";
import { PageHeader } from "@/components/layout/PageHeader";
import { DateRangeControl } from "@/components/dashboard/DateRangeControl";
import { MetricCard } from "@/components/dashboard/MetricCard";
import { ChartCard } from "@/components/dashboard/ChartCard";
import { ReportVolumeChart } from "@/components/dashboard/charts/ReportVolumeChart";
import { CategoryDistributionChart } from "@/components/dashboard/charts/CategoryDistributionChart";
import { StatusDistributionChart } from "@/components/dashboard/charts/StatusDistributionChart";
import { VerificationOutcomeChart } from "@/components/dashboard/charts/VerificationOutcomeChart";
import { HotspotsList } from "@/components/dashboard/HotspotsList";
import { PriorityReportsTable } from "@/components/dashboard/PriorityReportsTable";
import { RecentActivityTimeline } from "@/components/dashboard/RecentActivityTimeline";
import { VerificationQueueSnapshot } from "@/components/dashboard/VerificationQueueSnapshot";
import { Skeleton } from "@/components/ui/skeleton";
import { Alert, AlertDescription, AlertTitle } from "@/components/ui/alert";
import { useAuth } from "@/lib/auth/AuthProvider";
import { getDashboard } from "@/lib/api/dashboard";
import { ApiError } from "@/lib/api/client";
import { formatHours } from "@/lib/utils/format";
import { defaultDateRange } from "@/lib/utils/dateRange";

export default function DashboardPage() {
  const { admin } = useAuth();
  const [range, setRange] = useState(defaultDateRange());

  const { data, isLoading, isError, error } = useQuery({
    queryKey: ["dashboard", range.from, range.to],
    queryFn: () => getDashboard({ from: range.from, to: range.to }),
  });

  return (
    <div className="flex flex-col gap-6">
      <PageHeader
        title={`Welcome, ${admin?.fullName ?? ""}`}
        description="Operations overview for UComplain."
        actions={<DateRangeControl value={range} onChange={setRange} />}
      />

      {isError && (
        <Alert variant="destructive">
          <AlertTriangle className="size-4" />
          <AlertTitle>Couldn&apos;t load the dashboard</AlertTitle>
          <AlertDescription>
            {error instanceof ApiError ? error.message : "An unexpected error occurred. Please try again."}
          </AlertDescription>
        </Alert>
      )}

      {isLoading && <DashboardSkeleton />}

      {data && (
        <>
          <div className="grid grid-cols-2 gap-3 sm:grid-cols-2 lg:grid-cols-4">
            <MetricCard
              label="Total reports received"
              value={data.current.totalReportsReceived}
              previousValue={data.previous.totalReportsReceived}
              href="/reports"
              icon={FileText}
              accent="var(--chart-1)"
            />
            <MetricCard
              label="Awaiting verification"
              value={data.current.awaitingVerification}
              previousValue={data.previous.awaitingVerification}
              upIsGood={false}
              href="/verification"
              icon={Clock}
              accent="var(--chart-2)"
            />
            <MetricCard
              label="Verified, awaiting review"
              value={data.current.verifiedAwaitingReview}
              previousValue={data.previous.verifiedAwaitingReview}
              upIsGood={false}
              href="/reports?caseStatus=UnderReview"
              icon={ShieldCheck}
              accent="var(--chart-3)"
            />
            <MetricCard
              label="In progress"
              value={data.current.inProgress}
              previousValue={data.previous.inProgress}
              icon={Activity}
              accent="var(--chart-4)"
            />
            <MetricCard
              label="Resolved"
              value={data.current.resolved}
              previousValue={data.previous.resolved}
              href="/reports?caseStatus=Resolved"
              icon={CheckCircle2}
              accent="var(--chart-6)"
            />
            <MetricCard
              label="Rejected / duplicate / flagged"
              value={data.current.rejectedDuplicateOrFlagged}
              previousValue={data.previous.rejectedDuplicateOrFlagged}
              upIsGood={false}
              icon={XCircle}
              accent="var(--chart-8)"
            />
            <MetricCard
              label="Avg. verification time"
              value={data.current.averageVerificationTimeHours ?? 0}
              formatValue={() => formatHours(data.current.averageVerificationTimeHours)}
              icon={Timer}
              accent="var(--chart-7)"
            />
            <MetricCard
              label="Avg. resolution time"
              value={data.current.averageResolutionTimeHours ?? 0}
              formatValue={() => formatHours(data.current.averageResolutionTimeHours)}
              icon={CalendarClock}
              accent="var(--chart-5)"
            />
          </div>

          <div className="grid grid-cols-1 gap-4 lg:grid-cols-3">
            <ChartCard title="Report volume over time" className="lg:col-span-2">
              <ReportVolumeChart data={data.reportVolumeOverTime} />
            </ChartCard>
            <ChartCard title="Top hotspots" description="Locations with the most reports in range">
              <HotspotsList data={data.topHotspots} />
            </ChartCard>
          </div>

          <div className="grid grid-cols-1 gap-4 lg:grid-cols-3">
            <ChartCard title="Category distribution">
              <CategoryDistributionChart data={data.categoryDistribution} />
            </ChartCard>
            <ChartCard title="Status distribution">
              <StatusDistributionChart data={data.statusDistribution} />
            </ChartCard>
            <ChartCard title="Verification outcomes">
              <VerificationOutcomeChart data={data.verificationOutcomeDistribution} />
            </ChartCard>
          </div>

          <div className="grid grid-cols-1 gap-4 lg:grid-cols-3">
            <ChartCard title="Priority reports" description="Open High/Critical reports needing attention" className="lg:col-span-2">
              <PriorityReportsTable data={data.priorityReports} />
            </ChartCard>
            <ChartCard title="Verification queue">
              <VerificationQueueSnapshot data={data.verificationQueueSnapshot} />
            </ChartCard>
          </div>

          <ChartCard title="Recent activity">
            <RecentActivityTimeline data={data.recentActivity} />
          </ChartCard>
        </>
      )}
    </div>
  );
}

function DashboardSkeleton() {
  return (
    <div className="flex flex-col gap-6">
      <div className="grid grid-cols-2 gap-3 sm:grid-cols-2 lg:grid-cols-4">
        {Array.from({ length: 8 }).map((_, i) => (
          <Skeleton key={i} className="h-24 rounded-lg" />
        ))}
      </div>
      <div className="grid grid-cols-1 gap-4 lg:grid-cols-3">
        <Skeleton className="h-64 rounded-lg lg:col-span-2" />
        <Skeleton className="h-64 rounded-lg" />
      </div>
    </div>
  );
}
