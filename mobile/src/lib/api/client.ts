const BASE_URL = process.env.EXPO_PUBLIC_API_BASE_URL ?? 'http://localhost:5058';

/**
 * Mirrors the backend's `{ error: { code, message, details? } }` envelope
 * (GlobalExceptionHandler.cs / ValidationFilter.cs). `code` is one of
 * invalid_otp | invalid_credentials | not_found | business_rule_violation |
 * validation_error | forbidden | email_delivery_failed | storage_error |
 * internal_error, or "rate_limited" (429 — no body on that one).
 */
export class ApiError extends Error {
  code: string;
  status: number;
  details?: Record<string, string[]>;

  constructor(status: number, code: string, message: string, details?: Record<string, string[]>) {
    super(message);
    this.name = 'ApiError';
    this.status = status;
    this.code = code;
    this.details = details;
  }
}

type RequestOptions = {
  method?: 'GET' | 'POST' | 'PATCH' | 'PUT' | 'DELETE';
  body?: unknown;
  /** Send as multipart/form-data instead of JSON — used for file uploads. */
  form?: FormData;
  accessToken?: string;
  query?: Record<string, string | number | boolean | undefined>;
};

function buildUrl(path: string, query?: RequestOptions['query']): string {
  const url = new URL(path.replace(/^\//, ''), `${BASE_URL}/`);
  if (query) {
    for (const [key, value] of Object.entries(query)) {
      if (value !== undefined) url.searchParams.set(key, String(value));
    }
  }
  return url.toString();
}

/** Low-level, stateless request helper. Auth/refresh orchestration lives in ReporterAuthProvider. */
export async function apiRequest<T>(path: string, options: RequestOptions = {}): Promise<T> {
  const headers: Record<string, string> = { Accept: 'application/json' };
  if (options.accessToken) headers.Authorization = `Bearer ${options.accessToken}`;
  // Omit Content-Type for FormData — fetch sets the multipart boundary itself.
  if (!options.form && options.body !== undefined) headers['Content-Type'] = 'application/json';

  let response: Response;
  try {
    response = await fetch(buildUrl(path, options.query), {
      method: options.method ?? 'GET',
      headers,
      body: options.form ?? (options.body !== undefined ? JSON.stringify(options.body) : undefined),
    });
  } catch {
    throw new ApiError(0, 'network_error', 'Could not reach the server. Check your connection and try again.');
  }

  if (response.status === 204) return undefined as T;

  if (response.status === 429) {
    throw new ApiError(429, 'rate_limited', 'Too many attempts. Please wait a moment and try again.');
  }

  const text = await response.text();
  const json = text ? JSON.parse(text) : undefined;

  if (!response.ok) {
    const body = json as { error?: { code: string; message: string; details?: Record<string, string[]> } } | undefined;
    throw new ApiError(
      response.status,
      body?.error?.code ?? 'unknown_error',
      body?.error?.message ?? 'Something went wrong. Please try again.',
      body?.error?.details,
    );
  }

  return json as T;
}
