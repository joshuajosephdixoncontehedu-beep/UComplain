import { Redirect } from 'expo-router';
import { View } from 'react-native';

import { useReporterAuth } from '@/components/auth/reporter-auth-context';

export default function Index() {
  const { status } = useReporterAuth();

  if (status === 'loading') return <View className="flex-1 bg-canvas" />;

  return <Redirect href={status === 'authenticated' ? '/(app)/(tabs)/home' : '/(auth)/splash'} />;
}
