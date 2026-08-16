import { View } from 'react-native';

// Exact bar heights from Figma "11 New report · Evidence" (node 11:80) voice-note waveform.
const BARS = [6, 11, 16, 9, 19, 13, 7, 15, 20, 10, 14, 8, 17, 11, 6, 13, 18, 9, 12, 7, 15, 10, 6, 14, 8];

export function Waveform() {
  return (
    <View className="flex-row items-center gap-[3px]">
      {BARS.map((h, i) => (
        <View key={i} className="w-[3px] rounded-full bg-brand/40" style={{ height: h }} />
      ))}
    </View>
  );
}
