import { Ionicons } from '@expo/vector-icons';
import { router, Tabs } from 'expo-router';
import { Pressable, View } from 'react-native';
import { useSafeAreaInsets } from 'react-native-safe-area-context';

const BRAND = '#1D4ED8'; // --brand-primary
const MUTED = '#94A3B8'; // --text-subtle

const TAB_BAR_CONTENT_HEIGHT = 58;

/**
 * Figma "07 Home" (node 8:3): the bottom nav is Home / Reports / Map /
 * Profile, plus a floating center button that jumps straight into the new-
 * report wizard — not a fifth tab. Notifications isn't a tab at all; it's
 * reached from the bell icon in the Home header (see home.tsx).
 */
export default function TabsLayout() {
  const insets = useSafeAreaInsets();
  const tabBarHeight = TAB_BAR_CONTENT_HEIGHT + insets.bottom;

  return (
    <View className="flex-1">
      <Tabs
        screenOptions={{
          headerShown: false,
          tabBarActiveTintColor: BRAND,
          tabBarInactiveTintColor: MUTED,
          tabBarShowLabel: true,
          tabBarStyle: {
            height: tabBarHeight,
            paddingTop: 8,
            paddingBottom: insets.bottom + 6,
            backgroundColor: '#FFFFFF',
            borderTopWidth: 1,
            borderTopColor: '#E2E8F0',
            elevation: 0,
            shadowOpacity: 0,
          },
          tabBarLabelStyle: {
            fontSize: 11,
            fontWeight: '600',
          },
          tabBarItemStyle: {
            paddingTop: 2,
          },
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
        className="absolute left-1/2 h-16 w-16 -translate-x-1/2 items-center justify-center rounded-full bg-brand shadow-lg active:bg-brand-deep"
        style={{ bottom: tabBarHeight - 30, borderWidth: 4, borderColor: '#FFFFFF' }}>
        <Ionicons name="add" size={28} color="#FFFFFF" />
      </Pressable>
    </View>
  );
}
