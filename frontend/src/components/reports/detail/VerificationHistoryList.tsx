import { Timeline } from "./Timeline";
import { StatusBadge } from "@/components/ui/status-badge";
import { verificationResultTone } from "@/lib/utils/statusStyles";
import { formatDateTime } from "@/lib/utils/format";
import type { VerificationEventItem } from "@/types/reports";

export function VerificationHistoryList({ items }: { items: VerificationEventItem[] }) {
  return (
    <Timeline
      items={items}
      keyExtractor={(v) => v.id}
      emptyLabel="No verification decisions recorded yet."
      renderItem={(v) => (
        <div className="flex flex-col gap-1">
          <div className="flex flex-wrap items-center gap-2">
            <StatusBadge value={v.result} tone={verificationResultTone(v.result)} />
            <span className="text-xs text-muted-foreground">Attempt {v.attemptNumber}</span>
          </div>
          <p className="text-sm text-foreground">
            <span className="font-medium">{v.performedByAdminName ?? "System"}</span>
          </p>
          {v.notes && <p className="text-sm text-muted-foreground">&ldquo;{v.notes}&rdquo;</p>}
          <p className="text-xs text-muted-foreground">{formatDateTime(v.createdAt)}</p>
        </div>
      )}
    />
  );
}
