<script>
  import { onMount } from 'svelte';
  import { apiGet } from '../lib/api.js';
  import { formatNumber } from '../lib/format.js';
  import EntityLink from '../components/EntityLink.svelte';
  import AccessDeniedNotice from '../components/AccessDeniedNotice.svelte';
  import { isPermissionDeniedError } from '../lib/permissions.js';
  import { openPlayer, openItem } from '../lib/session.js';

  let data = null;
  let error = '';
  let forbidden = false;

  async function refresh() {
    forbidden = false;
    error = '';
    try {
      data = await apiGet('/api/overview');
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
    <h2>Live operations</h2>
    <button type="button" on:click={refresh}>Refresh</button>
  </div>

  {#if forbidden}
    <AccessDeniedNotice
      message="Vous n'avez pas l'autorisation d'accéder aux données de survol global."
    />
  {:else if error}
    <p class="empty-state danger">{error}</p>
  {/if}

  <div class="metric-grid">
    <article><span>Status</span><strong>{data?.status || '-'}</strong><small>global state</small></article>
    <article><span>Sessions</span><strong>{data?.activeSessions ?? '-'}</strong><small>connected clients</small></article>
    <article><span>Rooms</span><strong>{data?.activeRooms ?? '-'}</strong><small>active rooms</small></article>
    <article><span>Club</span><strong>{data?.activeClubSubscribers ?? '-'}</strong><small>active subscribers</small></article>
    <article><span>Memory</span><strong>{data?.managedMemoryMb ?? '-'}</strong><small>managed MB</small></article>
    <article><span>Packets/sec</span><strong>{formatNumber(data?.live?.packetsPerSecond, 2)}</strong><small>live traffic</small></article>
    <article><span>Errors/min</span><strong>{formatNumber(data?.live?.errorsPerMinute, 2)}</strong><small>handler failures</small></article>
    <article><span>Latency p50</span><strong>{formatNumber(data?.live?.latencyP50Ms, 2)} ms</strong><small>median</small></article>
    <article><span>Latency p95</span><strong>{formatNumber(data?.live?.latencyP95Ms, 2)} ms</strong><small>tail</small></article>
  </div>
</section>

<section class="split-grid">
  <div class="panel">
    <h2>Top abusers</h2>
    <table>
      <thead><tr><th>Player</th><th>Packets/min</th></tr></thead>
      <tbody>
        {#each data?.live?.topAbusers || [] as row}
          <tr>
            <td><EntityLink id={row.playerId} label={`player #${row.playerId}`} {openPlayer} {openItem} /></td>
            <td>{formatNumber(row.packetsPerMinute, 2)}</td>
          </tr>
        {:else}
          <tr><td colspan="2" class="muted">No active abuse signal.</td></tr>
        {/each}
      </tbody>
    </table>
  </div>

  <div class="panel">
    <h2>Top rooms</h2>
    <table>
      <thead><tr><th>Room</th><th>Packets/min</th></tr></thead>
      <tbody>
        {#each data?.live?.topRooms || [] as row}
          <tr><td>room #{row.roomId}</td><td>{formatNumber(row.packetsPerMinute, 2)}</td></tr>
        {:else}
          <tr><td colspan="2" class="muted">No room traffic yet.</td></tr>
        {/each}
      </tbody>
    </table>
  </div>
</section>
