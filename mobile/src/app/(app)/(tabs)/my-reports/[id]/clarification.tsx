import { Ionicons } from '@expo/vector-icons';
import { router, useLocalSearchParams } from 'expo-router';
import { useEffect, useState } from 'react';
import { ActivityIndicator, KeyboardAvoidingView, Platform, ScrollView, Text, TextInput, View } from 'react-native';

import { AppButton } from '@/components/ui/button';
import { BackButton } from '@/components/ui/back-button';
import { InfoBanner } from '@/components/ui/info-banner';
import { useReporterAuth } from '@/components/auth/reporter-auth-context';
import { useScreenTopOffset } from '@/hooks/use-screen-top-offset';
import { clarificationsApi } from '@/lib/api/clarifications';
import { ClarificationRequest, reportsApi } from '@/lib/api/reports';

const MAX_LENGTH = 500;

function formatDateTime(iso: string) {
  const d = new Date(iso);
  return `${d.toLocaleDateString('en-GB', { day: '2-digit', month: 'short', year: 'numeric' })}, ${d.toLocaleTimeString('en-GB', { hour: '2-digit', minute: '2-digit', hour12: false })}`;
}

/**
 * Figma "16 Needs clarification" (node 14:291). Two distinct backend actions share this layout:
 * replying to a pending clarification (clarificationId param → POST /clarifications/{id}/reply)
 * vs. proactively adding information with no open clarification (POST /reports/{id}/information).
 */
export default function NeedsClarification() {
  const { id, clarificationId } = useLocalSearchParams<{ id: string; clarificationId?: string }>();
  const { authorizedRequest } = useReporterAuth();
  const topOffset = useScreenTopOffset();

  const [clarification, setClarification] = useState<ClarificationRequest | null>(null);
  const [loading, setLoading] = useState(!!clarificationId);
  const [loadError, setLoadError] = useState(false);
  const [reply, setReply] = useState('');
  const [submitting, setSubmitting] = useState(false);

  useEffect(() => {
    if (!clarificationId) return;
    reportsApi
      .getClarifications(authorizedRequest, id)
      .then((list) => {
        const match = list.find((c) => c.id === clarificationId);
        if (match) setClarification(match);
        else setLoadError(true);
      })
      .catch(() => setLoadError(true))
      .finally(() => setLoading(false));
    // Fetch once for this clarification id.
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [id, clarificationId]);

  const onSend = async () => {
    setSubmitting(true);
    try {
      if (clarificationId) {
        await clarificationsApi.reply(authorizedRequest, clarificationId, reply.trim());
      } else {
        await reportsApi.addInformation(authorizedRequest, id, reply.trim());
      }
      router.replace(`/(app)/(tabs)/my-reports/${id}`);
    } finally {
      setSubmitting(false);
    }
  };

  if (loading) {
    return (
      <View className="flex-1 items-center justify-center bg-canvas">
        <ActivityIndicator color="#1D4ED8" />
      </View>
    );
  }
  if (clarificationId && (loadError || !clarification)) {
    return (
      <View className="flex-1 items-center justify-center bg-canvas px-5">
        <Text className="text-body text-status-critical">Could not load this request.</Text>
      </View>
    );
  }

  return (
    <KeyboardAvoidingView className="flex-1" behavior={Platform.OS === 'ios' ? 'padding' : undefined}>
    <ScrollView className="flex-1 bg-canvas" contentContainerClassName="px-5 pb-10" keyboardShouldPersistTaps="handled">
      <View className="flex-row items-center gap-5" style={{ marginTop: topOffset }}>
        <BackButton />
        <Text className="text-body-lg font-semibold text-ink">{clarification ? 'Needs clarification' : 'Add information'}</Text>
      </View>

      <View className="mt-9">
        <InfoBanner
          icon="information-circle-outline"
          tone={clarification ? 'warning' : 'info'}
          text={
            clarification
              ? 'More information needed — this report is paused until you reply. It has not entered the active queue yet.'
              : 'Add anything else that will help the verification team — it is added to this report for the record.'
          }
        />
      </View>

      {clarification ? (
        <View className="mt-4 gap-4 rounded-card border border-border bg-surface p-4">
          <View className="flex-row items-center gap-2.5">
            <View className="h-[34px] w-[34px] items-center justify-center rounded-full bg-brand-tint">
              <Text className="text-caption font-semibold text-brand">VT</Text>
            </View>
            <View>
              <Text className="text-body font-semibold text-ink">Verification team</Text>
              <Text className="text-body-sm text-muted">{formatDateTime(clarification.requestedAt)}</Text>
            </View>
          </View>
          <Text className="text-body text-secondary">{clarification.message}</Text>
        </View>
      ) : null}

      <View className="mt-6 gap-2">
        <Text className="text-label text-secondary">{clarification ? 'Your reply' : 'Your note'}</Text>
        <View className="gap-3 rounded-input border border-border bg-surface p-3.5">
          <TextInput
            className="min-h-[64px] text-body text-ink"
            placeholder={clarification ? 'Type your reply…' : 'Type your note…'}
            placeholderTextColor="#94A3B8"
            multiline
            maxLength={MAX_LENGTH}
            value={reply}
            onChangeText={setReply}
          />
          <View className="flex-row items-center justify-end">
            <Text className="text-caption text-muted">
              {reply.length} / {MAX_LENGTH}
            </Text>
          </View>
        </View>
      </View>

      {clarification ? (
        <View className="mt-4 flex-row items-center gap-2.5 rounded-card bg-surface-muted p-3">
          <Ionicons name="time-outline" size={17} color="#64748B" />
          <Text className="flex-1 text-body-sm text-secondary">Reply by {formatDateTime(clarification.dueAt)} or the report will be closed.</Text>
        </View>
      ) : null}

      <View className="mt-8">
        <AppButton title={clarification ? 'Send reply' : 'Add information'} disabled={!reply.trim() || submitting} onPress={onSend} />
      </View>
    </ScrollView>
    </KeyboardAvoidingView>
  );
}
