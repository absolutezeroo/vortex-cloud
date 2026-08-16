<script>
  // Confirm a write, show what it actually changes, and collect the operator's optional note.
  //
  // The reason this action gets audited with is built from the action itself (lib/changes.js): the
  // caller's summary sentence plus the fields that really differ. That is the part the system
  // already knows, and making an operator retype it before every price change was friction that
  // produced nothing. The free-text box is now for the part only they know -- "asked for by X" --
  // and is required only when there is no generated sentence to stand on (`noteOnly` false).
  //
  // The before/after list is also the thing worth reading before confirming: "Price: 2 → 4" catches
  // a mistyped field in a way "Save this offer?" never did.
  import Modal from './Modal.svelte';
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
  export let changes = [];
  export let noteOnly = true;

  const dispatch = createEventDispatcher();

  let note = '';
  let prevOpen = false;
  // Reset the field only on the closed -> open transition, so typing doesn't wipe it and a reused
  // modal never carries the previous action's note.
  $: {
    if (open && !prevOpen) {
      note = '';
    }
    prevOpen = open;
  }

  $: valid = noteOnly || reasonOk(note);

  function cancel() {
    dispatch('cancel');
  }

  function confirm() {
    if (!valid || busy) {
      return;
    }
    dispatch('confirm', note.trim());
  }
</script>

{#if open}
  <Modal
    {title}
    eyebrow={$t('common.confirm')}
    width={460}
    labelledBy="confirm-reason-title"
    on:close={cancel}
  >
    {#if summary}<p>{summary}</p>{/if}

    {#if changes && changes.length}
      <ul class="change-list">
        {#each changes as change}
          <li>
            <span class="change-label">{change.label}</span>
            <span class="change-from">{change.from}</span>
            <span class="change-arrow" aria-hidden="true">→</span>
            <span class="change-to">{change.to}</span>
          </li>
        {/each}
      </ul>
    {/if}

    <!-- Optional richer detail than a summary sentence: the config editor shows the old/new value,
         which is the thing the operator actually double-checks before confirming. -->
    <slot />
    <div class="op-field">
      <label for="confirm-reason-input">
        {noteOnly ? $t('common.noteOptional') : $t('common.reasonRequired')}
      </label>
      <!-- svelte-ignore a11y-autofocus -->
      <input
        id="confirm-reason-input"
        bind:value={note}
        placeholder={noteOnly ? $t('common.notePlaceholder') : $t('common.reasonPlaceholderChange')}
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
  </Modal>
{/if}

<style>
  .change-list {
    list-style: none;
    margin: 12px 0;
    padding: 10px 12px;
    display: grid;
    gap: 6px;
    border: 1px solid var(--border, rgba(128, 128, 128, 0.3));
    border-radius: 8px;
    font-size: 0.86rem;
  }

  .change-list li {
    display: flex;
    align-items: baseline;
    gap: 8px;
    flex-wrap: wrap;
  }

  .change-label {
    flex: 1 1 40%;
    min-width: 0;
    opacity: 0.75;
  }

  .change-from {
    text-decoration: line-through;
    opacity: 0.6;
  }

  .change-arrow {
    opacity: 0.5;
  }

  .change-to {
    font-weight: 600;
  }
</style>
