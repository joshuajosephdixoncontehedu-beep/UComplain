import { Ionicons } from '@expo/vector-icons';
import { router } from 'expo-router';
import { Pressable, Text, View } from 'react-native';

import { AppButton } from '@/components/ui/button';
import { BrandLockup } from '@/components/ui/brand-lockup';
import { OnboardingDots } from '@/components/ui/onboarding-dots';

const STEPS = [
  { icon: 'document-text-outline' as const, title: 'Report received', time: '08:22' },
  { icon: 'checkmark-circle-outline' as const, title: 'Verified as genuine', time: '09:05' },
  { icon: 'construct-outline' as const, title: 'Work in progress', time: 'Now' },
];

/** Figma "02b Onboarding · Track" (node 42:2). */
export default function OnboardingTrack() {
  return (
    <View className="flex-1 bg-canvas px-5">
      <View className="mt-[70px] flex-row items-center justify-between">
        <BrandLockup />
        <Pressable onPress={() => router.replace('/(auth)/sign-in')} hitSlop={8}>
          <Text className="text-body text-muted">Skip</Text>
        </Pressable>
      </View>

      {/* Hero: status timeline illustration */}
      <View className="mt-[50px] h-[300px] items-center justify-center">
        <View className="w-[270px] gap-5 rounded-card border border-border bg-surface p-4">
          {STEPS.map((step, i) => (
            <View key={step.title} className="flex-row gap-3">
              <View className="items-center">
                <View className="h-[26px] w-[26px] items-center justify-center rounded-full bg-brand-tint">
                  <Ionicons name={step.icon} size={14} color="#1D4ED8" />
                </View>
                {i < STEPS.length - 1 ? <View className="mt-1 h-4 w-[2px] bg-border" /> : null}
              </View>
              <View>
                <Text className="text-body font-semibold text-ink">{step.title}</Text>
                <Text className="text-caption text-muted">{step.time}</Text>
              </View>
            </View>
          ))}
        </View>

        <View className="absolute bottom-2 flex-row items-center gap-1.5 rounded-pill bg-surface px-3 py-1.5">
          <Ionicons name="person-outline" size={15} color="#1D4ED8" />
          <Text className="text-caption text-secondary">Officer assigned</Text>
        </View>
      </View>

      <View className="mt-10 gap-3">
        <Text className="text-display text-ink">Follow it from report to repair.</Text>
        <Text className="text-body text-secondary">
          Watch your report move through verification, assignment and repair — with a timestamp at every step.
        </Text>
      </View>

      <View className="flex-1" />

      <OnboardingDots index={1} />

      <View className="mb-10 mt-6 gap-3">
        <AppButton title="Next" onPress={() => router.push('/(auth)/onboarding/privacy')} />
        <AppButton title="I already have an account" variant="secondary" onPress={() => router.push('/(auth)/sign-in')} />
      </View>
    </View>
  );
}
