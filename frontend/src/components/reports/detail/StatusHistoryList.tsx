import { ArrowRight } from "lucide-react";
import { Timeline } from "./Timeline";
import { StatusBadge } from "@/components/ui/status-badge";
import { caseStatusTone } from "@/lib/utils/statusStyles";
import { formatDateTime } from "@/lib/utils/format";
import type { StatusHistoryItem } from "@/types/reports";

export function StatusHistoryList({ items }: { items: StatusHistoryItem[] }) {
  return (
    <Timeline
      items={items}
      keyExtractor={(s) => s.id}
      emptyLabel="No status changes recorded yet."
      renderItem={(s) => (
        <div className="flex flex-col gap-1">
          <div className="flex flex-wrap items-center gap-1.5">
            <StatusBadge value={s.previousStatus} tone={caseStatusTone(s.previousStatus)} />
            <ArrowRight className="size-3 text-muted-foreground" />
            <StatusBadge value={s.newStatus} tone={caseStatusTone(s.newStatus)} />
          </div>
          <p className="text-sm text-foreground">
            <span className="font-medium">{s.changedByAdminName}</span>
          </p>
          {s.notes && <p className="text-sm text-muted-foreground">{s.notes}</p>}
          <p className="text-xs text-muted-foreground">{formatDateTime(s.createdAt)}</p>
        </div>
      )}
    />
  );
}
