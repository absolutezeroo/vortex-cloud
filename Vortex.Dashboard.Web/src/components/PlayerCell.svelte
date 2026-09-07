<script lang="ts">
  // Player identity cell — an avatar-head tile next to the player's name, identical in every table
  // or list that references a player. The head comes from the server-rendered avatar URL and falls
  // back to a user glyph (via AssetImage's graceful fallback). Pass a slot to control the name
  // (e.g. wrap it in EntityLink); otherwise a plain name is shown.
  import { User } from '@lucide/svelte';
  import AssetImage from './AssetImage.svelte';

  type Props = {
    /** Null when the player row it names has been deleted. */
    name?: string | null;
    avatarUrl?: any;
    size?: number;
    children?: import('svelte').Snippet;
  };

  let {
    name = '',
    avatarUrl = null,
    size = 32,
    children
  }: Props = $props();
</script>

<span class="player">
  <AssetImage src={avatarUrl} {size} fallbackIcon={User} alt={name} />
  {#if children}{@render children()}{:else}<span class="player-name">{name}</span>{/if}
</span>
