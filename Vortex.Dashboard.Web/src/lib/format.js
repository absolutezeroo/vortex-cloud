import { get } from 'svelte/store';
import { locale } from './i18n.js';

// Dates and numbers follow the language the operator picked in the UI, not the browser and not a
// hardcoded en-US -- a French operator reading "1,234.5" where the rest of the row says "1 234,5"
// misreads the magnitude. BCP 47 tags, because Intl wants a region to pick separators.
const INTL_LOCALES = { en: 'en-US', fr: 'fr-FR' };

function intlLocale() {
  return INTL_LOCALES[get(locale)] || INTL_LOCALES.en;
}

export function formatDate(value, fallback = '-') {
  if (!value) {
    return fallback;
  }

  const parsed = Date.parse(value);
  if (!Number.isFinite(parsed)) {
    return value;
  }

  return new Date(parsed).toLocaleString(intlLocale());
}

export function formatNumber(value, decimals = 0) {
  const numeric = Number(value || 0);
  return new Intl.NumberFormat(intlLocale(), {
    maximumFractionDigits: decimals,
    minimumFractionDigits: decimals,
  }).format(numeric);
}

export function formatDuration(seconds) {
  const total = Math.max(0, Number(seconds || 0));
  const days = Math.floor(total / 86400);
  const hours = Math.floor((total % 86400) / 3600);
  const minutes = Math.floor((total % 3600) / 60);

  if (days > 0) {
    return `${days}d ${hours}h`;
  }

  if (hours > 0) {
    return `${hours}h ${minutes}m`;
  }

  return `${minutes}m`;
}

export function compactCorrelation(value) {
  return value ? String(value).substring(0, 8) : '-';
}

export function summarizeData(value) {
  if (!value) {
    return '-';
  }

  const text = String(value).trim();
  if (!text) {
    return '-';
  }

  try {
    const parsed = JSON.parse(text);
    if (!parsed || typeof parsed !== 'object') {
      return text;
    }

    return Object.entries(parsed)
      .filter(([, entryValue]) => entryValue !== null && entryValue !== undefined && entryValue !== '')
      .slice(0, 4)
      .map(([key, entryValue]) => `${key}=${entryValue}`)
      .join(' - ');
  } catch {
    return text.length > 160 ? `${text.substring(0, 160)}…` : text;
  }
}

/** A file size an operator reads at a glance -- a database dump is megabytes, not bytes. */
export function formatBytes(value) {
  const bytes = Number(value);

  if (!Number.isFinite(bytes) || bytes < 0) return '-';
  if (bytes < 1024) return `${bytes}\u00a0B`;

  const units = ['KB', 'MB', 'GB', 'TB'];
  let size = bytes / 1024;
  let unit = 0;

  while (size >= 1024 && unit < units.length - 1) {
    size /= 1024;
    unit += 1;
  }

  // Non-breaking space: "12.4 MB" must never wrap between the number and its unit.
  return `${size.toFixed(size >= 100 ? 0 : 1)}\u00a0${units[unit]}`;
}
