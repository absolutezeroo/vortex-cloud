<script>
  import Modal from './Modal.svelte';
  import { apiGet } from '../lib/api.js';
  import AccessDeniedNotice from './AccessDeniedNotice.svelte';
  import AssetImage from './AssetImage.svelte';
  import { House, User } from '@lucide/svelte';
  import { isPermissionDeniedError } from '../lib/permissions.js';
  import { LOGIC_GROUPS } from '../lib/furnitureEnums.js';
  import { t } from '../lib/i18n.js';

  
  /**
   * @typedef {Object} Props
   * @property {string} [kind] - kind: 'user' | 'furniture' | 'room'
   * @property {string} [title]
   * @property {any} onSelect
   * @property {any} onClose
   * @property {boolean} [canSelect]
   */

  /** @type {Props} */
  let {
    kind = 'user',
    title = 'Select',
    onSelect,
    onClose,
    canSelect = true
  } = $props();

  const ENDPOINTS = {
    furniture: '/api/v1/directory/furniture',
    room: '/api/v1/directory/rooms',
    user: '/api/v1/directory/players',
  };

  const endpoint = ENDPOINTS[kind] ?? ENDPOINTS.user;

  // Searching by name only works while you still remember the name. These are the other ways an
  // operator actually knows a thing: the id they just read in a log, the sprite an asset folder is
  // named after, and -- for furniture -- what the thing *does*. The server owns the ordering (see
  // OrderDefinitions/OrderPlayers/OrderRooms); this is only the vocabulary it accepts.
  const SORTS = {
    furniture: ['relevance', 'name', 'id', 'idDesc', 'sprite', 'logic'],
    room: ['relevance', 'name', 'id', 'idDesc'],
    user: ['relevance', 'name', 'id', 'idDesc'],
  };

  const SORT_LABELS = {
    relevance: 'pickerModal.sortRelevance',
    name: 'pickerModal.sortName',
    id: 'pickerModal.sortId',
    idDesc: 'pickerModal.sortIdDesc',
    sprite: 'pickerModal.sortSprite',
    logic: 'pickerModal.sortLogic',
  };

  const sorts = SORTS[kind] ?? SORTS.user;

  let query = $state('');
  let sort = $state('relevance');
  let logicFilter = $state('');
  let onlineOnly = $state(false);
  let rows = $state([]);
  let hasMore = $state(false);
  let loadingMore = $state(false);
  const PAGE_SIZE = 60;
  let loading = $state(false);
  let error = $state('');
  let forbidden = $state(false);

  $effect(() => {
    if (!canSelect) {
      forbidden = true;
      error = '';
      rows = [];
    } else {
      forbidden = false;
    }
  });

  const ACCESS_DENIED_KEYS = {
    furniture: 'pickerModal.furnitureAccessDenied',
    room: 'pickerModal.roomsAccessDenied',
    user: 'pickerModal.playersAccessDenied',
  };

  const EYEBROW_KEYS = {
    furniture: 'pickerModal.catalogFurniture',
    room: 'pickerModal.rooms',
    user: 'pickerModal.players',
  };

  const SEARCH_PLACEHOLDER_KEYS = {
    furniture: 'pickerModal.searchFurniturePlaceholder',
    room: 'pickerModal.searchRoomPlaceholder',
    user: 'pickerModal.searchPlayerPlaceholder',
  };

  let permissionMessage = $derived($t(ACCESS_DENIED_KEYS[kind] ?? ACCESS_DENIED_KEYS.user));

  // 'relevance' is the server's own default, so it is left out rather than sent as a value the
  // server's switch would have to carry a case for.
  function params(offset) {
    const parts = [`q=${encodeURIComponent(query.trim())}`, `limit=${PAGE_SIZE}`, `offset=${offset}`];

    if (sort !== 'relevance') parts.push(`sort=${encodeURIComponent(sort)}`);
    if (kind === 'furniture' && logicFilter) parts.push(`logic=${encodeURIComponent(logicFilter)}`);
    if (kind === 'user' && onlineOnly) parts.push('online=true');

    return parts.join('&');
  }

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
    hasMore = false;

    try {
      const data = await apiGet(`${endpoint}?${params(0)}`);
      rows = data.items || [];
      // The players endpoint does not page; absent hasMore simply means "that is everything".
      hasMore = Boolean(data.hasMore);
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

  // Appends rather than replacing: the list is browsed by scrolling, so a page jump would lose the
  // rows the operator already scrolled past.
  async function loadMore() {
    if (loadingMore || !hasMore) return;

    loadingMore = true;

    try {
      const data = await apiGet(`${endpoint}?${params(rows.length)}`);
      rows = [...rows, ...(data.items || [])];
      hasMore = Boolean(data.hasMore);
    } catch (err) {
      error = err.message;
      hasMore = false;
    } finally {
      loadingMore = false;
    }
  }

  function choose(item) {
    onSelect?.(item);
    onClose?.();
  }

  void load();
</script>

<Modal
  title={title}
  eyebrow={$t(EYEBROW_KEYS[kind] ?? EYEBROW_KEYS.user)}
  width={620}
  labelledBy="picker-modal-title"
  onclose={onClose}
>
  {#snippet header()}
    <button class="ghost-button" type="button" onclick={onClose}>
      {$t('pickerModal.close')}
    </button>
  {/snippet}

  <form class="toolbar" onsubmit={(event) => { event.preventDefault(); load(); }}>
    <input autocomplete="off" spellcheck="false"
      bind:value={query}
      placeholder={$t(SEARCH_PLACEHOLDER_KEYS[kind] ?? SEARCH_PLACEHOLDER_KEYS.user)}
      disabled={!canSelect}
    />
    <button type="submit" disabled={!canSelect}>{$t('pickerModal.search')}</button>
  </form>

  <!-- Changing a filter re-runs the search straight away: it is a narrowing of the same question,
       not a new one, so making the operator press Search again would only cost a click. -->
  <div class="pick-filters">
    <label>
      <span>{$t('pickerModal.sortLabel')}</span>
      <select bind:value={sort} onchange={load} disabled={!canSelect}>
        {#each sorts as option (option)}
          <option value={option}>{$t(SORT_LABELS[option])}</option>
        {/each}
      </select>
    </label>

    {#if kind === 'furniture'}
      <label>
        <span>{$t('pickerModal.logicLabel')}</span>
        <select bind:value={logicFilter} onchange={load} disabled={!canSelect}>
          <option value="">{$t('pickerModal.logicAll')}</option>
          {#each LOGIC_GROUPS as group (group.label)}
            <optgroup label={group.label}>
              {#each group.options as option (option.value)}
                <option value={option.value}>{option.label}</option>
              {/each}
            </optgroup>
          {/each}
        </select>
      </label>
    {:else if kind === 'user'}
      <label class="pick-check">
        <input type="checkbox" bind:checked={onlineOnly} onchange={load} disabled={!canSelect} />
        <span>{$t('pickerModal.onlineOnly')}</span>
      </label>
    {/if}

    {#if rows.length}
      <small class="pick-count">{$t('pickerModal.resultCount', { count: rows.length })}</small>
    {/if}
  </div>

  {#if forbidden}
    <AccessDeniedNotice message={permissionMessage} />
  {:else if error}
    <p class="empty-state danger" role="alert">{error}</p>
  {:else if loading}
    <p class="empty-state">{$t('pickerModal.loading')}</p>
  {/if}

  <div class="pick-list">
    {#each rows as row}
      {#if kind === 'furniture'}
        <button type="button" class="pick-row" onclick={() => choose(row)}>
          {#if row.iconUrl}
            <img class="pick-icon" src={row.iconUrl} alt="" width="38" height="38" loading="lazy" />
          {:else}
            <span class="pick-icon" aria-hidden="true">{row.spriteId}</span>
          {/if}
          <span class="pick-main">
            <strong>{row.name}</strong>
            <small>
              #{row.id} - sprite {row.spriteId} - {row.type}{row.logic ? ` - ${row.logic}` : ''}{row.canTrade
                ? ''
                : ` - ${$t('pickerModal.noTrade')}`}
            </small>
          </span>
        </button>
      {:else if kind === 'room'}
        <button type="button" class="pick-row" onclick={() => choose(row)}>
          <span class="pick-icon" aria-hidden="true"><House size={18} /></span>
          <span class="pick-dot" class:on={row.usersNow > 0} aria-hidden="true"></span>
          <span class="pick-main">
            <strong>{row.name}</strong>
            <small>
              #{row.id}{row.ownerName ? ` - ${row.ownerName}` : ''} - {$t('pickerModal.roomOccupancy', {
                users: row.usersNow,
                max: row.playersMax,
              })}
            </small>
          </span>
        </button>
      {:else}
        <button type="button" class="pick-row" onclick={() => choose(row)}>
          <AssetImage src={row.avatarUrl} alt={row.name} size={38} fallbackIcon={User} />
          <span class="pick-dot" class:on={row.online} aria-hidden="true"></span>
          <span class="pick-main">
            <strong>{row.name}</strong>
            <small>#{row.id} - {row.online ? $t('pickerModal.online') : $t('pickerModal.offline')}</small>
          </span>
        </button>
      {/if}
    {:else}
      {#if !loading}<p class="empty-state">{$t('pickerModal.noResults')}</p>{/if}
    {/each}
    {#if hasMore}
      <button type="button" class="ghost-button" onclick={loadMore} disabled={loadingMore}>
        {loadingMore ? $t('common.loading') : $t('pickerModal.loadMore')}
      </button>
    {/if}
  </div>
</Modal>

<style>
  .pick-filters {
    display: flex;
    flex-wrap: wrap;
    align-items: end;
    gap: 10px;
  }

  .pick-filters label {
    display: grid;
    gap: 4px;
    min-width: 0;
  }

  .pick-filters label span {
    color: var(--muted);
    font-size: 0.72rem;
    text-transform: uppercase;
  }

  .pick-filters select {
    max-width: 260px;
  }

  .pick-check {
    display: flex !important;
    align-items: center;
    gap: 7px;
    padding-bottom: 9px;
  }

  .pick-check span {
    text-transform: none !important;
    font-size: 0.86rem !important;
  }

  .pick-count {
    margin-left: auto;
    color: var(--muted);
    padding-bottom: 9px;
  }

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
    border-radius: 8px;
    background: var(--surface-strong);
    color: var(--ink);
    padding: 9px 11px;
  }

  .pick-row:hover {
    border-color: var(--line-strong);
    background: var(--surface-hover);
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
    border-radius: 8px;
    background: var(--input-bg);
    color: var(--accent);
    font-size: 0.72rem;
    font-weight: 700;
    object-fit: contain;
    image-rendering: pixelated;
    image-rendering: crisp-edges;
  }

  .pick-dot {
    width: 10px;
    height: 10px;
    flex: 0 0 auto;
    border-radius: 999px;
    background: var(--muted);
    box-shadow: 0 0 0 3px rgba(var(--muted-rgb), 0.12);
  }

  .pick-dot.on {
    background: var(--ok);
    box-shadow: 0 0 0 3px rgba(var(--ok-rgb), 0.18);
  }
</style>
