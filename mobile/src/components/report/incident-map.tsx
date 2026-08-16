import { Camera, Map as MapLibreMap, Marker, UserLocation } from '@maplibre/maplibre-react-native';
import { Ionicons } from '@expo/vector-icons';
import { View } from 'react-native';
import type { StyleProp, ViewStyle } from 'react-native';

export type IncidentMapMarker = { id: string; latitude: number; longitude: number; title: string; snippet?: string };

// OpenFreeMap's public instance — no API key, no account, no request limits (see
// openfreemap.org). Chosen over expo-maps/Google Maps specifically to avoid the
// Google Cloud billing + API key setup entirely.
const MAP_STYLE = 'https://tiles.openfreemap.org/styles/positron';

/**
 * MapLibre-backed map — native-only, requires a dev-client build (same as any real
 * map renderer; not available in Expo Go). Unlike Google/expo-maps, needs no API key.
 */
export function IncidentMap({
  center,
  zoom = 14,
  markers,
  style,
}: {
  center: { latitude: number; longitude: number };
  zoom?: number;
  markers: IncidentMapMarker[];
  style?: StyleProp<ViewStyle>;
}) {
  return (
    <MapLibreMap style={style ?? { flex: 1 }} mapStyle={MAP_STYLE}>
      <Camera initialViewState={{ center: [center.longitude, center.latitude], zoom }} />
      <UserLocation />
      {markers.map((m) => (
        <Marker key={m.id} id={m.id} lngLat={[m.longitude, m.latitude]}>
          <View className="h-9 w-9 items-center justify-center rounded-full bg-surface shadow-sm">
            <Ionicons name="location" size={18} color="#1D4ED8" />
          </View>
        </Marker>
      ))}
    </MapLibreMap>
  );
}
