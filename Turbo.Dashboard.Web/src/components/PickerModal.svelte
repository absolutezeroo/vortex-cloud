<script>
  import { apiGet } from '../lib/api.js';
  import AccessDeniedNotice from './AccessDeniedNotice.svelte';
  import { isPermissionDeniedError } from '../lib/permissions.js';

  // kind: 'user' | 'furniture'
  export let kind = 'user';
  export let title = 'Select';
  export let onSelect;
  export let onClose;
  export let canSelect = true;

  const endpoint = kind === 'furniture' ? '/api/v1/directory/furniture' : '/api/v1/directory/players';

  let query = '';
  let rows = [];
  let loading = false;
  let error = '';
  let forbidden = false;

  $: if (!canSelect) {
    forbidden = true;
    error = '';
    rows = [];
  } else {
    forbidden = false;
  }

  $: permissionMessage =
    kind === 'furniture'
      ? "Vous n'avez pas la permission de consulter le catalogue d'objets."
      : "Vous n'avez pas la permission de consulter les joueurs.";

  async function load() {
    if (!canSelect) {
      forbidden = true;
      error = '';
      rows = [];
      return;
    }

    loading = true;
    error = '';
    forbidden = false;
    rows = [];

    try {
      const data = await apiGet(`${endpoint}?q=${encodeURIComponent(query.trim())}&limit=60`);
      rows = data.items || [];
    } catch (err) {
      if (isPermissionDeniedError(err)) {
        forbidden = true;
        rows = [];
        return;
      }

      error = err.message;
      rows = [];
    } finally {
      loading = false;
    }
  }

  function choose(item) {
    onSelect?.(item);
    onClose?.();
  }

  void load();
</script>

<div class="modal-layer">
  <button class="modal-backdrop" type="button" aria-label="Close" on:click={onClose}></button>
  <section class="modal-panel" role="dialog" aria-modal="true" style="width: min(620px, 100%)">
    <header class="modal-header">
      <div>
        <p class="eyebrow">{kind === 'furniture' ? 'Catalog furniture' : 'Players'}</p>
        <h2>{title}</h2>
      </div>
      <button class="ghost-button" type="button" on:click={onClose}>Close</button>
    </header>

    <form class="toolbar" on:submit|preventDefault={load}>
      <input
        bind:value={query}
        placeholder={kind === 'furniture'
          ? 'search by name, id or sprite'
          : 'search by name or id'}
        disabled={!canSelect}
      />
      <button type="submit" disabled={!canSelect}>Search</button>
    </form>

    {#if forbidden}
      <AccessDeniedNotice message={permissionMessage} />
    {:else if error}
      <p class="empty-state danger">{error}</p>
    {:else if loading}
      <p class="empty-state">Loading...</p>
    {/if}

    <div class="pick-list">
      {#each rows as row}
        {#if kind === 'furniture'}
          <button type="button" class="pick-row" on:click={() => choose(row)}>
            {#if row.iconUrl}
              <img class="pick-icon" src={row.iconUrl} alt="" />
            {:else}
              <span class="pick-icon" aria-hidden="true">{row.spriteId}</span>
            {/if}
            <span class="pick-main">
              <strong>{row.name}</strong>
              <small>
                #{row.id} - sprite {row.spriteId} - {row.type}{row.canTrade ? '' : ' - no-trade'}
              </small>
            </span>
          </button>
        {:else}
          <button type="button" class="pick-row" on:click={() => choose(row)}>
            <span class="pick-dot" class:on={row.online} aria-hidden="true"></span>
            <span class="pick-main">
              <strong>{row.name}</strong>
              <small>#{row.id} - {row.online ? 'online' : 'offline'}</small>
            </span>
          </button>
        {/if}
      {:else}
        {#if !loading}<p class="empty-state">No results.</p>{/if}
      {/each}
    </div>
  </section>
</div>

<style>
  .pick-list {
    display: grid;
    gap: 6px;
    max-height: 52vh;
    overflow: auto;
  }

  .pick-row {
    display: flex;
    align-items: center;
    gap: 11px;
    width: 100%;
    text-align: left;
    border: 1px solid var(--line);
    border-radius: 10px;
    background: var(--surface-strong);
    color: var(--ink);
    padding: 9px 11px;
  }

  .pick-row:hover {
    border-color: var(--line-strong);
    background: #1a2738;
  }

  .pick-main {
    display: grid;
    gap: 2px;
    min-width: 0;
  }

  .pick-main small {
    color: var(--muted);
  }

  .pick-icon {
    width: 38px;
    height: 38px;
    flex: 0 0 auto;
    display: grid;
    place-items: center;
    border: 1px solid var(--line-strong);
    border-radius: 9px;
    background: #0f1724;
    color: var(--accent);
    font-size: 0.72rem;
    font-weight: 700;
    object-fit: contain;
  }

  .pick-dot {
    width: 10px;
    height: 10px;
    flex: 0 0 auto;
    border-radius: 999px;
    background: var(--muted);
    box-shadow: 0 0 0 3px rgba(139, 154, 173, 0.12);
  }

  .pick-dot.on {
    background: var(--ok);
    box-shadow: 0 0 0 3px rgba(86, 185, 145, 0.18);
  }
</style>
