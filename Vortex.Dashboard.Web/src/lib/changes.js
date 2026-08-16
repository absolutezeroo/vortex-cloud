// Turning a write into a sentence the audit can keep, without the operator typing it.
//
// Every dashboard write is audited with a reason, and until now that reason was a mandatory free-text
// box next to every button. Typing "price change" before changing a price is friction that produces
// nothing the system did not already know -- while the thing an auditor actually wants, what the
// value *was*, was nowhere.
//
// So the reason is now built from the change itself: the page's own summary sentence, plus the
// fields that really differ, rendered as `label: before -> after`. The operator's free text becomes
// an optional note for the part only they know -- "asked for by X", "after incident Y".
//
// The diff is computed here, in the browser, from the row the page already has on screen. That makes
// it a description of what the operator saw and intended, not a proof of what the database held a
// millisecond before the write. Good enough to read an audit line and understand it; not a substitute
// for the server recording before/after itself, which is a separate and larger job.
import { translate } from './i18n.js';

/** Longer than this and an audit line stops being readable; the detail payload still has the body. */
const MAX_REASON = 400;

/**
 * The fields that actually changed between two objects.
 *
 * @param {object} before Values as loaded (the row on screen).
 * @param {object} after Values as edited (the form).
 * @param {Array<{key: string, label: string, format?: (v: any) => string}>} fields
 *        Which keys to compare and what to call them. Only listed keys are looked at, so a form can
 *        carry scratch state without it leaking into the audit.
 * @returns {Array<{label: string, from: string, to: string}>}
 */
export function diffFields(before, after, fields) {
  if (!before || !after || !Array.isArray(fields)) return [];

  return fields.reduce((acc, field) => {
    const from = before[field.key];
    const to = after[field.key];

    if (!hasChanged(from, to)) return acc;

    const format = field.format ?? formatValue;

    acc.push({ label: field.label, from: format(from), to: format(to) });
    return acc;
  }, []);
}

/**
 * Renders a value the way an operator reads it rather than the way JSON holds it: a boolean is
 * yes/no, an empty field says so out loud instead of collapsing into whitespace the reader cannot
 * see.
 */
export function formatValue(value) {
  if (value === null || value === undefined || value === '') {
    return translate('common.changeEmpty');
  }

  if (typeof value === 'boolean') {
    return translate(value ? 'common.yes' : 'common.no');
  }

  if (Array.isArray(value)) {
    return value.length ? value.join(', ') : translate('common.changeEmpty');
  }

  return String(value);
}

/** `Price: 2 -> 4; Visible: yes -> no` */
export function formatChanges(changes) {
  if (!Array.isArray(changes) || changes.length === 0) return '';

  return changes.map((c) => `${c.label}: ${c.from} → ${c.to}`).join('; ');
}

/**
 * The reason actually posted and audited.
 *
 * @param {string} summary The page's own sentence for the action ("Delete offer #12 sofa_x").
 * @param {Array} changes  From {@link diffFields}; empty for a create or a delete.
 * @param {string} note    The operator's optional free text.
 */
export function buildAutoReason(summary, changes, note) {
  const parts = [];
  const base = (summary || '').trim();
  const diff = formatChanges(changes);
  const extra = (note || '').trim();

  if (base) parts.push(base);
  if (diff) parts.push(diff);
  if (extra) parts.push(extra);

  const reason = parts.join(' — ');

  return reason.length > MAX_REASON ? `${reason.slice(0, MAX_REASON - 1)}…` : reason;
}

/**
 * Whether the generated reason alone satisfies the server's three-character floor. When a page stages
 * a write with neither a summary nor a diff there is nothing to audit but the endpoint, so the note
 * has to carry it and the modal keeps asking for one.
 */
export function autoReasonSuffices(summary, changes) {
  return buildAutoReason(summary, changes, '').trim().length >= 3;
}

function hasChanged(from, to) {
  if (Array.isArray(from) || Array.isArray(to)) {
    return JSON.stringify(from ?? []) !== JSON.stringify(to ?? []);
  }

  // A form input hands back "4" where the loaded row held 4; that is not a change worth auditing.
  if (typeof from === 'number' || typeof to === 'number') {
    const a = Number(from);
    const b = Number(to);

    if (Number.isFinite(a) && Number.isFinite(b)) return a !== b;
  }

  return normalise(from) !== normalise(to);
}

function normalise(value) {
  if (value === null || value === undefined) return '';

  return typeof value === 'string' ? value.trim() : value;
}
