import { Text, View } from 'react-native';

/**
 * Figma "00 · Foundations" → STATUS BADGES & PRIORITY (node 3:147), extended
 * with "Closed" and "Withdrawn" — real values ReportStatusProjection.cs can
 * produce that weren't in the Foundations badge catalogue. Keys are the exact
 * BadgeLabel strings the server returns (see lib/report-status-projection.ts).
 */
export type ReportStatusBadge =
  | 'Awaiting verification'
  | 'Verified'
  | 'Under review'
  | 'Assigned'
  | 'In progress'
  | 'Resolved'
  | 'Needs clarification'
  | 'Rejected'
  | 'Duplicate'
  | 'Closed'
  | 'Withdrawn';

export type PriorityBadge = 'Critical' | 'High' | 'Medium' | 'Low';

type BadgeVariant = ReportStatusBadge | PriorityBadge;

const VARIANT_STYLES: Record<BadgeVariant, { bg: string; fg: string; dot: string }> = {
  'Awaiting verification': { bg: 'bg-status-pending-tint', fg: 'text-status-pending', dot: 'bg-status-pending' },
  Verified: { bg: 'bg-status-verified-tint', fg: 'text-status-verified', dot: 'bg-status-verified' },
  'Under review': { bg: 'bg-surface-muted', fg: 'text-secondary', dot: 'bg-secondary' },
  Assigned: { bg: 'bg-brand-tint', fg: 'text-brand', dot: 'bg-brand' },
  'In progress': { bg: 'bg-brand-tint', fg: 'text-brand', dot: 'bg-brand' },
  Resolved: { bg: 'bg-status-resolved-tint', fg: 'text-status-resolved', dot: 'bg-status-resolved' },
  Closed: { bg: 'bg-status-resolved-tint', fg: 'text-status-resolved', dot: 'bg-status-resolved' },
  'Needs clarification': { bg: 'bg-status-pending-tint', fg: 'text-status-pending', dot: 'bg-status-pending' },
  Rejected: { bg: 'bg-status-critical-tint', fg: 'text-status-critical', dot: 'bg-status-critical' },
  Duplicate: { bg: 'bg-surface-muted', fg: 'text-muted', dot: 'bg-muted' },
  Withdrawn: { bg: 'bg-surface-muted', fg: 'text-muted', dot: 'bg-muted' },
  Critical: { bg: 'bg-status-critical-tint', fg: 'text-status-critical', dot: 'bg-status-critical' },
  High: { bg: 'bg-status-pending-tint', fg: 'text-status-pending', dot: 'bg-status-pending' },
  Medium: { bg: 'bg-status-verified-tint', fg: 'text-status-verified', dot: 'bg-status-verified' },
  Low: { bg: 'bg-surface-muted', fg: 'text-muted', dot: 'bg-muted' },
};

const FALLBACK_STYLE = { bg: 'bg-surface-muted', fg: 'text-secondary', dot: 'bg-secondary' };

/** Renders any server-supplied label, styled if recognized, neutral gray otherwise (never crashes on an unknown value). */
export function StatusBadge({ variant, label }: { variant: BadgeVariant | string; label?: string }) {
  const s = VARIANT_STYLES[variant as BadgeVariant] ?? FALLBACK_STYLE;
  return (
    <View className={`flex-row items-center gap-1.5 self-start rounded-chip px-[11px] py-[6px] ${s.bg}`}>
      <View className={`h-1.5 w-1.5 rounded-full ${s.dot}`} />
      <Text className={`text-caption ${s.fg}`}>{label ?? variant}</Text>
    </View>
  );
}
