<script>
  // A removable chip -- an active filter, a selected row, a tag.
  //
  //   <Chip label="wf_act_chase" onremove={() => drop(id)} />
  //   <Chip label="read only" />       no onremove: no dismiss affordance is drawn
  import { X } from '@lucide/svelte';
  import { t } from '../lib/i18n.js';

  /**
   * @typedef {Object} Props
   * @property {string} [label]
   * @property {string} [tone] - '' | 'accent' | 'success' | 'warning' | 'danger'
   * @property {() => void} [onremove]
   * @property {import('svelte').Snippet} [children] - richer content in place of `label`
   */

  /** @type {Props} */
  let { label = '', tone = '', onremove, children } = $props();
</script>

<span class="chip" class:accent={tone === 'accent'} class:success={tone === 'success'} class:warning={tone === 'warning'} class:danger={tone === 'danger'}>
  {#if children}{@render children()}{:else}{label}{/if}
  {#if onremove}
    <button type="button" onclick={onremove} aria-label={`${$t('common.remove')} ${label}`}>
      <X size={12} strokeWidth={2.6} aria-hidden="true" />
    </button>
  {/if}
</span>

<style>
  .chip {
    display: inline-flex;
    align-items: center;
    gap: 6px;
    max-width: 100%;
    border: 1px solid var(--line-strong);
    border-radius: 999px;
    background: var(--surface-raised);
    color: var(--ink);
    padding: 3px 4px 3px 11px;
    font-size: 0.8rem;
    white-space: nowrap;
  }

  /* No remove button means nothing sits in the right gutter, so the padding evens back out. */
  .chip:not(:has(button)) {
    padding-right: 11px;
  }

  .chip.accent { border-color: rgba(var(--accent-rgb), 0.55); }
  .chip.success { border-color: var(--success-border); }
  .chip.warning { border-color: var(--warning-border); }
  .chip.danger { border-color: var(--danger-border); }

  button {
    display: grid;
    place-items: center;
    width: 18px;
    height: 18px;
    flex: 0 0 auto;
    border: 0;
    border-radius: 999px;
    background: transparent;
    color: var(--muted);
    padding: 0;
  }

  button:hover {
    background: var(--surface-hover);
    color: var(--ink);
  }
</style>
