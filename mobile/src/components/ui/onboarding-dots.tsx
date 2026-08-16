import { View } from 'react-native';

/** Figma "02a/b/c Onboarding": 3-dot pagination, active dot widens to a pill. */
export function OnboardingDots({ index, count = 3 }: { index: number; count?: number }) {
  return (
    <View className="flex-row gap-[7px]">
      {Array.from({ length: count }).map((_, i) => (
        <View
          key={i}
          className={`h-[7px] rounded-pill ${i === index ? 'w-[22px] bg-brand' : 'w-[7px] bg-border'}`}
        />
      ))}
    </View>
  );
}
