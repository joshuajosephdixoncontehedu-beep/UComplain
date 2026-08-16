import { Ionicons } from '@expo/vector-icons';
import * as Clipboard from 'expo-clipboard';
import { router, useLocalSearchParams } from 'expo-router';
import { useEffect, useState } from 'react';
import { Pressable, ScrollView, Text, View } from 'react-native';

import { AppButton } from '@/components/ui/button';
import { InfoBanner } from '@/components/ui/info-banner';
import { useReportDraft } from '@/components/report/report-draft-context';
import { useScreenTopOffset } from '@/hooks/use-screen-top-offset';

const NEXT_STEPS: { icon: keyof typeof Ionicons.glyphMap; title: string; description: string }[] = [
  { icon: 'search-outline', title: 'Verification check', description: 'We confirm the details and check for duplicates.' },
  { icon: 'person-outline', title: 'Officer review', description: 'A responder is assigned and takes action.' },
  { icon: 'notifications-outline', title: 'You get updates', description: 'We notify you at every status change.' },
];

/** Figma "13 Report submitted" (node 12:118) — wizard terminal screen. Shows the real case reference from the submit response. */
export default function ReportSubmitted() {
  const { caseReference } = useLocalSearchParams<{ caseReference?: string }>();
  const { reset } = useReportDraft();
  const topOffset = useScreenTopOffset(64);
  const [copied, setCopied] = useState(false);

  // Draft is consumed once submitted; clear it so a future report starts fresh.
  useEffect(() => reset(), [reset]);

  const onCopy = async () => {
    if (!caseReference) return;
    await Clipboard.setStringAsync(caseReference);
    setCopied(true);
    setTimeout(() => setCopied(false), 2000);
  };

  return (
    <ScrollView className="flex-1 bg-canvas" contentContainerClassName="items-center px-5 pb-10">
      <View className="h-24 w-24 items-center justify-center rounded-full bg-status-resolved-tint" style={{ marginTop: topOffset }}>
        <View className="h-[68px] w-[68px] items-center justify-center rounded-full bg-status-resolved">
          <Ionicons name="checkmark" size={34} color="#FFFFFF" />
        </View>
      </View>

      <View className="mt-6 items-center gap-2">
        <Text className="text-h1 text-ink">Report submitted</Text>
        <Text className="max-w-[310px] text-center text-body text-secondary">
          Thank you. Your report is now with the verification team.
        </Text>
      </View>

      <View className="mt-9 w-full flex-row items-center justify-between rounded-card border border-border bg-surface p-4">
        <View>
          <Text className="text-eyebrow uppercase tracking-wide text-muted">Case reference</Text>
          <Text className="mt-1 text-h2 text-ink">{caseReference ?? '—'}</Text>
        </View>
        <Pressable onPress={onCopy} className="flex-row items-center gap-1.5 rounded-input border border-border px-3 py-2">
          <Ionicons name={copied ? 'checkmark' : 'copy-outline'} size={15} color="#334155" />
          <Text className="text-label text-secondary">{copied ? 'Copied' : 'Copy'}</Text>
        </Pressable>
      </View>

      <Text className="mb-4 mt-9 w-full text-eyebrow uppercase tracking-wide text-muted">What happens next</Text>
      <View className="w-full">
        {NEXT_STEPS.map((step, i) => (
          <View key={step.title} className="flex-row gap-3">
            <View className="items-center">
              <View className="h-[38px] w-[38px] items-center justify-center rounded-full bg-brand-tint">
                <Ionicons name={step.icon} size={19} color="#1D4ED8" />
              </View>
              {i < NEXT_STEPS.length - 1 ? <View className="my-1 h-7 w-[2px] bg-border" /> : null}
            </View>
            <View className="flex-1 pb-3 pt-1.5">
              <Text className="text-body font-semibold text-ink">{step.title}</Text>
              <Text className="mt-0.5 text-body-sm text-muted">{step.description}</Text>
            </View>
          </View>
        ))}
      </View>

      <View className="mt-6 w-full">
        <InfoBanner
          icon="alert-circle-outline"
          tone="warning"
          text="Life-threatening emergency? Call 999 now — do not wait for this report."
        />
      </View>

      <View className="mb-6 mt-9 w-full gap-3">
        <AppButton title="Track this report" onPress={() => router.replace('/(app)/(tabs)/my-reports')} />
        <AppButton title="Back to home" variant="secondary" onPress={() => router.replace('/(app)/(tabs)/home')} />
      </View>
    </ScrollView>
  );
}
