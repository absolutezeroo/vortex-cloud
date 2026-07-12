<script>
  import { onMount } from 'svelte';
  import { apiGet } from '../lib/api.js';
  import { t, translate } from '../lib/i18n.js';

  // There is no manifest of which catalog icon ids actually have a file on the asset host --
  // the id -> filename pattern (icon_{id}.png) is fixed, but which ids are populated is not.
  // So "browse all available icons" means probing candidate ids and letting the browser's own
  // <img> load/error events tell us which ones are real, rather than trusting a hardcoded list.
  export let title = 'Select an icon';
  export let onSelect;
  export let onClose;

  const BATCH_SIZE = 60;

  let template = '';
  let templateLoading = true;
  let templateError = '';

  let probeIds = [];
  let loadedIds = new Set();
  let failedIds = new Set();
  let gridEl;

  function urlFor(id) {
    return template.replace('{id}', String(id));
  }

  function loadMore() {
    const start = probeIds.length > 0 ? probeIds[probeIds.length - 1] + 1 : 1;
    probeIds = [...probeIds, ...Array.from({ length: BATCH_SIZE }, (_, i) => start + i)];
  }

  // Infinite scroll: a sentinel element sits at the end of the grid, and an IntersectionObserver
  // (scoped to the scrollable grid itself via `root`, not the page viewport) triggers the next
  // batch as it scrolls into view -- no "Load more" button to click.
  function observeSentinel(node) {
    const observer = new IntersectionObserver(
      (entries) => {
        if (entries[0].isIntersecting) {
          loadMore();
        }
      },
      { root: gridEl, rootMargin: '300px' },
    );
    observer.observe(node);
    return {
      destroy() {
        observer.disconnect();
      },
    };
  }

  function markLoaded(id) {
    loadedIds = new Set(loadedIds).add(id);
  }

  function markFailed(id) {
    failedIds = new Set(failedIds).add(id);
  }

  function choose(id) {
    onSelect?.(id);
    onClose?.();
  }

  $: foundCount = loadedIds.size;
  $: probedCount = probeIds.length;
  $: settledCount = loadedIds.size + failedIds.size;

  onMount(async () => {
    try {
      const data = await apiGet('/api/v1/catalog/icon-template');
      template = data.template || '';
      if (!template) {
        templateError = translate('catalogIconPicker.noTemplate');
      } else {
        loadMore();
      }
    } catch (err) {
      templateError = err.code || err.message;
    } finally {
      templateLoading = false;
    }
  });
</script>

<div class="modal-layer">
  <button class="modal-backdrop" type="button" aria-label="Close" on:click={onClose}></button>
  <section class="modal-panel icon-picker" role="dialog" aria-modal="true" style="width: min(720px, 100%)">
    <header class="modal-header">
      <div>
        <p class="eyebrow">{$t('catalogIconPicker.eyebrow')}</p>
        <h2>{title}</h2>
      </div>
      <button class="ghost-button" type="button" on:click={onClose}>{$t('pickerModal.close')}</button>
    </header>

    {#if templateLoading}
      <p class="empty-state">{$t('pickerModal.loading')}</p>
    {:else if templateError}
      <p class="empty-state danger">{templateError}</p>
    {:else}
      <p class="muted">
        {$t('catalogIconPicker.foundSoFar', { found: foundCount, probed: probedCount, pending: probedCount - settledCount })}
      </p>

      <div class="icon-grid" bind:this={gridEl}>
        {#each probeIds as id (id)}
          {#if !failedIds.has(id)}
            <button
              type="button"
              class="icon-cell"
              class:pending={!loadedIds.has(id)}
              disabled={!loadedIds.has(id)}
              on:click={() => choose(id)}
              title={`icon_${id}`}
            >
              <img
                src={urlFor(id)}
                alt=""
                loading="lazy"
                on:load={() => markLoaded(id)}
                on:error={() => markFailed(id)}
              />
              <small>#{id}</small>
            </button>
          {/if}
        {/each}
        <div class="icon-grid-sentinel" use:observeSentinel aria-hidden="true"></div>
      </div>
    {/if}
  </section>
</div>

<style>
  .icon-picker :global(.modal-panel) {
    max-height: 82vh;
    display: flex;
    flex-direction: column;
  }

  .icon-grid {
    display: grid;
    grid-template-columns: repeat(auto-fill, minmax(64px, 1fr));
    gap: 8px;
    max-height: 52vh;
    overflow-y: auto;
    padding: 4px 4px 4px 0;
    scrollbar-width: thin;
    scrollbar-color: var(--line-strong) transparent;
  }

  .icon-grid::-webkit-scrollbar {
    width: 6px;
  }

  .icon-grid::-webkit-scrollbar-thumb {
    background: var(--line-strong);
    border-radius: 999px;
  }

  .icon-cell {
    display: grid;
    justify-items: center;
    gap: 4px;
    padding: 8px 4px;
    border: 1px solid var(--line-strong);
    border-radius: 9px;
    background: var(--input-bg);
    color: var(--muted);
    cursor: pointer;
  }

  .icon-cell:hover:not(:disabled) {
    border-color: rgba(var(--accent-rgb), 0.58);
    background: var(--surface-hover);
  }

  .icon-cell.pending {
    opacity: 0.35;
    cursor: default;
  }

  .icon-cell.pending img {
    visibility: hidden;
  }

  .icon-cell img {
    width: 32px;
    height: 32px;
    object-fit: contain;
    image-rendering: pixelated;
    image-rendering: crisp-edges;
  }

  .icon-cell small {
    font-size: 0.68rem;
  }

  .icon-grid-sentinel {
    grid-column: 1 / -1;
    height: 1px;
  }
</style>
