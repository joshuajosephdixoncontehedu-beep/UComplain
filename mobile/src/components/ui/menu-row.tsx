import { Ionicons } from '@expo/vector-icons';
import { Pressable, Text, View } from 'react-native';

type Props = {
  icon: keyof typeof Ionicons.glyphMap;
  title: string;
  subtitle?: string;
  tone?: 'default' | 'danger';
  onPress?: () => void;
};

/** Figma "19 Profile" / "20 Privacy & data": icon + title(+subtitle) + chevron row. */
export function MenuRow({ icon, title, subtitle, tone = 'default', onPress }: Props) {
  const isDanger = tone === 'danger';
  return (
    <Pressable onPress={onPress} className="flex-row items-center gap-3.5 px-3.5 py-3.5 active:bg-surface-muted">
      <View className={`h-[34px] w-[34px] items-center justify-center rounded-full ${isDanger ? 'bg-status-critical-tint' : 'bg-brand-tint'}`}>
        <Ionicons name={icon} size={17} color={isDanger ? '#B91C1C' : '#1D4ED8'} />
      </View>
      <View className="flex-1">
        <Text className={`text-body-lg ${isDanger ? 'font-semibold text-status-critical' : 'text-ink'}`}>{title}</Text>
        {subtitle ? <Text className="mt-0.5 text-body-sm text-muted">{subtitle}</Text> : null}
      </View>
      <Ionicons name="chevron-forward" size={17} color="#94A3B8" />
    </Pressable>
  );
}
