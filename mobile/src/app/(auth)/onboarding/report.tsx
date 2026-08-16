import { Ionicons } from '@expo/vector-icons';
import { router } from 'expo-router';
import { Pressable, Text, View } from 'react-native';

import { AppButton } from '@/components/ui/button';
import { BrandLockup } from '@/components/ui/brand-lockup';
import { OnboardingDots } from '@/components/ui/onboarding-dots';
import { StatusBadge } from '@/components/ui/status-badge';

/** Figma "02a Onboarding · Report" (node 6:32). */
export default function OnboardingReport() {
  return (
    <View className="flex-1 bg-canvas px-5">
      <View className="mt-[70px] flex-row items-center justify-between">
        <BrandLockup />
        <Pressable onPress={() => router.replace('/(auth)/sign-in')} hitSlop={8}>
          <Text className="text-body text-muted">Skip</Text>
        </Pressable>
      </View>

      {/* Hero: mock report card illustration */}
      <View className="mt-[50px] h-[300px] items-center justify-center">
        <View className="w-[262px] gap-3 rounded-card border border-border bg-surface p-4">
          <View className="flex-row items-center gap-3">
            <View className="h-9 w-9 items-center justify-center rounded-full bg-brand-tint">
              <Ionicons name="water-outline" size={19} color="#1D4ED8" />
            </View>
            <View>
              <Text className="text-body font-semibold text-ink">Blocked drainage</Text>
              <Text className="text-body-sm text-muted">Congo Cross, Freetown</Text>
            </View>
          </View>
          <View className="h-px bg-border" />
          <View className="flex-row items-center justify-between">
            <StatusBadge variant="Verified" />
            <Text className="text-caption text-muted">CIRS-2026-000148</Text>
          </View>
        </View>

        <View className="absolute bottom-[16px] right-[20px] h-16 w-16 items-center justify-center rounded-full bg-brand shadow-lg">
          <Ionicons name="camera-outline" size={28} color="#FFFFFF" />
        </View>

        <View className="absolute bottom-0 left-1 flex-row items-center gap-1.5 rounded-pill bg-surface px-3 py-1.5">
          <Ionicons name="location-outline" size={15} color="#1D4ED8" />
          <Text className="text-caption text-secondary">Location attached</Text>
        </View>
      </View>

      <View className="mt-10 gap-3">
        <Text className="text-display text-ink">See something? Report it.</Text>
        <Text className="text-body text-secondary">
          Send an incident report in under a minute. Add a photo, drop a location pin, and track exactly what
          happens next.
        </Text>
      </View>

      <View className="flex-1" />

      <OnboardingDots index={0} />

      <View className="mb-10 mt-6 gap-3">
        <AppButton title="Next" onPress={() => router.push('/(auth)/onboarding/track')} />
        <AppButton title="I already have an account" variant="secondary" onPress={() => router.push('/(auth)/sign-in')} />
      </View>
    </View>
  );
}
