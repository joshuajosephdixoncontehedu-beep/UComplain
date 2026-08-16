import { Ionicons } from '@expo/vector-icons';
import { Text, View } from 'react-native';

const TONES = {
  info: { bg: 'bg-brand-tint', color: '#1D4ED8' },
  warning: { bg: 'bg-status-critical-tint', color: '#B91C1C' },
} as const;

/** Figma: small reassurance/warning banner (icon + copy). */
export function InfoBanner({
  icon,
  text,
  tone = 'info',
}: {
  icon: keyof typeof Ionicons.glyphMap;
  text: string;
  tone?: keyof typeof TONES;
}) {
  const t = TONES[tone];
  return (
    <View className={`flex-row items-start gap-3 rounded-card p-3 ${t.bg}`}>
      <Ionicons name={icon} size={18} color={t.color} style={{ marginTop: 1 }} />
      <Text className="flex-1 text-body-sm text-secondary">{text}</Text>
    </View>
  );
}
