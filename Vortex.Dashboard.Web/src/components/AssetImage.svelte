<script>
  // Reusable asset image tile with a graceful fallback, so every dashboard surface renders Habbo
  // sprites (furniture, avatars, guild badges, promo art) the same crisp way without each page
  // repeating the `<img on:error>` dance. When `src` is missing or the asset host is unreachable the
  // tile shows a lucide fallback icon instead of a broken-image glyph. Mirrors the `.catalog-row-icon`
  // styling used elsewhere.
  import { Image } from '@lucide/svelte';

  export let src = null;
  export let alt = '';
  export let size = 32;
  /** Lucide icon component shown when there's no image or it fails to load. */
  export let fallbackIcon = Image;

  // Keyed on the failing src (rather than a bare boolean) so that changing the source — e.g. the
  // operator types a new filename — re-arms the <img> instead of staying pinned to the fallback of a
  // previously broken URL.
  let failedSrc = null;

  $: normalizedSrc = src ? String(src) : '';
  $: showFallback = normalizedSrc === '' || failedSrc === normalizedSrc;
  // Scale the fallback glyph to sit comfortably inside the tile at any size.
  $: iconSize = Math.max(12, Math.round(size * 0.55));

  function handleError() {
    failedSrc = normalizedSrc;
  }
</script>

<span class="asset-image" style="width: {size}px; height: {size}px;">
  {#if showFallback}
    <svelte:component this={fallbackIcon} size={iconSize} strokeWidth={2} aria-hidden="true" />
  {:else}
    <img src={normalizedSrc} {alt} loading="lazy" on:error={handleError} />
  {/if}
</span>

<style>
  .asset-image {
    flex: 0 0 auto;
    display: grid;
    place-items: center;
    border: 1px solid var(--line-strong);
    border-radius: 9px;
    background: var(--input-bg);
    color: var(--accent);
    overflow: hidden;
  }

  .asset-image img {
    width: 100%;
    height: 100%;
    object-fit: contain;
    image-rendering: pixelated;
    image-rendering: crisp-edges;
  }
</style>
