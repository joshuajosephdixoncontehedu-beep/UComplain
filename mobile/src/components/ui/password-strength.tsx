import { Text, View } from 'react-native';

const LEVELS = [
  { label: 'Weak', bar: 'bg-status-critical', text: 'text-status-critical' },
  { label: 'Fair', bar: 'bg-status-pending', text: 'text-status-pending' },
  { label: 'Good', bar: 'bg-status-verified', text: 'text-status-verified' },
  { label: 'Strong', bar: 'bg-status-resolved', text: 'text-status-resolved' },
] as const;

function score(password: string): number {
  if (!password) return -1;
  let s = 0;
  if (password.length >= 8) s += 1;
  if (/[A-Z]/.test(password) && /[a-z]/.test(password)) s += 1;
  if (/\d/.test(password)) s += 1;
  if (/[^A-Za-z0-9]/.test(password) || password.length >= 12) s += 1;
  return Math.min(s, LEVELS.length) - 1;
}

/** Figma "04 Create account" (node 7:2): 4-segment meter + level label. */
export function PasswordStrength({ password }: { password: string }) {
  const idx = score(password);
  if (idx < 0) return null;
  const level = LEVELS[idx];

  return (
    <View className="flex-row items-center gap-2">
      <View className="flex-1 flex-row gap-1">
        {LEVELS.map((l, i) => (
          <View key={l.label} className={`h-1 flex-1 rounded-full ${i <= idx ? level.bar : 'bg-surface-muted'}`} />
        ))}
      </View>
      <Text className={`text-caption ${level.text}`}>{level.label}</Text>
    </View>
  );
}
