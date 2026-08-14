import { format, formatDistanceToNow, parseISO } from "date-fns";

export function formatDateTime(value: string | null | undefined): string {
  if (!value) return "—";
  return format(parseISO(value), "d MMM yyyy, HH:mm");
}

export function formatDate(value: string | null | undefined): string {
  if (!value) return "—";
  return format(parseISO(value), "d MMM yyyy");
}

export function formatRelativeTime(value: string | null | undefined): string {
  if (!value) return "—";
  return formatDistanceToNow(parseISO(value), { addSuffix: true });
}

export function formatHours(value: number | null | undefined): string {
  if (value === null || value === undefined) return "—";
  if (value < 1) return `${Math.round(value * 60)} min`;
  if (value < 48) return `${value.toFixed(1)} hrs`;
  return `${(value / 24).toFixed(1)} days`;
}

export function formatNumber(value: number): string {
  return new Intl.NumberFormat("en-US").format(value);
}

export function formatPercentChange(current: number, previous: number): { label: string; direction: "up" | "down" | "flat" } {
  if (previous === 0) {
    if (current === 0) return { label: "No change", direction: "flat" };
    return { label: "New", direction: "up" };
  }
  const change = ((current - previous) / previous) * 100;
  if (Math.abs(change) < 1) return { label: "No change", direction: "flat" };
  return {
    label: `${change > 0 ? "+" : ""}${change.toFixed(0)}%`,
    direction: change > 0 ? "up" : "down",
  };
}
