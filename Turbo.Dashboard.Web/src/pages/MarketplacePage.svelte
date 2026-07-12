<script>
  import { onMount } from 'svelte';
  import { apiGet } from '../lib/api.js';
  import { formatNumber } from '../lib/format.js';
  import { isPermissionDeniedError } from '../lib/permissions.js';
  import AccessDeniedNotice from '../components/AccessDeniedNotice.svelte';
  import EntityLink from '../components/EntityLink.svelte';
  import LineChart from '../components/LineChart.svelte';
  import { openPlayer, openItem } from '../lib/session.js';
  import { t } from '../lib/i18n.js';

  const granularities = ['day', 'month', 'year'];

  // See the AuditPage `categoryLabel` note: translator must be passed explicitly so template call
  // sites stay reactive to locale changes.
  function granularityLabel(value, translator) {
    return translator(`common.granularity${value.charAt(0).toUpperCase()}${value.slice(1)}`);
  }

  let since = '';
  let until = '';
  let granularity = 'day';
  let loading = false;
  let forbidden = false;
  let error = '';
  let data = null;

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

  async function refresh() {
    loading = true;
    error = '';
    forbidden = false;

    const params = new URLSearchParams({ granularity });
    if (since) params.set('since', new Date(since).toISOString());
    if (until) params.set('until', new Date(`${until}T23:59:59`).toISOString());

    try {
      data = await apiGet(`/api/v1/economy/marketplace?${params}`);
    } catch (err) {
      if (isPermissionDeniedError(err)) {
        forbidden = true;
        data = null;
        return;
      }

      error = err.message;
      data = null;
    } finally {
      loading = false;
    }
  }

  $: salesSeries = data
    ? [
        {
          name: $t('marketplace.salesVolume'),
          color: 'var(--accent)',
          points: (data.timeline || []).map((p) => ({ label: p.label, value: p.volume })),
        },
      ]
    : [];

  $: countSeries = data
    ? [
        {
          name: $t('marketplace.salesCount'),
          color: 'var(--ok)',
          points: (data.timeline || []).map((p) => ({ label: p.label, value: p.sales })),
        },
      ]
    : [];

  onMount(() => {
    setDefaultWindow();
    void refresh();
  });
</script>

<section class="panel">
  <div class="panel-head"><h2>{$t('marketplace.title')}</h2></div>
  <p class="muted">{$t('marketplace.description')}</p>

  <form class="toolbar-grid" on:submit|preventDefault={refresh}>
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
    <button type="submit" disabled={loading}>{$t('common.refresh')}</button>
  </form>

  {#if loading}
    <p class="muted">{$t('marketplace.loading')}</p>
  {:else if forbidden}
    <AccessDeniedNotice message={$t('marketplace.accessDenied')} />
  {:else if error}
    <p class="empty-state danger">{error}</p>
  {/if}
</section>

{#if data}
  <div class="metric-grid" style="margin-top: 12px;">
    <article><span>{$t('marketplace.activeListings')}</span><strong>{formatNumber(data.totals.activeListings)}</strong></article>
    <article><span>{$t('marketplace.soldWindow')}</span><strong>{formatNumber(data.totals.soldCount)}</strong></article>
    <article><span>{$t('marketplace.volumeCredits')}</span><strong>{formatNumber(data.totals.totalVolume)}</strong></article>
    <article><span>{$t('marketplace.averagePrice')}</span><strong>{formatNumber(data.totals.averagePrice, 1)}</strong></article>
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
