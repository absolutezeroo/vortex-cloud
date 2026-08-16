// Reading an audit row.
//
// The `data` column is a JSON envelope the operations service writes: `{ actor, reason, detail,
// changes }`. The feed used to render it through summarizeData, which keeps the first four keys and
// stringifies anything nested -- so every dashboard row read `actor=... - reason=... -
// detail=[object Object]`, and the payload that mattered was the one part guaranteed not to show.
//
// `changes` is the before/after of the rows the write actually touched, captured from EF's change
// tracker (see Vortex.Database/Auditing). It is absent on operations that call a grain or use a bulk
// statement -- absent meaning "not recorded", never "nothing happened".
import { translate } from './i18n.js';

/**
 * @returns {{actor: string, reason: string, detail: object|null, changes: Array, raw: string}}
 */
export function parseAuditData(value) {
  const empty = { actor: '', reason: '', detail: null, changes: [], raw: '' };

  if (!value) return empty;

  const raw = String(value).trim();

  if (!raw) return empty;

  try {
    const parsed = JSON.parse(raw);

    if (!parsed || typeof parsed !== 'object') return { ...empty, raw };

    return {
      actor: typeof parsed.actor === 'string' ? parsed.actor : '',
      reason: typeof parsed.reason === 'string' ? parsed.reason : '',
      detail: parsed.detail && typeof parsed.detail === 'object' ? parsed.detail : null,
      changes: Array.isArray(parsed.changes) ? parsed.changes : [],
      raw,
    };
  } catch {
    // Not JSON: an older row, or something wrote plain text. Show it rather than hide it.
    return { ...empty, raw };
  }
}

/**
 * The one line the table cell shows. Prefers what the write did to the data over what the operator
 * said about it, because the first is the thing you scan a feed looking for.
 */
export function summarizeAudit(value) {
  const { reason, detail, changes, raw } = parseAuditData(value);

  if (changes.length > 0) {
    const first = changes[0];
    const more = changes.length > 1 ? ` (+${changes.length - 1})` : '';

    if (first.operation === 'delete') {
      return `${describeTarget(first)} ${translate('audit.wasDeleted')}${more}`;
    }

    const fields = fieldTransitions(first).slice(0, 2).join('; ');

    return fields
      ? `${describeTarget(first)} — ${fields}${more}`
      : `${describeTarget(first)}${more}`;
  }

  if (reason) return reason;

  if (detail) {
    const pairs = Object.entries(detail)
      .filter(([, v]) => v !== null && v !== undefined && v !== '')
      .map(([k, v]) => `${k}=${renderScalar(v)}`);

    if (pairs.length) return pairs.join(' · ');
  }

  return raw || '—';
}

/** `catalog_offers #12` */
export function describeTarget(change) {
  const table = change.table || change.entity || '?';

  return change.id ? `${table} #${change.id}` : table;
}

/** `Credits 2 → 4` for each field that moved. */
export function fieldTransitions(change) {
  const before = change.before || {};
  const after = change.after || {};

  return Object.keys(after).map(
    (key) => `${key} ${renderScalar(before[key])} → ${renderScalar(after[key])}`,
  );
}

/** Every column of a row that no longer exists. */
export function deletedFields(change) {
  return Object.entries(change.before || {}).map(([key, value]) => ({
    key,
    value: renderScalar(value),
  }));
}

function renderScalar(value) {
  if (value === null || value === undefined || value === '') {
    return translate('common.changeEmpty');
  }

  return String(value);
}
