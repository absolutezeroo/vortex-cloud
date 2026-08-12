<script>
  // The confirm step for the authoring pages, whose forms already contain the audited reason: this
  // reads it back with the summary so the operator sees exactly what is about to be written and
  // under what justification. (The pages that collect the reason at confirm time use
  // ConfirmReasonModal instead -- that one has the input, this one has the read-back.)
  //
  // Eight pages carried a character-for-character copy of this dialog, each with its own
  // `<domain>.confirmEyebrow` and its own copy of the "Reason: {reason}" string.
  //
  //   <ConfirmStagedModal {ops} eyebrow={$t('polls.confirmEyebrow')} />
  import Modal from './Modal.svelte';
  import { t } from '../lib/i18n.js';

  /** The createWriteOps store driving this dialog. */
  export let ops;
  export let eyebrow = '';
</script>

{#if $ops.pending}
  <Modal
    title={$ops.pending.title}
    {eyebrow}
    width={460}
    labelledBy="confirm-staged-title"
    on:close={() => ops.cancel()}
  >
    <p>{$ops.pending.summary}</p>
    {#if $ops.pending.reason}
      <p class="muted">{$t('common.reasonLabel', { reason: $ops.pending.reason })}</p>
    {/if}
    {#if $ops.error}<p class="empty-state danger">{$ops.error}</p>{/if}

    <svelte:fragment slot="actions">
      <button type="button" on:click={() => ops.confirm()} disabled={$ops.busy}>
        {$t('common.confirm')}
      </button>
      <button class="ghost-button" type="button" on:click={() => ops.cancel()}>
        {$t('common.cancel')}
      </button>
    </svelte:fragment>
  </Modal>
{/if}
