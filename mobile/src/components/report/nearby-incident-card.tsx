import { Ionicons } from '@expo/vector-icons';
import { Pressable, Text, View } from 'react-native';

import { StatusBadge } from '@/components/ui/status-badge';

/** Figma "17 Nearby incidents" bottom sheet card: icon+title/distance, divider, verified badge+time. */
export function NearbyIncidentCard({
  icon,
  title,
  distance,
  relativeTime,
  onPress,
}: {
  icon: keyof typeof Ionicons.glyphMap;
  title: string;
  distance: string;
  relativeTime: string;
  onPress?: () => void;
}) {
  return (
    <Pressable onPress={onPress} className="w-[210px] gap-3.5 rounded-card border border-border bg-surface p-3.5">
      <View className="flex-row items-center gap-2.5">
        <View className="h-[34px] w-[34px] items-center justify-center rounded-full bg-brand-tint">
          <Ionicons name={icon} size={17} color="#1D4ED8" />
        </View>
        <View className="flex-1">
          <Text className="text-body font-semibold text-ink" numberOfLines={1}>
            {title}
          </Text>
          <Text className="text-body-sm text-muted">{distance}</Text>
        </View>
      </View>
      <View className="h-px bg-border" />
      <View className="flex-row items-center justify-between">
        <StatusBadge variant="Verified" />
        <Text className="text-caption text-subtle">{relativeTime}</Text>
      </View>
    </Pressable>
  );
}
