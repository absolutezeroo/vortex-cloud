<script>
  import { onMount } from 'svelte';
  import { apiGet } from '../lib/api.js';
  import { formatDate, formatDuration, formatNumber } from '../lib/format.js';
  import AccessDeniedNotice from '../components/AccessDeniedNotice.svelte';
  import StatCard from '../components/StatCard.svelte';
  import { Activity, Cpu, Timer, Hash } from '@lucide/svelte';
  import { isPermissionDeniedError, hasDashboardCapability } from '../lib/permissions.js';
  import { CAPABILITIES } from '../lib/dashboardPermissions.js';
  import { identity } from '../lib/session.js';
  import { createWriteOps } from '../lib/writeOps.js';
  import ConfirmReasonModal from '../components/ConfirmReasonModal.svelte';
  import OpResult from '../components/OpResult.svelte';
  import { formatBytes } from '../lib/format.js';
  import TableFilter from '../components/TableFilter.svelte';
  import { filterRows } from '../lib/tableView.js';
  import { t, translate } from '../lib/i18n.js';

  let data = $state(null);
  let error = $state('');
  let forbidden = $state(false);

  // The coarse safety net. Listing and triggering only: where dumps go, how often and how many are
  // kept is bootstrap config on purpose -- a retention an operator can set to zero from the same
  // screen as the mistake is not a safety net. Restoring is not offered here either; rolling the
  // whole database back discards every change since, which belongs in a maintenance window.
  let backups = $state(null);

  // One row per dump ever taken, so this is the table that quietly gets long.
  let backupQuery = $state('');
  let backupView = $derived(filterRows(backups?.items || [], backupQuery));
  const backupOps = createWriteOps(loadBackups);

  let canBackup = $derived(hasDashboardCapability($identity, CAPABILITIES.opsDatabaseBackup));

  async function loadBackups() {
    if (!canBackup) return;

    try {
      backups = await apiGet('/api/v1/database/backups');
    } catch {
      // A denied or unreachable listing must not take the rest of the page down with it.
      backups = null;
    }
  }

  function stageBackup() {
    backupOps.ask(
      '/api/v1/operations/database/backup',
      {},
      translate('infrastructure.backupNow'),
      translate('infrastructure.backupSummary'),
      { key: 'backup', danger: false },
    );
  }
  let silos = $derived(data?.orleansCluster?.silos || []);
  let siloRows = $derived((() => {
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
  })());

  let siloMax = $derived(Math.max(1, ...siloRows.map((row) => row.count || 0)));

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
    void loadBackups();
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
    <button type="button" onclick={refresh}>{$t('common.refresh')}</button>
  </div>

  {#if forbidden}
    <AccessDeniedNotice message={$t('infrastructure.accessDenied')} />
  {:else if error}
    <p class="empty-state danger" role="alert">{error}</p>
  {/if}

  <div class="metric-grid">
    <StatCard label={$t('infrastructure.emulator')} sub={$t('infrastructure.process', { id: data?.runtime?.processId || '-' })}>
      {#snippet icon()}
        <Activity size={15} strokeWidth={2} aria-hidden="true" />
      {/snippet}
      {#snippet value()}
        <strong class={statusClass(data?.runtime?.status)}>{data?.runtime?.status || '-'}</strong>
      {/snippet}
    </StatCard>
    <StatCard label={$t('infrastructure.overallHealth')} sub={$t('infrastructure.dbAndOrleans')}>
      {#snippet icon()}
        <Activity size={15} strokeWidth={2} aria-hidden="true" />
      {/snippet}
      {#snippet value()}
        <strong class={statusClass(data?.overall)}>{data?.overall || '-'}</strong>
      {/snippet}
    </StatCard>
    <StatCard label={$t('infrastructure.uptime')} value={formatDuration(data?.runtime?.uptimeSeconds)} sub={$t('infrastructure.started', { date: formatDate(data?.runtime?.startedAtUtc) })}>
      {#snippet icon()}
        <Activity size={15} strokeWidth={2} aria-hidden="true" />
      {/snippet}
    </StatCard>
    <StatCard label={$t('infrastructure.environment')} value={data?.runtime?.environmentName || '-'} sub={data?.runtime?.machineName || '-'}>
      {#snippet icon()}
        <Activity size={15} strokeWidth={2} aria-hidden="true" />
      {/snippet}
    </StatCard>
    <StatCard label={$t('infrastructure.memory')} sub={$t('infrastructure.managed', { value: formatNumber(data?.runtime?.managedMemoryMb) })}>
      {#snippet icon()}
        <Cpu size={15} strokeWidth={2} aria-hidden="true" />
      {/snippet}
      {#snippet value()}
        <span>{formatNumber(data?.runtime?.workingSetMb)} MB</span>
      {/snippet}
    </StatCard>
    <StatCard label={$t('infrastructure.cpu')} value={data?.runtime?.processorCount ?? '-'} sub={$t('infrastructure.logicalProcessors')}>
      {#snippet icon()}
        <Cpu size={15} strokeWidth={2} aria-hidden="true" />
      {/snippet}
    </StatCard>
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
      <StatCard label={$t('infrastructure.latency')} sub={$t('infrastructure.canConnectProbe')}>
        {#snippet icon()}
          <Timer size={15} strokeWidth={2} aria-hidden="true" />
        {/snippet}
        {#snippet value()}
          <span>{formatNumber(data?.database?.latencyMs, 2)} ms</span>
        {/snippet}
      </StatCard>
      <StatCard label={$t('infrastructure.detail')} value={data?.database?.name || $t('infrastructure.database')} sub={data?.database?.detail || '-'}>
        {#snippet icon()}
          <Activity size={15} strokeWidth={2} aria-hidden="true" />
        {/snippet}
      </StatCard>
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
      <StatCard label={$t('infrastructure.probeLatency')} sub={$t('infrastructure.managementGrains')}>
        {#snippet icon()}
          <Timer size={15} strokeWidth={2} aria-hidden="true" />
        {/snippet}
        {#snippet value()}
          <span>{formatNumber(data?.orleans?.latencyMs, 2)} ms</span>
        {/snippet}
      </StatCard>
      <StatCard label={$t('infrastructure.activeSilos')} sub={data?.orleansCluster?.detail || data?.orleans?.detail || '-'}>
        {#snippet icon()}
          <Hash size={15} strokeWidth={2} aria-hidden="true" />
        {/snippet}
        {#snippet value()}
          <span>{data?.orleansCluster?.activeSiloCount ?? '-'}/{data?.orleansCluster?.siloCount ?? '-'}</span>
        {/snippet}
      </StatCard>
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

{#if canBackup}
  <section class="panel" style="margin-top: 12px;">
    <div class="panel-head">
      <div>
        <p class="eyebrow">{$t('infrastructure.eyebrowBackup')}</p>
        <h2>{$t('infrastructure.backups')}</h2>
      </div>
      <button type="button" onclick={stageBackup} disabled={$backupOps.busyKeys.backup}>
        {$t('infrastructure.backupNow')}
      </button>
    </div>

    {#if $backupOps.errors.backup}<p class="empty-state danger" role="alert">{$backupOps.errors.backup}</p>{/if}
    {#if $backupOps.results.backup}<OpResult result={$backupOps.results.backup} />{/if}

    {#if backups && !backups.configured}
      <p class="empty-state">{$t('infrastructure.backupNotConfigured')}</p>
    {:else if backups?.items?.length}
      <TableFilter bind:query={backupQuery} shown={backupView.length} total={backups?.items?.length || 0} />
      <table>
        <thead><tr><th>{$t('infrastructure.backupFile')}</th><th>{$t('infrastructure.backupSize')}</th><th>{$t('infrastructure.backupTaken')}</th></tr></thead>
        <tbody>
          {#each backupView as item}
            <tr>
              <td><code>{item.fileName}</code></td>
              <td>{formatBytes(item.sizeBytes)}</td>
              <td>{formatDate(item.createdUtc)}</td>
            </tr>
          {/each}
        </tbody>
      </table>
    {:else}
      <p class="empty-state">{$t('infrastructure.backupNone')}</p>
    {/if}
  </section>

  <ConfirmReasonModal
    open={Boolean($backupOps.pending)}
    title={$backupOps.pending?.title ?? ''}
    changes={$backupOps.pending?.changes ?? []}
    noteOnly={$backupOps.pending?.noteOnly ?? false}
    summary={$backupOps.pending?.summary ?? ''}
    confirmLabel={$t('infrastructure.backupNow')}
    busy={$backupOps.busy}
    error={$backupOps.error}
    danger={false}
    onconfirm={backupOps.confirm}
    oncancel={() => backupOps.cancel()}
  />
{/if}
