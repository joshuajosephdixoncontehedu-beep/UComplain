"use client";

import { createContext, useContext, useEffect, useState, useSyncExternalStore } from "react";
import { useRouter } from "next/navigation";
import { login as loginRequest, logout as logoutRequest, refresh as refreshRequest } from "@/lib/api/auth";
import { ApiError } from "@/lib/api/client";
import { clearRefreshToken, readRefreshToken, saveRefreshToken } from "./refreshTokenStorage";
import { clearSession, getAccessToken, getAdmin, setSession, subscribe } from "./tokenStore";
import type { AdminProfile } from "@/types/auth";

interface AuthContextValue {
  admin: AdminProfile | null;
  accessToken: string | null;
  /** True only while the initial silent-refresh-on-load check is in progress. */
  isInitializing: boolean;
  login: (email: string, password: string, rememberMe: boolean) => Promise<void>;
  logout: () => Promise<void>;
}

const AuthContext = createContext<AuthContextValue | null>(null);

// Module-level (not component-level) so React StrictMode's intentional double-invoke
// of effects in development can't fire two concurrent restore attempts — the refresh
// token is single-use, so a second concurrent call with the same token always fails
// right after the first one succeeds and rotates it.
let restoreSessionInFlight: Promise<void> | null = null;

function restoreSessionOnce(): Promise<void> {
  restoreSessionInFlight ??= (async () => {
    const storedRefreshToken = readRefreshToken();
    if (!storedRefreshToken) return;

    try {
      const tokens = await refreshRequest(storedRefreshToken);
      setSession(tokens.accessToken, tokens.admin);
      const rememberMe = window.localStorage.getItem("cirs_refresh_token") !== null;
      saveRefreshToken(tokens.refreshToken, rememberMe);
    } catch {
      clearRefreshToken();
    }
  })().finally(() => {
    restoreSessionInFlight = null;
  });

  return restoreSessionInFlight;
}

export function AuthProvider({ children }: { children: React.ReactNode }) {
  const router = useRouter();
  const [isInitializing, setIsInitializing] = useState(true);

  const accessToken = useSyncExternalStore(subscribe, getAccessToken, () => null);
  const admin = useSyncExternalStore(subscribe, getAdmin, () => null);

  useEffect(() => {
    let cancelled = false;

    restoreSessionOnce().finally(() => {
      if (!cancelled) setIsInitializing(false);
    });

    return () => {
      cancelled = true;
    };
  }, []);

  const login = async (email: string, password: string, rememberMe: boolean) => {
    const tokens = await loginRequest(email, password);
    setSession(tokens.accessToken, tokens.admin);
    saveRefreshToken(tokens.refreshToken, rememberMe);
  };

  const logout = async () => {
    const storedRefreshToken = readRefreshToken();
    if (storedRefreshToken) {
      try {
        await logoutRequest(storedRefreshToken);
      } catch (error) {
        // A failed revoke on the server shouldn't block the client from clearing its
        // own session — the token will simply expire naturally.
        if (!(error instanceof ApiError)) throw error;
      }
    }
    clearSession();
    clearRefreshToken();
    router.push("/login");
  };

  return (
    <AuthContext.Provider value={{ admin, accessToken, isInitializing, login, logout }}>
      {children}
    </AuthContext.Provider>
  );
}

export function useAuth(): AuthContextValue {
  const context = useContext(AuthContext);
  if (!context) {
    throw new Error("useAuth must be used within an AuthProvider");
  }
  return context;
}
