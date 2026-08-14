import { PageHeader } from "@/components/layout/PageHeader";
import { ComingSoon } from "@/components/layout/ComingSoon";

export default function AnalyticsPage() {
  return (
    <div className="flex flex-col gap-6">
      <PageHeader title="Analytics" description="Trends, workload, and response-time analytics." />
      <ComingSoon feature="Analytics charts and CSV export" />
    </div>
  );
}
