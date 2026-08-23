<script>
  // The dialog shell every modal in the dashboard was writing out by hand: the fixed layer, the
  // click-to-dismiss backdrop, the panel and its header. Thirteen copies of that markup existed and
  // none of them handled Escape or kept the keyboard inside the dialog, so tabbing out of an open
  // modal landed on the page behind it -- which is how an operator ends up typing a reason into a
  // form they cannot see. Both behaviours live here now, so every dialog gets them by existing.
  //
  //   <Modal title={...} eyebrow={...} width={480} onclose={...}>
  //     ...body...
  //     <svelte:fragment slot="actions">...buttons...</svelte:fragment>
  //   </Modal>
  import { useDialogBehaviour } from '../lib/dialogBehaviour.js';
  import { t } from '../lib/i18n.js';

  
  
  
  /**
   * @typedef {Object} Props
   * @property {string} [title]
   * @property {string} [eyebrow]
   * @property {number} [width] - Panel width in px; the panel still shrinks to the viewport on a narrow screen.
   * @property {boolean} [dismissible] - Set false for a dialog that must be dismissed with an explicit action.
   * @property {string} [labelledBy]
   * @property {boolean} [column] - Lay the panel out as a flex column instead of the default grid. What the pickers want from this
is a body that scrolls under a header that does not -- a long icon grid should not push the
search box off the top of the dialog.
   * @property {import('svelte').Snippet} [header]
   * @property {import('svelte').Snippet} [children]
   * @property {import('svelte').Snippet} [actions]
   * @property {() => void} [onclose] - called when the operator dismisses the dialog
   */

  /** @type {Props} */
  let {
    title = '',
    eyebrow = '',
    width = 1040,
    dismissible = true,
    labelledBy = 'modal-title',
    column = false,
    header,
    children,
    actions,
    onclose
  } = $props();

  let panel = $state();

  function close() {
    if (dismissible) {
      onclose?.();
    }
  }

  // Focus trap, Escape, and handing focus back on close -- shared with Drawer so there is one
  // implementation rather than two that drift.
  useDialogBehaviour(() => panel, { onClose: close });
</script>

<div class="modal-layer">
  <button
    class="modal-backdrop"
    type="button"
    aria-label={$t('common.close')}
    tabindex="-1"
    onclick={close}
  ></button>
  <section
    class="modal-panel"
    class:column
    role="dialog"
    aria-modal="true"
    aria-labelledby={title ? labelledBy : undefined}
    tabindex="-1"
    style="width: min({width}px, 100%)"
    bind:this={panel}
  >
    {#if title || eyebrow || header}
      <header class="modal-header">
        <div>
          {#if eyebrow}<p class="eyebrow">{eyebrow}</p>{/if}
          {#if title}<h2 id={labelledBy}>{title}</h2>{/if}
        </div>
        {@render header?.()}
      </header>
    {/if}

    {@render children?.()}

    {#if actions}
      <div class="op-actions">
        {@render actions?.()}
      </div>
    {/if}
  </section>
</div>

<style>
  /* Header pinned, body scrolling -- what the icon/image pickers want when the grid is long. */
  .modal-panel.column {
    display: flex;
    flex-direction: column;
    max-height: 82vh;
  }
</style>
