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
