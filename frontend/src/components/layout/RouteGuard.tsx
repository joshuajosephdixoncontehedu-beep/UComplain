"use client";

import { useEffect } from "react";
import { useRouter } from "next/navigation";
import { Loader2 } from "lucide-react";
import { useAuth } from "@/lib/auth/AuthProvider";

/**
 * Client-side route protection. There is no server-side check here (proxy.ts can't
 * see the access token — it's dev-grade in-memory/localStorage, not a cookie — see
 * lib/auth/tokenStore.ts) so this is the only gate. It briefly shows a loading state
 * during the initial silent-refresh check, then redirects to /login if that didn't
 * establish a session.
 */
export function RouteGuard({ children }: { children: React.ReactNode }) {
  const { admin, isInitializing } = useAuth();
  const router = useRouter();

  useEffect(() => {
    if (!isInitializing && !admin) {
      router.replace("/login");
    }
  }, [isInitializing, admin, router]);

  if (isInitializing || !admin) {
    return (
      <div className="flex min-h-screen items-center justify-center bg-background">
        <Loader2 className="size-6 animate-spin text-muted-foreground" aria-label="Loading" />
      </div>
    );
  }

  return <>{children}</>;
}
