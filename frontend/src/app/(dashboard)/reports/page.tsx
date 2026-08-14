import { PageHeader } from "@/components/layout/PageHeader";
import { ComingSoon } from "@/components/layout/ComingSoon";

export default function ReportsPage() {
  return (
    <div className="flex flex-col gap-6">
      <PageHeader title="Reports" description="Verified incident reports in the operational queue." />
      <ComingSoon feature="The reports list and detail views" />
    </div>
  );
}
