import { Ionicons } from '@expo/vector-icons';
import { Pressable, Text, View } from 'react-native';

/** Figma "12 Review report": eyebrow label + value + Edit link, one per field. */
export function SummaryRow({
  label,
  value,
  onEdit,
  children,
}: {
  label: string;
  value?: string;
  onEdit: () => void;
  children?: React.ReactNode;
}) {
  return (
    <View className="flex-row items-start justify-between border-b border-border py-3">
      <View className="flex-1 gap-1.5 pr-3">
        <Text className="text-eyebrow uppercase tracking-wide text-muted">{label}</Text>
        {value ? <Text className="text-body text-ink">{value}</Text> : children}
      </View>
      <Pressable onPress={onEdit} hitSlop={8} className="flex-row items-center gap-1">
        <Ionicons name="pencil-outline" size={14} color="#1D4ED8" />
        <Text className="text-body-sm font-semibold text-brand">Edit</Text>
      </Pressable>
    </View>
  );
}
