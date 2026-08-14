import { PageHeader } from "@/components/layout/PageHeader";
import { ComingSoon } from "@/components/layout/ComingSoon";

export default function UsersPage() {
  return (
    <div className="flex flex-col gap-6">
      <PageHeader title="Users" description="Community reporters who have submitted incidents via WhatsApp." />
      <ComingSoon feature="The reporter list and detail views" />
    </div>
  );
}
