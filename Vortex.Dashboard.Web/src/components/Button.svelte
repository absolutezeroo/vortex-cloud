<script>
  // Shared button — the single button grammar for the whole dashboard. `variant` maps to the
  // .btn-* kit in styles.css (gold primary / neutral ghost / red danger); `size` to .btn-sm.
  // Adopting this across pages removes the historical split where the same action ("Refresh")
  // rendered filled on some pages and ghost on others. Forwards clicks via on:click.
  export let variant = 'primary'; // 'primary' | 'ghost' | 'danger'
  export let size = 'md'; // 'md' | 'sm'
  export let type = 'button';
  export let disabled = false;
  export let title = undefined;
  export let ariaLabel = undefined;
</script>

<button
  {type}
  {title}
  {disabled}
  aria-label={ariaLabel}
  class="btn btn-{variant}"
  class:btn-sm={size === 'sm'}
  on:click
>
  <slot />
</button>
