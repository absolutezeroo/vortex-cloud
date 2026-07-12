<script>
  import { onMount } from 'svelte';
  import { apiGet } from '../lib/api.js';
  import { formatNumber } from '../lib/format.js';
  import EntityLink from '../components/EntityLink.svelte';
  import AccessDeniedNotice from '../components/AccessDeniedNotice.svelte';
  import { isPermissionDeniedError } from '../lib/permissions.js';
  import { openPlayer, openItem } from '../lib/session.js';
  import { t, locale } from '../lib/i18n.js';
  import { get } from 'svelte/store';

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

  $: packetsScale = Math.max(
    1,
    ...trend.map((entry) => entry.packetRate),
  );
  $: errorsScale = Math.max(
    1,
    ...trend.map((entry) => entry.errorRate),
  );
  $: latencyScale = Math.max(
    1,
    ...trend.map((entry) => entry.latencyP50),
  );

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
    <AccessDeniedNotice
      message={$t('overview.accessDenied')}
    />
  {:else if error}
    <p class="empty-state danger">{error}</p>
  {/if}

  <div class="metric-grid">
    <article><span>{$t('overview.status')}</span><strong>{data?.status || '-'}</strong><small>{$t('overview.globalState')}</small></article>
    <article><span>{$t('overview.sessions')}</span><strong>{data?.activeSessions ?? '-'}</strong><small>{$t('overview.connectedClients')}</small></article>
    <article><span>{$t('overview.rooms')}</span><strong>{data?.activeRooms ?? '-'}</strong><small>{$t('overview.activeRooms')}</small></article>
    <article><span>{$t('overview.club')}</span><strong>{data?.activeClubSubscribers ?? '-'}</strong><small>{$t('overview.activeSubscribers')}</small></article>
    <article><span>{$t('overview.memory')}</span><strong>{data?.managedMemoryMb ?? '-'}</strong><small>{$t('overview.managedMb')}</small></article>
    <article><span>{$t('overview.packetsPerSec')}</span><strong>{formatNumber(data?.live?.packetsPerSecond, 2)}</strong><small>{$t('overview.liveTraffic')}</small></article>
    <article><span>{$t('overview.errorsPerMin')}</span><strong>{formatNumber(data?.live?.errorsPerMinute, 2)}</strong><small>{$t('overview.handlerFailures')}</small></article>
    <article><span>{$t('overview.latencyP50')}</span><strong>{formatNumber(data?.live?.latencyP50Ms, 2)} ms</strong><small>{$t('overview.median')}</small></article>
    <article><span>{$t('overview.latencyP95')}</span><strong>{formatNumber(data?.live?.latencyP95Ms, 2)} ms</strong><small>{$t('overview.tail')}</small></article>
  </div>
</section>

<section class="panel">
  <div class="panel-head">
    <h2>{$t('overview.liveTrend')}</h2>
    <small class="muted">{$t('overview.lastTicks', { count: trend.length || 0 })}</small>
  </div>

  {#if trend.length === 0}
    <p class="empty-state">{$t('overview.waitingForTrend')}</p>
  {:else}
    <div class="split-grid">
      <article class="chart-wrap">
        <div class="donut" style="border-radius: 999px;" aria-label="Packets/sec trend"></div>
        <div>
          <p class="muted">{$t('overview.packetsPerSec')}</p>
          <strong>{formatNumber(data?.live?.packetsPerSecond, 2) || '-'}</strong>
          <small class="muted">{$t('overview.currentSample', { label: trend.at(-1)?.label ?? '-' })}</small>
        </div>
      </article>

      <div class="bar-chart">
        {#each trend as point}
          <div class="bar-row">
            <div class="bar-label">{point.label}</div>
            <div class="bar-track">
              <div
                class="bar-fill"
                style={`width: ${packetsScale > 0 ? (point.packetRate / packetsScale) * 100 : 0}%;`}
              ></div>
            </div>
            <span class="muted">{formatNumber(point.packetRate, 2)}</span>
          </div>
        {/each}
      </div>
    </div>

    <div class="split-grid" style="margin-top: 12px">
      <div class="bar-chart">
        {#each trend as point}
          <div class="bar-row">
            <div class="bar-label">{point.label}</div>
            <div class="bar-track">
              <div
                class="bar-fill"
                style={`background: linear-gradient(90deg, rgba(var(--warning-rgb), 0.95), rgba(var(--warning-rgb), 0.55)); width: ${errorsScale > 0 ? (point.errorRate / errorsScale) * 100 : 0}%;`}
              ></div>
            </div>
            <span class="muted">{formatNumber(point.errorRate, 2)} /m</span>
          </div>
        {/each}
      </div>

      <div class="bar-chart">
        {#each trend as point}
          <div class="bar-row">
            <div class="bar-label">{point.label}</div>
            <div class="bar-track">
              <div
                class="bar-fill"
                style={`background: linear-gradient(90deg, rgba(var(--accent-rgb), 0.95), rgba(var(--accent-rgb), 0.55)); width: ${latencyScale > 0 ? (point.latencyP50 / latencyScale) * 100 : 0}%;`}
              ></div>
            </div>
            <span class="muted">{formatNumber(point.latencyP50, 2)} ms</span>
          </div>
        {/each}
      </div>
    </div>
  {/if}
</section>

<section class="split-grid">
  <div class="panel">
    <h2>{$t('overview.topAbusers')}</h2>
    <table>
      <thead><tr><th>{$t('overview.colPlayer')}</th><th>{$t('overview.colPacketsMin')}</th></tr></thead>
      <tbody>
        {#each data?.live?.topAbusers || [] as row}
          <tr>
            <td><EntityLink id={row.playerId} label={`player #${row.playerId}`} {openPlayer} {openItem} /></td>
            <td>{formatNumber(row.packetsPerMinute, 2)}</td>
          </tr>
        {:else}
          <tr><td colspan="2" class="muted">{$t('overview.noAbuseSignal')}</td></tr>
        {/each}
      </tbody>
    </table>
  </div>

  <div class="panel">
    <h2>{$t('overview.topRooms')}</h2>
    <table>
      <thead><tr><th>{$t('overview.colRoom')}</th><th>{$t('overview.colPacketsMin')}</th></tr></thead>
      <tbody>
        {#each data?.live?.topRooms || [] as row}
          <tr><td>room #{row.roomId}</td><td>{formatNumber(row.packetsPerMinute, 2)}</td></tr>
        {:else}
          <tr><td colspan="2" class="muted">{$t('overview.noRoomTraffic')}</td></tr>
        {/each}
      </tbody>
    </table>
  </div>
</section>
