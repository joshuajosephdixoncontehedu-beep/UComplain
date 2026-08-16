import { Ionicons } from '@expo/vector-icons';
import DateTimePicker from '@react-native-community/datetimepicker';
import { router } from 'expo-router';
import { useEffect, useState } from 'react';
import { Platform, Pressable, ScrollView, Text, TextInput, View } from 'react-native';

import { AppButton } from '@/components/ui/button';
import { SegmentedControl } from '@/components/ui/segmented-control';
import { Severity, useReportDraft } from '@/components/report/report-draft-context';
import { WizardHeader } from '@/components/report/wizard-header';

const MAX_LENGTH = 500;
const SEVERITY_OPTIONS: { value: Severity; label: string }[] = [
  { value: 'low', label: 'Low' },
  { value: 'medium', label: 'Medium' },
  { value: 'high', label: 'High' },
  { value: 'critical', label: 'Critical' },
];

function formatDate(d: Date) {
  return d.toLocaleDateString('en-GB', { day: '2-digit', month: 'short', year: 'numeric' });
}
function formatTime(d: Date) {
  return d.toLocaleTimeString('en-GB', { hour: '2-digit', minute: '2-digit', hour12: false });
}

/** Figma "09 New report · Details" (node 8:251) — wizard step 2 of 4. */
export default function ReportDetails() {
  const { draft, update, sync, syncing, syncError } = useReportDraft();
  const [picker, setPicker] = useState<'date' | 'time' | null>(null);

  useEffect(() => {
    // Guard against deep-linking into step 2 without a category picked.
    if (!draft.category) router.replace('/(app)/report/category');
  }, [draft.category]);

  if (!draft.category) return null;

  const onPickerChange = (event: { type: string }, selected?: Date) => {
    const wasSpinner = Platform.OS === 'ios';
    if (!wasSpinner) setPicker(null);
    if (event.type !== 'set' || !selected) return;

    const merged = new Date(draft.incidentDate);
    if (picker === 'date') {
      merged.setFullYear(selected.getFullYear(), selected.getMonth(), selected.getDate());
    } else {
      merged.setHours(selected.getHours(), selected.getMinutes());
    }
    update({ incidentDate: merged });
  };

  return (
    <ScrollView className="flex-1 bg-canvas" contentContainerClassName="px-5 pb-10">
      <WizardHeader step={2} />

      <View className="mt-8 gap-5">
        <Pressable
          onPress={() => router.push('/(app)/report/category')}
          className="flex-row items-center gap-3 rounded-input border border-border bg-surface p-3">
          <View className="h-[30px] w-[30px] items-center justify-center rounded-full bg-brand-tint">
            <Ionicons name={draft.category.icon} size={17} color="#1D4ED8" />
          </View>
          <Text className="flex-1 text-body font-semibold text-ink">{draft.category.label}</Text>
          <Text className="text-body-sm font-semibold text-brand">Change</Text>
        </Pressable>

        <View className="gap-2">
          <Text className="text-label text-secondary">Describe what happened</Text>
          <View className="gap-3 rounded-input border border-border bg-surface p-3.5">
            <TextInput
              className="min-h-[110px] text-body text-ink"
              placeholder="What did you see? Include anything that would help a responder."
              placeholderTextColor="#94A3B8"
              multiline
              maxLength={MAX_LENGTH}
              value={draft.description}
              onChangeText={(t) => update({ description: t })}
            />
            <View className="flex-row items-center justify-end">
              <Text className="text-caption text-muted">
                {draft.description.length} / {MAX_LENGTH}
              </Text>
            </View>
          </View>
        </View>

        <View className="gap-2">
          <Text className="text-label text-secondary">When did it happen?</Text>
          <View className="flex-row gap-3">
            <Pressable
              onPress={() => setPicker('date')}
              className="h-12 flex-1 flex-row items-center gap-2.5 rounded-input border border-border bg-surface px-3.5">
              <Ionicons name="calendar-outline" size={18} color="#94A3B8" />
              <Text className="text-body text-ink">{formatDate(draft.incidentDate)}</Text>
            </Pressable>
            <Pressable
              onPress={() => setPicker('time')}
              className="h-12 flex-1 flex-row items-center gap-2.5 rounded-input border border-border bg-surface px-3.5">
              <Ionicons name="time-outline" size={18} color="#94A3B8" />
              <Text className="text-body text-ink">{formatTime(draft.incidentDate)}</Text>
            </Pressable>
          </View>
          {picker ? (
            <DateTimePicker
              value={draft.incidentDate}
              mode={picker}
              maximumDate={new Date()}
              display={Platform.OS === 'ios' ? 'spinner' : 'default'}
              onChange={onPickerChange}
            />
          ) : null}
        </View>

        <View className="gap-2">
          <Text className="text-label text-secondary">How serious is it?</Text>
          <SegmentedControl options={SEVERITY_OPTIONS} value={draft.severity} onChange={(v) => update({ severity: v })} />
        </View>
      </View>

      {syncError ? <Text className="mt-4 text-body-sm text-status-critical">{syncError}</Text> : null}

      <View className="mt-10">
        <AppButton
          title={syncing ? 'Saving…' : 'Continue'}
          disabled={!draft.description.trim() || !draft.severity || syncing}
          onPress={async () => {
            try {
              await sync();
              router.push('/(app)/report/location');
            } catch {
              // syncError already set.
            }
          }}
        />
      </View>
    </ScrollView>
  );
}
