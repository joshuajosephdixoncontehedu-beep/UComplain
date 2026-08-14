import { PageHeader } from "@/components/layout/PageHeader";
import { ComingSoon } from "@/components/layout/ComingSoon";

export default function AuditLogsPage() {
  return (
    <div className="flex flex-col gap-6">
      <PageHeader title="Audit Logs" description="Read-only record of every state-changing action. SuperAdmin only." />
      <ComingSoon feature="The audit log table" />
    </div>
  );
}
