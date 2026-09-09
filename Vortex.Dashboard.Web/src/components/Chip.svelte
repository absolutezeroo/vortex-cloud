<script lang="ts">
  // A removable chip -- an active filter, a selected row, a tag.
  //
  //   <Chip label="wf_act_chase" onremove={() => drop(id)} />
  //   <Chip label="read only" />       no onremove: no dismiss affordance is drawn
  import { X } from '@lucide/svelte';
  import { t } from '../lib/i18n';

  type Props = {
    label?: string;
    /** '' | 'accent' | 'success' | 'warning' | 'danger' */
    tone?: string;
    onremove?: () => void;
    /** richer content in place of `label` */
    children?: import('svelte').Snippet;
  };

  let { label = '', tone = '', onremove, children }: Props = $props();
</script>

<span class="chip" class:accent={tone === 'accent'} class:success={tone === 'success'} class:warning={tone === 'warning'} class:danger={tone === 'danger'}>
  {#if children}{@render children()}{:else}{label}{/if}
  {#if onremove}
    <!-- `chip-x` is not decoration: the theme paints every classless button inside a panel as a key,
         and that list of names is how a control opts out. Without it this one came back as a blue
         key on hover, which is the "button inside a button" that was reported. -->
    <button type="button" class="chip-x" onclick={onremove} aria-label={`${$t('common.remove')} ${label}`}>
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
    /* The right gutter used to hold the × plate; without it the chip evens out. */
    padding: 3px 9px 3px 11px;
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

  /* A glyph, not a control in its own right. It had a round hover plate the size of a small button,
     which drew a second object inside the chip -- and a chip is already the thing you click off. So
     it keeps its own hit area and gains no surface of its own; brightening is the whole feedback. */
  .chip-x {
    display: grid;
    place-items: center;
    width: 14px;
    height: 14px;
    flex: 0 0 auto;
    border: 0;
    background: transparent;
    color: var(--muted);
    padding: 0;
    opacity: 0.7;
  }

  .chip-x:hover {
    background: transparent;
    color: var(--ink);
    opacity: 1;
  }
</style>
