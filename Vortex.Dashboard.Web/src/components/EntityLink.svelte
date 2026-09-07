<script lang="ts">
  import { onMount } from 'svelte';
  import { User } from '@lucide/svelte';
  import { t } from '../lib/i18n';
  import AssetImage from './AssetImage.svelte';
  import { avatarCache, resolveAvatar } from '../lib/avatars';
  import { openRoom } from '../lib/session';

  // Show the real Habbo avatar head next to a player's name. Resolved lazily + batched via
  // lib/avatars.js; falls back to a neutral head only if the player has no figure. Set avatar={false}
  
  type Props = {
    type?: 'player' | 'item' | 'room';
    id: number | string | null | undefined;
    /** Null when the row it names has been deleted; the fallback below covers it. */
    label?: string | null;
    /**
     * Both are optional and both are called with `?.`: a table of players passes only the player
     * one, and declaring them required was a JSDoc habit rather than a fact about the component.
     */
    openPlayer?: (id: number | string | null | undefined, label?: string) => void;
    openItem?: (id: number | string | null | undefined) => void;
    /** for tight inline usages where a head would be noise. */
    avatar?: boolean;
  };

  let {
    type = 'player',
    id,
    label = '',
    openPlayer,
    openItem,
    avatar = true
  }: Props = $props();

  let hasId = $derived(id !== null && id !== undefined && id !== '');
  let numId = $derived(hasId ? Number(id) : null);
  let showAvatar = $derived(type === 'player' && avatar && numId !== null && !Number.isNaN(numId));
  let avatarUrl = $derived(showAvatar ? $avatarCache.get(numId!) : undefined);
  let resolvedLabel = $derived(
    label || $t(type === 'item' ? 'common.itemHash' : type === 'room' ? 'common.roomHash' : 'common.playerHash', { id: id ?? '' }),
  );

  onMount(() => {
    if (showAvatar) resolveAvatar(numId);
  });

  function open() {
    if (type === 'room') {
      openRoom(id);

      return;
    }

    if (type === 'item') {
      openItem?.(id);
      return;
    }

    openPlayer?.(id, label ?? undefined);
  }
</script>

{#if hasId}
  {#if showAvatar && avatarUrl}
    <span class="entity-avatar">
      <AssetImage src={avatarUrl} size={22} fallbackIcon={User} alt="" />
      <button type="button" class="entity-player" onclick={open}>{resolvedLabel}</button>
    </span>
  {:else}
    <button
      type="button"
      class:entity-player={type === 'player'}
      class:entity-item={type === 'item' || type === 'room'}
      onclick={open}
    >
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
