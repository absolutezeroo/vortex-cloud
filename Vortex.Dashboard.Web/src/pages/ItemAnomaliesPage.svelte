<script lang="ts">
  // "Has anything been duplicated?" — the one question item_events was built to answer, and the one
  // nothing asked it until now. This does not decide anything: a signature firing is a reason to
  // open the item's timeline, not a verdict, and the page says so rather than presenting a count as
  // a fraud total.
  import { onMount } from 'svelte';
  import { apiGet } from '../lib/api';
  import { formatDate, formatNumber } from '../lib/format';
  import AccessDeniedNotice from '../components/AccessDeniedNotice.svelte';
  import EmptyState from '../components/EmptyState.svelte';
  import EntityLink from '../components/EntityLink.svelte';
  import PageHeader from '../components/PageHeader.svelte';
  import TableFilter from '../components/TableFilter.svelte';
  import Pagination from '../components/Pagination.svelte';
  import { filterRows, pageOf, pageCountOf, PAGE_SIZE } from '../lib/tableView';
  import { isPermissionDeniedError } from '../lib/permissions';
  import { openPlayer, openItem } from '../lib/session';
  import { t } from '../lib/i18n';
  import type { ItemAnomalyScan } from '../lib/apiTypes';

  let data = $state<ItemAnomalyScan | null>(null);
  let error = $state('');
  let forbidden = $state(false);
  let loading = $state(false);

  // Days back, not a date pair: the question is "recently", and item_events is unbounded so an
  // open-ended scan is a table lock waiting to happen.
  let days = $state('7');
  let query = $state('');
  let kindFilter = $state('');

  let rows = $derived(data?.items ?? []);
  let visible = $derived(
    filterRows(
      rows.filter((row) => !kindFilter || row.kind === kindFilter),
      query,
    ),
  );

  // A ninety-day scan of a busy hotel answers thousands of signatures; drawing all of them was one
  // long page nobody read past the top of.
  let page = $state(1);
  let pageCount = $derived(pageCountOf(visible));
  let pageRows = $derived(pageOf(visible, page));

  $effect(() => {
    void query;
    void kindFilter;
    page = 1;
  });

  // Built from what came back rather than a hardcoded list, so a signature added server-side shows
  // up here without anyone remembering to add it.
  let kinds = $derived([...new Set(rows.map((row) => row.kind))].sort());

  async function refresh() {
    loading = true;
    error = '';
    forbidden = false;

    try {
      const since = new Date(Date.now() - Number(days) * 86_400_000).toISOString();

      data = await apiGet<ItemAnomalyScan>(
        `/api/v1/forensics/item-anomalies?since=${encodeURIComponent(since)}`,
      );
    } catch (err) {
      if (isPermissionDeniedError(err)) {
        forbidden = true;
        data = null;
        return;
      }

      error = (err as Error).message;
      data = null;
    } finally {
      loading = false;
    }
  }

  onMount(refresh);
</script>

<section class="panel">
  <PageHeader title={$t('itemAnomalies.title')} description={$t('itemAnomalies.description')}>
    {#snippet actions()}
      <label>
        {$t('itemAnomalies.window')}
        <select bind:value={days} onchange={refresh}>
          <option value="1">{$t('itemAnomalies.day1')}</option>
          <option value="7">{$t('itemAnomalies.day7')}</option>
          <option value="30">{$t('itemAnomalies.day30')}</option>
          <option value="90">{$t('itemAnomalies.day90')}</option>
        </select>
      </label>
      <button type="button" class="warning" onclick={refresh} disabled={loading}>
        {$t('common.refresh')}
      </button>
    {/snippet}
  </PageHeader>

  {#if forbidden}
    <AccessDeniedNotice message={$t('itemAnomalies.accessDenied')} />
  {:else if error}
    <p class="empty-state danger" role="alert">{error}</p>
  {/if}
</section>

{#if data && !forbidden}
  <section class="panel" style="margin-top: 12px;">
    <!-- The scanned count is the point of this line: without it an empty table reads as "nothing
         was looked at" exactly as much as "nothing is wrong", and those are opposite answers. -->
    <p class="muted">
      {$t('itemAnomalies.scanned', {
        items: formatNumber(data.itemsScanned),
        since: formatDate(data.since),
      })}
    </p>

    {#if rows.length}
      <div class="timeline-filters">
        <label>
          {$t('itemAnomalies.kind')}
          <select bind:value={kindFilter}>
            <option value="">{$t('itemAnomalies.kindAll')}</option>
            {#each kinds as kind}
              <option value={kind}>{$t(`itemAnomalies.kind_${kind}`)}</option>
            {/each}
          </select>
        </label>
      </div>

      <TableFilter bind:query shown={visible.length} total={rows.length} />

      <div class="table-wrap">
        <table>
          <thead>
            <tr>
              <th>{$t('itemAnomalies.colItem')}</th>
              <th>{$t('itemAnomalies.colKind')}</th>
              <th>{$t('itemAnomalies.colEvidence')}</th>
              <th>{$t('itemAnomalies.colOwner')}</th>
              <th>{$t('itemAnomalies.colLastSeen')}</th>
            </tr>
          </thead>
          <tbody>
            {#each pageRows as row (`${row.itemId}:${row.kind}`)}
              <tr>
                <td>
                  <EntityLink
                    type="item"
                    id={row.itemId}
                    label={`item #${row.itemId}`}
                    {openPlayer}
                    {openItem}
                  />
                  {#if row.definitionName}
                    <small class="muted">{row.definitionName}</small>
                  {/if}
                </td>
                <td><span class="chip">{$t(`itemAnomalies.kind_${row.kind}`)}</span></td>
                <td>{row.detail}</td>
                <td>
                  {#if row.ownerPlayerId}
                    <EntityLink
                      id={row.ownerPlayerId}
                      label={row.ownerName}
                      {openPlayer}
                      {openItem}
                    />
                  {:else}
                    <!-- The item is gone. Not a reason to hide the row: a deleted duplicate is
                         still a duplicate that was spent. -->
                    <span class="muted">{$t('itemAnomalies.itemGone')}</span>
                  {/if}
                </td>
                <td>{formatDate(row.lastSeen)}</td>
              </tr>
            {/each}
          </tbody>
        </table>
      </div>

      {#if pageCount > 1}
        <Pagination
          {page}
          {pageCount}
          total={visible.length}
          pageSize={PAGE_SIZE}
          label={$t('itemAnomalies.paginationLabel')}
          pageWord={$t('common.page')}
          prevLabel={$t('common.prev')}
          nextLabel={$t('common.next')}
          onchange={(next) => (page = next)}
        />
      {/if}
    {:else}
      <EmptyState message={$t('itemAnomalies.nothingFound')} />
    {/if}
  </section>
{/if}
