import { PageHeader } from "@/components/layout/PageHeader";
import { ComingSoon } from "@/components/layout/ComingSoon";

export default function CategoriesPage() {
  return (
    <div className="flex flex-col gap-6">
      <PageHeader title="Categories" description="Incident categories, default priority, and SLA configuration." />
      <ComingSoon feature="Category management" />
    </div>
  );
}
