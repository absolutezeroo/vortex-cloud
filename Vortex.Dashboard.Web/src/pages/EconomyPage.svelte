<script lang="ts">

  import { onMount } from 'svelte';
  import FilterBar from '../components/FilterBar.svelte';
  import LoadingOverlay from '../components/LoadingOverlay.svelte';
  import PageHeader from '../components/PageHeader.svelte';
  import Pagination from '../components/Pagination.svelte';
  import { apiGet } from '../lib/api';
  import { formatDate, formatNumber } from '../lib/format';
  import EntityLink from '../components/EntityLink.svelte';
  import AccessDeniedNotice from '../components/AccessDeniedNotice.svelte';
  import { readFilterValues } from '../lib/filters';
  import type { FilterField } from '../lib/filters';
  import { isPermissionDeniedError } from '../lib/permissions';
  import { readNumberParam, writeParams } from '../lib/urlState';
  import { openPlayer, openItem } from '../lib/session';
  import { t, translate } from '../lib/i18n';
  import type { EconomyLedgerEntry, EconomyLedgerPage } from '../lib/apiTypes';

  // The endpoint has taken page, limit, since and until from the start. This page asked for eighty
  // rows, sent no page, and dropped the total it came back with -- so the ledger was whatever the
  // last eighty movements happened to be, and row eighty-one was unreachable.
  const FILTERS: FilterField[] = [
    {
      id: 'player',
      label: translate('economy.colPlayer'),
      kind: 'entity',
      picker: 'user',
      anyLabel: translate('economy.anyPlayer'),
    },
    { id: 'since', label: translate('common.since'), kind: 'datetime' },
    { id: 'until', label: translate('common.until'), kind: 'datetime' },
  ];

  const LIMIT = 80;

  let filters = $state(readFilterValues(FILTERS));
  let page = $state(readNumberParam('page', 1));
  let rows = $state<EconomyLedgerEntry[]>([]);
  let total = $state(0);
  let loading = $state(false);
  let error = $state('');
  let forbidden = $state(false);

  let totalPages = $derived(Math.max(1, Math.ceil(total / LIMIT)));

  $effect(() => {
    writeParams({ page: page > 1 ? page : '' });
  });

  async function refresh() {
    const params = new URLSearchParams({ limit: String(LIMIT), page: String(page) });

    if (filters.player.trim()) {
      params.set('player', filters.player.trim());
    }

    if (filters.since) {
      params.set('since', new Date(filters.since).toISOString());
    }

    if (filters.until) {
      params.set('until', new Date(filters.until).toISOString());
    }

    loading = true;
    forbidden = false;
    error = '';

    try {
      const data = await apiGet<EconomyLedgerPage>(`/api/v1/economy/ledger?${params}`);
      rows = data.items || [];
      total = data.total || 0;
    } catch (err) {
      if (isPermissionDeniedError(err)) {
        forbidden = true;
        rows = [];
        total = 0;
        return;
      }

      error = (err as Error).message;
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

  onMount(refresh);
</script>

<section class="panel">
  <PageHeader title={$t('economy.title')} description={$t('economy.description')}>
    {#snippet actions()}
      <button type="button" onclick={refresh} class="warning">{$t('common.refresh')}</button>
    {/snippet}
  </PageHeader>
</section>

<!-- The player picker used to sit in the header beside Refresh, which read as a page action; it is
     a filter, and it now has company: the window the ledger answers for. -->
<section class="panel" style="margin-top: 12px;">
  <FilterBar fields={FILTERS} bind:values={filters} onchange={applyFilters} />

  {#if forbidden}
    <AccessDeniedNotice message={$t('economy.accessDenied')} />
  {:else if error}
    <p class="empty-state danger" role="alert">{error}</p>
  {/if}
</section>

<section class="panel" style="margin-top: 12px;">
  <p class="muted">{$t('economy.movements', { count: formatNumber(total) })}</p>

  <table>
    <thead><tr><th>{$t('economy.colTime')}</th><th>{$t('economy.colPlayer')}</th><th>{$t('economy.colCurrency')}</th><th>{$t('economy.colDelta')}</th><th>{$t('economy.colAfter')}</th><th>{$t('economy.colReason')}</th></tr></thead>
    <tbody>
      {#each rows as row}
        <tr>
          <td>{formatDate(row.occurredAt)}</td>
          <td><EntityLink id={row.playerId} label={row.playerName || ''} {openPlayer} {openItem} /></td>
          <td>{row.currency}</td>
          <td class:positive={Number(row.delta) > 0} class:negative={Number(row.delta) < 0}>{row.delta}</td>
          <td>{row.balanceAfter}</td>
          <td>{row.reason}</td>
        </tr>
      {:else}
        <tr><td colspan="6" class="muted">{$t('economy.noRows')}</td></tr>
      {/each}
    </tbody>
  </table>

  {#if totalPages > 1}
    <Pagination
      {page}
      pageCount={totalPages}
      {total}
      pageSize={LIMIT}
      label={$t('economy.paginationLabel')}
      pageWord={$t('common.page')}
      prevLabel={$t('common.prev')}
      nextLabel={$t('common.next')}
      disabled={loading}
      onchange={goToPage}
    />
  {/if}
</section>

<LoadingOverlay show={loading} />
