import { router } from 'expo-router';
import { useState } from 'react';
import { KeyboardAvoidingView, Platform, Pressable, ScrollView, Text, View } from 'react-native';

import { AppButton } from '@/components/ui/button';
import { BackButton } from '@/components/ui/back-button';
import { Checkbox } from '@/components/ui/checkbox';
import { InfoBanner } from '@/components/ui/info-banner';
import { TextField } from '@/components/ui/text-field';
import { useReporterAuth } from '@/components/auth/reporter-auth-context';
import { useScreenTopOffset } from '@/hooks/use-screen-top-offset';
import { ApiError } from '@/lib/api/client';

/**
 * Figma "03 Sign in" (node 6:84).
 * `invalid_credentials` covers both "wrong password" and "no such account"
 * identically per the contract — surface the server's message as-is, don't
 * try to distinguish which one happened.
 */
export default function SignIn() {
  const { login } = useReporterAuth();
  const topOffset = useScreenTopOffset();
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [rememberMe, setRememberMe] = useState(true);
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const onSubmit = async () => {
    setError(null);
    setSubmitting(true);
    try {
      await login({ email, password, rememberMe });
      router.replace('/(app)/(tabs)/home');
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Something went wrong. Please try again.');
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <KeyboardAvoidingView className="flex-1" behavior={Platform.OS === 'ios' ? 'padding' : undefined}>
      <ScrollView className="flex-1 bg-canvas" contentContainerClassName="px-5 pb-10" keyboardShouldPersistTaps="handled">
        <View style={{ marginTop: topOffset }}>
          <BackButton />
        </View>

        <View className="mt-[52px] gap-2">
          <Text className="text-display text-ink">Welcome back</Text>
          <Text className="text-body text-secondary">Sign in to submit and track your reports.</Text>
        </View>

        <View className="mt-9 gap-5">
          <TextField
            label="Email address"
            icon="mail-outline"
            value={email}
            onChangeText={setEmail}
            placeholder="amina.kargbo@email.sl"
            keyboardType="email-address"
            autoCapitalize="none"
            textContentType="emailAddress"
          />
          <TextField
            label="Password"
            icon="lock-closed-outline"
            isPassword
            value={password}
            onChangeText={setPassword}
            placeholder="Enter your password"
            textContentType="password"
          />

          <View className="flex-row items-center justify-between">
            <Pressable className="flex-row items-center gap-2" onPress={() => setRememberMe((v) => !v)}>
              <Checkbox checked={rememberMe} onChange={setRememberMe} />
              <Text className="text-body-sm text-secondary">Remember me</Text>
            </Pressable>
            <Pressable hitSlop={8}>
              <Text className="text-body-sm font-semibold text-brand">Forgot password?</Text>
            </Pressable>
          </View>

          {error ? <Text className="text-body-sm text-status-critical">{error}</Text> : null}

          <View className="mt-1 gap-3">
            <AppButton title={submitting ? 'Signing in…' : 'Sign in'} disabled={!email || !password || submitting} onPress={onSubmit} />

            <View className="flex-row items-center gap-3">
              <View className="h-px flex-1 bg-border" />
              <Text className="text-body-sm text-muted">or</Text>
              <View className="h-px flex-1 bg-border" />
            </View>

            <AppButton title="Create an account" variant="secondary" onPress={() => router.push('/(auth)/create-account')} />
          </View>
        </View>

        <View className="flex-1" />

        <View className="mt-10">
          <InfoBanner
            icon="shield-checkmark-outline"
            text="Your identity is never shown to the public. Only verified officers can see your contact details."
          />
        </View>
      </ScrollView>
    </KeyboardAvoidingView>
  );
}
