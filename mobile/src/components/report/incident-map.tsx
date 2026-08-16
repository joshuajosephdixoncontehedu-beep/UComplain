import { AppleMaps, GoogleMaps } from 'expo-maps';
import { Platform } from 'react-native';
import type { StyleProp, ViewStyle } from 'react-native';

export type IncidentMapMarker = { id: string; latitude: number; longitude: number; title: string; snippet?: string };

/**
 * Thin cross-platform wrapper over expo-maps — Google Maps on Android, Apple Maps on
 * iOS (no unified component; expo-maps exposes the two natively). Native-only: not
 * available in Expo Go or on web (see AGENTS.md — verify with a dev-client build, not
 * `expo export --platform web`).
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
  const cameraPosition = { coordinates: center, zoom };

  if (Platform.OS === 'ios') {
    return (
      <AppleMaps.View
        style={style}
        cameraPosition={cameraPosition}
        markers={markers.map((m) => ({
          id: m.id,
          coordinates: { latitude: m.latitude, longitude: m.longitude },
          title: m.title,
          tintColor: '#1D4ED8',
        }))}
        properties={{ isMyLocationEnabled: true }}
        uiSettings={{ myLocationButtonEnabled: true }}
      />
    );
  }

  return (
    <GoogleMaps.View
      style={style}
      cameraPosition={cameraPosition}
      markers={markers.map((m) => ({
        id: m.id,
        coordinates: { latitude: m.latitude, longitude: m.longitude },
        title: m.title,
        snippet: m.snippet,
        showCallout: true,
      }))}
      properties={{ isMyLocationEnabled: true }}
      uiSettings={{ myLocationButtonEnabled: true, zoomControlsEnabled: false, rotationGesturesEnabled: false }}
    />
  );
}
