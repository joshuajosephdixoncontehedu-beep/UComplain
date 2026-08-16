import { Ionicons } from '@expo/vector-icons';
import { Pressable, Text, View } from 'react-native';

const BLOCKS = [
  { top: 18, left: 14, w: 90, h: 64 },
  { top: 10, left: 128, w: 110, h: 72 },
  { top: 22, left: 262, w: 100, h: 58 },
  { top: 110, left: 10, w: 84, h: 70 },
  { top: 106, left: 240, w: 116, h: 66 },
  { top: 206, left: 20, w: 120, h: 52 },
  { top: 204, left: 168, w: 90, h: 54 },
];

/**
 * Figma "10 New report · Location" (node 11:2): a stylized abstract map
 * (blocks + two crossing roads + a center pin), reproducing the mock used in
 * the design frame itself rather than a live map.
 * TODO(next phase): swap for a real map (e.g. react-native-maps) with a
 * draggable pin and reverse-geocoding for the address line below it.
 */
export function MapPreview({ onUseMyLocation }: { onUseMyLocation?: () => void }) {
  return (
    <View className="h-[268px] w-full overflow-hidden rounded-card bg-surface-muted">
      {BLOCKS.map((b, i) => (
        <View
          key={i}
          className="absolute rounded-chip bg-border"
          style={{ top: b.top, left: b.left, width: b.w, height: b.h }}
        />
      ))}
      {/* crossing roads */}
      <View className="absolute left-0 right-0 h-3.5 bg-surface" style={{ top: 88 }} />
      <View className="absolute bottom-0 top-0 w-3.5 bg-surface" style={{ left: 104 }} />

      <View className="absolute inset-0 items-center justify-center">
        <View className="h-[74px] w-[74px] items-center justify-center rounded-full bg-brand/15">
          <View className="h-11 w-11 items-center justify-center rounded-full bg-brand">
            <Ionicons name="location" size={22} color="#FFFFFF" />
          </View>
        </View>
      </View>

      <Pressable
        onPress={onUseMyLocation}
        className="absolute bottom-3.5 left-3.5 flex-row items-center gap-2 rounded-pill bg-surface px-3 py-2.5 shadow-sm">
        <Ionicons name="navigate-outline" size={16} color="#1D4ED8" />
        <Text className="text-label text-brand">Use my location</Text>
      </Pressable>
    </View>
  );
}
