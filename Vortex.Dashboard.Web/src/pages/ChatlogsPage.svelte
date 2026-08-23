<script>
  import { onMount } from 'svelte';
  import { apiGet, describeApiError } from '../lib/api.js';
  import { formatDate } from '../lib/format.js';
  import { readNumberParam, writeParams } from '../lib/urlState.js';
  import { isPermissionDeniedError } from '../lib/permissions.js';
  import { openPlayer, openItem } from '../lib/session.js';
  import AccessDeniedNotice from '../components/AccessDeniedNotice.svelte';
  import EntityLink from '../components/EntityLink.svelte';
  import Pagination from '../components/Pagination.svelte';
  import PickerModal from '../components/PickerModal.svelte';
  import { t } from '../lib/i18n.js';

  let text = $state('');
  let since = $state('');
  let until = $state('');
  let limit = $state(100);
  let page = $state(readNumberParam('page', 1));

  // Held as {id, name} pairs from the picker: the search is by id, so a rename cannot orphan a
  // saved filter, and the operator still sees who they picked.
  let player = $state(null);
  let room = $state(null);
  let picking = $state(null);

  let rows = $state([]);
  let total = $state(0);
  let resultWindow = $state(null);
  let loading = $state(false);
  let error = $state('');
  let forbidden = $state(false);
  let searched = $state(false);

  let totalPages = $derived(Math.max(1, Math.ceil(total / limit)));
  // The server refuses an unfiltered search outright, so the button says so before the round trip.
  let hasFilter = $derived(Boolean(text.trim()) || player !== null || room !== null);

  $effect(() => {
    writeParams({ page: page > 1 ? page : '' });
  });

  function buildParams() {
    const params = new URLSearchParams({ limit: String(limit), page: String(page) });

    if (text.trim()) params.set('q', text.trim());
    if (player) params.set('player', String(player.id));
    if (room) params.set('room', String(room.id));
    if (since) params.set('since', new Date(since).toISOString());
    if (until) params.set('until', new Date(until).toISOString());

    return params;
  }

  async function refresh() {
    if (!hasFilter) {
      return;
    }

    loading = true;
    error = '';
    forbidden = false;

    try {
      const data = await apiGet(`/api/v1/chatlogs?${buildParams()}`);
      rows = data.items || [];
      total = data.total || 0;
      resultWindow = data.window || null;
      searched = true;
    } catch (err) {
      if (isPermissionDeniedError(err)) {
        forbidden = true;
        rows = [];
        total = 0;
        return;
      }

      error = describeApiError(err);
      rows = [];
      total = 0;
    } finally {
      loading = false;
    }
  }

  function applyFilters() {
    page = 1;
    void refresh();
  }

  function goToPage(next) {
    page = Math.min(totalPages, Math.max(1, next));
    void refresh();
  }

  onMount(() => {
    if (hasFilter) {
      void refresh();
    }
  });
</script>

<section class="panel">
  <div class="panel-head">
    <h2>{$t('chatlogs.title')}</h2>
    <button type="button" onclick={refresh} disabled={loading || !hasFilter}>
      {$t('common.refresh')}
    </button>
  </div>

  <p class="muted">{$t('chatlogs.privacyNotice')}</p>

  <form class="toolbar-grid" onsubmit={(event) => { event.preventDefault(); applyFilters(); }}>
    <label>
      {$t('chatlogs.text')}
      <input
        autocomplete="off"
        spellcheck="false"
        type="text"
        bind:value={text}
        placeholder={$t('chatlogs.textPlaceholder')}
      />
    </label>
    <label>
      {$t('chatlogs.player')}
      <button type="button" class="picker-button" onclick={() => (picking = 'player')}>
        {player ? player.name : $t('chatlogs.anyPlayer')}
      </button>
    </label>
    <label>
      {$t('chatlogs.room')}
      <button type="button" class="picker-button" onclick={() => (picking = 'room')}>
        {room ? room.name : $t('chatlogs.anyRoom')}
      </button>
    </label>
    <label>
      {$t('chatlogs.since')}
      <input autocomplete="off" spellcheck="false" type="datetime-local" bind:value={since} />
    </label>
    <label>
      {$t('chatlogs.until')}
      <input autocomplete="off" spellcheck="false" type="datetime-local" bind:value={until} />
    </label>
    <label>
      {$t('chatlogs.pageSize')}
      <input autocomplete="off" spellcheck="false" type="number" min="10" max="500" bind:value={limit} />
    </label>
    <button type="submit" disabled={!hasFilter}>{$t('common.filter')}</button>
    {#if player || room}
      <button type="button" onclick={() => { player = null; room = null; }}>
        {$t('chatlogs.clearTargets')}
      </button>
    {/if}
  </form>

  {#if forbidden}
    <AccessDeniedNotice message={$t('chatlogs.accessDenied')} />
  {:else if error}
    <p class="empty-state danger" role="alert">{error}</p>
  {:else if loading}
    <p class="muted">{$t('chatlogs.loading')}</p>
  {:else if !hasFilter}
    <p class="muted">{$t('chatlogs.filterRequired')}</p>
  {:else if searched}
    <p class="muted">
      {$t('chatlogs.found', { count: total })}
      {#if resultWindow}
        <span> · {formatDate(resultWindow.since)} → {formatDate(resultWindow.until)}</span>
      {/if}
    </p>
  {/if}

  <div class="table-wrap">
    <table>
      <thead>
        <tr>
          <th>{$t('chatlogs.colTime')}</th>
          <th>{$t('chatlogs.colRoom')}</th>
          <th>{$t('chatlogs.colPlayer')}</th>
          <th>{$t('chatlogs.colTarget')}</th>
          <th>{$t('chatlogs.colMessage')}</th>
        </tr>
      </thead>
      <tbody>
        {#each rows as row}
          <tr>
            <td>{formatDate(row.createdAt)}</td>
            <td>{row.roomName || `#${row.roomId}`}</td>
            <td><EntityLink id={row.playerId} label={row.playerName || ''} {openPlayer} {openItem} /></td>
            <td>
              {#if row.targetPlayerId}
                <EntityLink id={row.targetPlayerId} label={row.targetPlayerName || ''} {openPlayer} {openItem} />
              {:else}
                <span class="muted">—</span>
              {/if}
            </td>
            <td class="message">{row.message}</td>
          </tr>
        {:else}
          <tr><td colspan="5" class="muted">{$t('chatlogs.noRows')}</td></tr>
        {/each}
      </tbody>
    </table>
  </div>

  <Pagination
    page={page}
    pageCount={totalPages}
    pageWord={$t('common.page')}
    prevLabel={$t('common.prev')}
    nextLabel={$t('common.next')}
    disabled={loading}
    onchange={goToPage}
  />
</section>

{#if picking === 'player'}
  <PickerModal
    kind="user"
    title={$t('chatlogs.pickPlayer')}
    onSelect={(picked) => {
      player = { id: picked.id, name: picked.name };
      picking = null;
    }}
    onClose={() => (picking = null)}
  />
{:else if picking === 'room'}
  <PickerModal
    kind="room"
    title={$t('chatlogs.pickRoom')}
    onSelect={(picked) => {
      room = { id: picked.id, name: picked.name };
      picking = null;
    }}
    onClose={() => (picking = null)}
  />
{/if}

<style>
  .picker-button {
    text-align: left;
  }

  .message {
    white-space: pre-wrap;
    word-break: break-word;
    max-width: 40rem;
  }
</style>
