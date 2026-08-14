import Link from "next/link";
import { Minus, TrendingDown, TrendingUp } from "lucide-react";
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
}

export function MetricCard({ label, value, previousValue, upIsGood = true, href, formatValue }: MetricCardProps) {
  const trend = previousValue !== undefined ? formatPercentChange(value, previousValue) : null;
  const trendIsPositive =
    trend && ((trend.direction === "up" && upIsGood) || (trend.direction === "down" && !upIsGood));
  const trendIsNegative =
    trend && ((trend.direction === "up" && !upIsGood) || (trend.direction === "down" && upIsGood));

  const content = (
    <div className="flex flex-col gap-1 rounded-lg border border-border bg-card p-4 transition-colors hover:border-primary/40">
      <span className="text-sm text-muted-foreground">{label}</span>
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
