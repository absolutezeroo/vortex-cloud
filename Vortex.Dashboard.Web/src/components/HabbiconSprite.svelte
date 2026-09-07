<script lang="ts">
  // One Habbicon, cropped out of the hotel's spritesheet.
  //
  // Habbicons are not one file each -- the client ships a single sheet plus a metadata file naming
  // one frame per id, and resolves the picture by id. So there is no URL to hand `AssetImage`: the
  // crop is the whole job, and CSS does it with `background-position`. One request draws all 33.
  //
  // The offsets arrive already flipped to a top-left origin (the pack is authored bottom-left); see
  // `HabbiconArtwork.cs`. Rendered at the sheet's native frame size on purpose -- these are 40px
  // pixel-art frames and a fractional scale turns them to mush.
  import { Image } from '@lucide/svelte';

  type Props = {
    /** Spritesheet URL, or null when no asset pack is installed. */
    sheet?: string|null;
    /** Frame offsets, or null when the pack has no frame for this id. */
    sprite?: {x: number, y: number}|null;
    /** Frame edge in pixels; must be the pack's own frame size. */
    size?: number;
    /** What this Habbicon is, for screen readers and the tooltip. */
    alt?: string;
  };

  let { sheet = null, sprite = null, size = 40, alt = '' }: Props = $props();

  // Both halves are required: a sheet with no frame would draw whatever sits at (0,0) under every id
  // the pack forgot, which is a wrong picture rather than a missing one.
  let drawable = $derived(Boolean(sheet) && sprite != null);
  let iconSize = $derived(Math.max(12, Math.round(size * 0.55)));
</script>

<span
  class="habbicon-sprite"
  class:is-empty={!drawable}
  style="width: {size}px; height: {size}px;"
  style:background-image={drawable ? `url("${sheet}")` : null}
  style:background-position={drawable ? `${-sprite!.x}px ${-sprite!.y}px` : null}
  title={alt}
  role={drawable ? 'img' : null}
  aria-label={drawable ? alt : null}
>
  {#if !drawable}
    <Image size={iconSize} strokeWidth={2} aria-hidden="true" />
  {/if}
</span>

<style>
  .habbicon-sprite {
    flex: 0 0 auto;
    display: grid;
    place-items: center;
    border: 1px solid var(--line-strong);
    border-radius: 8px;
    background-color: var(--input-bg);
    background-repeat: no-repeat;
    color: var(--accent);
    overflow: hidden;
    image-rendering: pixelated;
  }

  .habbicon-sprite.is-empty {
    color: var(--muted);
  }
</style>
