import Link from "next/link";
import { formatNumber } from "@/lib/utils/format";
import type { VerificationQueueSnapshot as VerificationQueueSnapshotType } from "@/types/dashboard";

const ROWS: { key: keyof VerificationQueueSnapshotType; label: string; tab: string }[] = [
  { key: "pending", label: "Pending", tab: "pending" },
  { key: "needsClarification", label: "Needs Clarification", tab: "needs-clarification" },
  { key: "suspectedDuplicate", label: "Suspected Duplicate", tab: "suspected-duplicate" },
  { key: "flaggedAbuse", label: "Flagged Abuse", tab: "flagged-abuse" },
  { key: "rejected", label: "Rejected", tab: "rejected" },
];

export function VerificationQueueSnapshot({ data }: { data: VerificationQueueSnapshotType }) {
  return (
    <ul className="flex flex-col divide-y divide-border">
      {ROWS.map((row) => (
        <li key={row.key}>
          <Link
            href={`/verification?tab=${row.tab}`}
            className="flex items-center justify-between py-2 text-sm hover:text-primary"
          >
            <span className="text-foreground">{row.label}</span>
            <span className="font-medium tabular-nums text-muted-foreground">{formatNumber(data[row.key])}</span>
          </Link>
        </li>
      ))}
    </ul>
  );
}
