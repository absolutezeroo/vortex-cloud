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

    current = { endpoint, body, key, reason: options.reason, onSuccess: options.onSuccess };
    state.update((s) => ({
      ...s,
      // `danger` is forwarded to ConfirmReasonModal, which paints a destructive write differently.
      pending: { title, summary, reason: options.reason, danger: options.danger === true },
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
   * Commit the staged write. `reason` comes from ConfirmReasonModal on the pages that collect it
   * there; the authoring pages (polls, quests, targeted offers) instead carry the reason as a field
   * of the form being saved and pass it to `ask` as `options.reason`, so they call this with no
   * argument. Either way the write is audited with a reason -- there is no path that posts without.
   */
  async function confirm(reason) {
    if (!current) return false;

    const { endpoint, body, key, onSuccess: onOpSuccess } = current;

    reason = reason ?? current.reason;

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
        rememberReason(reason);
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
