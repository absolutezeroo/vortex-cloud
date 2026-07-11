<script>
  import { onMount } from 'svelte';
  import { apiGet } from '../lib/api.js';
  import { formatNumber } from '../lib/format.js';
  import { isPermissionDeniedError } from '../lib/permissions.js';
  import AccessDeniedNotice from '../components/AccessDeniedNotice.svelte';
  import LineChart from '../components/LineChart.svelte';

  const granularities = [
    { value: 'day', label: 'Day' },
    { value: 'month', label: 'Month' },
    { value: 'year', label: 'Year' },
  ];

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
    if (since) params.set('since', new Date(since).toISOString());
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
      ? 'Uncategorized (no matching audit event)'
      : action.replaceAll('.', ' ').replaceAll('_', ' ');
  }

  onMount(() => {
    setDefaultWindow();
    void refresh();
  });
</script>

<section class="panel">
  <div class="panel-head"><h2>Spend trends</h2></div>
  <p class="muted">
    How much of each currency was spent vs earned, per day/month/year. Currency names come from
    this hotel's own <code>currency_types</code> configuration (credits, and whatever your
    secondary/activity currencies are named).
  </p>

  <form class="toolbar-grid" on:submit|preventDefault={refresh}>
    <label>
      Since
      <input type="date" bind:value={since} />
    </label>
    <label>
      Until
      <input type="date" bind:value={until} />
    </label>
    <label>
      Granularity
      <select bind:value={granularity}>
        {#each granularities as g}
          <option value={g.value}>{g.label}</option>
        {/each}
      </select>
    </label>
    <button type="submit" disabled={loading}>Refresh</button>
  </form>

  {#if loading}
    <p class="muted">Loading trends...</p>
  {:else if forbidden}
    <AccessDeniedNotice message="Vous n'avez pas l'autorisation d'accéder aux tendances économiques." />
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
        <small>spent · {formatNumber(data.totals[currency]?.earned || 0)} earned · {formatNumber(data.totals[currency]?.transactionCount || 0)} txns</small>
      </article>
    {/each}
  </div>

  <div class="panel" style="margin-top: 12px;">
    <div class="panel-head"><h2>Spent per {granularity}</h2></div>
    <LineChart series={spendSeries} valueFormatter={(v) => formatNumber(v)} />
  </div>

  <div class="panel" style="margin-top: 12px;">
    <div class="panel-head"><h2>Earned per {granularity}</h2></div>
    <LineChart series={earnedSeries} valueFormatter={(v) => formatNumber(v)} />
  </div>

  <div class="panel" style="margin-top: 12px;">
    <div class="panel-head"><h2>Spent on what</h2></div>
    <p class="muted">
      Attributed by matching each debit's correlation id to the audit event of the operation that
      caused it (catalog purchase, marketplace, LTD raffle, admin grant, ...). "Uncategorized" means
      no matching audit event was found for that debit.
    </p>
    <table>
      <thead><tr><th>Source</th><th>Currency</th><th>Spent</th><th>Transactions</th></tr></thead>
      <tbody>
        {#each categories as row}
          <tr>
            <td>{actionLabel(row.action)}</td>
            <td>{row.currency}</td>
            <td>{formatNumber(row.spend)}</td>
            <td>{formatNumber(row.transactionCount)}</td>
          </tr>
        {:else}
          <tr><td colspan="4" class="muted">No spend in this window.</td></tr>
        {/each}
      </tbody>
    </table>
  </div>
{:else if !loading && !forbidden && !error}
  <p class="empty-state" style="margin-top: 12px;">No economy activity in this window.</p>
{/if}
