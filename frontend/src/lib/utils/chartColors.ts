import type { NamedCount } from "@/types/dashboard";

/**
 * The validated categorical palette (see the dataviz skill's palette.md) — a fixed
 * hue order that is never cycled or reassigned when a filter changes the series
 * count. At most 8 slots are safe; anything beyond that folds into "Other" (see
 * foldToOther) rather than reusing a slot or introducing a 9th ad-hoc hue.
 */
export const CHART_COLORS = [
  "var(--chart-1)",
  "var(--chart-2)",
  "var(--chart-3)",
  "var(--chart-4)",
  "var(--chart-5)",
  "var(--chart-6)",
  "var(--chart-7)",
  "var(--chart-8)",
] as const;

/**
 * Caps a sorted (descending) NamedCount list at `limit` slots, aggregating the
 * remainder into a trailing "Other" entry so a chart never silently drops data or
 * exceeds the validated 8-color palette.
 */
export function foldToOther(items: NamedCount[], limit = 8): NamedCount[] {
  if (items.length <= limit) return items;
  const kept = items.slice(0, limit - 1);
  const otherCount = items.slice(limit - 1).reduce((sum, item) => sum + item.count, 0);
  return [...kept, { name: "Other", count: otherCount }];
}
