import { Ionicons } from '@expo/vector-icons';
import * as Location from 'expo-location';
import { useEffect, useState } from 'react';
import { ActivityIndicator, Pressable, ScrollView, Text, View } from 'react-native';

import { NearbyIncidentCard } from '@/components/report/nearby-incident-card';
import { PublicIncident, PublicIncidentAgeBucket, publicMapApi } from '@/lib/api/public-map';
import { iconForCategory } from '@/lib/use-categories';

// Abstract "city blocks" reproducing the Figma map mock (node 17:2) layout — no map
// library is wired up (see the flagged decision against react-native-maps), so real
// incidents are plotted onto this decorative canvas by bearing/distance from the user.
const BLOCKS = [
  { top: 60, left: 10, w: 120, h: 90 },
  { top: 40, left: 160, w: 130, h: 100 },
  { top: 70, left: 318, w: 90, h: 80 },
  { top: 190, left: 0, w: 110, h: 110 },
  { top: 182, left: 142, w: 140, h: 120 },
  { top: 196, left: 312, w: 96, h: 104 },
  { top: 344, left: 16, w: 124, h: 86 },
  { top: 336, left: 172, w: 116, h: 96 },
  { top: 350, left: 318, w: 90, h: 80 },
  { top: 472, left: 30, w: 140, h: 70 },
  { top: 466, left: 204, w: 120, h: 78 },
];

const RADIUS_M = 3000;
const CENTER = { top: 262, left: 196 };
const MAX_PIN_RADIUS = 230;
const MIN_PIN_RADIUS = 34;

const AGE_LABEL: Record<PublicIncidentAgeBucket, string> = {
  Today: 'Today',
  ThisWeek: 'This week',
  ThisMonth: 'This month',
  Older: 'Earlier',
};

function distanceLabel(meters: number) {
  return meters >= 1000 ? `${(meters / 1000).toFixed(1)} km away` : `${Math.round(meters)} m away`;
}

/** Places an incident on the abstract map canvas by real bearing/distance from the user's position. */
function pinPosition(incident: PublicIncident, userLat: number, userLng: number) {
  const dLat = incident.latitude - userLat;
  const dLng = (incident.longitude - userLng) * Math.cos((userLat * Math.PI) / 180);
  const bearing = Math.atan2(dLng, dLat);
  const pixelRadius = MIN_PIN_RADIUS + Math.min(incident.distanceMeters / RADIUS_M, 1) * (MAX_PIN_RADIUS - MIN_PIN_RADIUS);
  return { top: CENTER.top + Math.cos(bearing) * pixelRadius, left: CENTER.left + Math.sin(bearing) * pixelRadius };
}

type LoadState = 'loading' | 'denied' | 'error' | 'ready';

/** Figma "17 Nearby incidents" (node 17:2) — the "Map" tab. */
export default function NearbyIncidents() {
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
      <View className="h-[620px] w-full overflow-hidden bg-surface-muted">
        {BLOCKS.map((b, i) => (
          <View key={i} className="absolute rounded-chip bg-border" style={{ top: b.top, left: b.left, width: b.w, height: b.h }} />
        ))}
        <View className="absolute left-0 right-0 h-2.5 bg-surface" style={{ top: 158 }} />
        <View className="absolute left-0 right-0 h-2.5 bg-surface" style={{ top: 312 }} />
        <View className="absolute left-0 right-0 h-2.5 bg-surface" style={{ top: 442 }} />
        <View className="absolute bottom-0 top-0 w-2.5 bg-surface" style={{ left: 118 }} />
        <View className="absolute bottom-0 top-0 w-2.5 bg-surface" style={{ left: 292 }} />

        {position ? (
          <View className="absolute h-24 w-24 items-center justify-center rounded-full bg-brand/15" style={{ top: CENTER.top - 48, left: CENTER.left - 48 }}>
            <View className="h-[46px] w-[46px] items-center justify-center rounded-full bg-brand">
              <Ionicons name="location" size={22} color="#FFFFFF" />
            </View>
          </View>
        ) : null}

        {position &&
          incidents.map((incident) => {
            const pos = pinPosition(incident, position.latitude, position.longitude);
            return (
              <View
                key={incident.id}
                className="absolute h-[38px] w-[38px] items-center justify-center rounded-full bg-surface shadow-sm"
                style={{ top: pos.top - 19, left: pos.left - 19 }}>
                <Ionicons name={iconForCategory(null, incident.categoryIconKey)} size={18} color="#1D4ED8" />
              </View>
            );
          })}

        {state === 'loading' ? (
          <View className="absolute inset-0 items-center justify-center">
            <ActivityIndicator color="#1D4ED8" />
          </View>
        ) : null}

        {state === 'denied' ? (
          <View className="absolute inset-0 items-center justify-center gap-3 px-10">
            <Ionicons name="location-outline" size={28} color="#64748B" />
            <Text className="text-center text-body-sm text-secondary">Location access is needed to show incidents near you.</Text>
            <Pressable onPress={load} className="rounded-pill bg-brand px-4 py-2">
              <Text className="text-body-sm font-semibold text-surface">Try again</Text>
            </Pressable>
          </View>
        ) : null}

        {state === 'error' ? (
          <View className="absolute inset-0 items-center justify-center gap-3 px-10">
            <Text className="text-center text-body-sm text-status-critical">Could not load nearby incidents.</Text>
            <Pressable onPress={load} className="rounded-pill bg-brand px-4 py-2">
              <Text className="text-body-sm font-semibold text-surface">Try again</Text>
            </Pressable>
          </View>
        ) : null}

        <Pressable
          onPress={load}
          className="absolute right-5 h-11 w-11 items-center justify-center rounded-full bg-surface shadow-sm"
          style={{ top: 462 }}>
          <Ionicons name="navigate-outline" size={20} color="#1D4ED8" />
        </Pressable>
      </View>

      <View className="-mt-6 flex-1 rounded-t-card bg-canvas pt-2.5">
        <View className="mx-auto h-1 w-10 rounded-full bg-border" />

        <View className="mt-4 flex-row items-center justify-between px-5">
          <Text className="text-body-lg font-semibold text-ink">
            {state === 'ready' ? `${incidents.length} verified incident${incidents.length === 1 ? '' : 's'} nearby` : 'Nearby incidents'}
          </Text>
        </View>

        <View className="mt-2.5 flex-row items-center gap-1.5 px-5">
          <Ionicons name="shield-checkmark-outline" size={14} color="#64748B" />
          <Text className="text-caption text-muted">Only verified reports are shown publicly</Text>
        </View>

        {state === 'ready' && incidents.length === 0 ? (
          <Text className="mt-6 px-5 text-body-sm text-muted">Nothing verified within {(RADIUS_M / 1000).toFixed(1)} km yet.</Text>
        ) : (
          <ScrollView horizontal showsHorizontalScrollIndicator={false} className="mt-6" contentContainerClassName="gap-3 px-5 pb-8">
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
