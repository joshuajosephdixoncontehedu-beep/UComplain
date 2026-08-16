import { router } from 'expo-router';
import { useState } from 'react';
import { KeyboardAvoidingView, Platform, Pressable, ScrollView, Text, View } from 'react-native';

import { AppButton } from '@/components/ui/button';
import { BackButton } from '@/components/ui/back-button';
import { Checkbox } from '@/components/ui/checkbox';
import { PasswordStrength } from '@/components/ui/password-strength';
import { TextField } from '@/components/ui/text-field';
import { useReporterAuth } from '@/components/auth/reporter-auth-context';
import { useScreenTopOffset } from '@/hooks/use-screen-top-offset';
import { ApiError } from '@/lib/api/client';

/**
 * Figma "04 Create account" (node 7:2), extended with Phone number + Confirm
 * password — RegisterReporterRequest requires both (RegisterReporterRequestValidator.cs)
 * even though the design doesn't show them as fields.
 */
export default function CreateAccount() {
  const { register } = useReporterAuth();
  const topOffset = useScreenTopOffset();
  const [fullName, setFullName] = useState('');
  const [email, setEmail] = useState('');
  const [phoneNumber, setPhoneNumber] = useState('');
  const [password, setPassword] = useState('');
  const [confirmPassword, setConfirmPassword] = useState('');
  const [agreed, setAgreed] = useState(false);
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const canSubmit = agreed && fullName.trim() && email.trim() && phoneNumber.trim() && password && confirmPassword;

  const onSubmit = async () => {
    setError(null);
    setSubmitting(true);
    try {
      await register({ fullName, email, phoneNumber, password, confirmPassword, consentAccepted: agreed });
      router.push({ pathname: '/(auth)/verify-email', params: { email } });
    } catch (err) {
      // validation_error details are field-keyed; the top-level message is generic,
      // so surface the first field-level message when present for something actionable.
      const firstDetail = err instanceof ApiError && err.details ? Object.values(err.details)[0]?.[0] : undefined;
      setError(firstDetail ?? (err instanceof ApiError ? err.message : 'Something went wrong. Please try again.'));
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
          <Text className="text-display text-ink">Create your account</Text>
          <Text className="text-body text-secondary">
            You need an account so we can verify reports and send you updates.
          </Text>
        </View>

        <View className="mt-10 gap-5">
          <TextField
            label="Full name"
            icon="person-outline"
            value={fullName}
            onChangeText={setFullName}
            placeholder="Amina Kargbo"
            autoCapitalize="words"
            textContentType="name"
          />
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
            label="Phone number"
            icon="call-outline"
            value={phoneNumber}
            onChangeText={setPhoneNumber}
            placeholder="+23276111999"
            keyboardType="phone-pad"
            textContentType="telephoneNumber"
          />
          <View className="gap-2">
            <TextField
              label="Password"
              icon="lock-closed-outline"
              isPassword
              value={password}
              onChangeText={setPassword}
              placeholder="Create a password"
              textContentType="newPassword"
            />
            <PasswordStrength password={password} />
          </View>
          <TextField
            label="Confirm password"
            icon="lock-closed-outline"
            isPassword
            value={confirmPassword}
            onChangeText={setConfirmPassword}
            placeholder="Re-enter your password"
            textContentType="newPassword"
          />

          <Pressable className="flex-row items-start gap-2.5" onPress={() => setAgreed((v) => !v)}>
            <View className="mt-0.5">
              <Checkbox checked={agreed} onChange={setAgreed} />
            </View>
            <Text className="flex-1 text-body-sm text-secondary">
              I agree to the <Text className="font-semibold text-brand">Terms of Use</Text> and confirm that I will
              submit truthful reports.
            </Text>
          </Pressable>

          {error ? <Text className="text-body-sm text-status-critical">{error}</Text> : null}

          <AppButton title={submitting ? 'Creating account…' : 'Create account'} disabled={!canSubmit || submitting} onPress={onSubmit} />
        </View>

        <View className="flex-1" />

        <View className="mt-10 flex-row justify-center gap-1.5">
          <Text className="text-body-sm text-secondary">Already registered?</Text>
          <Pressable onPress={() => router.push('/(auth)/sign-in')} hitSlop={8}>
            <Text className="text-body-sm font-semibold text-brand">Sign in</Text>
          </Pressable>
        </View>
      </ScrollView>
    </KeyboardAvoidingView>
  );
}
