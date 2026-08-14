import { PageHeader } from "@/components/layout/PageHeader";
import { ComingSoon } from "@/components/layout/ComingSoon";

export default function AdministratorsPage() {
  return (
    <div className="flex flex-col gap-6">
      <PageHeader title="Administrators" description="Manage administrator accounts and roles. SuperAdmin only." />
      <ComingSoon feature="Administrator management" />
    </div>
  );
}
