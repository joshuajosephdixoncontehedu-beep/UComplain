import { Ionicons } from '@expo/vector-icons';
import * as Location from 'expo-location';
import { router } from 'expo-router';
import { useState } from 'react';
import { ActivityIndicator, KeyboardAvoidingView, Platform, ScrollView, Text, TextInput, View } from 'react-native';

import { AppButton } from '@/components/ui/button';
import { MapPreview } from '@/components/report/map-preview';
import { useReportDraft } from '@/components/report/report-draft-context';
import { WizardHeader } from '@/components/report/wizard-header';

function formatAddress(place: Location.LocationGeocodedAddress): { label: string; subtitle: string } {
  const label = place.street ?? place.name ?? place.district ?? 'Current location';
  const region = [place.city, place.region].filter(Boolean).join(', ') || place.country || '';
  return { label, subtitle: region };
}

/** Figma "10 New report · Location" (node 11:2) — wizard step 3 of 4. */
export default function ReportLocation() {
  const { draft, update, sync, syncing, syncError } = useReportDraft();
  const [locating, setLocating] = useState(false);
  const [locationError, setLocationError] = useState<string | null>(null);

  const onUseMyLocation = async () => {
    setLocationError(null);
    setLocating(true);
    try {
      const { status } = await Location.requestForegroundPermissionsAsync();
      if (status !== 'granted') {
        setLocationError('Location permission was denied. Enable it in Settings to attach your position.');
        return;
      }
      const position = await Location.getCurrentPositionAsync({ accuracy: Location.Accuracy.Balanced });
      const [place] = await Location.reverseGeocodeAsync({
        latitude: position.coords.latitude,
        longitude: position.coords.longitude,
      });
      const { label, subtitle } = place
        ? formatAddress(place)
        : { label: `${position.coords.latitude.toFixed(4)}, ${position.coords.longitude.toFixed(4)}`, subtitle: '' };

      update({
        latitude: position.coords.latitude,
        longitude: position.coords.longitude,
        locationLabel: label,
        locationSubtitle: subtitle
          ? `${subtitle} · ${position.coords.latitude.toFixed(4)}, ${position.coords.longitude.toFixed(4)}`
          : `${position.coords.latitude.toFixed(4)}, ${position.coords.longitude.toFixed(4)}`,
      });
    } catch {
      setLocationError('Could not get your location. Check that location services are enabled.');
    } finally {
      setLocating(false);
    }
  };

  return (
    <KeyboardAvoidingView className="flex-1" behavior={Platform.OS === 'ios' ? 'padding' : undefined}>
      <ScrollView className="flex-1 bg-canvas" contentContainerClassName="px-5 pb-10" keyboardShouldPersistTaps="handled">
        <WizardHeader step={3} />

        <View className="mt-8 gap-1.5">
          <Text className="text-h1 text-ink">Where is it?</Text>
          <Text className="text-body text-secondary">Use your current position, or describe the location and add a nearby landmark.</Text>
        </View>

        <View className="mt-6">
          <MapPreview onUseMyLocation={onUseMyLocation} />
          {locating ? (
            <View className="mt-3 flex-row items-center justify-center gap-2">
              <ActivityIndicator color="#1D4ED8" size="small" />
              <Text className="text-body-sm text-muted">Getting your location…</Text>
            </View>
          ) : null}
          {locationError ? <Text className="mt-3 text-center text-body-sm text-status-critical">{locationError}</Text> : null}
        </View>

        <View className="mt-5 gap-5">
          <View className="gap-2">
            <View className="flex-row items-center justify-between">
              <Text className="text-label text-secondary">Describe the location</Text>
              {draft.latitude !== undefined ? (
                <View className="flex-row items-center gap-1">
                  <Ionicons name="checkmark-circle" size={13} color="#0369A1" />
                  <Text className="text-caption text-status-verified">GPS attached</Text>
                </View>
              ) : null}
            </View>
            {draft.locationSubtitle ? <Text className="text-body-sm text-muted">{draft.locationSubtitle}</Text> : null}
            <TextInput
              className="h-12 rounded-input border border-border bg-surface px-3.5 text-body text-ink"
              placeholder="e.g. Congo Cross Bridge, Freetown"
              placeholderTextColor="#94A3B8"
              value={draft.locationLabel ?? ''}
              onChangeText={(t) => update({ locationLabel: t, latitude: undefined, longitude: undefined, locationSubtitle: undefined })}
            />
          </View>

          <View className="gap-2">
            <View className="flex-row items-center justify-between">
              <Text className="text-label text-secondary">Nearest landmark</Text>
              <Text className="text-body-sm text-subtle">Optional</Text>
            </View>
            <TextInput
              className="h-12 rounded-input border border-border bg-surface px-3.5 text-body text-ink"
              placeholder="Opposite the Shell filling station"
              placeholderTextColor="#94A3B8"
              value={draft.landmark}
              onChangeText={(t) => update({ landmark: t })}
            />
          </View>
        </View>

        {syncError ? <Text className="mt-4 text-body-sm text-status-critical">{syncError}</Text> : null}

        <View className="mt-10">
          <AppButton
            title={syncing ? 'Saving…' : 'Continue'}
            disabled={!draft.locationLabel?.trim() || syncing}
            onPress={async () => {
              try {
                await sync();
                router.push('/(app)/report/evidence');
              } catch {
                // syncError already set.
              }
            }}
          />
        </View>
      </ScrollView>
    </KeyboardAvoidingView>
  );
}
