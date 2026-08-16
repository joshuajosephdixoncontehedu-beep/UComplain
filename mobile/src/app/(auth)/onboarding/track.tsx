import { router } from 'expo-router';
import { Pressable, Text, View } from 'react-native';

import { AppButton } from '@/components/ui/button';
import { BrandLockup } from '@/components/ui/brand-lockup';
import { OnboardingDots } from '@/components/ui/onboarding-dots';
import { TrackIllustration } from '@/components/ui/onboarding-illustration';
import { useScreenTopOffset } from '@/hooks/use-screen-top-offset';

/** Figma "02b Onboarding · Track" (node 42:2). */
export default function OnboardingTrack() {
  const topOffset = useScreenTopOffset();
  return (
    <View className="flex-1 bg-canvas px-5">
      <View className="flex-row items-center justify-between" style={{ marginTop: topOffset }}>
        <BrandLockup />
        <Pressable onPress={() => router.replace('/(auth)/sign-in')} hitSlop={8}>
          <Text className="text-body text-muted">Skip</Text>
        </Pressable>
      </View>

      <View className="mt-[50px] h-[300px] items-center justify-center">
        <TrackIllustration />
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
