"use client";

import { useState } from "react";
import { ChevronLeft } from "lucide-react";
import { cn } from "@/lib/utils";
import { SidebarNav } from "./SidebarNav";
import type { AdminRole } from "@/types/enums";

const STORAGE_KEY = "cirs_sidebar_collapsed";

export function Sidebar({ role }: { role: AdminRole }) {
  // Sidebar only ever mounts client-side (RouteGuard gates the whole authenticated
  // shell behind a client auth check), so reading localStorage in the initializer
  // is safe — there's no server-rendered version of this component to mismatch.
  const [collapsed, setCollapsed] = useState(() => {
    if (typeof window === "undefined") return false;
    return window.localStorage.getItem(STORAGE_KEY) === "true";
  });

  const toggle = () => {
    setCollapsed((prev) => {
      const next = !prev;
      window.localStorage.setItem(STORAGE_KEY, String(next));
      return next;
    });
  };

  return (
    <aside
      className={cn(
        "relative hidden shrink-0 border-r border-sidebar-border bg-sidebar transition-[width] duration-300 ease-in-out lg:block",
        collapsed ? "w-16" : "w-60",
      )}
    >
      <div
        className={cn(
          "fixed h-screen transition-[width] duration-300 ease-in-out",
          collapsed ? "w-16" : "w-60",
        )}
      >
        <SidebarNav role={role} collapsed={collapsed} />
      </div>

      <button
        type="button"
        onClick={toggle}
        aria-label={collapsed ? "Expand sidebar" : "Collapse sidebar"}
        aria-pressed={collapsed}
        className="absolute top-16 -right-3 z-10 flex size-6 items-center justify-center rounded-full border border-sidebar-border bg-sidebar text-sidebar-foreground shadow-sm transition-colors hover:bg-sidebar-accent hover:text-white focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-sidebar-ring"
      >
        <ChevronLeft
          className={cn("size-3.5 transition-transform duration-300", collapsed && "rotate-180")}
          aria-hidden="true"
        />
      </button>
    </aside>
  );
}
