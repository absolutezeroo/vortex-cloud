<script>
  // The edit surface for the whole dashboard. Every create/edit form used to be spliced into the
  // page itself: a twelve-field panel under the table pushed the list off screen, and a row's edit
  // form expanded inside the grid and broke the cards around it. You lost your place to change one
  // number.
  //
  // A drawer rather than a modal, deliberately: these forms are long, and a modal wide enough for
  // twelve fields is a full-screen box -- as disruptive as the inline version, minus the context.
  // The drawer leaves the list visible beside it, so what you were looking at is still there when
  // you save. It also pins the title and the buttons, which is what stops "Save" from ending up
  // somewhere below the fold of a long form.
  //
  // Modals still own short confirmations (ConfirmReasonModal) and pickers; those are answers, not
  // editing.
  //
  //   {#if editing}
  //     <Drawer title="Edit definition" eyebrow="Furniture" onclose={() => (editing = null)}>
  //       ...fields...
  //       {#snippet actions()}<button ...>Save</button>{/snippet}
  //     </Drawer>
  //   {/if}
  import { cubicOut } from 'svelte/easing';
  import { useDialogBehaviour } from '../lib/dialogBehaviour.js';
  import { t } from '../lib/i18n.js';
  import { X } from '@lucide/svelte';

  /**
   * @typedef {Object} Props
   * @property {string} [title]
   * @property {string} [eyebrow] - small label above the title, usually what is being edited
   * @property {number} [width] - panel width in px; it still shrinks to the viewport on a narrow screen
   * @property {boolean} [dismissible] - false for an edit that must be resolved with an explicit choice
   * @property {string} [labelledBy]
   * @property {() => void} [onclose]
   * @property {import('svelte').Snippet} [children] - the form
   * @property {import('svelte').Snippet} [actions] - pinned to the bottom, never scrolls away
   */

  /** @type {Props} */
  let {
    title = '',
    eyebrow = '',
    width = 560,
    dismissible = true,
    labelledBy = 'drawer-title',
    onclose,
    children,
    actions,
  } = $props();

  let panel = $state();

  // A CSS animation only ever plays on the way IN: the node is gone the moment the
  // parent's {#if} goes false, so the drawer vanished instead of closing. A transition
  // is what makes Svelte wait -- an {#if} block holds its outro, and outros propagate
  // into child components, so the same directive covers both directions.
  const REDUCED =
    typeof matchMedia !== 'undefined' && matchMedia('(prefers-reduced-motion: reduce)').matches;

  // Written out rather than reached for from svelte/transition's `fly`: this needs a
  // percentage of the panel's own width, whatever `width` prop it was given, and no
  // opacity change -- a drawer that fades while it slides reads as a dialog, not a panel.
  function slide(node, { duration = 280 } = {}) {
    return {
      duration: REDUCED ? 0 : duration,
      easing: cubicOut,
      css: (t) => `transform: translateX(${(1 - t) * 100}%)`,
    };
  }

  function veil(node, { duration = 240 } = {}) {
    return { duration: REDUCED ? 0 : duration, css: (t) => `opacity: ${t}` };
  }

  // The drawer is the only edit surface in the dashboard, so one guard here covers every form
  // instead of forty pages each remembering to add one. Any input inside the panel arms it; closing
  // the drawer takes the listener away with the component. Only covers leaving the tab -- hash
  // navigation inside the SPA does not fire beforeunload, and svelte-spa-router has no guard hook.
  let dirty = $state(false);

  function guardUnload(event) {
    if (!dirty) return;
    event.preventDefault();
    event.returnValue = '';
  }

  function close() {
    if (dismissible) {
      onclose?.();
    }
  }

  useDialogBehaviour(() => panel, { onClose: close });
</script>

<svelte:window onbeforeunload={guardUnload} />

<div class="drawer-layer">
  <button
    class="drawer-backdrop"
    type="button"
    aria-label={$t('common.close')}
    tabindex="-1"
    onclick={close}
    transition:veil
  ></button>
  <section
    class="drawer-panel"
    oninput={() => (dirty = true)}
    onchange={() => (dirty = true)}
    role="dialog"
    aria-modal="true"
    aria-labelledby={title ? labelledBy : undefined}
    tabindex="-1"
    style="width: min({width}px, 100%)"
    bind:this={panel}
    transition:slide
  >
    <header class="drawer-header">
      <div>
        {#if eyebrow}<p class="eyebrow">{eyebrow}</p>{/if}
        {#if title}<h2 id={labelledBy}>{title}</h2>{/if}
      </div>
      {#if dismissible}
        <button type="button" class="drawer-close" onclick={close} aria-label={$t('common.close')}>
          <X size={18} strokeWidth={2} aria-hidden="true" />
        </button>
      {/if}
    </header>

    <div class="drawer-body">
      {@render children?.()}
    </div>

    {#if actions}
      <footer class="drawer-actions">
        {@render actions()}
      </footer>
    {/if}
  </section>
</div>

<style>
  .drawer-layer {
    position: fixed;
    inset: 0;
    z-index: 60;
  }

  .drawer-backdrop {
    position: absolute;
    inset: 0;
    border: 0;
    background: var(--overlay-bg);
  }

  .drawer-panel {
    position: absolute;
    top: 0;
    right: 0;
    bottom: 0;
    display: flex;
    flex-direction: column;
    background: var(--surface);
    border-left: 1px solid var(--line-strong);
    box-shadow: -18px 0 48px rgba(0, 0, 0, 0.38);
  }

  .drawer-header,
  .drawer-actions {
    flex: none;
    padding: 16px 20px;
  }

  .drawer-header {
    display: flex;
    align-items: flex-start;
    justify-content: space-between;
    gap: 12px;
    border-bottom: 1px solid var(--line);
  }

  .drawer-header h2 {
    margin: 0;
    font-size: 1.05rem;
  }

  .drawer-close {
    display: grid;
    place-items: center;
    border: 1px solid var(--line);
    border-radius: 8px;
    background: transparent;
    color: var(--muted);
    padding: 6px;
  }

  .drawer-close:hover {
    background: var(--surface-hover);
    color: var(--ink);
  }

  /* The only part that scrolls, so the title above and the buttons below stay put however long the
     form gets. */
  .drawer-body {
    flex: 1;
    overflow-y: auto;
    overscroll-behavior: contain;
    padding: 18px 20px;
  }

  .drawer-actions {
    display: flex;
    align-items: center;
    gap: 8px;
    border-top: 1px solid var(--line);
    background: var(--surface-strong);
  }
</style>
