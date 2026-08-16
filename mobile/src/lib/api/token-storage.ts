import * as SecureStore from 'expo-secure-store';

import { ReporterProfile } from '@/lib/api/auth';

export type StoredSession = {
  accessToken: string;
  accessTokenExpiresAt: string;
  refreshToken: string;
  refreshTokenExpiresAt: string;
  reporter: ReporterProfile;
};

const KEY = 'reporter_session';

/**
 * Reporter access/refresh tokens, persisted in the OS keychain/keystore via
 * expo-secure-store — never AsyncStorage, since these are bearer credentials.
 * Session category (remembered vs. short-lived) is decided server-side at
 * issuance and carried through refresh (mobile-api-contract.md); the client
 * just stores whatever it's given.
 */
export const tokenStorage = {
  async read(): Promise<StoredSession | null> {
    const raw = await SecureStore.getItemAsync(KEY);
    if (!raw) return null;
    try {
      return JSON.parse(raw) as StoredSession;
    } catch {
      return null;
    }
  },

  async write(session: StoredSession): Promise<void> {
    await SecureStore.setItemAsync(KEY, JSON.stringify(session));
  },

  async clear(): Promise<void> {
    await SecureStore.deleteItemAsync(KEY);
  },
};
