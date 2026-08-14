import { PageHeader } from "@/components/layout/PageHeader";
import { ComingSoon } from "@/components/layout/ComingSoon";

export default function SettingsPage() {
  return (
    <div className="flex flex-col gap-6">
      <PageHeader title="Settings" description="Organisation, notification, verification-rule, and privacy settings." />
      <ComingSoon feature="Settings management" />
    </div>
  );
}
