// All dashboard requests are same-origin and authenticated by the HttpOnly session cookie issued by
// POST /api/login. There is no token to carry; the browser attaches the cookie automatically.

import { connectionIssue } from './session';
import { translate } from './i18n';
import type { Identity } from './permissions';

const DEFAULT_TIMEOUT_MS = 8000;
const LOGIN_TIMEOUT_MS = 10000;
const LOGOUT_TIMEOUT_MS = 3000;

/** What a refusal carries beyond its message. */
export type ApiErrorOptions = {
  code?: string;
  status?: number;
  path?: string;
  connection?: boolean;
  correlationId?: string;
  cause?: unknown;
};

export class ApiError extends Error {
  readonly code: string;
  readonly status: number;
  readonly path: string;
  /** True when the emulator could not be reached at all, as opposed to refusing. */
  readonly connection: boolean;
  readonly correlationId: string;

  constructor(message: string, options: ApiErrorOptions = {}) {
    super(message);
    this.name = 'ApiError';
    this.code = options.code || message;
    this.status = options.status || 0;
    this.path = options.path || '';
    this.connection = options.connection === true;
    // The server puts one on every reply, header included, so a refusal with no body at all -- a 403
    // from authorization, a 429 from the limiter -- still gives the operator something to quote.
    this.correlationId = options.correlationId || '';

    if (options.cause) {
      this.cause = options.cause;
    }
  }
}

/** How much of an error the helpers below need to read; anything thrown may be shaped like this. */
type ErrorLike = { code?: string; status?: number; message?: string; connection?: boolean };

/** Per-call knobs, distinct from the fetch init the request builds itself. */
export type RequestOptions = { timeoutMs?: number };

export function isConnectionError(error: ErrorLike | null | undefined): boolean {
  return (
    error?.connection === true ||
    error?.code === 'request_timeout' ||
    error?.code === 'network_unavailable' ||
    error?.code === 'invalid_api_response'
  );
}

export function isTimeoutError(error: ErrorLike | null | undefined): boolean {
  return error?.code === 'request_timeout';
}

export function isAuthError(error: ErrorLike | null | undefined): boolean {
  return (
    error?.status === 401 ||
    error?.code === 'unauthenticated' ||
    error?.code === 'unauthorized'
  );
}

export function describeApiError(error: ErrorLike | null | undefined): string {
  if (error?.code === 'request_timeout') {
    return translate('errors.requestTimeout');
  }

  if (error?.code === 'network_unavailable') {
    return translate('errors.networkUnavailable');
  }

  if (error?.code === 'invalid_api_response') {
    return translate('errors.invalidApiResponse');
  }

  if (error?.status === 429) {
    return translate('errors.tooManyRequests');
  }

  if (error?.code === 'window_too_large') {
    return translate('errors.windowTooLarge');
  }

  if (error?.code === 'invalid_date') {
    return translate('errors.invalidDate');
  }

  if (error?.code === 'invalid_code') {
    return translate('errors.invalidCode');
  }

  if (error?.code === 'wrong_password') {
    return translate('errors.wrongPassword');
  }

  if (error?.code === 'password_too_short') {
    return translate('errors.passwordTooShort');
  }

  return error?.message || translate('errors.requestFailed');
}

async function request<T>(
  path: string,
  options: RequestInit,
  requestOptions: RequestOptions = {},
): Promise<T> {
  const timeoutMs = requestOptions.timeoutMs ?? DEFAULT_TIMEOUT_MS;
  const controller = new AbortController();
  const timeoutId = setTimeout(() => controller.abort(), timeoutMs);

  try {
    const response = await fetch(path, {
      credentials: 'same-origin',
      ...options,
      signal: controller.signal,
    });

    connectionIssue.set(null);

    const wantsJson = response.status !== 204 && response.status !== 205;
    let data: any = null;

    if (wantsJson) {
      const contentType = response.headers.get('content-type') || '';
      const isJson = contentType.includes('application/json');

      if (!isJson) {
        const raw = await response.text();
        const sample = raw.replace(/\s+/g, ' ').trim().slice(0, 80);
        const message = sample.length > 0 ? `invalid_api_response:${sample}` : 'invalid_api_response';

        throw new ApiError(message, {
          code: 'invalid_api_response',
          status: response.status,
          path,
          connection: path.startsWith('/api/'),
        });
      }

      try {
        data = await response.json();
      } catch (e) {
        throw new ApiError('invalid_json', {
          code: 'invalid_json',
          status: response.status,
          path,
          cause: e,
        });
      }
    }

    if (!response.ok) {
      const code = data && data.error ? data.error : `HTTP ${response.status}`;

      throw new ApiError(code, {
        code,
        status: response.status,
        path,
        correlationId: (data && data.correlationId) || response.headers.get('X-Correlation-Id') || '',
      });
    }

    return data as T;
  } catch (e) {
    const error = normalizeRequestError(e, path);

    if (isConnectionError(error)) {
      connectionIssue.set({
        code: error.code,
        message: describeApiError(error),
        path: error.path || path,
        occurredAt: new Date().toISOString(),
      });
    }

    throw error;
  } finally {
    clearTimeout(timeoutId);
  }
}

function normalizeRequestError(error: unknown, path: string): ApiError {
  if (error instanceof ApiError) {
    return error;
  }

  if ((error as { name?: string })?.name === 'AbortError') {
    return new ApiError('request_timeout', {
      code: 'request_timeout',
      path,
      connection: true,
      cause: error,
    });
  }

  if (typeof navigator !== 'undefined' && navigator.onLine === false) {
    return new ApiError('network_unavailable', {
      code: 'network_unavailable',
      path,
      connection: true,
      cause: error,
    });
  }

  if (error instanceof TypeError) {
    return new ApiError('network_unavailable', {
      code: 'network_unavailable',
      path,
      connection: true,
      cause: error,
    });
  }

  return new ApiError(
    error instanceof Error ? error.message : String(error),
    { path, cause: error },
  );
}

/**
 * A GET that answers JSON. The response type is the caller's to name, from `apiTypes.d.ts` --
 * generated from the C# contracts, so a renamed field is a type error here rather than an
 * `undefined` in the markup.
 *
 * Callers that pass no type get `unknown`, which is honest: an endpoint whose reads still return
 * `object` on the server has no shape to promise.
 */
export function apiGet<T = unknown>(path: string, options: RequestOptions = {}): Promise<T> {
  return request<T>(path, { headers: { Accept: 'application/json' } }, options);
}

export function apiPost<T = unknown>(
  path: string,
  body?: unknown,
  options: RequestOptions = {},
): Promise<T> {
  return request<T>(
    path,
    {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(body ?? {}),
    },
    options,
  );
}

export function getIdentity(options: RequestOptions = {}): Promise<Identity> {
  return apiGet<Identity>('/api/me', options);
}

// `code` is the second factor, sent only on the retry: the server answers mfa_required to the
// first attempt when the account has one, and the same credentials go back up with the code.
export function login(email: string, password: string, code?: string) {
  return apiPost('/api/login', { email, password, code }, { timeoutMs: LOGIN_TIMEOUT_MS });
}

export function logout() {
  return apiPost('/api/logout', {}, { timeoutMs: LOGOUT_TIMEOUT_MS });
}
