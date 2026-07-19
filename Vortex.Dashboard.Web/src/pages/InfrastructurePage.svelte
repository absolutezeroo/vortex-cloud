<script>
  import { onMount } from 'svelte';
  import { apiGet } from '../lib/api.js';
  import { formatDate, formatDuration, formatNumber } from '../lib/format.js';
  import AccessDeniedNotice from '../components/AccessDeniedNotice.svelte';
  import { isPermissionDeniedError } from '../lib/permissions.js';
  import { t } from '../lib/i18n.js';

  let data = null;
  let error = '';
  let forbidden = false;
  $: silos = data?.orleansCluster?.silos || [];
  $: siloRows = (() => {
    const buckets = new Map();

    for (const silo of silos) {
      const status = (silo.status || 'unknown').trim();
      buckets.set(status, (buckets.get(status) || 0) + 1);
    }

    return [...buckets.entries()]
      .map(([status, count]) => ({
        status,
        count,
      }))
      .sort((a, b) => b.count - a.count || String(a.status).localeCompare(String(b.status)));
  })();

  $: siloMax = Math.max(1, ...siloRows.map((row) => row.count || 0));

  async function refresh() {
    forbidden = false;
    error = '';
    try {
      data = await apiGet('/api/v1/monitoring/infrastructure');
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
      return 'status-badge status-badge--ok';
    }

    if (normalized === 'degraded' || normalized === 'yellow') {
      return 'status-badge status-badge--warn';
    }

    if (normalized === 'down' || normalized === 'critical' || normalized === 'offline' || normalized === 'failed') {
      return 'status-badge status-badge--bad';
    }

    return 'status-badge status-badge--unknown';
  }

  function siloColor(status) {
    const normalized = String(status || '').trim().toLowerCase();

    if (normalized === 'active') {
      return 'var(--ok)';
    }

    if (normalized === 'inactive' || normalized === 'deactivating' || normalized === 'standby') {
      return 'var(--warning)';
    }

    return 'var(--danger)';
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
      <p class="eyebrow">{$t('infrastructure.eyebrowRuntime')}</p>
      <h2>{$t('infrastructure.title')}</h2>
    </div>
    <button type="button" on:click={refresh}>{$t('common.refresh')}</button>
  </div>

  {#if forbidden}
    <AccessDeniedNotice message={$t('infrastructure.accessDenied')} />
  {:else if error}
    <p class="empty-state danger">{error}</p>
  {/if}

  <div class="metric-grid">
    <article>
      <span>{$t('infrastructure.emulator')}</span>
      <strong class={statusClass(data?.runtime?.status)}>{data?.runtime?.status || '-'}</strong>
      <small>{$t('infrastructure.process', { id: data?.runtime?.processId || '-' })}</small>
    </article>
    <article>
      <span>{$t('infrastructure.overallHealth')}</span>
      <strong class={statusClass(data?.overall)}>{data?.overall || '-'}</strong>
      <small>{$t('infrastructure.dbAndOrleans')}</small>
    </article>
    <article>
      <span>{$t('infrastructure.uptime')}</span>
      <strong>{formatDuration(data?.runtime?.uptimeSeconds)}</strong>
      <small>{$t('infrastructure.started', { date: formatDate(data?.runtime?.startedAtUtc) })}</small>
    </article>
    <article>
      <span>{$t('infrastructure.environment')}</span>
      <strong>{data?.runtime?.environmentName || '-'}</strong>
      <small>{data?.runtime?.machineName || '-'}</small>
    </article>
    <article>
      <span>{$t('infrastructure.memory')}</span>
      <strong>{formatNumber(data?.runtime?.workingSetMb)} MB</strong>
      <small>{$t('infrastructure.managed', { value: formatNumber(data?.runtime?.managedMemoryMb) })}</small>
    </article>
    <article>
      <span>{$t('infrastructure.cpu')}</span>
      <strong>{data?.runtime?.processorCount ?? '-'}</strong>
      <small>{$t('infrastructure.logicalProcessors')}</small>
    </article>
  </div>
</section>

<section class="split-grid">
  <div class="panel">
    <div class="panel-head">
      <div>
        <p class="eyebrow">{$t('infrastructure.eyebrowDatabase')}</p>
        <h2>{$t('infrastructure.persistenceHealth')}</h2>
      </div>
      <strong class={statusClass(data?.database?.status)}>{data?.database?.status || '-'}</strong>
    </div>

    <div class="metric-grid compact">
      <article>
        <span>{$t('infrastructure.latency')}</span>
        <strong>{formatNumber(data?.database?.latencyMs, 2)} ms</strong>
        <small>{$t('infrastructure.canConnectProbe')}</small>
      </article>
      <article>
        <span>{$t('infrastructure.detail')}</span>
        <strong>{data?.database?.name || $t('infrastructure.database')}</strong>
        <small>{data?.database?.detail || '-'}</small>
      </article>
    </div>
  </div>

  <div class="panel">
    <div class="panel-head">
      <div>
        <p class="eyebrow">{$t('infrastructure.eyebrowOrleans')}</p>
        <h2>{$t('infrastructure.clusterHealth')}</h2>
      </div>
      <strong class={statusClass(data?.orleans?.status)}>{data?.orleans?.status || '-'}</strong>
    </div>

    <div class="metric-grid compact">
      <article>
        <span>{$t('infrastructure.probeLatency')}</span>
        <strong>{formatNumber(data?.orleans?.latencyMs, 2)} ms</strong>
        <small>{$t('infrastructure.managementGrains')}</small>
      </article>
      <article>
        <span>{$t('infrastructure.activeSilos')}</span>
        <strong>{data?.orleansCluster?.activeSiloCount ?? '-'}/{data?.orleansCluster?.siloCount ?? '-'}</strong>
        <small>{data?.orleansCluster?.detail || data?.orleans?.detail || '-'}</small>
      </article>
    </div>
  </div>
</section>

  <section class="panel">
    <div class="panel-head">
      <div>
        <p class="eyebrow">{$t('infrastructure.eyebrowMembership')}</p>
      <h2>{$t('infrastructure.silos')}</h2>
    </div>
    <span class={statusClass(data?.orleansCluster?.status)}>{data?.orleansCluster?.status || '-'}</span>
  </div>

  <table>
    <thead>
      <tr><th>{$t('infrastructure.colAddress')}</th><th>{$t('infrastructure.colStatus')}</th></tr>
    </thead>
    <tbody>
      {#each data?.orleansCluster?.silos || [] as silo}
        <tr>
          <td><code>{silo.address}</code></td>
          <td class={statusClass(silo.status)}>{silo.status}</td>
        </tr>
      {:else}
        <tr><td colspan="2" class="muted">{$t('infrastructure.noSiloData')}</td></tr>
      {/each}
    </tbody>
  </table>
</section>

<section class="panel" style="margin-top: 12px;">
  <div class="panel-head">
    <h2>{$t('infrastructure.siloDistribution')}</h2>
  </div>

  <div class="bar-chart">
    {#each siloRows as row}
      <div class="bar-row">
        <div class="bar-label">{row.status}</div>
        <div class="bar-track">
          <div
            class="bar-fill"
            style={`background: linear-gradient(90deg, ${siloColor(row.status)}, ${siloColor(row.status)}aa); width: ${siloMax > 0 ? (row.count / siloMax) * 100 : 0}%;`}
          ></div>
        </div>
        <span class="muted">{row.count}</span>
      </div>
    {:else}
      <p class="muted">{$t('infrastructure.noSiloBuckets')}</p>
    {/each}
  </div>
</section>

<section class="panel">
  <div class="panel-head">
    <div>
      <p class="eyebrow">{$t('infrastructure.eyebrowHost')}</p>
      <h2>{$t('infrastructure.runtimeDetails')}</h2>
    </div>
  </div>

  <table>
    <tbody>
      <tr><th>{$t('infrastructure.framework')}</th><td>{data?.runtime?.frameworkDescription || '-'}</td></tr>
      <tr><th>{$t('infrastructure.os')}</th><td>{data?.runtime?.osDescription || '-'}</td></tr>
      <tr><th>{$t('infrastructure.machine')}</th><td>{data?.runtime?.machineName || '-'}</td></tr>
      <tr><th>{$t('infrastructure.startedUtc')}</th><td>{formatDate(data?.runtime?.startedAtUtc)}</td></tr>
    </tbody>
  </table>
</section>
