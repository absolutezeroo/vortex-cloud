import { writable } from 'svelte/store';

// Persisted UI theme, independent of the authenticated account -- it's a local browser
// preference (localStorage), not tied to any user/session, so it applies before login and
// survives across accounts on the same machine. "blue" is the original/default look; "dark" and
// "white" are the two later additions (see styles.css's :root[data-theme] blocks).
const STORAGE_KEY = 'turbo-dashboard-theme';

export const THEMES = [
  { value: 'blue', label: 'Blue' },
  { value: 'dark', label: 'Dark' },
  { value: 'white', label: 'White' },
];

const VALID_VALUES = THEMES.map((t) => t.value);
const DEFAULT_THEME = 'blue';

function readStoredTheme() {
  if (typeof localStorage === 'undefined') return DEFAULT_THEME;

  try {
    const stored = localStorage.getItem(STORAGE_KEY);
    return VALID_VALUES.includes(stored) ? stored : DEFAULT_THEME;
  } catch {
    return DEFAULT_THEME;
  }
}

function applyTheme(value) {
  if (typeof document === 'undefined') return;
  document.documentElement.setAttribute('data-theme', value);
}

export const theme = writable(readStoredTheme());

// Runs immediately on import (Svelte stores call subscribers eagerly) so the theme attribute is
// set before first paint as long as this module is imported early (see main.js) -- avoids a
// flash of the wrong theme on load.
theme.subscribe((value) => {
  applyTheme(value);

  if (typeof localStorage === 'undefined') return;

  try {
    localStorage.setItem(STORAGE_KEY, value);
  } catch {
    // Best-effort only; private browsing / quota errors shouldn't break the app.
  }
});

export function setTheme(value) {
  if (!VALID_VALUES.includes(value)) return;
  theme.set(value);
}
