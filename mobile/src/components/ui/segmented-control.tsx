import { Pressable, Text, View } from 'react-native';

type Option<T extends string> = { value: T; label: string };

/** Figma "09 New report · Details": 4-way severity picker (Low/Medium/High/Critical). */
export function SegmentedControl<T extends string>({
  options,
  value,
  onChange,
}: {
  options: Option<T>[];
  value: T | undefined;
  onChange: (v: T) => void;
}) {
  return (
    <View className="flex-row gap-1 rounded-input bg-surface-muted p-1">
      {options.map((opt) => {
        const selected = opt.value === value;
        return (
          <Pressable
            key={opt.value}
            onPress={() => onChange(opt.value)}
            className={`flex-1 items-center justify-center rounded-chip py-2 ${selected ? 'bg-surface' : ''}`}>
            <Text className={`text-label ${selected ? 'font-semibold text-ink' : 'text-muted'}`}>{opt.label}</Text>
          </Pressable>
        );
      })}
    </View>
  );
}
