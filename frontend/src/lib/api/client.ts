import { clearRefreshToken, readRefreshToken, saveRefreshToken } from "@/lib/auth/refreshTokenStorage";
import { clearSession, getAccessToken, setSession } from "@/lib/auth/tokenStore";
import type { ApiErrorBody } from "@/types/common";

const API_BASE_URL = process.env.NEXT_PUBLIC_API_BASE_URL ?? "http://localhost:5058";

export class ApiError extends Error {
  constructor(
    public readonly status: number,
    public readonly code: string,
    message: string,
    public readonly details?: Record<string, string[]>,
  ) {
    super(message);
    this.name = "ApiError";
  }
}

interface ApiFetchOptions extends Omit<RequestInit, "body"> {
  body?: unknown;
  /** Auth endpoints (login/refresh/logout) must never trigger the 401 refresh-and-retry loop. */
  skipAuthRefresh?: boolean;
}

let refreshInFlight: Promise<boolean> | null = null;

/**
 * Calls POST /api/admin/auth/refresh directly (not via apiFetch, to avoid a circular
 * 401-retry loop) and, on success, updates the in-memory access token and persisted
 * refresh token. Deduplicated so concurrent 401s only trigger one refresh call.
 */
async function refreshSession(): Promise<boolean> {
  if (refreshInFlight) return refreshInFlight;

  refreshInFlight = (async () => {
    const refreshToken = readRefreshToken();
    if (!refreshToken) return false;

    try {
      const response = await fetch(`${API_BASE_URL}/api/admin/auth/refresh`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ refreshToken }),
      });

      if (!response.ok) {
        clearSession();
        clearRefreshToken();
        return false;
      }

      const data = await response.json();
      setSession(data.accessToken, data.admin);
      const rememberMe = window.localStorage.getItem("cirs_refresh_token") !== null;
      saveRefreshToken(data.refreshToken, rememberMe);
      return true;
    } catch {
      return false;
    }
  })();

  try {
    return await refreshInFlight;
  } finally {
    refreshInFlight = null;
  }
}

export async function apiFetch<T>(path: string, options: ApiFetchOptions = {}): Promise<T> {
  const { body, skipAuthRefresh, headers, ...rest } = options;

  const doFetch = async (): Promise<Response> => {
    const accessToken = getAccessToken();
    return fetch(`${API_BASE_URL}${path}`, {
      ...rest,
      headers: {
        "Content-Type": "application/json",
        ...(accessToken ? { Authorization: `Bearer ${accessToken}` } : {}),
        ...headers,
      },
      body: body === undefined ? undefined : JSON.stringify(body),
    });
  };

  let response = await doFetch();

  if (response.status === 401 && !skipAuthRefresh) {
    const refreshed = await refreshSession();
    if (refreshed) {
      response = await doFetch();
    }
  }

  if (response.status === 204) {
    return undefined as T;
  }

  const isJson = response.headers.get("content-type")?.includes("application/json");
  const payload = isJson ? await response.json() : await response.text();

  if (!response.ok) {
    const errorBody: ApiErrorBody | undefined = isJson ? (payload as { error?: ApiErrorBody }).error : undefined;
    throw new ApiError(
      response.status,
      errorBody?.code ?? "unknown_error",
      errorBody?.message ?? "An unexpected error occurred.",
      errorBody?.details,
    );
  }

  return payload as T;
}

export function apiGet<T>(path: string, options?: ApiFetchOptions) {
  return apiFetch<T>(path, { ...options, method: "GET" });
}

export function apiPost<T>(path: string, body?: unknown, options?: ApiFetchOptions) {
  return apiFetch<T>(path, { ...options, method: "POST", body });
}

export function apiPatch<T>(path: string, body?: unknown, options?: ApiFetchOptions) {
  return apiFetch<T>(path, { ...options, method: "PATCH", body });
}

export function buildQueryString(params: Record<string, unknown>): string {
  const query = new URLSearchParams();
  for (const [key, value] of Object.entries(params)) {
    if (value === undefined || value === null || value === "") continue;
    query.set(key, String(value));
  }
  const qs = query.toString();
  return qs ? `?${qs}` : "";
}
