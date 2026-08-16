import { Ionicons } from '@expo/vector-icons';
import { useState } from 'react';
import { Linking, ScrollView, Text, View } from 'react-native';

import { BackButton } from '@/components/ui/back-button';
import { MenuRow } from '@/components/ui/menu-row';
import { ToggleRow } from '@/components/ui/toggle-row';
import { useReporterAuth } from '@/components/auth/reporter-auth-context';
import { useScreenTopOffset } from '@/hooks/use-screen-top-offset';

/**
 * Real screen: the in-app Notifications tab is always live (server-side events, not a
 * preference). What's a real, backend-backed choice is the "Notifications" consent grant
 * recorded at signup (POST /api/mobile/auth/consent is an upsert, so re-posting it here
 * updates the same record) plus the OS-level notification permission, which we can only
 * deep-link to (RN has no cross-platform read/write API for it outside a push-token flow,
 * and the backend doesn't send pushes yet — DevicesController is persistence-only).
 */
export default function NotificationSettings() {
  const { recordConsent } = useReporterAuth();
  const topOffset = useScreenTopOffset();
  const [statusUpdates, setStatusUpdates] = useState(true);
  const [saving, setSaving] = useState(false);

  const onToggle = async (value: boolean) => {
    setStatusUpdates(value);
    setSaving(true);
    try {
      await recordConsent([{ consentType: 'Notifications', granted: value }]);
    } finally {
      setSaving(false);
    }
  };

  return (
    <ScrollView className="flex-1 bg-canvas" contentContainerClassName="pb-10">
      <View className="flex-row items-center gap-5 px-5" style={{ marginTop: topOffset }}>
        <BackButton />
        <Text className="text-body-lg font-semibold text-ink">Notification settings</Text>
      </View>

      <Text className="mb-2 mt-9 px-5 text-eyebrow uppercase tracking-wide text-muted">In the app</Text>
      <View className="mx-5 rounded-card border border-border bg-surface">
        <ToggleRow
          title="Report status updates"
          subtitle="Verification, assignment, resolution and clarification requests"
          value={statusUpdates}
          onValueChange={onToggle}
        />
      </View>
      {saving ? <Text className="mt-2 px-5 text-caption text-muted">Saving…</Text> : null}

      <Text className="mb-2 mt-9 px-5 text-eyebrow uppercase tracking-wide text-muted">On this device</Text>
      <View className="mx-5 rounded-card border border-border bg-surface">
        <MenuRow
          icon="settings-outline"
          title="Device notification permission"
          subtitle="Manage push permission in your phone's settings"
          onPress={() => Linking.openSettings()}
        />
      </View>

      <View className="mx-5 mt-4 flex-row items-start gap-2.5 rounded-card bg-surface-muted p-3">
        <Ionicons name="information-circle-outline" size={16} color="#64748B" style={{ marginTop: 1 }} />
        <Text className="flex-1 text-body-sm text-secondary">
          Updates always appear in the Notifications tab. Push alerts to your device are not sent yet.
        </Text>
      </View>
    </ScrollView>
  );
}
