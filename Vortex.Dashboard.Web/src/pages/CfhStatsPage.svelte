<script>
  import { onMount } from 'svelte';
  import { apiGet } from '../lib/api.js';
  import { formatNumber } from '../lib/format.js';
  import { isPermissionDeniedError } from '../lib/permissions.js';
  import { openPlayer } from '../lib/session.js';
  import AccessDeniedNotice from '../components/AccessDeniedNotice.svelte';
  import EntityLink from '../components/EntityLink.svelte';
  import LineChart from '../components/LineChart.svelte';
  import { t } from '../lib/i18n.js';

  const granularities = ['day', 'month', 'year'];

  function granularityLabel(value, translator) {
    return translator(`common.granularity${value.charAt(0).toUpperCase()}${value.slice(1)}`);
  }

  const closeReasonKeys = {
    Useless: 'cfhStats.reasonUseless',
    Sanctioned: 'cfhStats.reasonSanctioned',
    Resolved: 'cfhStats.reasonResolved',
  };

  function closeReasonLabel(value, translator) {
    return translator(closeReasonKeys[value] || value);
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
      data = await apiGet(`/api/v1/cfh/stats?${params}`);
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

  $: timelineSeries = data
    ? [
        {
          name: $t('cfhStats.totalTickets'),
          color: 'var(--accent)',
          points: (data.timeline || []).map((p) => ({ label: p.label, value: p.ticketsCreated })),
        },
      ]
    : [];

  $: sanctionRatePercent = data ? Math.round(data.totals.sanctionRate * 1000) / 10 : 0;

  onMount(() => {
    setDefaultWindow();
    void refresh();
  });
</script>

<section class="panel">
  <div class="panel-head"><h2>{$t('cfhStats.title')}</h2></div>
  <p class="muted">{$t('cfhStats.description')}</p>

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
    <AccessDeniedNotice message={$t('cfhStats.accessDenied')} />
  {:else if error}
    <p class="empty-state danger">{error}</p>
  {/if}
</section>

{#if data}
  <div class="metric-grid" style="margin-top: 12px;">
    <article><span>{$t('cfhStats.totalTickets')}</span><strong>{formatNumber(data.totals.totalTickets)}</strong></article>
    <article><span>{$t('cfhStats.openCount')}</span><strong>{formatNumber(data.totals.openCount)}</strong></article>
    <article><span>{$t('cfhStats.pickedCount')}</span><strong>{formatNumber(data.totals.pickedCount)}</strong></article>
    <article><span>{$t('cfhStats.closedCount')}</span><strong>{formatNumber(data.totals.closedCount)}</strong></article>
    <article><span>{$t('cfhStats.sanctionedCount')}</span><strong>{formatNumber(data.totals.sanctionedCount)}</strong></article>
    <article><span>{$t('cfhStats.sanctionRate')}</span><strong>{sanctionRatePercent}%</strong></article>
    <article><span>{$t('cfhStats.avgResolutionMinutes')}</span><strong>{data.totals.avgResolutionMinutes}</strong></article>
  </div>

  <div class="panel" style="margin-top: 12px;">
    <div class="panel-head"><h2>{$t('cfhStats.timelineTitle', { granularity: granularityLabel(granularity, $t) })}</h2></div>
    <LineChart series={timelineSeries} valueFormatter={(v) => formatNumber(v)} />
  </div>

  <div class="panel" style="margin-top: 12px;">
    <div class="panel-head"><h2>{$t('cfhStats.byCloseReasonTitle')}</h2></div>
    <div class="table-wrap">
      <table>
        <thead><tr><th>{$t('cfhStats.colReason')}</th><th>{$t('cfhStats.colCount')}</th></tr></thead>
        <tbody>
          {#each data.byCloseReason || [] as row}
            <tr><td>{closeReasonLabel(row.reason, $t)}</td><td>{formatNumber(row.count)}</td></tr>
          {:else}
            <tr><td colspan="2" class="muted">{$t('cfhStats.noTickets')}</td></tr>
          {/each}
        </tbody>
      </table>
    </div>
  </div>

  <div class="panel" style="margin-top: 12px;">
    <div class="panel-head"><h2>{$t('cfhStats.topTopicsTitle')}</h2></div>
    <div class="table-wrap">
      <table>
        <thead><tr><th>{$t('cfhStats.colTopic')}</th><th>{$t('cfhStats.colCount')}</th></tr></thead>
        <tbody>
          {#each data.topTopics || [] as row}
            <tr><td>{row.topicName}</td><td>{formatNumber(row.count)}</td></tr>
          {:else}
            <tr><td colspan="2" class="muted">{$t('cfhStats.noTickets')}</td></tr>
          {/each}
        </tbody>
      </table>
    </div>
  </div>

  <div class="panel" style="margin-top: 12px;">
    <div class="panel-head"><h2>{$t('cfhStats.topReportedTitle')}</h2></div>
    <div class="table-wrap">
      <table>
        <thead><tr><th>{$t('cfhStats.colPlayer')}</th><th>{$t('cfhStats.colReports')}</th></tr></thead>
        <tbody>
          {#each data.topReportedPlayers || [] as row}
            <tr>
              <td><EntityLink type="player" id={row.playerId} label={row.playerName} {openPlayer} /></td>
              <td>{formatNumber(row.reportCount)}</td>
            </tr>
          {:else}
            <tr><td colspan="2" class="muted">{$t('cfhStats.noTickets')}</td></tr>
          {/each}
        </tbody>
      </table>
    </div>
  </div>
{/if}
