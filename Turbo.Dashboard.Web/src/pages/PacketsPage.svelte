<script>
  import { onMount } from 'svelte';
  import { apiGet } from '../lib/api.js';
  import { formatNumber } from '../lib/format.js';
  import AccessDeniedNotice from '../components/AccessDeniedNotice.svelte';
  import { isPermissionDeniedError } from '../lib/permissions.js';

  let data = null;
  let error = '';
  let forbidden = false;

  $: topOperations = data?.topOperations || [];
  $: topFailedOperations = data?.topFailedOperations || [];
  $: topOpsScale = Math.max(
    1,
    ...topOperations.map((row) => Number(row.packetsPerMinute || 0)),
  );
  $: topFailedScale = Math.max(
    1,
    ...topFailedOperations.map((row) => Number(row.packetsPerMinute || 0)),
  );

  async function refresh() {
    forbidden = false;
    error = '';

    try {
      data = await apiGet('/api/v1/monitoring/packet-stats');
    } catch (err) {
      if (isPermissionDeniedError(err)) {
        forbidden = true;
        error = '';
        return;
      }

      error = err.message;
    }
  }

  onMount(refresh);
</script>

<section class="panel">
  <div class="panel-head">
    <h2>Packet telemetry</h2>
    <button type="button" on:click={refresh}>Refresh</button>
  </div>

  {#if forbidden}
    <AccessDeniedNotice message="Vous n'avez pas l'autorisation d'accéder aux métriques packet." />
  {:else if error}
    <p class="empty-state danger">{error}</p>
  {/if}

  <div class="metric-grid compact">
    <article><span>Packets/sec</span><strong>{formatNumber(data?.packetsPerSecond, 2)}</strong></article>
    <article><span>Errors/min</span><strong>{formatNumber(data?.errorsPerMinute, 2)}</strong></article>
    <article><span>Latency p50</span><strong>{formatNumber(data?.latencyP50Ms, 2)} ms</strong></article>
    <article><span>Latency p95</span><strong>{formatNumber(data?.latencyP95Ms, 2)} ms</strong></article>
  </div>
</section>

<section class="split-grid">
  <div class="panel">
    <h2>Top operations</h2>
    <div class="bar-chart">
      {#each topOperations as row}
        <div class="bar-row">
          <div class="bar-label">{row.operation}</div>
          <div class="bar-track">
            <div
              class="bar-fill"
              style={`width:${topOpsScale > 0 ? (Number(row.packetsPerMinute || 0) / topOpsScale) * 100 : 0}%;`}
            ></div>
          </div>
          <span class="muted">{formatNumber(row.packetsPerMinute, 2)}</span>
        </div>
      {:else}
        <p class="muted">No operation buckets.</p>
      {/each}
    </div>
    <table>
      <thead><tr><th>Operation</th><th>Packets/min</th></tr></thead>
      <tbody>
        {#each data?.topOperations || [] as row}
          <tr><td><code>{row.operation}</code></td><td>{formatNumber(row.packetsPerMinute, 2)}</td></tr>
        {:else}
          <tr><td colspan="2" class="muted">No operation buckets.</td></tr>
        {/each}
      </tbody>
    </table>
  </div>
  <div class="panel">
    <h2>Failed operations</h2>
    <div class="bar-chart">
      {#each topFailedOperations as row}
        <div class="bar-row">
          <div class="bar-label">{row.operation}</div>
          <div class="bar-track">
            <div
              class="bar-fill"
              style={`background: linear-gradient(90deg, rgba(223, 111, 123, 0.95), rgba(223, 111, 123, 0.55)); width:${topFailedScale > 0 ? (Number(row.packetsPerMinute || 0) / topFailedScale) * 100 : 0}%;`}
            ></div>
          </div>
          <span class="muted">{formatNumber(row.packetsPerMinute, 2)}</span>
        </div>
      {:else}
        <p class="muted">No failed packets.</p>
      {/each}
    </div>
    <table>
      <thead><tr><th>Operation</th><th>Packets/min</th></tr></thead>
      <tbody>
        {#each data?.topFailedOperations || [] as row}
          <tr><td><code>{row.operation}</code></td><td>{formatNumber(row.packetsPerMinute, 2)}</td></tr>
        {:else}
          <tr><td colspan="2" class="muted">No failed packets.</td></tr>
        {/each}
      </tbody>
    </table>
  </div>
</section>
