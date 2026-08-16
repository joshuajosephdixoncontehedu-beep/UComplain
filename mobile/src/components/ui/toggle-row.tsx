import { Switch, Text, View } from 'react-native';

/** Figma "20 Privacy & data" → YOUR CHOICES: title + subtitle + Switch, no icon. */
export function ToggleRow({
  title,
  subtitle,
  value,
  onValueChange,
}: {
  title: string;
  subtitle: string;
  value: boolean;
  onValueChange: (v: boolean) => void;
}) {
  return (
    <View className="flex-row items-center gap-3 px-3.5 py-4">
      <View className="flex-1">
        <Text className="text-body-lg text-ink">{title}</Text>
        <Text className="mt-0.5 text-body-sm text-muted">{subtitle}</Text>
      </View>
      <Switch value={value} onValueChange={onValueChange} trackColor={{ true: '#1D4ED8', false: '#E2E8F0' }} />
    </View>
  );
}
