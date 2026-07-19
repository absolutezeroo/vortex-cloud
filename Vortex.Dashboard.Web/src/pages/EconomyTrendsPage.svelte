<script>
  import { onMount } from 'svelte';
  import { apiGet } from '../lib/api.js';
  import { formatNumber } from '../lib/format.js';
  import { isPermissionDeniedError } from '../lib/permissions.js';
  import AccessDeniedNotice from '../components/AccessDeniedNotice.svelte';
  import LineChart from '../components/LineChart.svelte';
  import { t, translate } from '../lib/i18n.js';

  const granularities = ['day', 'month', 'year'];

  function granularityLabel(value, translator) {
    return translator(`common.granularity${value.charAt(0).toUpperCase()}${value.slice(1)}`);
  }

  const currencyColors = [
    'var(--accent)',
    'var(--ok)',
    'var(--warning)',
    'var(--danger)',
    '#9f6ce1',
  ];

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

  function colorFor(index) {
    return currencyColors[index % currencyColors.length];
  }

  async function refresh() {
    loading = true;
    error = '';
    forbidden = false;

    const params = new URLSearchParams({ granularity });
    // Both bounds are parsed as local time (no trailing "Z") so the picked calendar day maps to
    // the operator's own midnight/end-of-day, not UTC midnight -- mixing a bare "YYYY-MM-DD" (parsed
    // as UTC per spec) for `since` with a local "T23:59:59" for `until` skewed the window by the
    // local UTC offset, silently clipping or including hours the date pickers didn't show.
    if (since) params.set('since', new Date(`${since}T00:00:00`).toISOString());
    if (until) params.set('until', new Date(`${until}T23:59:59`).toISOString());

    try {
      data = await apiGet(`/api/v1/economy/trends?${params}`);
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

  $: currencies = data?.currencies || [];
  $: spendSeries = (data?.series || []).map((s, i) => ({
    name: s.currency,
    color: colorFor(i),
    points: (s.points || []).map((p) => ({ label: p.label, value: p.spend })),
  }));
  $: earnedSeries = (data?.series || []).map((s, i) => ({
    name: s.currency,
    color: colorFor(i),
    points: (s.points || []).map((p) => ({ label: p.label, value: p.earned })),
  }));
  $: categories = data?.categories || [];

  function actionLabel(action) {
    // Audit action keys are dotted machine names (e.g. "economy.catalog_purchase") — humanize them
    // rather than showing the raw key.
    return action === 'uncategorized'
      ? translate('economyTrends.uncategorized')
      : action.replaceAll('.', ' ').replaceAll('_', ' ');
  }

  onMount(() => {
    setDefaultWindow();
    void refresh();
  });
</script>

<section class="panel">
  <div class="panel-head"><h2>{$t('economyTrends.title')}</h2></div>
  <p class="muted">
    {$t('economyTrends.descriptionBefore')} <code>currency_types</code> {$t('economyTrends.descriptionAfter')}
  </p>

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
    <p class="muted">{$t('economyTrends.loading')}</p>
  {:else if forbidden}
    <AccessDeniedNotice message={$t('economyTrends.accessDenied')} />
  {:else if error}
    <p class="empty-state danger">{error}</p>
  {/if}
</section>

{#if currencies.length > 0}
  <div class="metric-grid" style="margin-top: 12px;">
    {#each currencies as currency, i}
      <article style={`border-left: 3px solid ${colorFor(i)};`}>
        <span>{currency}</span>
        <strong>{formatNumber(data.totals[currency]?.spend || 0)}</strong>
        <small>{$t('economyTrends.spentSuffix')} · {formatNumber(data.totals[currency]?.earned || 0)} {$t('economyTrends.earnedSuffix')} · {formatNumber(data.totals[currency]?.transactionCount || 0)} {$t('economyTrends.txnsSuffix')}</small>
      </article>
    {/each}
  </div>

  <div class="panel" style="margin-top: 12px;">
    <div class="panel-head"><h2>{$t('economyTrends.spentPer', { granularity: granularityLabel(granularity, $t) })}</h2></div>
    <LineChart series={spendSeries} valueFormatter={(v) => formatNumber(v)} />
  </div>

  <div class="panel" style="margin-top: 12px;">
    <div class="panel-head"><h2>{$t('economyTrends.earnedPer', { granularity: granularityLabel(granularity, $t) })}</h2></div>
    <LineChart series={earnedSeries} valueFormatter={(v) => formatNumber(v)} />
  </div>

  <div class="panel" style="margin-top: 12px;">
    <div class="panel-head"><h2>{$t('economyTrends.spentOnWhat')}</h2></div>
    <p class="muted">
      {$t('economyTrends.attributionNote')}
    </p>
    <div class="table-wrap">
      <table>
        <thead><tr><th>{$t('economyTrends.colSource')}</th><th>{$t('economyTrends.colCurrency')}</th><th>{$t('economyTrends.colSpent')}</th><th>{$t('economyTrends.colTransactions')}</th></tr></thead>
        <tbody>
          {#each categories as row}
            <tr>
              <td>{actionLabel(row.action)}</td>
              <td>{row.currency}</td>
              <td>{formatNumber(row.spend)}</td>
              <td>{formatNumber(row.transactionCount)}</td>
            </tr>
          {:else}
            <tr><td colspan="4" class="muted">{$t('economyTrends.noSpend')}</td></tr>
          {/each}
        </tbody>
      </table>
    </div>
  </div>
{:else if !loading && !forbidden && !error}
  <p class="empty-state" style="margin-top: 12px;">{$t('economyTrends.noActivity')}</p>
{/if}
