"use client";

import { RouteGuard } from "@/components/layout/RouteGuard";
import { Sidebar } from "@/components/layout/Sidebar";
import { TopNav } from "@/components/layout/TopNav";
import { useAuth } from "@/lib/auth/AuthProvider";

function AuthenticatedShell({ children }: { children: React.ReactNode }) {
  const { admin } = useAuth();
  if (!admin) return null;

  return (
    <div className="flex min-h-screen bg-background">
      <Sidebar role={admin.role} />
      <div className="flex min-w-0 flex-1 flex-col">
        <TopNav role={admin.role} />
        <main className="flex-1 px-4 py-6 sm:px-6 lg:px-8">{children}</main>
      </div>
    </div>
  );
}

export default function DashboardLayout({ children }: { children: React.ReactNode }) {
  return (
    <RouteGuard>
      <AuthenticatedShell>{children}</AuthenticatedShell>
    </RouteGuard>
  );
}
