<script>
  import { onMount } from 'svelte';
  import { apiGet } from '../lib/api.js';
  import { formatNumber } from '../lib/format.js';
  import EntityLink from '../components/EntityLink.svelte';
  import AccessDeniedNotice from '../components/AccessDeniedNotice.svelte';
  import StatCard from '../components/StatCard.svelte';
  import LineChart from '../components/LineChart.svelte';
  import EmptyState from '../components/EmptyState.svelte';
  import { isPermissionDeniedError } from '../lib/permissions.js';
  import { openPlayer, openItem } from '../lib/session.js';
  import { t, locale } from '../lib/i18n.js';
  import { get } from 'svelte/store';
  import { Activity, Users, DoorOpen, Sparkles, Cpu, Gauge, TriangleAlert, Timer } from '@lucide/svelte';

  let data = null;
  let error = '';
  let forbidden = false;
  let trend = [];
  const maxTrendSamples = 20;

  function addTrendSample(snapshot) {
    const packetRate = Number(snapshot?.live?.packetsPerSecond ?? 0);
    const errorRate = Number(snapshot?.live?.errorsPerMinute ?? 0);
    const p50 = Number(snapshot?.live?.latencyP50Ms ?? 0);
    const label = new Date().toLocaleTimeString(get(locale) === 'fr' ? 'fr-FR' : 'en-US', {
      hour: '2-digit',
      minute: '2-digit',
      second: '2-digit',
    });

    trend = [
      ...trend.slice(-1 * (maxTrendSamples - 1)),
      { label, packetRate, errorRate, latencyP50: p50, at: Date.now() },
    ];
  }

  // The live trend feeds the shared LineChart (one charting primitive across the whole dashboard)
  // instead of the old hand-rolled bar rows + empty donut. Sky accent matches the token palette.
  $: packetsSeries = [
    {
      name: $t('overview.packetsPerSec'),
      color: '#4fb0e6',
      points: trend.map((entry) => ({ label: entry.label, value: entry.packetRate })),
    },
  ];

  async function refresh() {
    forbidden = false;
    error = '';
    try {
      data = await apiGet('/api/v1/monitoring/overview');
      addTrendSample(data);
    } catch (err) {
      if (isPermissionDeniedError(err)) {
        forbidden = true;
        data = null;
        return;
      }

      error = err.message;
    }
  }

  onMount(() => {
    void refresh();
    const interval = setInterval(refresh, 10000);
    return () => clearInterval(interval);
  });
</script>

<section class="panel">
  <div class="panel-head">
    <h2>{$t('overview.title')}</h2>
    <button type="button" on:click={refresh}>{$t('common.refresh')}</button>
  </div>

  {#if forbidden}
    <AccessDeniedNotice message={$t('overview.accessDenied')} />
  {:else if error}
    <EmptyState kind="error" message={error} />
  {/if}

  <div class="stats">
    <StatCard label={$t('overview.status')} value={data?.status || '-'}>
      <Activity slot="icon" size={15} strokeWidth={2} aria-hidden="true" />
    </StatCard>
    <StatCard label={$t('overview.sessions')} value={data?.activeSessions ?? '-'}>
      <Users slot="icon" size={15} strokeWidth={2} aria-hidden="true" />
    </StatCard>
    <StatCard label={$t('overview.rooms')} value={data?.activeRooms ?? '-'}>
      <DoorOpen slot="icon" size={15} strokeWidth={2} aria-hidden="true" />
    </StatCard>
    <StatCard accent label={$t('overview.club')} value={data?.activeClubSubscribers ?? '-'}>
      <Sparkles slot="icon" size={15} strokeWidth={2} aria-hidden="true" />
    </StatCard>
    <StatCard label={$t('overview.memory')} value={data?.managedMemoryMb ?? '-'}>
      <Cpu slot="icon" size={15} strokeWidth={2} aria-hidden="true" />
    </StatCard>
    <StatCard label={$t('overview.packetsPerSec')} value={formatNumber(data?.live?.packetsPerSecond, 2)}>
      <Gauge slot="icon" size={15} strokeWidth={2} aria-hidden="true" />
    </StatCard>
    <StatCard label={$t('overview.errorsPerMin')} value={formatNumber(data?.live?.errorsPerMinute, 2)}>
      <TriangleAlert slot="icon" size={15} strokeWidth={2} aria-hidden="true" />
    </StatCard>
    <StatCard label={$t('overview.latencyP50')} value={`${formatNumber(data?.live?.latencyP50Ms, 2)} ms`}>
      <Timer slot="icon" size={15} strokeWidth={2} aria-hidden="true" />
    </StatCard>
    <StatCard label={$t('overview.latencyP95')} value={`${formatNumber(data?.live?.latencyP95Ms, 2)} ms`}>
      <Timer slot="icon" size={15} strokeWidth={2} aria-hidden="true" />
    </StatCard>
  </div>
</section>

<section class="panel">
  <div class="panel-head">
    <h2>{$t('overview.liveTrend')}</h2>
    <small class="muted">{$t('overview.lastTicks', { count: trend.length || 0 })}</small>
  </div>

  {#if trend.length === 0}
    <EmptyState kind="loading" message={$t('overview.waitingForTrend')} />
  {:else}
    <LineChart series={packetsSeries} height={220} valueFormatter={(v) => formatNumber(v, 2)} />
  {/if}
</section>

<section class="split-grid">
  <div class="panel">
    <h2>{$t('overview.topAbusers')}</h2>
    <div class="table-wrap">
      <table>
        <thead><tr><th>{$t('overview.colPlayer')}</th><th>{$t('overview.colPacketsMin')}</th></tr></thead>
        <tbody>
          {#each data?.live?.topAbusers || [] as row}
            <tr>
              <td><EntityLink id={row.playerId} label={`player #${row.playerId}`} {openPlayer} {openItem} /></td>
              <td class="num-cell">{formatNumber(row.packetsPerMinute, 2)}</td>
            </tr>
          {:else}
            <tr><td colspan="2" class="muted">{$t('overview.noAbuseSignal')}</td></tr>
          {/each}
        </tbody>
      </table>
    </div>
  </div>

  <div class="panel">
    <h2>{$t('overview.topRooms')}</h2>
    <div class="table-wrap">
      <table>
        <thead><tr><th>{$t('overview.colRoom')}</th><th>{$t('overview.colPacketsMin')}</th></tr></thead>
        <tbody>
          {#each data?.live?.topRooms || [] as row}
            <tr><td>room #{row.roomId}</td><td class="num-cell">{formatNumber(row.packetsPerMinute, 2)}</td></tr>
          {:else}
            <tr><td colspan="2" class="muted">{$t('overview.noRoomTraffic')}</td></tr>
          {/each}
        </tbody>
      </table>
    </div>
  </div>
</section>

<style>
  /* Numeric columns line up on the condensed counter face, like the rest of the dashboard. */
  .num-cell {
    font-family: var(--numerals);
    font-variant-numeric: tabular-nums;
  }
</style>
