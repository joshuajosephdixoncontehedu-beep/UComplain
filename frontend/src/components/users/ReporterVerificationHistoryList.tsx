import Link from "next/link";
import { Timeline } from "@/components/reports/detail/Timeline";
import { StatusBadge } from "@/components/ui/status-badge";
import { verificationResultTone } from "@/lib/utils/statusStyles";
import { formatDateTime } from "@/lib/utils/format";
import type { ReporterVerificationEvent } from "@/types/reporters";

export function ReporterVerificationHistoryList({ items }: { items: ReporterVerificationEvent[] }) {
  return (
    <Timeline
      items={items}
      keyExtractor={(v) => v.id}
      emptyLabel="No verification decisions recorded yet."
      renderItem={(v) => (
        <div className="flex flex-col gap-1">
          <div className="flex flex-wrap items-center gap-2">
            <StatusBadge value={v.result} tone={verificationResultTone(v.result)} />
            <Link href={`/reports/${v.incidentReportId}`} className="text-xs font-medium text-primary hover:underline">
              {v.caseReference}
            </Link>
          </div>
          {v.notes && <p className="text-sm text-muted-foreground">&ldquo;{v.notes}&rdquo;</p>}
          <p className="text-xs text-muted-foreground">{formatDateTime(v.createdAt)}</p>
        </div>
      )}
    />
  );
}
