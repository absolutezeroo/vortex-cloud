import { derived, writable, get } from 'svelte/store';
import en from './locales/en';
import fr from './locales/fr';

// Persisted UI language, independent of the authenticated account -- same pattern as theme.js
// (local browser preference, applies before login, survives across accounts on one machine).
// Falls back to the browser's own language on first visit rather than hardcoding a default, so a
// French-speaking operator's browser lands them in French without anyone having to pick it.
const STORAGE_KEY = 'turbo-dashboard-locale';

type Locale = 'en' | 'fr';

/**
 * What a translation takes: names to substitute into `{placeholders}`.
 *
 * Null is allowed because half of what gets interpolated here is a display name, and a display
 * name is null wherever its row has been deleted. interpolate() already String()s every value,
 * so the substitution was never the problem -- the type was just narrower than the code.
 */
type TranslationParams = Record<string, string | number | null | undefined>;

const DICTIONARIES: Record<Locale, unknown> = { en, fr };

export const LOCALES = [
  { value: 'en', label: 'EN' },
  { value: 'fr', label: 'FR' },
];

const VALID_VALUES: string[] = LOCALES.map((l) => l.value);

function detectDefaultLocale(): Locale {
  if (typeof localStorage !== 'undefined') {
    try {
      const stored = localStorage.getItem(STORAGE_KEY);
      if (stored && VALID_VALUES.includes(stored)) return stored as Locale;
    } catch {
      // Fall through to browser-language detection.
    }
  }

  if (typeof navigator !== 'undefined' && navigator.language?.toLowerCase().startsWith('fr')) {
    return 'fr';
  }

  return 'en';
}

function resolve(dict: unknown, key: string): unknown {
  return key
    .split('.')
    .reduce<unknown>(
      (node, part) =>
        node && typeof node === 'object' ? (node as Record<string, unknown>)[part] : undefined,
      dict,
    );
}

function interpolate(str: string, params?: TranslationParams): string {
  if (!params) return str;
  return str.replace(/\{(\w+)\}/g, (match, name) =>
    params[name] !== undefined ? String(params[name]) : match,
  );
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

export function setLocale(value: string) {
  if (!VALID_VALUES.includes(value)) return;
  locale.set(value as Locale);
}

// A page that builds a label in a helper takes the translator itself rather than the locale, so the
// call site stays reactive; this is the type of what it receives.
export type Translator = (key: string, params?: TranslationParams) => string;

// Reactive translator for templates: `{$t('audit.title')}` or `{$t('common.giveTo', { name })}`.
// Missing keys fall back to English, then to the raw key itself (visibly wrong instead of a blank
// UI, so a missed translation is easy to spot rather than silently disappearing).
export const t = derived(
  locale,
  ($locale) =>
    (key: string, params?: TranslationParams): string => {
      const dict = DICTIONARIES[$locale] || DICTIONARIES.en;
      const value = resolve(dict, key) ?? resolve(DICTIONARIES.en, key) ?? key;
      return interpolate(String(value), params);
    },
);

// Non-reactive one-shot translator for use outside components (e.g. inside plain .js helpers that
// build a string once rather than re-rendering on locale change).
export function translate(key: string, params?: TranslationParams): string {
  return get(t)(key, params);
}
