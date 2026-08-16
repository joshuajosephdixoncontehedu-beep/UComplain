import { Image } from 'expo-image';
import { Text, View } from 'react-native';

function initials(name: string): string {
  const parts = name.trim().split(/\s+/);
  return ((parts[0]?.[0] ?? '') + (parts[parts.length - 1]?.[0] ?? '')).toUpperCase();
}

/** Figma: 44×44 avatar (e.g. Home header). Shows the reporter's real photo when set, initials otherwise. */
export function Avatar({ name, size = 44, photoUrl }: { name: string; size?: number; photoUrl?: string | null }) {
  if (photoUrl) {
    return (
      <Image
        source={{ uri: photoUrl }}
        style={{ width: size, height: size, borderRadius: size / 2 }}
        contentFit="cover"
      />
    );
  }

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
