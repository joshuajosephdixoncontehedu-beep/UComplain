import { formatNumber } from "@/lib/utils/format";
import type { NamedCount } from "@/types/dashboard";

export function HotspotsList({ data }: { data: NamedCount[] }) {
  if (data.length === 0) {
    return <p className="py-6 text-center text-sm text-muted-foreground">No reports in this date range.</p>;
  }

  const max = Math.max(...data.map((d) => d.count));

  return (
    <ol className="flex flex-col gap-2.5">
      {data.map((location, index) => (
        <li key={location.name} className="flex items-center gap-3">
          <span className="w-4 shrink-0 text-xs font-medium text-muted-foreground">{index + 1}</span>
          <div className="min-w-0 flex-1">
            <div className="flex items-baseline justify-between gap-2">
              <span className="truncate text-sm text-foreground">{location.name}</span>
              <span className="shrink-0 text-xs font-medium tabular-nums text-muted-foreground">
                {formatNumber(location.count)}
              </span>
            </div>
            <div className="mt-1 h-1.5 w-full overflow-hidden rounded-full bg-muted">
              <div
                className="h-full rounded-full bg-primary"
                style={{ width: `${Math.max(4, (location.count / max) * 100)}%` }}
              />
            </div>
          </div>
        </li>
      ))}
    </ol>
  );
}
