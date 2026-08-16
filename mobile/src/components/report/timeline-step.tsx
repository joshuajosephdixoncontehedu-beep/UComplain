import { Ionicons } from '@expo/vector-icons';
import { View, Text } from 'react-native';

/** Figma "15 Report detail" → PROGRESS: icon + connecting line, title + timestamp/status. */
export function TimelineStep({
  icon,
  title,
  detail,
  done,
  isLast,
}: {
  icon: keyof typeof Ionicons.glyphMap;
  title: string;
  detail: string;
  done: boolean;
  isLast?: boolean;
}) {
  return (
    <View className="flex-row gap-[13px]">
      <View className="items-center">
        <View className={`h-8 w-8 items-center justify-center rounded-full ${done ? 'bg-brand-tint' : 'bg-surface-muted'}`}>
          <Ionicons name={icon} size={16} color={done ? '#1D4ED8' : '#94A3B8'} />
        </View>
        {!isLast ? <View className="mt-1 h-[18px] w-[2px] bg-border" /> : null}
      </View>
      <View className="flex-1 pb-[18px]">
        <Text className={`text-body-lg ${done ? 'text-ink' : 'text-muted'}`}>{title}</Text>
        <Text className="mt-1 text-body-sm text-muted">{detail}</Text>
      </View>
    </View>
  );
}
