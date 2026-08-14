import Link from "next/link";
import { Minus, TrendingDown, TrendingUp, type LucideIcon } from "lucide-react";
import { cn } from "@/lib/utils";
import { formatNumber, formatPercentChange } from "@/lib/utils/format";

interface MetricCardProps {
  label: string;
  value: number;
  previousValue?: number;
  /** When false, an upward change reads as bad (e.g. "awaiting verification"). Defaults to true. */
  upIsGood?: boolean;
  href?: string;
  formatValue?: (value: number) => string;
  icon?: LucideIcon;
  /** A `var(--chart-N)` value — identity color for this tile, not a status signal. */
  accent?: string;
}

export function MetricCard({
  label,
  value,
  previousValue,
  upIsGood = true,
  href,
  formatValue,
  icon: Icon,
  accent,
}: MetricCardProps) {
  const trend = previousValue !== undefined ? formatPercentChange(value, previousValue) : null;
  const trendIsPositive =
    trend && ((trend.direction === "up" && upIsGood) || (trend.direction === "down" && !upIsGood));
  const trendIsNegative =
    trend && ((trend.direction === "up" && !upIsGood) || (trend.direction === "down" && upIsGood));

  const content = (
    <div className="flex flex-col gap-2 rounded-lg border border-border bg-card p-4 transition-colors hover:border-primary/40">
      <div className="flex items-start justify-between gap-2">
        <span className="text-sm text-muted-foreground">{label}</span>
        {Icon && accent && (
          <span
            className="flex size-8 shrink-0 items-center justify-center rounded-lg"
            style={{ backgroundColor: `color-mix(in srgb, ${accent} 14%, transparent)`, color: accent }}
          >
            <Icon className="size-4" aria-hidden="true" />
          </span>
        )}
      </div>
      <span className="text-2xl font-semibold tracking-tight text-foreground">
        {formatValue ? formatValue(value) : formatNumber(value)}
      </span>
      {trend && (
        <span
          className={cn(
            "flex items-center gap-1 text-xs font-medium",
            trendIsPositive && "text-success",
            trendIsNegative && "text-destructive",
            !trendIsPositive && !trendIsNegative && "text-muted-foreground",
          )}
        >
          {trend.direction === "up" && <TrendingUp className="size-3.5" aria-hidden="true" />}
          {trend.direction === "down" && <TrendingDown className="size-3.5" aria-hidden="true" />}
          {trend.direction === "flat" && <Minus className="size-3.5" aria-hidden="true" />}
          {trend.label}
          <span className="text-muted-foreground">vs. prior period</span>
        </span>
      )}
    </div>
  );

  if (!href) return content;

  return (
    <Link href={href} className="block rounded-lg focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring">
      {content}
    </Link>
  );
}
