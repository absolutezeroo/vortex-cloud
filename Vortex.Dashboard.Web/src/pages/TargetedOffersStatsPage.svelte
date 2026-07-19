<script>
  import { onMount } from 'svelte';
  import { apiGet } from '../lib/api.js';
  import { formatNumber } from '../lib/format.js';
  import { isPermissionDeniedError } from '../lib/permissions.js';
  import AccessDeniedNotice from '../components/AccessDeniedNotice.svelte';
  import LineChart from '../components/LineChart.svelte';
  import AssetImage from '../components/AssetImage.svelte';
  import { Package } from '@lucide/svelte';
  import { t } from '../lib/i18n.js';

  const granularities = ['day', 'month', 'year'];

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
    if (since) params.set('since', new Date(`${since}T00:00:00`).toISOString());
    if (until) params.set('until', new Date(`${until}T23:59:59`).toISOString());

    try {
      data = await apiGet(`/api/targeted-offers/stats?${params}`);
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

  $: purchaseSeries = data
    ? [
        {
          name: $t('targetedOffersStats.purchaseCount'),
          color: 'var(--accent)',
          points: (data.timeline || []).map((p) => ({ label: p.label, value: p.purchaseCount })),
        },
      ]
    : [];

  $: creditsSeries = data
    ? [
        {
          name: $t('targetedOffersStats.totalCreditsSpent'),
          color: 'var(--warning)',
          points: (data.timeline || []).map((p) => ({ label: p.label, value: p.creditsSpent })),
        },
      ]
    : [];

  onMount(() => {
    setDefaultWindow();
    void refresh();
  });
</script>

<section class="panel">
  <div class="panel-head"><h2>{$t('targetedOffersStats.title')}</h2></div>
  <p class="muted">{$t('targetedOffersStats.description')}</p>

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
    <p class="muted">{$t('common.loading')}</p>
  {:else if forbidden}
    <AccessDeniedNotice message={$t('targetedOffersStats.accessDenied')} />
  {:else if error}
    <p class="empty-state danger">{error}</p>
  {/if}
</section>

{#if data}
  <div class="metric-grid" style="margin-top: 12px;">
    <article><span>{$t('targetedOffersStats.purchaseCount')}</span><strong>{formatNumber(data.totals.purchaseCount)}</strong></article>
    <article><span>{$t('targetedOffersStats.totalCreditsSpent')}</span><strong>{formatNumber(data.totals.totalCreditsSpent)}</strong></article>
    <article><span>{$t('targetedOffersStats.totalActivityPointsSpent')}</span><strong>{formatNumber(data.totals.totalActivityPointsSpent)}</strong></article>
    <article><span>{$t('targetedOffersStats.totalQuantity')}</span><strong>{formatNumber(data.totals.totalQuantity)}</strong></article>
  </div>

  <div class="panel" style="margin-top: 12px;">
    <div class="panel-head"><h2>{$t('targetedOffersStats.timelineTitle', { granularity: granularityLabel(granularity, $t) })}</h2></div>
    <LineChart series={purchaseSeries} valueFormatter={(v) => formatNumber(v)} />
  </div>

  <div class="panel" style="margin-top: 12px;">
    <div class="panel-head"><h2>{$t('targetedOffersStats.creditsTimelineTitle', { granularity: granularityLabel(granularity, $t) })}</h2></div>
    <LineChart series={creditsSeries} valueFormatter={(v) => formatNumber(v)} />
  </div>

  <div class="panel" style="margin-top: 12px;">
    <div class="panel-head"><h2>{$t('targetedOffersStats.topOffersTitle')}</h2></div>
    <div class="table-wrap">
      <table>
        <thead>
          <tr>
            <th>{$t('targetedOffersStats.colOffer')}</th>
            <th>{$t('targetedOffersStats.colPurchases')}</th>
            <th>{$t('targetedOffersStats.colQuantity')}</th>
            <th>{$t('targetedOffersStats.colCredits')}</th>
            <th>{$t('targetedOffersStats.colActivityPoints')}</th>
          </tr>
        </thead>
        <tbody>
          {#each data.topOffers || [] as row}
            <tr>
              <td>
                <span style="display: inline-flex; align-items: center; gap: 8px;">
                  <AssetImage src={row.furniIconUrl} alt={row.offerName} size={26} fallbackIcon={Package} />
                  <span>{row.offerName}</span>
                </span>
              </td>
              <td>{formatNumber(row.purchaseCount)}</td>
              <td>{formatNumber(row.quantity)}</td>
              <td>{formatNumber(row.creditsSpent)}</td>
              <td>{formatNumber(row.activityPointsSpent)}</td>
            </tr>
          {:else}
            <tr><td colspan="5" class="muted">{$t('targetedOffersStats.noPurchases')}</td></tr>
          {/each}
        </tbody>
      </table>
    </div>
  </div>
{/if}
