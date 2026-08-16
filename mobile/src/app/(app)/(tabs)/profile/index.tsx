import { Ionicons } from '@expo/vector-icons';
import { router } from 'expo-router';
import { useEffect, useState } from 'react';
import { ActivityIndicator, ScrollView, Text, View } from 'react-native';

import { Avatar } from '@/components/ui/avatar';
import { MenuRow } from '@/components/ui/menu-row';
import { useReporterAuth } from '@/components/auth/reporter-auth-context';
import { meApi, ReporterStats } from '@/lib/api/me';

function memberSinceLabel(iso: string) {
  return new Date(iso).toLocaleDateString('en-GB', { month: 'long', year: 'numeric' });
}

/** Figma "19 Profile" (node 18:2). */
export default function Profile() {
  const { reporter, authorizedRequest, logout } = useReporterAuth();
  const reporterName = reporter?.fullName ?? '';
  const email = reporter?.email ?? '';

  const [stats, setStats] = useState<ReporterStats | null>(null);
  useEffect(() => {
    meApi
      .getStats(authorizedRequest)
      .then(setStats)
      .catch(() => undefined);
    // Fetch once on mount.
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  return (
    <ScrollView className="flex-1 bg-canvas">
      <Text className="mt-[68px] px-5 text-h1 text-ink">Profile</Text>

      <View className="mx-5 mt-10 flex-row items-center gap-4 rounded-card border border-border bg-surface p-4">
        <Avatar name={reporterName} size={56} />
        <View className="flex-1">
          <Text className="text-body-lg font-semibold text-ink">{reporterName}</Text>
          <Text className="mt-0.5 text-body-sm text-muted">{email}</Text>
          <View className="mt-2 flex-row items-center gap-2">
            {reporter?.emailVerified ? (
              <View className="flex-row items-center gap-1.5 self-start rounded-pill bg-status-verified-tint px-2.5 py-1">
                <Ionicons name="checkmark-circle" size={12} color="#0369A1" />
                <Text className="text-[11px] font-semibold text-status-verified">Verified account</Text>
              </View>
            ) : null}
            {stats ? <Text className="text-caption text-muted">Member since {memberSinceLabel(stats.memberSince)}</Text> : null}
          </View>
        </View>
      </View>

      <View className="mx-5 mt-5 flex-row rounded-card border border-border bg-surface py-4">
        {!stats ? (
          <View className="flex-1 items-center py-2">
            <ActivityIndicator color="#1D4ED8" />
          </View>
        ) : (
          [
            { value: stats.totalReports, label: 'Submitted' },
            { value: stats.activeReports, label: 'Active' },
            { value: stats.resolvedReports, label: 'Resolved' },
          ].map((s) => (
            <View key={s.label} className="flex-1 items-center gap-1">
              <Text className="text-h1 text-ink">{s.value}</Text>
              <Text className="text-caption text-muted">{s.label}</Text>
            </View>
          ))
        )}
      </View>

      <View className="mx-5 mt-6 rounded-card border border-border bg-surface">
        <MenuRow icon="person-outline" title="Personal details" subtitle="Name, email and contact" />
        <View className="h-px bg-border" />
        <MenuRow icon="notifications-outline" title="Notification settings" subtitle="Alerts for status changes" />
        <View className="h-px bg-border" />
        <MenuRow icon="lock-closed-outline" title="Privacy & data" subtitle="Consent, retention and deletion" onPress={() => router.push('/(app)/(tabs)/profile/privacy')} />
      </View>

      <View className="mx-5 mt-4 rounded-card border border-border bg-surface">
        <MenuRow icon="language-outline" title="Language" subtitle="English (Sierra Leone)" />
        <View className="h-px bg-border" />
        <MenuRow icon="help-buoy-outline" title="Help & support" />
        <View className="h-px bg-border" />
        <MenuRow icon="information-circle-outline" title="About U Complain" subtitle="Version 1.0.0" />
      </View>

      <View className="mx-5 mb-32 mt-4 rounded-card border border-border bg-surface">
        <MenuRow
          icon="log-out-outline"
          title="Sign out"
          tone="danger"
          onPress={() => logout().then(() => router.replace('/(auth)/sign-in'))}
        />
      </View>
    </ScrollView>
  );
}
