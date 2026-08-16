import { Ionicons } from '@expo/vector-icons';
import { router, useLocalSearchParams } from 'expo-router';
import { useEffect, useState } from 'react';
import { Pressable, ScrollView, Text, View } from 'react-native';

import { AppButton } from '@/components/ui/button';
import { BackButton } from '@/components/ui/back-button';
import { InfoBanner } from '@/components/ui/info-banner';
import { OtpInput } from '@/components/ui/otp-input';
import { useReporterAuth } from '@/components/auth/reporter-auth-context';
import { useScreenTopOffset } from '@/hooks/use-screen-top-offset';
import { ApiError } from '@/lib/api/client';

const RESEND_SECONDS = 45; // matches Otp__ResendCooldownSeconds default (mobile-api-contract.md)

/**
 * Figma "05 Verify email" (node 7:76).
 * `invalid_otp` is always the same generic error per the contract
 * (missing/wrong/expired/used/attempt-limited are indistinguishable on
 * purpose) — we surface the server's message as-is rather than guessing why.
 */
export default function VerifyEmail() {
  const { email } = useLocalSearchParams<{ email?: string }>();
  const { verifyEmailOtp, resendEmailOtp } = useReporterAuth();
  const topOffset = useScreenTopOffset();
  const [code, setCode] = useState('');
  const [secondsLeft, setSecondsLeft] = useState(RESEND_SECONDS);
  const [submitting, setSubmitting] = useState(false);
  const [resending, setResending] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    const t = setInterval(() => setSecondsLeft((s) => Math.max(0, s - 1)), 1000);
    return () => clearInterval(t);
  }, []);

  const mm = Math.floor(secondsLeft / 60);
  const ss = String(secondsLeft % 60).padStart(2, '0');

  const onVerify = async () => {
    if (!email) return;
    setError(null);
    setSubmitting(true);
    try {
      await verifyEmailOtp({ email, otpCode: code });
      router.replace('/(auth)/consent');
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Something went wrong. Please try again.');
    } finally {
      setSubmitting(false);
    }
  };

  const onResend = async () => {
    if (!email) return;
    setResending(true);
    try {
      await resendEmailOtp(email);
      setSecondsLeft(RESEND_SECONDS);
    } catch {
      // resend-email-otp deliberately never reveals success/failure per-account; ignore.
    } finally {
      setResending(false);
    }
  };

  return (
    <ScrollView className="flex-1 bg-canvas" contentContainerClassName="px-5 pb-10">
      <View style={{ marginTop: topOffset }}>
        <BackButton />
      </View>

      <View className="mt-9 h-16 w-16 items-center justify-center rounded-full bg-brand-tint">
        <Ionicons name="mail-open-outline" size={30} color="#1D4ED8" />
      </View>

      <View className="mt-5 gap-2">
        <Text className="text-display text-ink">Check your email</Text>
        <Text className="text-body text-secondary">
          We sent a 6-digit verification code to {email ?? 'your email address'}
        </Text>
      </View>

      <View className="mt-8">
        <OtpInput value={code} onChange={setCode} />
      </View>

      {error ? <Text className="mt-4 text-center text-body-sm text-status-critical">{error}</Text> : null}

      <View className="mt-9 gap-4">
        <View className="flex-row justify-center gap-1.5">
          <Text className="text-body-sm text-secondary">Resend code in</Text>
          {secondsLeft > 0 ? (
            <Text className="text-body-sm font-semibold text-ink">
              {mm}:{ss}
            </Text>
          ) : (
            <Pressable onPress={onResend} disabled={resending} hitSlop={8}>
              <Text className="text-body-sm font-semibold text-brand">{resending ? 'Sending…' : 'Resend'}</Text>
            </Pressable>
          )}
        </View>
        <AppButton title={submitting ? 'Verifying…' : 'Verify email'} disabled={code.length < 6 || submitting} onPress={onVerify} />
      </View>

      <View className="flex-1" />

      <View className="mt-10">
        <InfoBanner
          icon="help-circle-outline"
          text="No code yet? Check your spam folder, or tap resend once the timer ends."
        />
      </View>
    </ScrollView>
  );
}
