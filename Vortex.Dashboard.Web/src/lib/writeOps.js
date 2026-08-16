// Every admin page repeats the same four things: hold the pending write, post it with the reason the
// modal collected, remember that reason, and refresh. Written once here so a new page cannot
// accidentally ship a write that skips the audited reason -- and so the pages stay about their
// domain instead of about plumbing.
//
// Usage in a page:
//   const ops = createWriteOps(refresh);
//   ops.ask('/api/v1/operations/...', { id }, title, summary)   // opens the modal
//   <ConfirmReasonModal open={Boolean($ops.pending)} ... on:confirm={(e) => ops.confirm(e.detail)} />
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

const EMPTY = { pending: null, busy: false, error: '', result: null, key: '' };

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
      const result = await apiPost(endpoint, { ...body, reason });

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
        await (onOpSuccess ?? onSuccess)?.();
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
      // rather than surfacing a bare HTTP code the operator cannot act on.
      const message = isPermissionDeniedError(err)
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
