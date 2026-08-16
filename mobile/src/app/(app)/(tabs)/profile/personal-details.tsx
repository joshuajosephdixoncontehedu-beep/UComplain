import { router } from 'expo-router';
import { useState } from 'react';
import { ScrollView, Text, View } from 'react-native';

import { AppButton } from '@/components/ui/button';
import { BackButton } from '@/components/ui/back-button';
import { TextField } from '@/components/ui/text-field';
import { useReporterAuth } from '@/components/auth/reporter-auth-context';
import { useScreenTopOffset } from '@/hooks/use-screen-top-offset';
import { meApi } from '@/lib/api/me';

/** Real name edit, backed by PATCH /api/mobile/me. Email/phone are shown but not
 *  editable here — UpdateMyProfileRequest only accepts fullName/languagePreference. */
export default function PersonalDetails() {
  const { reporter, authorizedRequest, setReporter } = useReporterAuth();
  const topOffset = useScreenTopOffset();
  const [fullName, setFullName] = useState(reporter?.fullName ?? '');
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const onSave = async () => {
    setError(null);
    setSaving(true);
    try {
      const updated = await meApi.updateProfile(authorizedRequest, { fullName: fullName.trim() });
      await setReporter(updated);
      router.back();
    } catch {
      setError('Could not save your changes. Please try again.');
    } finally {
      setSaving(false);
    }
  };

  return (
    <ScrollView className="flex-1 bg-canvas" contentContainerClassName="px-5 pb-10">
      <View className="flex-row items-center gap-5" style={{ marginTop: topOffset }}>
        <BackButton />
        <Text className="text-body-lg font-semibold text-ink">Personal details</Text>
      </View>

      <View className="mt-9 gap-5">
        <TextField label="Full name" icon="person-outline" value={fullName} onChangeText={setFullName} autoCapitalize="words" />
        <View className="gap-1.5">
          <Text className="text-label text-secondary">Email address</Text>
          <View className="h-12 justify-center rounded-input border border-border bg-surface-muted px-3.5">
            <Text className="text-body text-muted">{reporter?.email}</Text>
          </View>
        </View>
        {reporter?.phoneNumber ? (
          <View className="gap-1.5">
            <Text className="text-label text-secondary">Phone number</Text>
            <View className="h-12 justify-center rounded-input border border-border bg-surface-muted px-3.5">
              <Text className="text-body text-muted">{reporter.phoneNumber}</Text>
            </View>
          </View>
        ) : null}
      </View>

      {error ? <Text className="mt-4 text-body-sm text-status-critical">{error}</Text> : null}

      <View className="mt-9">
        <AppButton title={saving ? 'Saving…' : 'Save changes'} disabled={!fullName.trim() || saving} onPress={onSave} />
      </View>
    </ScrollView>
  );
}
