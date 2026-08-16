import { Image } from 'expo-image';
import { router } from 'expo-router';
import { useEffect } from 'react';
import { Text, View } from 'react-native';

const icon = require('@/assets/images/brand-mark.png');

/**
 * Figma "01 Splash" (node 6:3). No button in the design — the screen
 * auto-advances. Real gating (has the reporter seen onboarding? are they
 * already signed in?) belongs in src/app/index.tsx once the auth/token
 * layer exists; this always goes to onboarding for now.
 */
export default function Splash() {
  useEffect(() => {
    const t = setTimeout(() => router.replace('/(auth)/onboarding/report'), 1600);
    return () => clearTimeout(t);
  }, []);

  return (
    <View className="flex-1 items-center justify-center bg-canvas px-6">
      <View className="absolute h-[430px] w-[430px] rounded-full bg-brand-tint" style={{ top: 60 }} />

      <View className="items-center gap-3">
        <Image source={icon} style={{ width: 118, height: 118, borderRadius: 28 }} contentFit="cover" />
        <Text className="text-[32px] font-bold tracking-wide text-ink">U COMPLAIN</Text>
        <Text className="max-w-[290px] text-center text-body text-secondary">
          Report incidents in your community safely and anonymously
        </Text>
      </View>

      <Text className="absolute bottom-20 text-body-sm text-muted">A community safety service · Sierra Leone</Text>
    </View>
  );
}
