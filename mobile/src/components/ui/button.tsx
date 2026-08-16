import { Pressable, Text } from 'react-native';

type Props = {
  title: string;
  onPress?: () => void;
  variant?: 'primary' | 'secondary';
  disabled?: boolean;
};

/** Figma: "Button / <label>" — full-width, 52px tall, rounded-input. */
export function AppButton({ title, onPress, variant = 'primary', disabled }: Props) {
  const isPrimary = variant === 'primary';
  return (
    <Pressable
      onPress={onPress}
      disabled={disabled}
      className={`h-[52px] w-full items-center justify-center rounded-input ${
        isPrimary ? 'bg-brand active:bg-brand-deep' : 'border border-border bg-surface active:bg-surface-muted'
      } ${disabled ? 'opacity-50' : ''}`}>
      <Text className={`text-base font-semibold ${isPrimary ? 'text-surface' : 'text-ink'}`}>{title}</Text>
    </Pressable>
  );
}
