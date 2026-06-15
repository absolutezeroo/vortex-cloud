let token = new URLSearchParams(window.location.search).get('token') || '';

export function getToken() {
  return token;
}

export function setToken(value) {
  token = value || '';
}

export async function apiGet(path) {
  const response = await fetch(path, {
    headers: token ? { 'X-Admin-Token': token } : undefined,
  });

  if (!response.ok) {
    throw new Error(`HTTP ${response.status}`);
  }

  return response.json();
}

export async function apiPost(path, body) {
  const response = await fetch(path, {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
      ...(token ? { 'X-Admin-Token': token } : {}),
    },
    body: JSON.stringify(body ?? {}),
  });

  let data = null;
  try {
    data = await response.json();
  } catch {
    data = null;
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
