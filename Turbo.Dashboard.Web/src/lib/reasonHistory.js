import { writable } from 'svelte/store';

// Shared "why this change?" suggestions for every mutating form's reason field. Operators kept
// having to retype the same reason for every row of a bulk edit (e.g. fixing sprite ids across many
// furniture definitions from a converter import) -- this backs a <datalist> so the browser offers
// past + common reasons instead of forcing free text every time, while still allowing any free text.
const STORAGE_KEY = 'turbo-dashboard-reason-history';
const MAX_ENTRIES = 25;

const PRESET_REASONS = [
  'Bulk import correction',
  'Fix typo',
  'Data cleanup',
  'Correct category/type',
  'Update pricing',
  'Add missing item',
  'Remove duplicate',
  'Sync with converter import',
  'Testing',
];

function readStoredHistory() {
  if (typeof localStorage === 'undefined') return [];

  try {
    const raw = localStorage.getItem(STORAGE_KEY);
    const parsed = raw ? JSON.parse(raw) : [];
    return Array.isArray(parsed) ? parsed.filter((r) => typeof r === 'string') : [];
  } catch {
    return [];
  }
}

function dedupe(entries) {
  return Array.from(new Set(entries.map((r) => r.trim()).filter(Boolean)));
}

export const reasonSuggestions = writable(dedupe([...readStoredHistory(), ...PRESET_REASONS]));

export function rememberReason(reason) {
  const trimmed = (reason || '').trim();
  if (!trimmed) return;

  const history = readStoredHistory().filter((r) => r !== trimmed);
  history.unshift(trimmed);
  const capped = history.slice(0, MAX_ENTRIES);

  if (typeof localStorage !== 'undefined') {
    try {
      localStorage.setItem(STORAGE_KEY, JSON.stringify(capped));
    } catch {
      // Best-effort only; private browsing / quota errors shouldn't break the form.
    }
  }

  reasonSuggestions.set(dedupe([...capped, ...PRESET_REASONS]));
}
