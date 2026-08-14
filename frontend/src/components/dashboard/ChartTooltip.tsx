import { formatNumber } from "@/lib/utils/format";

interface ChartTooltipEntry {
  value?: number | string;
  name?: string;
  color?: string;
}

interface ChartTooltipProps {
  active?: boolean;
  payload?: ChartTooltipEntry[];
  label?: string | number;
}

/**
 * Shared Recharts tooltip: value leads (bold, high-contrast), label follows
 * (muted) — the reader already has the category from the axis and wants the
 * number. A short color line-key rather than a filled swatch box.
 */
export function ChartTooltip({ active, payload, label }: ChartTooltipProps) {
  if (!active || !payload?.length) return null;

  return (
    <div className="rounded-md border border-border bg-popover px-3 py-2 text-xs shadow-md">
      {label !== undefined && <div className="mb-1 font-medium text-popover-foreground">{label}</div>}
      <div className="flex flex-col gap-1">
        {payload.map((entry, index) => (
          <div key={index} className="flex items-center gap-2">
            <span aria-hidden="true" className="h-0.5 w-3 shrink-0 rounded-full" style={{ backgroundColor: entry.color }} />
            <span className="font-semibold text-popover-foreground">
              {typeof entry.value === "number" ? formatNumber(entry.value) : entry.value}
            </span>
            {entry.name && <span className="text-muted-foreground">{entry.name}</span>}
          </div>
        ))}
      </div>
    </div>
  );
}
