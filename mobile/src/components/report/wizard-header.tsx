import { router } from 'expo-router';
import { Text, View } from 'react-native';

import { BackButton } from '@/components/ui/back-button';

/** Figma "08–11 New report · …" headers: back + title + step label + progress bar. */
export function WizardHeader({ step, onBack }: { step: 1 | 2 | 3 | 4; onBack?: () => void }) {
  return (
    <View className="mt-[68px] gap-[18px]">
      <View className="flex-row items-center justify-between">
        <BackButton onPress={onBack ?? (() => router.back())} />
        <Text className="text-body-lg font-semibold text-ink">New report</Text>
        <Text className="text-body-sm text-muted">Step {step} of 4</Text>
      </View>
      <View className="h-[5px] w-full rounded-pill bg-surface-muted">
        <View className="h-[5px] rounded-pill bg-brand" style={{ width: `${(step / 4) * 100}%` }} />
      </View>
    </View>
  );
}
