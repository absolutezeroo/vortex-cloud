<script>
  import { onMount } from 'svelte';
  import { apiGet } from '../lib/api.js';
  import { formatNumber } from '../lib/format.js';
  import AccessDeniedNotice from '../components/AccessDeniedNotice.svelte';
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
    <article><span>{$t('packets.packetsPerSec')}</span><strong>{formatNumber(data?.packetsPerSecond, 2)}</strong></article>
    <article><span>{$t('packets.errorsPerMin')}</span><strong>{formatNumber(data?.errorsPerMinute, 2)}</strong></article>
    <article><span>{$t('packets.latencyP50')}</span><strong>{formatNumber(data?.latencyP50Ms, 2)} ms</strong></article>
    <article><span>{$t('packets.latencyP95')}</span><strong>{formatNumber(data?.latencyP95Ms, 2)} ms</strong></article>
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
