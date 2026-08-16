<script>
  // Shared button — the single button grammar for the whole dashboard. `variant` maps to the
  // .btn-* kit in styles.css (gold primary / neutral ghost / red danger); `size` to .btn-sm.
  // Adopting this across pages removes the historical split where the same action ("Refresh")
  
  /**
   * @typedef {Object} Props
   * @property {string} [variant] - rendered filled on some pages and ghost on others - 'primary' | 'ghost' | 'danger'
   * @property {string} [size] - 'md' | 'sm'
   * @property {string} [type]
   * @property {boolean} [disabled]
   * @property {any} [title]
   * @property {any} [ariaLabel]
   * @property {(event: MouseEvent) => void} [onclick]
   * @property {import('svelte').Snippet} [children]
   */

  /** @type {Props} */
  let {
    variant = 'primary',
    size = 'md',
    type = 'button',
    disabled = false,
    title = undefined,
    ariaLabel = undefined,
    // Svelte 4 forwarded the click with `on:click`; in Svelte 5 a handler is an ordinary prop, so
    // the caller's `onclick` is simply passed through to the element below.
    onclick = undefined,
    children
  } = $props();
</script>

<button
  {type}
  {title}
  {disabled}
  aria-label={ariaLabel}
  class="btn btn-{variant}"
  class:btn-sm={size === 'sm'}
  {onclick}
>
  {@render children?.()}
</button>
