<script>

  import { apiGet } from '../lib/api.js';
  import { createResource } from '../lib/resource.js';
  import { formatNumber } from '../lib/format.js';
  import AccessDeniedNotice from '../components/AccessDeniedNotice.svelte';
  import PageHeader from '../components/PageHeader.svelte';
  import EntityLink from '../components/EntityLink.svelte';
  import LineChart from '../components/LineChart.svelte';
  import StatCard from '../components/StatCard.svelte';
  import { ShoppingCart, ShoppingBag, Coins } from '@lucide/svelte';
  import { openPlayer, openItem } from '../lib/session.js';
  import { t } from '../lib/i18n.js';

  const granularities = ['day', 'month', 'year'];

  // See the AuditPage `categoryLabel` note: translator must be passed explicitly so template call
  // sites stay reactive to locale changes.
  function granularityLabel(value, translator) {
    return translator(`common.granularity${value.charAt(0).toUpperCase()}${value.slice(1)}`);
  }

  let since = $state('');
  let until = $state('');
  let granularity = $state('day');

  function toLocalDateValue(value) {
    const date = new Date(value);
    return Number.isNaN(date.getTime()) ? '' : date.toISOString().slice(0, 10);
  }

  function setDefaultWindow() {
    const end = new Date();
    const start = new Date(end.getTime() - 30 * 24 * 60 * 60 * 1000);
    since = toLocalDateValue(start);
    until = toLocalDateValue(end);
  }

  // Synchronously at init: the window is part of the cache key below, so it has to hold real dates
  // before the first read is described.
  setDefaultWindow();

  // The window and granularity ARE the identity of this read, so they belong in the key: flipping
  // back to a range already looked at is served from cache, and the form no longer needs to ask for
  // a refresh -- changing a field changes the key, and the read follows.
  const marketplace = createResource(
    () => ['marketplace', since, until, granularity],
    () => {
      const params = new URLSearchParams({ granularity });
      if (since) params.set('since', new Date(since).toISOString());
      if (until) params.set('until', new Date(`${until}T23:59:59`).toISOString());

      return apiGet(`/api/v1/economy/marketplace?${params}`);
    }
  );

  let data = $derived(marketplace.data);

  let salesSeries = $derived(data
    ? [
        {
          name: $t('marketplace.salesVolume'),
          color: 'var(--accent)',
          points: (data.timeline || []).map((p) => ({ label: p.label, value: p.volume })),
        },
      ]
    : []);

  let countSeries = $derived(data
    ? [
        {
          name: $t('marketplace.salesCount'),
          color: 'var(--ok)',
          points: (data.timeline || []).map((p) => ({ label: p.label, value: p.sales })),
        },
      ]
    : []);
</script>

<section class="panel">
  <PageHeader title={$t('marketplace.title')} description={$t('marketplace.description')}>
    {#snippet actions()}
      <button type="button" onclick={marketplace.refresh} disabled={marketplace.loading}>{$t('common.refresh')}</button>
    {/snippet}
  </PageHeader>

  <!-- Changing a field already re-reads (it changes the key), so this button no longer means "apply
       the filters" -- it means "read again now", which is why it invalidates rather than refetches
       blindly. -->
  <form class="toolbar-grid" onsubmit={(event) => { event.preventDefault(); marketplace.refresh(); }}>
    <label>
      {$t('common.since')}
      <input type="date" bind:value={since} />
    </label>
    <label>
      {$t('common.until')}
      <input type="date" bind:value={until} />
    </label>
    <label>
      {$t('common.granularity')}
      <select bind:value={granularity}>
        {#each granularities as g}
          <option value={g}>{granularityLabel(g, $t)}</option>
        {/each}
      </select>
    </label>
  </form>

  {#if marketplace.loading}
    <p class="muted">{$t('marketplace.loading')}</p>
  {:else if marketplace.forbidden}
    <AccessDeniedNotice message={$t('marketplace.accessDenied')} />
  {:else if marketplace.error}
    <p class="empty-state danger">{marketplace.error}</p>
  {/if}
</section>

{#if data}
  <div class="metric-grid" style="margin-top: 12px;">
    <StatCard label={$t('marketplace.activeListings')} value={formatNumber(data.totals.activeListings)}>
      {#snippet icon()}
        <ShoppingCart size={15} strokeWidth={2} aria-hidden="true" />
      {/snippet}
    </StatCard>
    <StatCard label={$t('marketplace.soldWindow')} value={formatNumber(data.totals.soldCount)}>
      {#snippet icon()}
        <ShoppingBag size={15} strokeWidth={2} aria-hidden="true" />
      {/snippet}
    </StatCard>
    <StatCard label={$t('marketplace.volumeCredits')} value={formatNumber(data.totals.totalVolume)} accent>
      {#snippet icon()}
        <Coins size={15} strokeWidth={2} aria-hidden="true" />
      {/snippet}
    </StatCard>
    <StatCard label={$t('marketplace.averagePrice')} value={formatNumber(data.totals.averagePrice, 1)} accent>
      {#snippet icon()}
        <Coins size={15} strokeWidth={2} aria-hidden="true" />
      {/snippet}
    </StatCard>
  </div>

  <div class="split-grid" style="margin-top: 12px;">
    <div class="panel">
      <div class="panel-head"><h2>{$t('marketplace.volumePer', { granularity: granularityLabel(granularity, $t) })}</h2></div>
      <LineChart series={salesSeries} valueFormatter={(v) => formatNumber(v)} />
    </div>
    <div class="panel">
      <div class="panel-head"><h2>{$t('marketplace.salesPer', { granularity: granularityLabel(granularity, $t) })}</h2></div>
      <LineChart series={countSeries} valueFormatter={(v) => formatNumber(v)} />
    </div>
  </div>

  <div class="panel" style="margin-top: 12px;">
    <h3>{$t('marketplace.topSellers')}</h3>
    <table>
      <thead><tr><th>{$t('marketplace.colSeller')}</th><th>{$t('marketplace.colSales')}</th><th>{$t('marketplace.colVolume')}</th></tr></thead>
      <tbody>
        {#each data.topSellers || [] as row}
          <tr>
            <td><EntityLink id={row.sellerId} label={row.sellerName || `player #${row.sellerId}`} {openPlayer} {openItem} /></td>
            <td>{formatNumber(row.sales)}</td>
            <td>{formatNumber(row.volume)}</td>
          </tr>
        {:else}
          <tr><td colspan="3" class="muted">{$t('marketplace.noSales')}</td></tr>
        {/each}
      </tbody>
    </table>
  </div>
{/if}
