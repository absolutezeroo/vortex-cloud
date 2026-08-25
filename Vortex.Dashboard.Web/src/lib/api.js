// All dashboard requests are same-origin and authenticated by the HttpOnly session cookie issued by
// POST /api/login. There is no token to carry; the browser attaches the cookie automatically.

import { connectionIssue } from './session.js';
import { translate } from './i18n.js';

const DEFAULT_TIMEOUT_MS = 8000;
const LOGIN_TIMEOUT_MS = 10000;
const LOGOUT_TIMEOUT_MS = 3000;

export class ApiError extends Error {
  constructor(message, options = {}) {
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

export function isConnectionError(error) {
  return (
    error?.connection === true ||
    error?.code === 'request_timeout' ||
    error?.code === 'network_unavailable' ||
    error?.code === 'invalid_api_response'
  );
}

export function isTimeoutError(error) {
  return error?.code === 'request_timeout';
}

export function isAuthError(error) {
  return (
    error?.status === 401 ||
    error?.code === 'unauthenticated' ||
    error?.code === 'unauthorized'
  );
}

export function describeApiError(error) {
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

async function request(path, options, requestOptions = {}) {
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
    let data = null;

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

    return data;
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

function normalizeRequestError(error, path) {
  if (error instanceof ApiError) {
    return error;
  }

  if (error?.name === 'AbortError') {
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

  return error;
}

export function apiGet(path, options = {}) {
  return request(path, { headers: { Accept: 'application/json' } }, options);
}

export function apiPost(path, body, options = {}) {
  return request(
    path,
    {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(body ?? {}),
    },
    options
  );
}

export function getIdentity(options = {}) {
  return apiGet('/api/me', options);
}

// `code` is the second factor, sent only on the retry: the server answers mfa_required to the
// first attempt when the account has one, and the same credentials go back up with the code.
export function login(email, password, code) {
  return apiPost('/api/login', { email, password, code }, { timeoutMs: LOGIN_TIMEOUT_MS });
}

export function logout() {
  return apiPost('/api/logout', {}, { timeoutMs: LOGOUT_TIMEOUT_MS });
}
