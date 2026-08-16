import { createContext, ReactNode, useCallback, useContext, useEffect, useMemo, useState } from 'react';

import { ApiError, apiRequest } from '@/lib/api/client';
import { authApi, AuthTokenResponse, ConsentType, ReporterProfile } from '@/lib/api/auth';
import { StoredSession, tokenStorage } from '@/lib/api/token-storage';

type AuthStatus = 'loading' | 'authenticated' | 'unauthenticated';

export type AuthorizedRequestOptions = {
  method?: 'GET' | 'POST' | 'PATCH' | 'PUT' | 'DELETE';
  body?: unknown;
  form?: FormData;
  query?: Record<string, string | number | boolean | undefined>;
};

type ReporterAuthContextValue = {
  status: AuthStatus;
  reporter: ReporterProfile | null;
  register: typeof authApi.register;
  verifyEmailOtp: (input: { email: string; otpCode: string }) => Promise<void>;
  resendEmailOtp: (email: string) => Promise<void>;
  login: (input: { email: string; password: string; rememberMe?: boolean }) => Promise<void>;
  logout: () => Promise<void>;
  recordConsent: (grants: { consentType: ConsentType; granted: boolean }[]) => Promise<void>;
  /** Update the locally-cached reporter profile after a successful PATCH /api/mobile/me. */
  setReporter: (reporter: ReporterProfile) => Promise<void>;
  /** Authenticated request helper: attaches the access token and retries once after a refresh on 401. */
  authorizedRequest: <T>(path: string, options?: AuthorizedRequestOptions) => Promise<T>;
};

const ReporterAuthContext = createContext<ReporterAuthContextValue | null>(null);

async function persist(auth: AuthTokenResponse) {
  const session: StoredSession = {
    accessToken: auth.accessToken,
    accessTokenExpiresAt: auth.accessTokenExpiresAt,
    refreshToken: auth.refreshToken,
    refreshTokenExpiresAt: auth.refreshTokenExpiresAt,
    reporter: auth.reporter,
  };
  await tokenStorage.write(session);
  return session;
}

export function ReporterAuthProvider({ children }: { children: ReactNode }) {
  const [status, setStatus] = useState<AuthStatus>('loading');
  const [session, setSession] = useState<StoredSession | null>(null);

  useEffect(() => {
    tokenStorage.read().then((stored) => {
      setSession(stored);
      setStatus(stored ? 'authenticated' : 'unauthenticated');
    });
  }, []);

  const register = useCallback((input: Parameters<typeof authApi.register>[0]) => authApi.register(input), []);

  const verifyEmailOtp = useCallback(async (input: { email: string; otpCode: string }) => {
    const auth = await authApi.verifyEmailOtp(input);
    setSession(await persist(auth));
    setStatus('authenticated');
  }, []);

  const resendEmailOtp = useCallback((email: string) => authApi.resendEmailOtp({ email }).then(() => undefined), []);

  const login = useCallback(async (input: { email: string; password: string; rememberMe?: boolean }) => {
    const auth = await authApi.login(input);
    setSession(await persist(auth));
    setStatus('authenticated');
  }, []);

  const logout = useCallback(async () => {
    if (session) {
      // Best-effort — a failed logout call shouldn't block clearing the local session.
      await authApi.logout(session.refreshToken, session.accessToken).catch(() => undefined);
    }
    await tokenStorage.clear();
    setSession(null);
    setStatus('unauthenticated');
  }, [session]);

  const recordConsent = useCallback(
    async (grants: { consentType: ConsentType; granted: boolean }[]) => {
      if (!session) throw new Error('recordConsent called with no active session');
      await authApi.recordConsent(session.accessToken, grants);
    },
    [session],
  );

  const setReporter = useCallback(
    async (reporter: ReporterProfile) => {
      if (!session) throw new Error('setReporter called with no active session');
      const next = { ...session, reporter };
      await tokenStorage.write(next);
      setSession(next);
    },
    [session],
  );

  const authorizedRequest = useCallback(
    async <T,>(path: string, options: AuthorizedRequestOptions = {}): Promise<T> => {
      if (!session) throw new Error('authorizedRequest called with no active session');

      try {
        return await apiRequest<T>(path, { ...options, accessToken: session.accessToken });
      } catch (err) {
        if (!(err instanceof ApiError) || err.status !== 401) throw err;

        // Access token expired — refresh once and retry.
        try {
          const refreshed = await authApi.refresh(session.refreshToken);
          const nextSession = await persist(refreshed);
          setSession(nextSession);
          return await apiRequest<T>(path, { ...options, accessToken: nextSession.accessToken });
        } catch {
          await tokenStorage.clear();
          setSession(null);
          setStatus('unauthenticated');
          throw err;
        }
      }
    },
    [session],
  );

  const value = useMemo<ReporterAuthContextValue>(
    () => ({
      status,
      reporter: session?.reporter ?? null,
      register,
      verifyEmailOtp,
      resendEmailOtp,
      login,
      logout,
      recordConsent,
      setReporter,
      authorizedRequest,
    }),
    [status, session, register, verifyEmailOtp, resendEmailOtp, login, logout, recordConsent, setReporter, authorizedRequest],
  );

  return <ReporterAuthContext.Provider value={value}>{children}</ReporterAuthContext.Provider>;
}

export function useReporterAuth() {
  const ctx = useContext(ReporterAuthContext);
  if (!ctx) throw new Error('useReporterAuth must be used within a ReporterAuthProvider');
  return ctx;
}
