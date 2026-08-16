<script>
  import { onMount } from 'svelte';
  import { User } from '@lucide/svelte';
  import { t } from '../lib/i18n.js';
  import AssetImage from './AssetImage.svelte';
  import { avatarCache, resolveAvatar } from '../lib/avatars.js';

  // Show the real Habbo avatar head next to a player's name. Resolved lazily + batched via
  // lib/avatars.js; falls back to a neutral head only if the player has no figure. Set avatar={false}
  
  /**
   * @typedef {Object} Props
   * @property {string} [type]
   * @property {any} id
   * @property {string} [label]
   * @property {any} openPlayer
   * @property {any} openItem
   * @property {boolean} [avatar] - for tight inline usages where a head would be noise.
   */

  /** @type {Props} */
  let {
    type = 'player',
    id,
    label = '',
    openPlayer,
    openItem,
    avatar = true
  } = $props();

  let hasId = $derived(id !== null && id !== undefined && id !== '');
  let numId = $derived(hasId ? Number(id) : null);
  let showAvatar = $derived(type === 'player' && avatar && numId !== null && !Number.isNaN(numId));
  let avatarUrl = $derived(showAvatar ? $avatarCache.get(numId) : undefined);
  let resolvedLabel = $derived(label || $t(type === 'item' ? 'common.itemHash' : 'common.playerHash', { id }));

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
      <button type="button" class="entity-player" onclick={open}>{resolvedLabel}</button>
    </span>
  {:else}
    <button type="button" class:entity-player={type === 'player'} class:entity-item={type === 'item'} onclick={open}>
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
