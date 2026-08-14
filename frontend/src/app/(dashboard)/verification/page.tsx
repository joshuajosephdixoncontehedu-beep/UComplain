import { PageHeader } from "@/components/layout/PageHeader";
import { ComingSoon } from "@/components/layout/ComingSoon";

export default function VerificationPage() {
  return (
    <div className="flex flex-col gap-6">
      <PageHeader title="Verification Queue" description="Reports awaiting a human verification decision — not yet active operational cases." />
      <ComingSoon feature="The verification queue" />
    </div>
  );
}
