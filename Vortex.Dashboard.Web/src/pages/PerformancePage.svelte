<script>
  // Room contention: what the tick actually spends its time on, step by step, plus the latency of
  // the calls every room makes to the (single, global) room directory grain.
  //
  // The API returns the current rolling window, not a time series -- the server keeps samples, not
  // history. The chart's history is therefore accumulated client-side by polling, which is also why
  // it starts empty and fills in: one point per refresh, capped at CHART_POINTS.
  import { onMount } from 'svelte';
  import { apiGet } from '../lib/api.js';
  import { formatNumber } from '../lib/format.js';
  import AccessDeniedNotice from '../components/AccessDeniedNotice.svelte';
  import EmptyState from '../components/EmptyState.svelte';
  import StatCard from '../components/StatCard.svelte';
  import LineChart from '../components/LineChart.svelte';
  import { Timer, Activity, Repeat } from '@lucide/svelte';
  import { isPermissionDeniedError } from '../lib/permissions.js';
  import { t } from '../lib/i18n.js';

  const REFRESH_MS = 5000;
  const CHART_POINTS = 60;

  // Fixed palette so a step keeps its colour across refreshes even as rows reorder by cost.
  const STEP_COLORS = [
    'var(--accent)',
    '#e0b341',
    '#4fb3d9',
    '#8f7ae5',
    '#5bc98c',
    '#e07a5f',
    '#c46fa8',
    '#7f9cc4',
    '#d4a373',
    '#6fbfa8',
  ];

  let data = null;
  let error = '';
  let forbidden = false;
  let history = [];

  $: steps = data?.steps || [];
  $: directoryCalls = data?.directoryCalls || [];
  $: tick = data?.tick || null;
  $: windowSeconds = data?.windowSeconds || 0;

  // A window with zero ticks means nothing is loaded or metrics are off -- distinguish the two so
  // the operator is not left guessing which.
  $: idle = (tick?.count || 0) === 0 && steps.length === 0;

  $: colorFor = (name) => {
    const index = stepOrder.indexOf(name);
    return STEP_COLORS[(index < 0 ? 0 : index) % STEP_COLORS.length];
  };

  // Assignment order, not cost order, so colours are stable while the table re-sorts.
  let stepOrder = [];

  $: chartSeries = stepOrder
    .filter((name) => history.some((point) => point.values[name] !== undefined))
    .map((name) => ({
      name,
      color: colorFor(name),
      points: history.map((point) => ({
        label: point.label,
        value: point.values[name] ?? 0,
      })),
    }));

  async function refresh() {
    forbidden = false;
    error = '';

    try {
      const next = await apiGet('/api/v1/monitoring/room-performance');
      data = next;

      for (const step of next.steps || []) {
        if (!stepOrder.includes(step.name)) {
          stepOrder = [...stepOrder, step.name];
        }
      }

      const values = {};
      for (const step of next.steps || []) {
        values[step.name] = Number(step.p95Ms || 0);
      }

      history = [
        ...history,
        { label: new Date().toLocaleTimeString(), values },
      ].slice(-CHART_POINTS);
    } catch (err) {
      if (isPermissionDeniedError(err)) {
        forbidden = true;
        error = '';
        return;
      }

      error = err.message;
    }
  }

  onMount(() => {
    void refresh();
    const interval = setInterval(refresh, REFRESH_MS);
    return () => clearInterval(interval);
  });
</script>

<section class="panel">
  <div class="panel-head">
    <div>
      <p class="eyebrow">{$t('nav.performanceShort')}</p>
      <h2>{$t('performance.title')}</h2>
    </div>
    <button type="button" on:click={refresh}>{$t('common.refresh')}</button>
  </div>

  {#if forbidden}
    <AccessDeniedNotice message={$t('performance.accessDenied')} />
  {:else if error}
    <p class="empty-state danger">{error}</p>
  {:else}
    <p class="muted">{$t('performance.description', { seconds: windowSeconds })}</p>

    <div class="metric-grid compact">
      <StatCard label={$t('performance.tickP50')}>
        <Timer slot="icon" size={15} strokeWidth={2} aria-hidden="true" />
        <span slot="value">{formatNumber(tick?.p50Ms, 2)} ms</span>
      </StatCard>
      <StatCard label={$t('performance.tickP95')}>
        <Timer slot="icon" size={15} strokeWidth={2} aria-hidden="true" />
        <span slot="value">{formatNumber(tick?.p95Ms, 2)} ms</span>
      </StatCard>
      <StatCard label={$t('performance.tickP99')}>
        <Timer slot="icon" size={15} strokeWidth={2} aria-hidden="true" />
        <span slot="value">{formatNumber(tick?.p99Ms, 2)} ms</span>
      </StatCard>
      <StatCard label={$t('performance.tickCount')} value={formatNumber(tick?.count)}>
        <Activity slot="icon" size={15} strokeWidth={2} aria-hidden="true" />
      </StatCard>
    </div>
  {/if}
</section>

{#if !forbidden && !error}
  <section class="panel">
    <h2>{$t('performance.chartTitle')}</h2>
    <LineChart
      series={chartSeries}
      valueFormatter={(v) => `${formatNumber(v, 2)} ms`}
      emptyMessage={$t('performance.noSteps')}
    />
  </section>

  <section class="panel">
    <h2>{$t('performance.stepsTitle')}</h2>

    {#if idle}
      <EmptyState message={$t('performance.noSteps')} />
    {:else}
      <div class="bar-chart">
        {#each steps as row}
          <div class="bar-row">
            <div class="bar-label"><code>{row.name}</code></div>
            <div class="bar-track">
              <div
                class="bar-fill"
                style={`background:${colorFor(row.name)}; width:${Math.min(100, Number(row.shareOfTickPercent || 0))}%;`}
              ></div>
            </div>
            <span class="muted">{formatNumber(row.shareOfTickPercent, 1)}%</span>
          </div>
        {/each}
      </div>

      <table>
        <thead>
          <tr>
            <th>{$t('performance.colStep')}</th>
            <th>{$t('performance.colCount')}</th>
            <th>{$t('performance.colP50')}</th>
            <th>{$t('performance.colP95')}</th>
            <th>{$t('performance.colP99')}</th>
            <th>{$t('performance.colSum')}</th>
            <th>{$t('performance.colShare')}</th>
          </tr>
        </thead>
        <tbody>
          {#each steps as row}
            <tr>
              <td><code>{row.name}</code></td>
              <td>{formatNumber(row.count)}</td>
              <td>{formatNumber(row.p50Ms, 2)} ms</td>
              <td>{formatNumber(row.p95Ms, 2)} ms</td>
              <td>{formatNumber(row.p99Ms, 2)} ms</td>
              <td>{formatNumber(row.sumMs, 1)} ms</td>
              <td>{formatNumber(row.shareOfTickPercent, 1)}%</td>
            </tr>
          {/each}
        </tbody>
      </table>
    {/if}
  </section>

  <section class="panel">
    <h2>{$t('performance.directoryTitle')}</h2>
    <table>
      <thead>
        <tr>
          <th>{$t('performance.colMethod')}</th>
          <th>{$t('performance.colCount')}</th>
          <th>{$t('performance.colP50')}</th>
          <th>{$t('performance.colP95')}</th>
          <th>{$t('performance.colP99')}</th>
        </tr>
      </thead>
      <tbody>
        {#each directoryCalls as row}
          <tr>
            <td><code>{row.name}</code></td>
            <td>{formatNumber(row.count)}</td>
            <td>{formatNumber(row.p50Ms, 2)} ms</td>
            <td>{formatNumber(row.p95Ms, 2)} ms</td>
            <td>{formatNumber(row.p99Ms, 2)} ms</td>
          </tr>
        {:else}
          <tr>
            <td colspan="5" class="muted">{$t('performance.noDirectoryCalls')}</td>
          </tr>
        {/each}
      </tbody>
    </table>
  </section>
{/if}

<style>
  .bar-label code {
    font-size: 0.85em;
  }
</style>
