import type { AdminProfile } from "@/types/auth";

/**
 * Development-grade token storage: the access token lives only in this in-memory
 * module (never persisted — cleared on full page reload), and the refresh token is
 * persisted in localStorage/sessionStorage (see refreshTokenStorage.ts) so a silent
 * refresh can re-establish a session after a reload without asking for credentials
 * again. This is simpler than a full httpOnly-cookie BFF proxy in front of every one
 * of the ~40 backend endpoints, at the cost of the access token being readable by any
 * script running on the page (XSS) for the ~15 minutes it's valid. Production
 * hardening would move token issuance/storage behind httpOnly, Secure, SameSite
 * cookies set by a server-side proxy.
 *
 * Plain module state (not React state) so the API client can read/write the token
 * synchronously outside the React tree; AuthProvider subscribes for reactive updates.
 */

let accessToken: string | null = null;
let admin: AdminProfile | null = null;

type Listener = () => void;
const listeners = new Set<Listener>();

export function getAccessToken(): string | null {
  return accessToken;
}

export function getAdmin(): AdminProfile | null {
  return admin;
}

export function setSession(token: string | null, profile: AdminProfile | null): void {
  accessToken = token;
  admin = profile;
  listeners.forEach((listener) => listener());
}

export function clearSession(): void {
  setSession(null, null);
}

export function subscribe(listener: Listener): () => void {
  listeners.add(listener);
  return () => listeners.delete(listener);
}
