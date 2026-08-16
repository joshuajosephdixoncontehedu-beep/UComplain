import { router } from 'expo-router';
import { Pressable, Text, View } from 'react-native';

import { AppButton } from '@/components/ui/button';
import { BrandLockup } from '@/components/ui/brand-lockup';
import { OnboardingDots } from '@/components/ui/onboarding-dots';
import { ReportIllustration } from '@/components/ui/onboarding-illustration';
import { useScreenTopOffset } from '@/hooks/use-screen-top-offset';

/** Figma "02a Onboarding · Report" (node 6:32). */
export default function OnboardingReport() {
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
        <ReportIllustration />
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
