import { SidebarNav } from "./SidebarNav";
import type { AdminRole } from "@/types/enums";

export function Sidebar({ role }: { role: AdminRole }) {
  return (
    <aside className="hidden w-60 shrink-0 border-r border-sidebar-border bg-sidebar lg:block">
      <div className="fixed h-screen w-60">
        <SidebarNav role={role} />
      </div>
    </aside>
  );
}
