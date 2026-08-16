import { Ionicons } from '@expo/vector-icons';
import { Pressable, Text, View } from 'react-native';

import { StatusBadge } from '@/components/ui/status-badge';

type Props = {
  icon: keyof typeof Ionicons.glyphMap;
  title: string;
  /** MobileReportListItemDto has no location field — only report-detail responses carry LocationDescription. */
  location?: string;
  /** The server's real BadgeLabel (ReportStatusProjection) — StatusBadge falls back to a neutral style for anything unrecognized. */
  status: string;
  caseReference: string;
  relativeTime: string;
  /** 0–1. Figma "14 My reports": a thin progress bar under the title (absent on Home's cards). */
  progress?: number;
  onPress?: () => void;
};

/** Figma "Report card": icon + title(/location), optional progress, divider, status badge + ref + time. */
export function ReportCard({ icon, title, location, status, caseReference, relativeTime, progress, onPress }: Props) {
  return (
    <Pressable onPress={onPress} className="gap-4 rounded-card border border-border bg-surface p-4 active:opacity-70">
      <View className="flex-row items-center gap-3">
        <View className="h-10 w-10 items-center justify-center rounded-full bg-brand-tint">
          <Ionicons name={icon} size={20} color="#1D4ED8" />
        </View>
        <View className="flex-1">
          <Text className="text-body font-semibold text-ink" numberOfLines={1}>
            {title}
          </Text>
          {location ? (
            <View className="mt-1 flex-row items-center gap-1">
              <Ionicons name="location-outline" size={14} color="#64748B" />
              <Text className="flex-1 text-body-sm text-muted" numberOfLines={1}>
                {location}
              </Text>
            </View>
          ) : null}
        </View>
      </View>

      {progress !== undefined ? (
        <View className="h-1 rounded-full bg-surface-muted">
          <View className="h-1 rounded-full bg-brand" style={{ width: `${Math.round(progress * 100)}%` }} />
        </View>
      ) : null}

      <View className="h-px bg-border" />

      <View className="flex-row items-center justify-between">
        <StatusBadge variant={status} />
        <View className="flex-row items-center gap-2">
          <Text className="text-caption text-muted">{caseReference}</Text>
          <Text className="text-caption text-subtle">{relativeTime}</Text>
        </View>
      </View>
    </Pressable>
  );
}
