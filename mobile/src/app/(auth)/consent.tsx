import { Ionicons } from '@expo/vector-icons';
import { router } from 'expo-router';
import { useState } from 'react';
import { Pressable, ScrollView, Switch, Text, View } from 'react-native';

import { AppButton } from '@/components/ui/button';
import { Checkbox } from '@/components/ui/checkbox';
import { useReporterAuth } from '@/components/auth/reporter-auth-context';
import { useScreenTopOffset } from '@/hooks/use-screen-top-offset';
import { ConsentType } from '@/lib/api/auth';

type PermissionKey = 'location' | 'camera' | 'notifications';

const PERMISSIONS: { key: PermissionKey; consentType: ConsentType; icon: keyof typeof Ionicons.glyphMap; title: string; description: string }[] = [
  {
    key: 'location',
    consentType: 'Location',
    icon: 'location-outline',
    title: 'Location',
    description: 'Attaches a precise pin so responders find the incident.',
  },
  {
    key: 'camera',
    consentType: 'Camera',
    icon: 'camera-outline',
    title: 'Camera & photos',
    description: 'Lets you attach evidence to strengthen your report.',
  },
  {
    key: 'notifications',
    consentType: 'Notifications',
    icon: 'notifications-outline',
    title: 'Notifications',
    description: 'Tells you when your report is verified or resolved.',
  },
];

/** Figma "06 Consent & permissions" (node 7:130). */
export default function Consent() {
  const { recordConsent } = useReporterAuth();
  const topOffset = useScreenTopOffset(48);
  const [granted, setGranted] = useState<Record<PermissionKey, boolean>>({
    location: true,
    camera: true,
    notifications: true,
  });
  const [dataConsent, setDataConsent] = useState(false);
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const onSubmit = async () => {
    setError(null);
    setSubmitting(true);
    try {
      await recordConsent([
        ...PERMISSIONS.map((p) => ({ consentType: p.consentType, granted: granted[p.key] })),
        { consentType: 'DataProcessing' as ConsentType, granted: dataConsent },
      ]);
      router.replace('/(app)/(tabs)/home');
    } catch {
      setError('Could not save your choices. Please try again.');
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <ScrollView className="flex-1 bg-canvas" contentContainerClassName="px-5 pb-10">
      <View className="gap-2" style={{ marginTop: topOffset }}>
        <Text className="text-display text-ink">Before you report</Text>
        <Text className="text-body text-secondary">
          U Complain needs a few permissions to make your reports useful to responders.
        </Text>
      </View>

      <View className="mt-9 gap-4">
        {PERMISSIONS.map((p) => (
          <View key={p.key} className="flex-row items-center gap-3 rounded-card border border-border bg-surface p-3.5">
            <View className="h-10 w-10 items-center justify-center rounded-full bg-brand-tint">
              <Ionicons name={p.icon} size={20} color="#1D4ED8" />
            </View>
            <View className="flex-1">
              <Text className="text-body font-semibold text-ink">{p.title}</Text>
              <Text className="text-body-sm text-muted">{p.description}</Text>
            </View>
            <Switch
              value={granted[p.key]}
              onValueChange={(v) => setGranted((g) => ({ ...g, [p.key]: v }))}
              trackColor={{ true: '#1D4ED8', false: '#E2E8F0' }}
            />
          </View>
        ))}

        <View className="gap-3 rounded-card bg-surface-muted p-4">
          <View className="flex-row items-center gap-2">
            <Ionicons name="lock-closed-outline" size={18} color="#334155" />
            <Text className="text-body font-semibold text-ink">How we handle your data</Text>
          </View>
          <Text className="text-body-sm text-secondary">
            Your email is stored as an encrypted reference. Responders see a masked contact only. Reports are
            retained for 24 months, then deleted.
          </Text>
          <Pressable className="flex-row items-start gap-2.5" onPress={() => setDataConsent((v) => !v)}>
            <View className="mt-0.5">
              <Checkbox checked={dataConsent} onChange={setDataConsent} />
            </View>
            <Text className="flex-1 text-body-sm text-secondary">
              I consent to my report data being processed for incident response.
            </Text>
          </Pressable>
        </View>
      </View>

      <View className="flex-1" />

      {error ? <Text className="mb-3 text-center text-body-sm text-status-critical">{error}</Text> : null}

      <View className="mt-10 gap-3">
        <AppButton
          title={submitting ? 'Saving…' : 'Agree and continue'}
          disabled={!dataConsent || submitting}
          onPress={onSubmit}
        />
        <AppButton title="Read the full privacy notice" variant="secondary" onPress={() => {}} />
      </View>
    </ScrollView>
  );
}
