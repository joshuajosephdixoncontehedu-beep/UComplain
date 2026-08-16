import { Ionicons } from '@expo/vector-icons';
import * as Location from 'expo-location';
import { useEffect, useState } from 'react';
import { ActivityIndicator, Pressable, ScrollView, Text, View } from 'react-native';
import { useSafeAreaInsets } from 'react-native-safe-area-context';

import { IncidentMap } from '@/components/report/incident-map';
import { NearbyIncidentCard } from '@/components/report/nearby-incident-card';
import { PublicIncident, PublicIncidentAgeBucket, publicMapApi } from '@/lib/api/public-map';
import { iconForCategory } from '@/lib/use-categories';

const RADIUS_M = 3000;

const AGE_LABEL: Record<PublicIncidentAgeBucket, string> = {
  Today: 'Today',
  ThisWeek: 'This week',
  ThisMonth: 'This month',
  Older: 'Earlier',
};

function distanceLabel(meters: number) {
  return meters >= 1000 ? `${(meters / 1000).toFixed(1)} km away` : `${Math.round(meters)} m away`;
}

type LoadState = 'loading' | 'denied' | 'error' | 'ready';

/** Figma "17 Nearby incidents" (node 17:2) — the "Map" tab, backed by a real expo-maps view. */
export default function NearbyIncidents() {
  const insets = useSafeAreaInsets();
  const [position, setPosition] = useState<{ latitude: number; longitude: number } | null>(null);
  const [incidents, setIncidents] = useState<PublicIncident[]>([]);
  const [state, setState] = useState<LoadState>('loading');

  const load = async () => {
    setState('loading');
    const { status } = await Location.requestForegroundPermissionsAsync();
    if (status !== 'granted') {
      setState('denied');
      return;
    }
    try {
      const pos = await Location.getCurrentPositionAsync({ accuracy: Location.Accuracy.Balanced });
      setPosition({ latitude: pos.coords.latitude, longitude: pos.coords.longitude });
      const nearby = await publicMapApi.getNearby(pos.coords.latitude, pos.coords.longitude, RADIUS_M);
      setIncidents(nearby);
      setState('ready');
    } catch {
      setState('error');
    }
  };

  useEffect(() => {
    load();
  }, []);

  return (
    <View className="flex-1 bg-canvas">
      <View className="flex-[3]">
        {position ? (
          <IncidentMap
            style={{ flex: 1 }}
            center={position}
            markers={incidents.map((incident) => ({
              id: incident.id,
              latitude: incident.latitude,
              longitude: incident.longitude,
              title: incident.categoryName,
              snippet: distanceLabel(incident.distanceMeters),
            }))}
          />
        ) : (
          <View className="flex-1 items-center justify-center bg-surface-muted">
            {state === 'loading' ? <ActivityIndicator color="#1D4ED8" /> : null}
            {state === 'denied' ? (
              <View className="items-center gap-3 px-10">
                <Ionicons name="location-outline" size={28} color="#64748B" />
                <Text className="text-center text-body-sm text-secondary">Location access is needed to show incidents near you.</Text>
                <Pressable onPress={load} className="rounded-pill bg-brand px-4 py-2">
                  <Text className="text-body-sm font-semibold text-surface">Try again</Text>
                </Pressable>
              </View>
            ) : null}
            {state === 'error' ? (
              <View className="items-center gap-3 px-10">
                <Text className="text-center text-body-sm text-status-critical">Could not load nearby incidents.</Text>
                <Pressable onPress={load} className="rounded-pill bg-brand px-4 py-2">
                  <Text className="text-body-sm font-semibold text-surface">Try again</Text>
                </Pressable>
              </View>
            ) : null}
          </View>
        )}

        <View
          className="absolute left-5 right-5 flex-row items-center gap-3 rounded-pill bg-surface px-3.5 shadow-sm"
          style={{ top: insets.top + 12, height: 46 }}>
          <Ionicons name="shield-checkmark-outline" size={16} color="#1D4ED8" />
          <Text className="flex-1 text-body-sm text-ink" numberOfLines={1}>
            {state === 'ready' ? `${incidents.length} verified incident${incidents.length === 1 ? '' : 's'} nearby` : 'Nearby incidents'}
          </Text>
          <Pressable onPress={load} hitSlop={8}>
            <Ionicons name="refresh-outline" size={19} color="#334155" />
          </Pressable>
        </View>
      </View>

      <View className="flex-[2] rounded-t-card bg-canvas pt-2.5">
        <View className="mx-auto h-1 w-10 rounded-full bg-border" />

        <View className="mt-2.5 flex-row items-center gap-1.5 px-5">
          <Ionicons name="shield-checkmark-outline" size={14} color="#64748B" />
          <Text className="text-caption text-muted">Only verified reports are shown publicly</Text>
        </View>

        {state === 'ready' && incidents.length === 0 ? (
          <Text className="mt-6 px-5 text-body-sm text-muted">Nothing verified within {(RADIUS_M / 1000).toFixed(1)} km yet.</Text>
        ) : (
          <ScrollView horizontal showsHorizontalScrollIndicator={false} className="mt-4" contentContainerClassName="items-center gap-3 px-5 pb-8">
            {incidents.map((incident) => (
              <NearbyIncidentCard
                key={incident.id}
                icon={iconForCategory(null, incident.categoryIconKey)}
                title={incident.categoryName}
                distance={distanceLabel(incident.distanceMeters)}
                relativeTime={AGE_LABEL[incident.ageBucket]}
              />
            ))}
          </ScrollView>
        )}
      </View>
    </View>
  );
}
