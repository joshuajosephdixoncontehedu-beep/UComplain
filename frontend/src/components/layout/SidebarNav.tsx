"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";
import { cn } from "@/lib/utils";
import { navItems } from "./nav-items";
import { Logo } from "./Logo";
import { Tooltip, TooltipContent, TooltipTrigger } from "@/components/ui/tooltip";
import type { AdminRole } from "@/types/enums";

interface SidebarNavProps {
  role: AdminRole;
  onNavigate?: () => void;
  /** Icon-only rail mode, driven by Sidebar's collapse toggle (desktop only). */
  collapsed?: boolean;
}

export function SidebarNav({ role, onNavigate, collapsed = false }: SidebarNavProps) {
  const pathname = usePathname();

  return (
    <div className="flex h-full flex-col">
      <div className="flex h-14 items-center gap-2 px-4">
        <Logo size={32} />
        <div
          className={cn(
            "flex flex-col overflow-hidden leading-none whitespace-nowrap transition-all duration-300",
            collapsed ? "max-w-0 opacity-0" : "max-w-40 opacity-100",
          )}
        >
          <span className="text-sm font-semibold text-white">UComplain</span>
          <span className="text-[11px] text-sidebar-foreground/70">Admin Portal</span>
        </div>
      </div>

      <nav aria-label="Primary" className="flex-1 space-y-0.5 overflow-x-hidden overflow-y-auto px-2 py-2">
        {navItems
          .filter((item) => !item.visible || item.visible(role))
          .map((item) => {
            const isActive = pathname === item.href || pathname.startsWith(`${item.href}/`);
            const Icon = item.icon;

            const linkClassName = cn(
              "flex items-center gap-2.5 rounded-md px-2.5 py-2 text-sm font-medium transition-colors focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-sidebar-ring",
              collapsed && "justify-center",
              isActive
                ? "bg-sidebar-accent text-sidebar-accent-foreground"
                : "text-sidebar-foreground hover:bg-sidebar-accent/60 hover:text-sidebar-accent-foreground",
            );

            const label = (
              <span
                className={cn(
                  "overflow-hidden whitespace-nowrap transition-all duration-300",
                  collapsed ? "max-w-0 opacity-0" : "max-w-40 opacity-100",
                )}
              >
                {item.label}
              </span>
            );

            if (collapsed) {
              return (
                <Tooltip key={item.href}>
                  <TooltipTrigger
                    render={
                      <Link
                        href={item.href}
                        onClick={onNavigate}
                        aria-current={isActive ? "page" : undefined}
                        aria-label={item.label}
                        className={linkClassName}
                      />
                    }
                  >
                    <Icon className="size-4 shrink-0" aria-hidden="true" />
                  </TooltipTrigger>
                  <TooltipContent side="right">{item.label}</TooltipContent>
                </Tooltip>
              );
            }

            return (
              <Link
                key={item.href}
                href={item.href}
                onClick={onNavigate}
                aria-current={isActive ? "page" : undefined}
                className={linkClassName}
              >
                <Icon className="size-4 shrink-0" aria-hidden="true" />
                {label}
              </Link>
            );
          })}
      </nav>
    </div>
  );
}
