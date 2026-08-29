// Every admin page repeats the same four things: hold the pending write, post it with the reason the
// modal collected, remember that reason, and refresh. Written once here so a new page cannot
// accidentally ship a write that skips the audited reason -- and so the pages stay about their
// domain instead of about plumbing.
//
// Usage in a page:
//   const ops = createWriteOps(refresh);
//   ops.ask('/api/v1/operations/...', { id }, title, summary)   // opens the modal
//   <ConfirmReasonModal open={Boolean($ops.pending)} ... onconfirm={ops.confirm} oncancel={() => ops.cancel()} />
//   The modal takes callback props, not events: `on:confirm` compiles, renders, and silently does
//   nothing -- the confirm button becomes a button that closes nothing and posts nothing.
//
// Pages that host several independent forms (a create panel, a per-row edit, a nested child editor)
// pass a `key` so each one shows its own busy state, error and OpResult instead of sharing one
// banner at the top of the page:
//   ops.ask(endpoint, body, title, summary, { key: `edit:${id}`, valid, onSuccess })
//   {#if $ops.results[`edit:${id}`]}<OpResult result={$ops.results[`edit:${id}`]} />{/if}
// The keyed and unkeyed forms are the same machinery -- unkeyed writes simply use the '' key -- so a
// page can start simple and add keys to the forms that need them.
import { writable } from 'svelte/store';
import { apiPost } from './api.js';
import { isPermissionDeniedError } from './permissions.js';
import { rememberReason } from './reasonHistory.js';
import { translate } from './i18n.js';
import { describeOpError } from './opErrors.js';
import { autoReasonSuffices, buildAutoReason } from './changes.js';
import { requestStepUp } from './stepUp.js';

const EMPTY = { pending: null, busy: false, error: '', result: null, key: '' };

// The two 403s that are about the operator's second factor rather than their capabilities.
const STEP_UP_CODES = new Set(['mfa_step_up_required', 'mfa_enrolment_required']);

/**
 * Post a write, and if the server refuses it for want of a recent second factor, collect one and try
 * the same write again.
 *
 * Once, deliberately. A second refusal after a code the server just accepted is not a code problem,
 * and looping on it would sit an operator in front of a dialog that never closes.
 *
 * `mfa_enrolment_required` is not retried at all: no dialog helps somebody who has no factor to
 * prove, and it falls through to the message that tells them to enrol one.
 */
async function postWithStepUp(endpoint, payload) {
  try {
    return await apiPost(endpoint, payload);
  } catch (err) {
    if (err?.code !== 'mfa_step_up_required') {
      throw err;
    }

    if (!(await requestStepUp())) {
      throw err;
    }

    return apiPost(endpoint, payload);
  }
}

export function createWriteOps(onSuccess) {
  const state = writable({ ...EMPTY, results: {}, errors: {}, busyKeys: {} });
  let current = null;

  /**
   * Stage a write for confirmation. `options.valid` lets a page reject its own form before the modal
   * opens -- the operator gets the "fill every field" message where the form is instead of typing a
   * reason first and being refused afterwards.
   */
  function ask(endpoint, body, title, summary, options = {}) {
    const key = options.key ?? '';

    if (options.valid === false) {
      const message = options.invalidMessage || translate('common.fillFields');

      state.update((s) => ({
        ...s,
        error: message,
        key,
        errors: { ...s.errors, [key]: message },
      }));
      return false;
    }

    const changes = options.changes ?? [];

    current = {
      endpoint,
      body,
      key,
      summary,
      changes,
      reason: options.reason,
      onSuccess: options.onSuccess,
    };
    state.update((s) => ({
      ...s,
      // `danger` is forwarded to ConfirmReasonModal, which paints a destructive write differently.
      // `changes` is the before/after list the modal shows and the audited reason is built from --
      // see lib/changes.js. `noteOnly` tells the modal the generated reason already stands on its
      // own, so the free-text box is a note rather than a gate.
      pending: {
        title,
        summary,
        changes,
        reason: options.reason,
        noteOnly: Boolean(options.reason) || autoReasonSuffices(summary, changes),
        danger: options.danger === true,
      },
      key,
      error: '',
      errors: { ...s.errors, [key]: '' },
    }));
    return true;
  }

  function cancel() {
    current = null;
    state.update((s) => ({ ...s, pending: null, error: '' }));
  }

  /**
   * Commit the staged write.
   *
   * The argument is the operator's optional *note*, not the whole reason. What gets audited is built
   * from the action itself -- the page's summary sentence plus the fields that actually changed --
   * with the note appended when there is one. A page that still owns its reason as a form field
   * (`options.reason`) keeps that as the base instead, so both styles audit a real sentence and
   * neither can post without one.
   */
  async function confirm(note) {
    if (!current) return false;

    const { endpoint, body, key, summary, changes, onSuccess: onOpSuccess } = current;

    const reason = current.reason
      ? buildAutoReason(current.reason, [], note)
      : buildAutoReason(summary, changes, note);

    state.update((s) => ({
      ...s,
      busy: true,
      error: '',
      busyKeys: { ...s.busyKeys, [key]: true },
      errors: { ...s.errors, [key]: '' },
    }));

    try {
      const result = await postWithStepUp(endpoint, { ...body, reason });

      if (result.ok) {
        // Only what the operator actually typed. Remembering the generated half would fill the
        // suggestion list with sentences nobody wrote and nobody wants offered back.
        rememberReason(note);
        current = null;
        state.update((s) => ({
          ...s,
          pending: null,
          busy: false,
          result,
          busyKeys: { ...s.busyKeys, [key]: false },
          results: { ...s.results, [key]: result },
        }));
        // BOTH, never one or the other. These are different jobs: the per-operation callback is
        // page-local state (close the drawer, clear the form), the page-level one re-reads the data.
        // `??` made the second silently replace the first, so every form that wanted to close itself
        // stopped refreshing its own list -- 16 call sites across 8 pages, each looking like "the
        // dashboard needs a reload to show what I just added".
        // The result is handed to the per-operation callback: ArticlesPage asks for the new id so it
        // can open what was just created, and had been receiving undefined every time.
        await onOpSuccess?.(result);
        await onSuccess?.();
        return true;
      }

      // Kept open on a domain rejection (say achievement_has_progress) so the operator can read the
      // reason without losing what they typed.
      //
      // The server puts that reason in OperationResult.message. This read `result.error`, a field no
      // dashboard response has ever carried, so every refusal in the whole dashboard collapsed to
      // "Failed" -- the cause travelled all the way from the domain and was dropped on this line.
      const message = describeOpError(result.message);

      state.update((s) => ({
        ...s,
        busy: false,
        result,
        error: message,
        busyKeys: { ...s.busyKeys, [key]: false },
        results: { ...s.results, [key]: result },
        errors: { ...s.errors, [key]: message },
      }));
      return false;
    } catch (err) {
      // A 403 here means the session lost the capability between the page load and the write; say so
      // rather than surfacing a bare HTTP code the operator cannot act on. A step-up refusal is also
      // a 403 and is emphatically not that, so it is read first -- telling an operator they lack
      // rights they do hold sends them to an administrator for a code dialog.
      const message = STEP_UP_CODES.has(err?.code)
        ? translate(`opError.${err.code}`)
        : isPermissionDeniedError(err)
          ? translate('common.insufficientRightsAction')
          : err.code || err.message;

      state.update((s) => ({
        ...s,
        busy: false,
        error: message,
        busyKeys: { ...s.busyKeys, [key]: false },
        errors: { ...s.errors, [key]: message },
      }));
      return false;
    }
  }

  /**
   * Show an error under `key` without staging anything -- for the checks a page makes before it
   * would even open the modal, typically "you do not hold the capability this button writes with".
   */
  function fail(key, message) {
    state.update((s) => ({
      ...s,
      error: message,
      key,
      errors: { ...s.errors, [key]: message },
    }));
  }

  /** Clears one key's banner (or every banner when called with no key). */
  function clear(key) {
    state.update((s) =>
      key === undefined
        ? { ...s, ...EMPTY, results: {}, errors: {}, busyKeys: {} }
        : {
            ...s,
            results: { ...s.results, [key]: null },
            errors: { ...s.errors, [key]: '' },
          }
    );
  }

  return { subscribe: state.subscribe, ask, cancel, confirm, fail, clear };
}
