import { Image } from 'expo-image';
import { Text, View } from 'react-native';

const icon = require('@/assets/images/brand-mark.png');

/** Figma "Brand lockup": 26×26 mark + "U COMPLAIN" wordmark, top-left on onboarding. */
export function BrandLockup() {
  return (
    <View className="flex-row items-center gap-2">
      <Image source={icon} style={{ width: 26, height: 26, borderRadius: 6 }} contentFit="cover" />
      <Text className="text-base font-bold tracking-wide text-ink">U COMPLAIN</Text>
    </View>
  );
}
