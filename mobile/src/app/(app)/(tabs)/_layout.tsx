import { Ionicons } from '@expo/vector-icons';
import { router, Tabs } from 'expo-router';
import { Pressable, View } from 'react-native';

const BRAND = '#1D4ED8'; // --brand-primary
const MUTED = '#94A3B8'; // --text-subtle

/**
 * Figma "07 Home" (node 8:3): the bottom nav is Home / Reports / Map /
 * Profile, plus a floating center button that jumps straight into the new-
 * report wizard — not a fifth tab. Notifications isn't a tab at all; it's
 * reached from the bell icon in the Home header (see home.tsx).
 */
export default function TabsLayout() {
  return (
    <View className="flex-1">
      <Tabs
        screenOptions={{
          headerShown: false,
          tabBarActiveTintColor: BRAND,
          tabBarInactiveTintColor: MUTED,
        }}>
        <Tabs.Screen
          name="home"
          options={{
            title: 'Home',
            tabBarIcon: ({ color, size }) => <Ionicons name="home-outline" color={color} size={size} />,
          }}
        />
        <Tabs.Screen
          name="my-reports"
          options={{
            title: 'Reports',
            tabBarIcon: ({ color, size }) => <Ionicons name="document-text-outline" color={color} size={size} />,
          }}
        />
        <Tabs.Screen
          name="nearby-incidents"
          options={{
            title: 'Map',
            tabBarIcon: ({ color, size }) => <Ionicons name="map-outline" color={color} size={size} />,
          }}
        />
        <Tabs.Screen
          name="profile"
          options={{
            title: 'Profile',
            tabBarIcon: ({ color, size }) => <Ionicons name="person-outline" color={color} size={size} />,
          }}
        />
        {/* Reachable route, hidden from the tab bar — see home.tsx's bell icon. */}
        <Tabs.Screen name="notifications" options={{ href: null }} />
      </Tabs>

      <Pressable
        onPress={() => router.push('/(app)/report/category')}
        className="absolute bottom-6 left-1/2 h-[62px] w-[62px] -translate-x-1/2 items-center justify-center rounded-full bg-brand shadow-lg active:bg-brand-deep">
        <Ionicons name="add" size={28} color="#FFFFFF" />
      </Pressable>
    </View>
  );
}
