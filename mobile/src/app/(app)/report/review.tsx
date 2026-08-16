import { router } from 'expo-router';
import { useEffect, useState } from 'react';
import { Pressable, ScrollView, Text, View } from 'react-native';

import { AppButton } from '@/components/ui/button';
import { BackButton } from '@/components/ui/back-button';
import { Checkbox } from '@/components/ui/checkbox';
import { InfoBanner } from '@/components/ui/info-banner';
import { useReportDraft } from '@/components/report/report-draft-context';
import { SummaryRow } from '@/components/report/summary-row';
import { useScreenTopOffset } from '@/hooks/use-screen-top-offset';

function formatWhen(d: Date) {
  const date = d.toLocaleDateString('en-GB', { day: '2-digit', month: 'short', year: 'numeric' });
  const time = d.toLocaleTimeString('en-GB', { hour: '2-digit', minute: '2-digit', hour12: false });
  return `${date} at ${time}`;
}

function evidenceSummary(photoCount: number, hasVoiceNote: boolean) {
  const parts: string[] = [];
  if (photoCount) parts.push(`${photoCount} photo${photoCount === 1 ? '' : 's'}`);
  if (hasVoiceNote) parts.push('1 voice note');
  return parts.length ? parts.join(' · ') : 'None added';
}

/** Figma "12 Review report" (node 12:2). Submits via POST /reports/drafts/{id}/submit. */
export default function ReviewReport() {
  const { draft, submit } = useReportDraft();
  const topOffset = useScreenTopOffset();
  const [confirmed, setConfirmed] = useState(false);
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (!draft.category) router.replace('/(app)/report/category');
  }, [draft.category]);

  if (!draft.category) return null;

  const onSubmit = async () => {
    setError(null);
    setSubmitting(true);
    try {
      const report = await submit();
      router.replace({ pathname: '/(app)/report/submitted', params: { caseReference: report.caseReference } });
    } catch {
      setError('Could not submit your report. Please try again.');
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <ScrollView className="flex-1 bg-canvas" contentContainerClassName="px-5 pb-10">
      <View className="flex-row items-center gap-5" style={{ marginTop: topOffset }}>
        <BackButton />
        <Text className="text-body-lg font-semibold text-ink">Review your report</Text>
      </View>

      <View className="mt-8">
        <SummaryRow label="Category" value={draft.category.label} onEdit={() => router.push('/(app)/report/category')} />
        <SummaryRow label="Description" value={draft.description} onEdit={() => router.push('/(app)/report/details')} />
        <SummaryRow label="When" value={formatWhen(draft.incidentDate)} onEdit={() => router.push('/(app)/report/details')} />
        <SummaryRow
          label="Location"
          value={draft.locationLabel ? `${draft.locationLabel}${draft.locationSubtitle ? `, ${draft.locationSubtitle.split('·')[0].trim()}` : ''}` : 'Not set'}
          onEdit={() => router.push('/(app)/report/location')}
        />
        <SummaryRow
          label="Severity"
          value={draft.severity ? draft.severity[0].toUpperCase() + draft.severity.slice(1) : 'Not set'}
          onEdit={() => router.push('/(app)/report/details')}
        />
        <SummaryRow
          label="Evidence"
          value={evidenceSummary(draft.photos.length, !!draft.voiceNote)}
          onEdit={() => router.push('/(app)/report/evidence')}
        />
      </View>

      <Pressable className="mt-6 flex-row items-start gap-3" onPress={() => setConfirmed((v) => !v)}>
        <View className="mt-0.5">
          <Checkbox checked={confirmed} onChange={setConfirmed} />
        </View>
        <Text className="flex-1 text-body-sm text-secondary">
          I confirm this report is truthful and accurate to the best of my knowledge.
        </Text>
      </Pressable>

      <View className="mt-5">
        <InfoBanner
          icon="warning-outline"
          tone="warning"
          text="Knowingly filing a false report is an offence. Every submission is checked by a person before action is taken."
        />
      </View>

      {error ? <Text className="mt-4 text-body-sm text-status-critical">{error}</Text> : null}

      <View className="mt-8">
        <AppButton title={submitting ? 'Submitting…' : 'Submit report'} disabled={!confirmed || submitting} onPress={onSubmit} />
      </View>
    </ScrollView>
  );
}
