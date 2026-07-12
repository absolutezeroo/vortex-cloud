<script>
  import { t } from '../lib/i18n.js';

  export let type = 'player';
  export let id;
  export let label = '';
  export let openPlayer;
  export let openItem;

  $: resolvedLabel = label || $t(type === 'item' ? 'common.itemHash' : 'common.playerHash', { id });

  function open() {
    if (type === 'item') {
      openItem?.(id);
      return;
    }

    openPlayer?.(id, label);
  }
</script>

{#if id !== null && id !== undefined && id !== ''}
  <button type="button" class:entity-player={type === 'player'} class:entity-item={type === 'item'} on:click={open}>
    {resolvedLabel}
  </button>
{:else}
  <span class="muted">-</span>
{/if}
