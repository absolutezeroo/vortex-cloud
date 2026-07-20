<script>
  import { onMount } from 'svelte';
  import { apiGet } from '../lib/api.js';
  import { formatNumber } from '../lib/format.js';
  import AccessDeniedNotice from '../components/AccessDeniedNotice.svelte';
  import StatCard from '../components/StatCard.svelte';
  import { Gauge, TriangleAlert, Timer } from '@lucide/svelte';
  import { isPermissionDeniedError } from '../lib/permissions.js';
  import { t } from '../lib/i18n.js';

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
    <h2>{$t('packets.title')}</h2>
    <button type="button" on:click={refresh}>{$t('common.refresh')}</button>
  </div>

  {#if forbidden}
    <AccessDeniedNotice message={$t('packets.accessDenied')} />
  {:else if error}
    <p class="empty-state danger">{error}</p>
  {/if}

  <div class="metric-grid compact">
    <StatCard label={$t('packets.packetsPerSec')} value={formatNumber(data?.packetsPerSecond, 2)}>
      <Gauge slot="icon" size={15} strokeWidth={2} aria-hidden="true" />
    </StatCard>
    <StatCard label={$t('packets.errorsPerMin')} value={formatNumber(data?.errorsPerMinute, 2)}>
      <TriangleAlert slot="icon" size={15} strokeWidth={2} aria-hidden="true" />
    </StatCard>
    <StatCard label={$t('packets.latencyP50')}>
      <Timer slot="icon" size={15} strokeWidth={2} aria-hidden="true" />
      <span slot="value">{formatNumber(data?.latencyP50Ms, 2)} ms</span>
    </StatCard>
    <StatCard label={$t('packets.latencyP95')}>
      <Timer slot="icon" size={15} strokeWidth={2} aria-hidden="true" />
      <span slot="value">{formatNumber(data?.latencyP95Ms, 2)} ms</span>
    </StatCard>
  </div>
</section>

<section class="split-grid">
  <div class="panel">
    <h2>{$t('packets.topOperations')}</h2>
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
        <p class="muted">{$t('packets.noOperationBuckets')}</p>
      {/each}
    </div>
    <table>
      <thead><tr><th>{$t('packets.colOperation')}</th><th>{$t('packets.colPacketsMin')}</th></tr></thead>
      <tbody>
        {#each data?.topOperations || [] as row}
          <tr><td><code>{row.operation}</code></td><td>{formatNumber(row.packetsPerMinute, 2)}</td></tr>
        {:else}
          <tr><td colspan="2" class="muted">{$t('packets.noOperationBuckets')}</td></tr>
        {/each}
      </tbody>
    </table>
  </div>
  <div class="panel">
    <h2>{$t('packets.failedOperations')}</h2>
    <div class="bar-chart">
      {#each topFailedOperations as row}
        <div class="bar-row">
          <div class="bar-label">{row.operation}</div>
          <div class="bar-track">
            <div
              class="bar-fill"
              style={`background: linear-gradient(90deg, rgba(var(--danger-rgb), 0.95), rgba(var(--danger-rgb), 0.55)); width:${topFailedScale > 0 ? (Number(row.packetsPerMinute || 0) / topFailedScale) * 100 : 0}%;`}
            ></div>
          </div>
          <span class="muted">{formatNumber(row.packetsPerMinute, 2)}</span>
        </div>
      {:else}
        <p class="muted">{$t('packets.noFailedPackets')}</p>
      {/each}
    </div>
    <table>
      <thead><tr><th>{$t('packets.colOperation')}</th><th>{$t('packets.colPacketsMin')}</th></tr></thead>
      <tbody>
        {#each data?.topFailedOperations || [] as row}
          <tr><td><code>{row.operation}</code></td><td>{formatNumber(row.packetsPerMinute, 2)}</td></tr>
        {:else}
          <tr><td colspan="2" class="muted">{$t('packets.noFailedPackets')}</td></tr>
        {/each}
      </tbody>
    </table>
  </div>
</section>
