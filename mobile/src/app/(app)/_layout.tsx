import { Stack } from 'expo-router';

/**
 * Wraps the authenticated area: the tab bar ((tabs)) plus stacks pushed on
 * top of it, like the new-report wizard (report/...), which isn't a tab.
 */
export default function AppLayout() {
  return (
    <Stack screenOptions={{ headerShown: false }}>
      <Stack.Screen name="(tabs)" />
      <Stack.Screen name="report" options={{ presentation: 'modal' }} />
    </Stack>
  );
}
