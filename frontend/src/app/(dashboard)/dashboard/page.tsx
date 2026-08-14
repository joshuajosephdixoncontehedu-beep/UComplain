"use client";

import { PageHeader } from "@/components/layout/PageHeader";
import { ComingSoon } from "@/components/layout/ComingSoon";
import { useAuth } from "@/lib/auth/AuthProvider";

export default function DashboardPage() {
  const { admin } = useAuth();

  return (
    <div className="flex flex-col gap-6">
      <PageHeader title={`Welcome, ${admin?.fullName ?? ""}`} description="Operations overview for the Community Incident Reporting System." />
      <ComingSoon feature="The full dashboard (metrics, charts, priority reports)" />
    </div>
  );
}
