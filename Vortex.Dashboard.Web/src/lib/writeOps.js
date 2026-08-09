// Every admin page repeats the same four things: hold the pending write, post it with the reason the
// modal collected, remember that reason, and refresh. Written once here so a new page cannot
// accidentally ship a write that skips the audited reason -- and so the pages stay about their
// domain instead of about plumbing.
//
// Usage in a page:
//   const ops = createWriteOps(refresh);
//   ops.ask('/api/v1/operations/...', { id }, title, summary)   // opens the modal
//   <ConfirmReasonModal open={Boolean($ops.pending)} ... on:confirm={(e) => ops.confirm(e.detail)} />
import { writable } from 'svelte/store';
import { apiPost } from './api.js';
import { rememberReason } from './reasonHistory.js';
import { translate } from './i18n.js';

export function createWriteOps(onSuccess) {
  const state = writable({ pending: null, busy: false, error: '', result: null });
  let current = null;

  function ask(endpoint, body, title, summary) {
    current = { endpoint, body };
    state.update((s) => ({ ...s, pending: { title, summary }, error: '' }));
  }

  function cancel() {
    current = null;
    state.update((s) => ({ ...s, pending: null, error: '' }));
  }

  async function confirm(reason) {
    if (!current) return false;

    state.update((s) => ({ ...s, busy: true, error: '' }));

    try {
      const result = await apiPost(current.endpoint, { ...current.body, reason });

      if (result.ok) {
        rememberReason(reason);
        current = null;
        state.update((s) => ({ ...s, pending: null, busy: false, result }));
        await onSuccess?.();
        return true;
      }

      // Kept open on a domain rejection (say achievement_has_progress) so the operator can read the
      // reason without losing what they typed.
      state.update((s) => ({
        ...s,
        busy: false,
        result,
        error: result.error || translate('common.resultFailed'),
      }));
      return false;
    } catch (err) {
      state.update((s) => ({ ...s, busy: false, error: err.message }));
      return false;
    }
  }

  return { subscribe: state.subscribe, ask, cancel, confirm };
}
