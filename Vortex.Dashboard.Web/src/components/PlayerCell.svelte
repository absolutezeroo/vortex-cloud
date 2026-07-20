<script>
  // Player identity cell — an avatar-head tile next to the player's name, identical in every table
  // or list that references a player. The head comes from the server-rendered avatar URL and falls
  // back to a user glyph (via AssetImage's graceful fallback). Pass a slot to control the name
  // (e.g. wrap it in EntityLink); otherwise a plain name is shown.
  import { User } from '@lucide/svelte';
  import AssetImage from './AssetImage.svelte';

  export let name = '';
  export let avatarUrl = null;
  export let size = 32;
</script>

<span class="player">
  <AssetImage src={avatarUrl} {size} fallbackIcon={User} alt={name} />
  <slot><span class="player-name">{name}</span></slot>
</span>
