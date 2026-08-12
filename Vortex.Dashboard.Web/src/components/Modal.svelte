<script>
  // The dialog shell every modal in the dashboard was writing out by hand: the fixed layer, the
  // click-to-dismiss backdrop, the panel and its header. Thirteen copies of that markup existed and
  // none of them handled Escape or kept the keyboard inside the dialog, so tabbing out of an open
  // modal landed on the page behind it -- which is how an operator ends up typing a reason into a
  // form they cannot see. Both behaviours live here now, so every dialog gets them by existing.
  //
  //   <Modal title={...} eyebrow={...} width={480} on:close={...}>
  //     ...body...
  //     <svelte:fragment slot="actions">...buttons...</svelte:fragment>
  //   </Modal>
  import { createEventDispatcher, onDestroy, onMount } from 'svelte';

  export let title = '';
  export let eyebrow = '';
  /** Panel width in px; the panel still shrinks to the viewport on a narrow screen. */
  export let width = 1040;
  /** Set false for a dialog that must be dismissed with an explicit action. */
  export let dismissible = true;
  export let labelledBy = 'modal-title';
  /**
   * Lay the panel out as a flex column instead of the default grid. What the pickers want from this
   * is a body that scrolls under a header that does not -- a long icon grid should not push the
   * search box off the top of the dialog.
   */
  export let column = false;

  const dispatch = createEventDispatcher();

  let panel;
  let previouslyFocused = null;

  const FOCUSABLE =
    'a[href], button:not([disabled]), input:not([disabled]), select:not([disabled]), textarea:not([disabled]), [tabindex]:not([tabindex="-1"])';

  function close() {
    if (dismissible) {
      dispatch('close');
    }
  }

  // Tab and Shift+Tab wrap inside the panel. Reading the focusable list on each keypress rather than
  // caching it keeps conditional fields (a duration input that only exists when "permanent" is
  // unchecked) in the cycle.
  function trapFocus(event) {
    const focusable = Array.from(panel?.querySelectorAll(FOCUSABLE) ?? []).filter(
      (el) => el.offsetParent !== null || el === document.activeElement
    );

    if (focusable.length === 0) {
      event.preventDefault();
      return;
    }

    const first = focusable[0];
    const last = focusable[focusable.length - 1];

    if (event.shiftKey && document.activeElement === first) {
      event.preventDefault();
      last.focus();
    } else if (!event.shiftKey && document.activeElement === last) {
      event.preventDefault();
      first.focus();
    }
  }

  function onKeydown(event) {
    if (event.key === 'Escape') {
      event.stopPropagation();
      close();
      return;
    }

    if (event.key === 'Tab') {
      trapFocus(event);
    }
  }

  onMount(() => {
    previouslyFocused = document.activeElement;

    // Focus the first control so the operator can start typing; falls back to the panel itself for a
    // dialog that is only text plus a close button.
    const target = panel?.querySelector(FOCUSABLE) ?? panel;
    target?.focus?.();

    window.addEventListener('keydown', onKeydown, true);
  });

  onDestroy(() => {
    window.removeEventListener('keydown', onKeydown, true);
    // Hand focus back to whatever opened the dialog, so dismissing it does not drop the caret at the
    // top of the document.
    previouslyFocused?.focus?.();
  });
</script>

<div class="modal-layer">
  <button
    class="modal-backdrop"
    type="button"
    aria-label="Close"
    tabindex="-1"
    on:click={close}
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
    {#if title || eyebrow || $$slots.header}
      <header class="modal-header">
        <div>
          {#if eyebrow}<p class="eyebrow">{eyebrow}</p>{/if}
          {#if title}<h2 id={labelledBy}>{title}</h2>{/if}
        </div>
        <slot name="header" />
      </header>
    {/if}

    <slot />

    {#if $$slots.actions}
      <div class="op-actions">
        <slot name="actions" />
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
