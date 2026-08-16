import { apiRequest } from '@/lib/api/client';

// Mirrors backend/src/CommunityIncidentReporting.Application/Features/MobileAuth/Dtos/*.cs
// (camelCase on the wire — ASP.NET Core's default JSON naming policy).

export type ReporterProfile = {
  id: string;
  fullName: string;
  email: string;
  phoneNumber: string | null;
  emailVerified: boolean;
  isActive: boolean;
  isRestricted: boolean;
  lastLoginAt: string | null;
  createdAt: string;
};

export type AuthTokenResponse = {
  accessToken: string;
  accessTokenExpiresAt: string;
  refreshToken: string;
  refreshTokenExpiresAt: string;
  reporter: ReporterProfile;
};

export type ConsentType = 'Location' | 'Camera' | 'Notifications' | 'DataProcessing';
export type ConsentDto = { id: string; consentType: ConsentType; granted: boolean; policyVersion: string; grantedAt: string };

const POLICY_VERSION = '2026-08-01'; // TODO(next phase): source from a real policy-version config, not a literal.

export const authApi = {
  register: (input: { fullName: string; email: string; phoneNumber: string; password: string; confirmPassword: string; consentAccepted: boolean }) =>
    apiRequest<{ reporterId: string; email: string; verificationRequired: boolean; message: string }>('api/mobile/auth/register', {
      method: 'POST',
      body: input,
    }),

  verifyEmailOtp: (input: { email: string; otpCode: string }) =>
    apiRequest<AuthTokenResponse>('api/mobile/auth/verify-email-otp', { method: 'POST', body: input }),

  resendEmailOtp: (input: { email: string }) =>
    apiRequest<{ message: string }>('api/mobile/auth/resend-email-otp', { method: 'POST', body: input }),

  login: (input: { email: string; password: string; rememberMe?: boolean }) =>
    apiRequest<AuthTokenResponse>('api/mobile/auth/login', { method: 'POST', body: input }),

  refresh: (refreshToken: string) =>
    apiRequest<AuthTokenResponse>('api/mobile/auth/refresh', { method: 'POST', body: { refreshToken } }),

  logout: (refreshToken: string, accessToken: string) =>
    apiRequest<void>('api/mobile/auth/logout', { method: 'POST', body: { refreshToken }, accessToken }),

  me: (accessToken: string) => apiRequest<ReporterProfile>('api/mobile/auth/me', { accessToken }),

  recordConsent: (
    accessToken: string,
    grants: { consentType: ConsentType; granted: boolean }[],
  ) =>
    apiRequest<ConsentDto[]>('api/mobile/auth/consent', {
      method: 'POST',
      accessToken,
      body: { consents: grants.map((g) => ({ ...g, policyVersion: POLICY_VERSION })) },
    }),
};

// TODO(next phase): forgot-password / verify-password-reset-otp / reset-password —
// no "Forgot password" screen exists in the Figma design yet, so these aren't wired.
