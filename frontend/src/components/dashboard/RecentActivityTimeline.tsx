import { Activity } from "lucide-react";
import { formatRelativeTime } from "@/lib/utils/format";
import { humanizePascalCase } from "@/lib/utils/statusStyles";
import type { AuditLogEntryItem } from "@/types/reports";

export function RecentActivityTimeline({ data }: { data: AuditLogEntryItem[] }) {
  if (data.length === 0) {
    return <p className="py-6 text-center text-sm text-muted-foreground">No recent activity.</p>;
  }

  return (
    <ol className="flex flex-col">
      {data.map((entry, index) => (
        <li key={entry.id} className="relative flex gap-3 pb-4 last:pb-0">
          {index < data.length - 1 && <span className="absolute top-5 left-[9px] h-full w-px bg-border" aria-hidden="true" />}
          <span className="mt-0.5 flex size-[18px] shrink-0 items-center justify-center rounded-full bg-muted">
            <Activity className="size-2.5 text-muted-foreground" aria-hidden="true" />
          </span>
          <div className="min-w-0 flex-1">
            <p className="text-sm text-foreground">
              <span className="font-medium">{entry.adminUserName ?? "System"}</span>{" "}
              <span className="text-muted-foreground">{humanizePascalCase(entry.action)}</span>
            </p>
            <p className="text-xs text-muted-foreground">{formatRelativeTime(entry.createdAt)}</p>
          </div>
        </li>
      ))}
    </ol>
  );
}
