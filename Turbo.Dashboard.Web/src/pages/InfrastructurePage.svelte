<script>
  import { onMount } from 'svelte';
  import { apiGet } from '../lib/api.js';
  import { formatDate, formatDuration, formatNumber } from '../lib/format.js';
  import AccessDeniedNotice from '../components/AccessDeniedNotice.svelte';
  import { isPermissionDeniedError } from '../lib/permissions.js';

  let data = null;
  let error = '';
  let forbidden = false;

  async function refresh() {
    forbidden = false;
    error = '';
    try {
      data = await apiGet('/api/infrastructure');
    } catch (err) {
      if (isPermissionDeniedError(err)) {
        forbidden = true;
        data = null;
        return;
      }

      error = err.message;
    }
  }

  function statusClass(status) {
    const normalized = String(status || '').toLowerCase();

    if (normalized === 'healthy' || normalized === 'running' || normalized === 'active') {
      return 'positive';
    }

    if (normalized === 'degraded') {
      return 'event-item';
    }

    return 'negative';
  }

  onMount(() => {
    void refresh();
    const interval = setInterval(refresh, 10000);
    return () => clearInterval(interval);
  });
</script>

<section class="panel">
  <div class="panel-head">
    <div>
      <p class="eyebrow">Runtime</p>
      <h2>Emulator infrastructure</h2>
    </div>
    <button type="button" on:click={refresh}>Refresh</button>
  </div>

  {#if forbidden}
    <AccessDeniedNotice message="Vous n'avez pas l'autorisation d'accéder aux métriques infrastructure." />
  {:else if error}
    <p class="empty-state danger">{error}</p>
  {/if}

  <div class="metric-grid">
    <article>
      <span>Emulator</span>
      <strong class={statusClass(data?.runtime?.status)}>{data?.runtime?.status || '-'}</strong>
      <small>process #{data?.runtime?.processId || '-'}</small>
    </article>
    <article>
      <span>Overall health</span>
      <strong class={statusClass(data?.overall)}>{data?.overall || '-'}</strong>
      <small>DB + Orleans</small>
    </article>
    <article>
      <span>Uptime</span>
      <strong>{formatDuration(data?.runtime?.uptimeSeconds)}</strong>
      <small>started {formatDate(data?.runtime?.startedAtUtc)}</small>
    </article>
    <article>
      <span>Environment</span>
      <strong>{data?.runtime?.environmentName || '-'}</strong>
      <small>{data?.runtime?.machineName || '-'}</small>
    </article>
    <article>
      <span>Memory</span>
      <strong>{formatNumber(data?.runtime?.workingSetMb)} MB</strong>
      <small>{formatNumber(data?.runtime?.managedMemoryMb)} MB managed</small>
    </article>
    <article>
      <span>CPU</span>
      <strong>{data?.runtime?.processorCount ?? '-'}</strong>
      <small>logical processors</small>
    </article>
  </div>
</section>

<section class="split-grid">
  <div class="panel">
    <div class="panel-head">
      <div>
        <p class="eyebrow">Database</p>
        <h2>Persistence health</h2>
      </div>
      <strong class={statusClass(data?.database?.status)}>{data?.database?.status || '-'}</strong>
    </div>

    <div class="metric-grid compact">
      <article>
        <span>Latency</span>
        <strong>{formatNumber(data?.database?.latencyMs, 2)} ms</strong>
        <small>CanConnectAsync probe</small>
      </article>
      <article>
        <span>Detail</span>
        <strong>{data?.database?.name || 'database'}</strong>
        <small>{data?.database?.detail || '-'}</small>
      </article>
    </div>
  </div>

  <div class="panel">
    <div class="panel-head">
      <div>
        <p class="eyebrow">Orleans</p>
        <h2>Cluster health</h2>
      </div>
      <strong class={statusClass(data?.orleans?.status)}>{data?.orleans?.status || '-'}</strong>
    </div>

    <div class="metric-grid compact">
      <article>
        <span>Probe latency</span>
        <strong>{formatNumber(data?.orleans?.latencyMs, 2)} ms</strong>
        <small>management + presence grains</small>
      </article>
      <article>
        <span>Active silos</span>
        <strong>{data?.orleansCluster?.activeSiloCount ?? '-'}/{data?.orleansCluster?.siloCount ?? '-'}</strong>
        <small>{data?.orleansCluster?.detail || data?.orleans?.detail || '-'}</small>
      </article>
    </div>
  </div>
</section>

<section class="panel">
  <div class="panel-head">
    <div>
      <p class="eyebrow">Orleans membership</p>
      <h2>Silos</h2>
    </div>
    <span class={statusClass(data?.orleansCluster?.status)}>{data?.orleansCluster?.status || '-'}</span>
  </div>

  <table>
    <thead>
      <tr><th>Address</th><th>Status</th></tr>
    </thead>
    <tbody>
      {#each data?.orleansCluster?.silos || [] as silo}
        <tr>
          <td><code>{silo.address}</code></td>
          <td class={statusClass(silo.status)}>{silo.status}</td>
        </tr>
      {:else}
        <tr><td colspan="2" class="muted">No silo membership data available.</td></tr>
      {/each}
    </tbody>
  </table>
</section>

<section class="panel">
  <div class="panel-head">
    <div>
      <p class="eyebrow">Host</p>
      <h2>Runtime details</h2>
    </div>
  </div>

  <table>
    <tbody>
      <tr><th>Framework</th><td>{data?.runtime?.frameworkDescription || '-'}</td></tr>
      <tr><th>OS</th><td>{data?.runtime?.osDescription || '-'}</td></tr>
      <tr><th>Machine</th><td>{data?.runtime?.machineName || '-'}</td></tr>
      <tr><th>Started UTC</th><td>{formatDate(data?.runtime?.startedAtUtc)}</td></tr>
    </tbody>
  </table>
</section>
