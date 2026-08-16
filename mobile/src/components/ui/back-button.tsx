import { Ionicons } from '@expo/vector-icons';
import { router } from 'expo-router';
import { Pressable } from 'react-native';

/** Figma: 24×24 back chevron in the header of sign-in/create-account/verify-email. */
export function BackButton({ onPress }: { onPress?: () => void }) {
  return (
    <Pressable onPress={onPress ?? (() => router.back())} hitSlop={8}>
      <Ionicons name="chevron-back" size={24} color="#0F172A" />
    </Pressable>
  );
}
