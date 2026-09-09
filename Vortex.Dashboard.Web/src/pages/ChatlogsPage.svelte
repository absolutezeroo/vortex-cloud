<script lang="ts">
  import { onMount } from 'svelte';
  import { apiGet, describeApiError } from '../lib/api';
  import { formatDate } from '../lib/format';
  import { readNumberParam, writeParams } from '../lib/urlState';
  import { isPermissionDeniedError } from '../lib/permissions';
  import { openPlayer, openItem } from '../lib/session';
  import AccessDeniedNotice from '../components/AccessDeniedNotice.svelte';
  import PageHeader from '../components/PageHeader.svelte';
  import EntityLink from '../components/EntityLink.svelte';
  import Pagination from '../components/Pagination.svelte';
  import FilterBar from '../components/FilterBar.svelte';
  import LoadingOverlay from '../components/LoadingOverlay.svelte';
  import { readFilterValues } from '../lib/filters';
  import type { FilterField } from '../lib/filters';
  import { t, translate } from '../lib/i18n';
  import type { ChatlogEntry, ChatlogPage, ChatlogWindow } from '../lib/apiTypes';

  const FILTERS: FilterField[] = [
    {
      id: 'q',
      label: translate('chatlogs.text'),
      kind: 'text',
      placeholder: translate('chatlogs.textPlaceholder'),
    },
    {
      id: 'player',
      label: translate('chatlogs.player'),
      kind: 'entity',
      picker: 'user',
      anyLabel: translate('chatlogs.anyPlayer'),
    },
    {
      id: 'room',
      label: translate('chatlogs.room'),
      kind: 'entity',
      picker: 'room',
      anyLabel: translate('chatlogs.anyRoom'),
    },
    { id: 'since', label: translate('chatlogs.since'), kind: 'datetime' },
    { id: 'until', label: translate('chatlogs.until'), kind: 'datetime' },
  ];

  let filters = $state(readFilterValues(FILTERS));
  let limit = $state(100);
  let page = $state(readNumberParam('page', 1));


  let rows = $state<ChatlogEntry[]>([]);
  let total = $state(0);
  let resultWindow = $state<ChatlogWindow | null>(null);
  let loading = $state(false);
  let error = $state('');
  let forbidden = $state(false);
  let searched = $state(false);

  let totalPages = $derived(Math.max(1, Math.ceil(total / limit)));
  // The server refuses an unfiltered search outright, so the page says so before the round trip.
  // A date window alone is not enough for it -- it wants something to search FOR.
  let hasFilter = $derived(
    Boolean(filters.q.trim()) || Boolean(filters.player) || Boolean(filters.room),
  );

  $effect(() => {
    writeParams({ page: page > 1 ? page : '' });
  });

  function buildParams() {
    const params = new URLSearchParams({ limit: String(limit), page: String(page) });

    if (filters.q.trim()) params.set('q', filters.q.trim());
    if (filters.player) params.set('player', filters.player);
    if (filters.room) params.set('room', filters.room);
    if (filters.since) params.set('since', new Date(filters.since).toISOString());
    if (filters.until) params.set('until', new Date(filters.until).toISOString());

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
      const data = await apiGet<ChatlogPage>(`/api/v1/chatlogs?${buildParams()}`);
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

  function goToPage(next: number) {
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
  <PageHeader title={$t('chatlogs.title')} description={$t('chatlogs.privacyNotice')}>
    {#snippet actions()}
      <button type="button" onclick={refresh} disabled={loading || !hasFilter} class="warning">
        {$t('common.refresh')}
      </button>
    {/snippet}
  </PageHeader>
</section>

<section class="panel">

  <FilterBar fields={FILTERS} bind:values={filters} onchange={applyFilters} />

  <!-- See AuditPage: how much of the answer comes back at once is a setting, not a filter. -->
  <label class="page-size">
    {$t('chatlogs.pageSize')}
    <input
      autocomplete="off"
      spellcheck="false"
      type="number"
      min="10"
      max="500"
      bind:value={limit}
      onchange={applyFilters}
    />
  </label>
</section>

<section class="panel">

  {#if forbidden}
    <AccessDeniedNotice message={$t('chatlogs.accessDenied')} />
  {:else if error}
    <p class="empty-state danger" role="alert">{error}</p>
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

<LoadingOverlay show={loading} label={$t('chatlogs.loading')} />

<style>
  /* The page size sits under the filter row: a setting, not a question about the data. */
  .page-size {
    display: inline-flex;
    align-items: center;
    gap: 8px;
    margin-top: 10px;
    color: var(--muted);
    font-size: 0.82rem;
  }

  .page-size input {
    width: 96px;
  }

  .message {
    white-space: pre-wrap;
    word-break: break-word;
    max-width: 40rem;
  }
</style>
