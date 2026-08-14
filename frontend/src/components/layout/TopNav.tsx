"use client";

import { useState } from "react";
import { Bell, Menu, Search } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Sheet, SheetContent, SheetTitle } from "@/components/ui/sheet";
import { ProfileDropdown } from "./ProfileDropdown";
import { SidebarNav } from "./SidebarNav";
import type { AdminRole } from "@/types/enums";

export function TopNav({ role }: { role: AdminRole }) {
  const [mobileNavOpen, setMobileNavOpen] = useState(false);

  return (
    <header className="sticky top-0 z-30 flex h-14 items-center gap-3 border-b border-border bg-card px-4">
      <Sheet open={mobileNavOpen} onOpenChange={setMobileNavOpen}>
        <SheetContent side="left" className="w-60 bg-sidebar p-0 [&_svg]:text-sidebar-foreground">
          <SheetTitle className="sr-only">Navigation</SheetTitle>
          <SidebarNav role={role} onNavigate={() => setMobileNavOpen(false)} />
        </SheetContent>
        <Button
          variant="ghost"
          size="icon"
          className="lg:hidden"
          aria-label="Open navigation menu"
          onClick={() => setMobileNavOpen(true)}
        >
          <Menu className="size-5" aria-hidden="true" />
        </Button>
      </Sheet>

      <div className="relative hidden max-w-sm flex-1 sm:block">
        <Search
          className="pointer-events-none absolute top-1/2 left-2.5 size-4 -translate-y-1/2 text-muted-foreground"
          aria-hidden="true"
        />
        <Input
          type="search"
          placeholder="Search cases, reporters, admins…"
          aria-label="Global search (not yet available)"
          disabled
          className="h-9 pl-8"
        />
      </div>

      <div className="flex flex-1 items-center justify-end gap-1.5">
        <Button
          variant="ghost"
          size="icon"
          aria-label="Notifications (not yet available)"
          disabled
          className="text-muted-foreground"
        >
          <Bell className="size-4.5" aria-hidden="true" />
        </Button>
        <ProfileDropdown />
      </div>
    </header>
  );
}
