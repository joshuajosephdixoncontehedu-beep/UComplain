/**
 * The refresh token is persisted client-side so a page reload can silently
 * re-authenticate. "Remember me" controls which storage: localStorage survives
 * browser restarts, sessionStorage clears when the tab closes. Documented as
 * development-grade in the README alongside tokenStore.ts.
 */

const STORAGE_KEY = "cirs_refresh_token";

export function saveRefreshToken(token: string, rememberMe: boolean): void {
  clearRefreshToken();
  const storage = rememberMe ? window.localStorage : window.sessionStorage;
  storage.setItem(STORAGE_KEY, token);
}

export function readRefreshToken(): string | null {
  if (typeof window === "undefined") return null;
  return window.localStorage.getItem(STORAGE_KEY) ?? window.sessionStorage.getItem(STORAGE_KEY);
}

export function clearRefreshToken(): void {
  if (typeof window === "undefined") return;
  window.localStorage.removeItem(STORAGE_KEY);
  window.sessionStorage.removeItem(STORAGE_KEY);
}
