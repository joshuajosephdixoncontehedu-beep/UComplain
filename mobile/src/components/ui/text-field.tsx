import { Ionicons } from '@expo/vector-icons';
import { useState } from 'react';
import { Pressable, Text, TextInput, TextInputProps, View } from 'react-native';

type Props = {
  label: string;
  icon: keyof typeof Ionicons.glyphMap;
  isPassword?: boolean;
} & TextInputProps;

/**
 * Figma: "Input / <label>" — label above a 48px row with a leading icon.
 * Password fields get a trailing show/hide toggle (not in the base Figma
 * icon inventory I could pull, but standard for this pattern).
 */
export function TextField({ label, icon, isPassword, ...inputProps }: Props) {
  const [hidden, setHidden] = useState(!!isPassword);

  return (
    <View className="gap-2">
      <Text className="text-label text-secondary">{label}</Text>
      <View className="h-12 w-full flex-row items-center gap-3 rounded-input border border-border bg-surface px-3.5">
        <Ionicons name={icon} size={19} color="#94A3B8" />
        <TextInput
          className="flex-1 text-body text-ink"
          placeholderTextColor="#94A3B8"
          secureTextEntry={hidden}
          {...inputProps}
        />
        {isPassword ? (
          <Pressable onPress={() => setHidden((h) => !h)} hitSlop={8}>
            <Ionicons name={hidden ? 'eye-outline' : 'eye-off-outline'} size={19} color="#94A3B8" />
          </Pressable>
        ) : null}
      </View>
    </View>
  );
}
