<script>
  // Reusable "confirm a destructive action + capture the mandatory reason" modal. A caller opens it
  // with a title/summary, the operator types the reason inside the modal (no more reason input sitting
  // beside every button), and `confirm` fires with the trimmed reason. The caller keeps the modal
  // open on failure by passing `error` (e.g. offer_has_purchases) and clearing `busy`.
  import { createEventDispatcher } from 'svelte';
  import { CircleX } from '@lucide/svelte';
  import { reasonOk } from '../lib/validation.js';
  import { t } from '../lib/i18n.js';

  export let open = false;
  export let title = '';
  export let summary = '';
  export let confirmLabel = '';
  export let busy = false;
  export let error = '';
  export let danger = true;

  const dispatch = createEventDispatcher();

  let reason = '';
  let prevOpen = false;
  // Reset the field only on the closed -> open transition, so typing doesn't wipe it and a reused
  // modal never carries the previous action's reason.
  $: {
    if (open && !prevOpen) {
      reason = '';
    }
    prevOpen = open;
  }

  $: valid = reasonOk(reason);

  function cancel() {
    dispatch('cancel');
  }

  function confirm() {
    if (!valid || busy) {
      return;
    }
    dispatch('confirm', reason.trim());
  }
</script>

{#if open}
  <div class="modal-layer">
    <button class="modal-backdrop" type="button" aria-label="Cancel" on:click={cancel}></button>
    <section class="modal-panel" role="dialog" aria-modal="true" style="width: min(460px, 100%)">
      <header class="modal-header">
        <div>
          <p class="eyebrow">{$t('common.confirm')}</p>
          <h2>{title}</h2>
        </div>
      </header>
      {#if summary}<p>{summary}</p>{/if}
      <div class="op-field">
        <label for="confirm-reason-input">{$t('common.reasonRequired')}</label>
        <!-- svelte-ignore a11y-autofocus -->
        <input
          id="confirm-reason-input"
          bind:value={reason}
          placeholder={$t('common.reasonPlaceholderChange')}
          list="reason-history"
          autofocus
          on:keydown={(e) => e.key === 'Enter' && confirm()}
        />
      </div>
      {#if error}<p class="op-result danger"><CircleX size={16} strokeWidth={2} aria-hidden="true" /> {error}</p>{/if}
      <div class="op-actions">
        <button type="button" class:danger on:click={confirm} disabled={busy || !valid}>
          {confirmLabel || $t('common.confirm')}
        </button>
        <button class="ghost-button" type="button" on:click={cancel}>{$t('common.cancel')}</button>
      </div>
    </section>
  </div>
{/if}

<style>
  .op-actions button.danger {
    color: var(--danger);
    border-color: rgba(var(--danger-rgb), 0.4);
  }
</style>
