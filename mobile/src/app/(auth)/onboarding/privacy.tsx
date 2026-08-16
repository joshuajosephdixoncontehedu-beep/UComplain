import { Ionicons } from '@expo/vector-icons';
import { router } from 'expo-router';
import { Text, View } from 'react-native';

import { AppButton } from '@/components/ui/button';
import { BrandLockup } from '@/components/ui/brand-lockup';
import { OnboardingDots } from '@/components/ui/onboarding-dots';

/** Figma "02c Onboarding · Privacy" (node 42:75) — last step, no "Skip". */
export default function OnboardingPrivacy() {
  return (
    <View className="flex-1 bg-canvas px-5">
      <View className="mt-[70px]">
        <BrandLockup />
      </View>

      {/* Hero: anonymity/verification illustration */}
      <View className="mt-[50px] h-[300px] items-center justify-center">
        <View className="w-[262px] gap-3 rounded-card border border-border bg-surface p-4">
          <View className="flex-row items-center gap-3">
            <View className="h-10 w-10 items-center justify-center rounded-full bg-brand-tint">
              <Ionicons name="person-circle-outline" size={19} color="#1D4ED8" />
            </View>
            <View>
              <Text className="text-eyebrow uppercase tracking-wide text-muted">Reported by</Text>
              <Text className="text-body font-semibold text-ink">Anonymous resident</Text>
            </View>
          </View>
          <View className="h-px bg-border" />
          <View className="flex-row items-center gap-2">
            <Ionicons name="shield-checkmark-outline" size={16} color="#0369A1" />
            <Text className="text-body-sm text-secondary">Checked by a person before action</Text>
          </View>
        </View>

        <View className="absolute bottom-2 flex-row items-center gap-1.5 rounded-pill bg-surface px-3 py-1.5">
          <Ionicons name="eye-off-outline" size={15} color="#1D4ED8" />
          <Text className="text-caption text-secondary">Never shown publicly</Text>
        </View>
      </View>

      <View className="mt-10 gap-3">
        <Text className="text-display text-ink">Your name stays out of it.</Text>
        <Text className="text-body text-secondary">
          A person checks every report before action is taken, and your identity is never shown on the public map.
        </Text>
      </View>

      <View className="flex-1" />

      <OnboardingDots index={2} />

      <View className="mb-10 mt-6 gap-3">
        <AppButton title="Get started" onPress={() => router.push('/(auth)/create-account')} />
        <AppButton title="I already have an account" variant="secondary" onPress={() => router.push('/(auth)/sign-in')} />
      </View>
    </View>
  );
}
