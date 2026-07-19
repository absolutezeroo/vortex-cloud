import { derived, writable, get } from 'svelte/store';
import en from './locales/en.js';
import fr from './locales/fr.js';

// Persisted UI language, independent of the authenticated account -- same pattern as theme.js
// (local browser preference, applies before login, survives across accounts on one machine).
// Falls back to the browser's own language on first visit rather than hardcoding a default, so a
// French-speaking operator's browser lands them in French without anyone having to pick it.
const STORAGE_KEY = 'turbo-dashboard-locale';

const DICTIONARIES = { en, fr };

export const LOCALES = [
  { value: 'en', label: 'EN' },
  { value: 'fr', label: 'FR' },
];

const VALID_VALUES = LOCALES.map((l) => l.value);

function detectDefaultLocale() {
  if (typeof localStorage !== 'undefined') {
    try {
      const stored = localStorage.getItem(STORAGE_KEY);
      if (VALID_VALUES.includes(stored)) return stored;
    } catch {
      // Fall through to browser-language detection.
    }
  }

  if (typeof navigator !== 'undefined' && navigator.language?.toLowerCase().startsWith('fr')) {
    return 'fr';
  }

  return 'en';
}

function resolve(dict, key) {
  return key
    .split('.')
    .reduce((node, part) => (node && typeof node === 'object' ? node[part] : undefined), dict);
}

function interpolate(str, params) {
  if (!params) return str;
  return str.replace(/\{(\w+)\}/g, (match, name) => (params[name] !== undefined ? params[name] : match));
}

export const locale = writable(detectDefaultLocale());

locale.subscribe((value) => {
  if (typeof localStorage === 'undefined') return;

  try {
    localStorage.setItem(STORAGE_KEY, value);
  } catch {
    // Best-effort only; private browsing / quota errors shouldn't break the app.
  }
});

export function setLocale(value) {
  if (!VALID_VALUES.includes(value)) return;
  locale.set(value);
}

// Reactive translator for templates: `{$t('audit.title')}` or `{$t('common.giveTo', { name })}`.
// Missing keys fall back to English, then to the raw key itself (visibly wrong instead of a blank
// UI, so a missed translation is easy to spot rather than silently disappearing).
export const t = derived(locale, ($locale) => (key, params) => {
  const dict = DICTIONARIES[$locale] || DICTIONARIES.en;
  const value = resolve(dict, key) ?? resolve(DICTIONARIES.en, key) ?? key;
  return interpolate(value, params);
});

// Non-reactive one-shot translator for use outside components (e.g. inside plain .js helpers that
// build a string once rather than re-rendering on locale change).
export function translate(key, params) {
  return get(t)(key, params);
}
