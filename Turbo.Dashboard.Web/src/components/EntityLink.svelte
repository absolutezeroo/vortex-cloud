<script>
  export let type = 'player';
  export let id;
  export let label = '';
  export let openPlayer;
  export let openItem;

  $: resolvedLabel = label || (type === 'item' ? `item #${id}` : `player #${id}`);

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
