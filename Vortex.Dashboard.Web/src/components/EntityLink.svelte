<script>
  import { onMount } from 'svelte';
  import { User } from '@lucide/svelte';
  import { t } from '../lib/i18n.js';
  import AssetImage from './AssetImage.svelte';
  import { avatarCache, resolveAvatar } from '../lib/avatars.js';

  export let type = 'player';
  export let id;
  export let label = '';
  export let openPlayer;
  export let openItem;
  // Show the real Habbo avatar head next to a player's name. Resolved lazily + batched via
  // lib/avatars.js; falls back to a neutral head only if the player has no figure. Set avatar={false}
  // for tight inline usages where a head would be noise.
  export let avatar = true;

  $: hasId = id !== null && id !== undefined && id !== '';
  $: numId = hasId ? Number(id) : null;
  $: showAvatar = type === 'player' && avatar && numId !== null && !Number.isNaN(numId);
  $: avatarUrl = showAvatar ? $avatarCache.get(numId) : undefined;
  $: resolvedLabel = label || $t(type === 'item' ? 'common.itemHash' : 'common.playerHash', { id });

  onMount(() => {
    if (showAvatar) resolveAvatar(numId);
  });

  function open() {
    if (type === 'item') {
      openItem?.(id);
      return;
    }

    openPlayer?.(id, label);
  }
</script>

{#if hasId}
  {#if showAvatar && avatarUrl}
    <span class="entity-avatar">
      <AssetImage src={avatarUrl} size={22} fallbackIcon={User} alt="" />
      <button type="button" class="entity-player" on:click={open}>{resolvedLabel}</button>
    </span>
  {:else}
    <button type="button" class:entity-player={type === 'player'} class:entity-item={type === 'item'} on:click={open}>
      {resolvedLabel}
    </button>
  {/if}
{:else}
  <span class="muted">-</span>
{/if}

<style>
  .entity-avatar {
    display: inline-flex;
    align-items: center;
    gap: 6px;
    vertical-align: middle;
    min-width: 0;
  }
</style>
