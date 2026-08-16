import { Ionicons } from '@expo/vector-icons';
import { Pressable } from 'react-native';

export function Checkbox({ checked, onChange }: { checked: boolean; onChange: (v: boolean) => void }) {
  return (
    <Pressable
      onPress={() => onChange(!checked)}
      hitSlop={8}
      className={`h-5 w-5 items-center justify-center rounded-chip ${
        checked ? 'bg-brand' : 'border border-border bg-surface'
      }`}>
      {checked ? <Ionicons name="checkmark" size={14} color="#FFFFFF" /> : null}
    </Pressable>
  );
}
