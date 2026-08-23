<script>
  // A tooltip that appears on hover AND on keyboard focus -- the reason to use this rather than the
  // native `title` attribute, which never shows for a keyboard user and cannot be styled.
  //
  //   <Tooltip text="Unique remaining / edition size">
  //     <span class="op-chip">3/50</span>
  //   </Tooltip>

  /**
   * @typedef {Object} Props
   * @property {string} text
   * @property {string} [placement] - 'top' | 'bottom'
   * @property {import('svelte').Snippet} [children]
   */

  /** @type {Props} */
  let { text = '', placement = 'top', children } = $props();

  let open = $state(false);
</script>

<span
  class="tip-wrap"
  onmouseenter={() => (open = true)}
  onmouseleave={() => (open = false)}
  onfocusin={() => (open = true)}
  onfocusout={() => (open = false)}
>
  {@render children?.()}
  {#if open && text}
    <span class="tip" class:bottom={placement === 'bottom'} role="tooltip">{text}</span>
  {/if}
</span>

<style>
  .tip-wrap {
    position: relative;
    display: inline-flex;
  }

  .tip {
    position: absolute;
    left: 50%;
    bottom: calc(100% + 8px);
    transform: translateX(-50%);
    z-index: 90;
    max-width: 260px;
    width: max-content;
    border: 1px solid var(--line-strong);
    border-radius: 8px;
    background: var(--surface-raised);
    color: var(--ink);
    padding: 6px 9px;
    font-size: 0.78rem;
    font-weight: 400;
    text-transform: none;
    white-space: normal;
    box-shadow: var(--shadow);
    pointer-events: none;
  }

  .tip.bottom {
    bottom: auto;
    top: calc(100% + 8px);
  }

  /* The pointer, drawn as a rotated square so it inherits the same border and fill. */
  .tip::after {
    content: '';
    position: absolute;
    left: 50%;
    top: 100%;
    width: 8px;
    height: 8px;
    margin: -5px 0 0 -4px;
    border-right: 1px solid var(--line-strong);
    border-bottom: 1px solid var(--line-strong);
    background: var(--surface-raised);
    transform: rotate(45deg);
  }

  .tip.bottom::after {
    top: 0;
    margin-top: -4px;
    border: 0;
    border-left: 1px solid var(--line-strong);
    border-top: 1px solid var(--line-strong);
  }
</style>
