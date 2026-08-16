import { useRef, useState } from 'react';
import { Pressable, Text, TextInput, View } from 'react-native';

const LENGTH = 6;

/**
 * Figma "05 Verify email" (node 7:76): six 50×60 boxes. A single hidden
 * TextInput drives focus/keyboard; the boxes are a visual projection of its
 * value, with a blinking caret in the active empty box.
 */
export function OtpInput({ value, onChange }: { value: string; onChange: (v: string) => void }) {
  const inputRef = useRef<TextInput>(null);
  const [focused, setFocused] = useState(false);
  const digits = value.padEnd(LENGTH, ' ').slice(0, LENGTH).split('');
  const activeIndex = Math.min(value.length, LENGTH - 1);

  return (
    <Pressable onPress={() => inputRef.current?.focus()} className="flex-row justify-between">
      <TextInput
        ref={inputRef}
        value={value}
        onChangeText={(text) => onChange(text.replace(/[^0-9]/g, '').slice(0, LENGTH))}
        onFocus={() => setFocused(true)}
        onBlur={() => setFocused(false)}
        keyboardType="number-pad"
        maxLength={LENGTH}
        className="absolute h-px w-px opacity-0"
        autoFocus
      />
      {digits.map((digit, i) => {
        const isActive = focused && i === activeIndex;
        return (
          <View
            key={i}
            className={`h-[60px] w-[50px] items-center justify-center rounded-input border bg-surface ${
              isActive ? 'border-brand' : 'border-border'
            }`}>
            {digit.trim() ? (
              <Text className="text-[28px] font-semibold text-ink">{digit}</Text>
            ) : isActive ? (
              <View className="h-[26px] w-[2px] bg-brand" />
            ) : null}
          </View>
        );
      })}
    </Pressable>
  );
}
