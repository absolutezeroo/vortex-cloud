<script lang="ts">
  // Shared button — the single button grammar for the whole dashboard. `variant` maps to the
  // .btn-* kit in styles.css (gold primary / neutral ghost / red danger); `size` to .btn-sm.
  // Adopting this across pages removes the historical split where the same action ("Refresh")
  
  type Props = {
    /** rendered filled on some pages and ghost on others - 'primary' | 'ghost' | 'danger' */
    variant?: string;
    /** 'md' | 'sm' */
    size?: string;
    type?: 'button' | 'submit' | 'reset';
    disabled?: boolean;
    title?: any;
    ariaLabel?: any;
    onclick?: (event: MouseEvent) => void;
    children?: import('svelte').Snippet;
  };

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
  }: Props = $props();
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
