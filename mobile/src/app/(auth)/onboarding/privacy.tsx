import { router } from 'expo-router';
import { Text, View } from 'react-native';

import { AppButton } from '@/components/ui/button';
import { BrandLockup } from '@/components/ui/brand-lockup';
import { OnboardingDots } from '@/components/ui/onboarding-dots';
import { PrivacyIllustration } from '@/components/ui/onboarding-illustration';
import { useScreenTopOffset } from '@/hooks/use-screen-top-offset';

/** Figma "02c Onboarding · Privacy" (node 42:75) — last step, no "Skip". */
export default function OnboardingPrivacy() {
  const topOffset = useScreenTopOffset();
  return (
    <View className="flex-1 bg-canvas px-5">
      <View style={{ marginTop: topOffset }}>
        <BrandLockup />
      </View>

      <View className="mt-[50px] h-[300px] items-center justify-center">
        <PrivacyIllustration />
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
