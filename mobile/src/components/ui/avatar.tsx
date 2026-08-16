import { Text, View } from 'react-native';

function initials(name: string): string {
  const parts = name.trim().split(/\s+/);
  return ((parts[0]?.[0] ?? '') + (parts[parts.length - 1]?.[0] ?? '')).toUpperCase();
}

/** Figma: 44×44 initials avatar (e.g. Home header, "AK" for Amina Kargbo). */
export function Avatar({ name, size = 44 }: { name: string; size?: number }) {
  return (
    <View
      className="items-center justify-center rounded-full bg-brand-tint"
      style={{ width: size, height: size }}>
      <Text className="font-semibold text-brand" style={{ fontSize: size * 0.36 }}>
        {initials(name)}
      </Text>
    </View>
  );
}
