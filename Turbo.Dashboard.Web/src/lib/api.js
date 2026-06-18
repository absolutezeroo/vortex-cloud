// All dashboard requests are same-origin and authenticated by the HttpOnly session cookie issued by
// POST /api/login. There is no token to carry; the browser attaches the cookie automatically.

async function request(path, options) {
  const response = await fetch(path, { credentials: 'same-origin', ...options });

  const wantsJson = response.status !== 204 && response.status !== 205;
  let data = null;

  if (wantsJson) {
    const contentType = response.headers.get('content-type') || '';
    const isJson = contentType.includes('application/json');

    if (!isJson) {
      const raw = await response.text();
      const sample = raw.replace(/\s+/g, ' ').trim().slice(0, 80);

      const bodyError = sample.length > 0 ? `non_json:${sample}` : 'non_json';
      const error = new Error(bodyError);
      error.code = bodyError;
      error.status = response.status;
      throw error;
    }

    try {
      data = await response.json();
    } catch {
      data = null;
    }
  }

  if (!response.ok) {
    const code = data && data.error ? data.error : `HTTP ${response.status}`;
    const error = new Error(code);
    error.code = code;
    error.status = response.status;
    throw error;
  }

  return data;
}

export function apiGet(path) {
  return request(path, { headers: { Accept: 'application/json' } });
}

export function apiPost(path, body) {
  return request(path, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(body ?? {}),
  });
}

export function getIdentity() {
  return apiGet('/api/me');
}

export function login(email, password) {
  return apiPost('/api/login', { email, password });
}

export function logout() {
  return apiPost('/api/logout', {});
}
