<script>
  import { onMount } from 'svelte';
  import { apiGet } from '../lib/api.js';
  import { formatDate, formatNumber } from '../lib/format.js';
  import AccessDeniedNotice from '../components/AccessDeniedNotice.svelte';
  import StatCard from '../components/StatCard.svelte';
  import { Activity, TriangleAlert } from '@lucide/svelte';
  import { isPermissionDeniedError } from '../lib/permissions.js';
  import { t } from '../lib/i18n.js';

  let data = null;
  let error = '';
  let forbidden = false;

  $: signals = data?.signals || [];
  $: topErrorGroups = data?.topErrorGroups || [];
  $: topGroups = topErrorGroups.slice(0, 12);

  $: criticalCount = signals.filter(
    (signal) => String(signal.severity || '').toLowerCase() === 'critical'
  ).length;
  $: degradedCount = signals.filter(
    (signal) => String(signal.severity || '').toLowerCase() === 'degraded'
  ).length;
  $: healthyCount = Math.max(0, signals.length - criticalCount - degradedCount);

  $: signalBuckets = [
    { severity: 'critical', label: $t('incidents.critical'), count: criticalCount, color: 'var(--danger)' },
    { severity: 'degraded', label: $t('incidents.degraded'), count: degradedCount, color: 'var(--warning)' },
    { severity: 'healthy', label: $t('incidents.healthy'), count: healthyCount, color: 'var(--ok)' },
  ];
  $: severityScale = Math.max(1, ...signalBuckets.map((bucket) => bucket.count || 0));
  $: topErrorScale = Math.max(1, ...topGroups.map((entry) => entry.totalOccurrences || 0));

  async function refresh() {
    forbidden = false;
    error = '';

    try {
      data = await apiGet('/api/v1/monitoring/incidents');
    } catch (err) {
      if (isPermissionDeniedError(err)) {
        forbidden = true;
        error = '';
        return;
      }

      error = err.message;
    }
  }

  function severityClass(value) {
    const normalized = String(value || '').toLowerCase();

    if (normalized === 'critical') {
      return 'status-badge status-badge--bad';
    }

    if (normalized === 'degraded') {
      return 'status-badge status-badge--warn';
    }

    return 'status-badge status-badge--ok';
  }

  function formatRate(value) {
    const parsed = Number(value || 0);

    return `${formatNumber(parsed, 2)} /min`;
  }

  onMount(refresh);
</script>

<section class="panel">
  <div class="panel-head">
    <h2>{$t('incidents.title')}</h2>
    <button type="button" on:click={refresh}>{$t('common.refresh')}</button>
  </div>

  {#if forbidden}
    <AccessDeniedNotice message={$t('incidents.accessDenied')} />
  {:else if error}
    <p class="empty-state danger">{error}</p>
  {/if}

  <div class="incident-banner">
    <div>
      <span class="muted">{$t('incidents.overallSeverity')}</span>
      <strong class={severityClass(data?.overallSeverity)}>{data?.overallSeverity || '-'}</strong>
    </div>
    <span>{formatDate(data?.generatedAt)}</span>
  </div>

  <div class="metric-grid compact" style="margin-top: 12px;">
    <StatCard label={$t('incidents.activeSignals')} value={formatNumber(signals.length)} sub={$t('incidents.latestSnapshot')}>
      <Activity slot="icon" size={15} strokeWidth={2} aria-hidden="true" />
    </StatCard>
    <StatCard label={$t('incidents.errorSpikes')} value={formatRate(data?.errorSpikesPerMinute)} sub={$t('incidents.runtimeErrorsPerMin')}>
      <TriangleAlert slot="icon" size={15} strokeWidth={2} aria-hidden="true" />
    </StatCard>
    <StatCard label={$t('incidents.loginFailedSpikes')} value={formatRate(data?.loginFailedSpikesPerMinute)} sub={$t('incidents.authFailuresPerMin')}>
      <TriangleAlert slot="icon" size={15} strokeWidth={2} aria-hidden="true" />
    </StatCard>
    <StatCard label={$t('incidents.topErrorGroups')} value={formatNumber(topErrorGroups.length)} sub={$t('incidents.windowAggregation')}>
      <TriangleAlert slot="icon" size={15} strokeWidth={2} aria-hidden="true" />
    </StatCard>
  </div>
</section>

<section class="split-grid">
  <article class="panel">
    <h3>{$t('incidents.severityDistribution')}</h3>
    <div class="bar-chart">
      {#each signalBuckets as entry}
        <div class="bar-row">
          <div class="bar-label">{entry.label}</div>
          <div class="bar-track">
            <div
              class="bar-fill"
              style={`background: linear-gradient(90deg, ${entry.color}, ${entry.color}aa); width: ${severityScale > 0 ? (entry.count / severityScale) * 100 : 0}%;`}
            ></div>
          </div>
          <span class="muted">{formatNumber(entry.count)}</span>
        </div>
      {/each}
    </div>
  </article>

  <article class="panel">
    <h3>{$t('incidents.topErrorGroups')}</h3>
    <div class="bar-chart">
      {#each topGroups as entry}
        <div class="bar-row">
          <div class="bar-label">
            <div>{entry.fingerprint}</div>
            <small class="muted">
              {entry.source || '-'} · {entry.operation || '-'}
            </small>
          </div>
          <div class="bar-track">
            <div
              class="bar-fill"
              style={`background: linear-gradient(90deg, rgba(var(--danger-rgb), 0.95), rgba(var(--danger-rgb), 0.55)); width: ${topErrorScale > 0 ? (entry.totalOccurrences / topErrorScale) * 100 : 0}%;`}
            ></div>
          </div>
          <span class="muted">{formatNumber(entry.totalOccurrences)}</span>
        </div>
      {:else}
        <p class="muted">{$t('incidents.noErrorGroups')}</p>
      {/each}
    </div>
  </article>
</section>

<section class="panel" style="margin-top: 12px;">
  <h3>{$t('incidents.activeSignalsTitle')}</h3>
  <div class="incident-list">
    {#each signals as signal}
      <article>
        <div class="split-grid" style="grid-template-columns: 1fr auto; align-items: center; gap: 10px;">
          <strong>{signal.title || signal.code || signal.name}</strong>
          <span class={severityClass(signal.severity)}>{signal.severity || $t('incidents.healthy')}</span>
        </div>
        <p>{signal.summary}</p>
        <p class="muted">
          {$t('incidents.observedThreshold', { observed: formatNumber(signal.observed, 2), threshold: formatNumber(signal.threshold, 2) })}
          • {$t('incidents.detected', { date: formatDate(signal.detectedAt) })}
        </p>
      </article>
    {:else}
      <p class="muted">{$t('incidents.noActiveSignal')}</p>
    {/each}
  </div>
</section>

<section class="panel" style="margin-top: 12px;">
  <h3>{$t('incidents.errorGroupDetails')}</h3>
  <table>
    <thead>
      <tr>
        <th>{$t('incidents.colFingerprint')}</th>
        <th>{$t('incidents.colSource')}</th>
        <th>{$t('incidents.colOperation')}</th>
        <th>{$t('incidents.colException')}</th>
        <th>{$t('incidents.colOccurrences')}</th>
        <th>{$t('incidents.colActor')}</th>
        <th>{$t('incidents.colRoom')}</th>
        <th>{$t('incidents.colLastSeen')}</th>
      </tr>
    </thead>
    <tbody>
      {#each topGroups as entry}
        <tr>
          <td>{entry.fingerprint}</td>
          <td>{entry.source || '-'}</td>
          <td>{entry.operation || '-'}</td>
          <td>{entry.exceptionType || '-'}</td>
          <td>{formatNumber(entry.totalOccurrences || 0)}</td>
          <td>{entry.lastActorPlayerId || '-'}</td>
          <td>{entry.lastRoomId || '-'}</td>
          <td>{formatDate(entry.lastSeenAt)}</td>
        </tr>
      {:else}
        <tr><td colspan="8" class="muted">{$t('incidents.noGroupedData')}</td></tr>
      {/each}
    </tbody>
  </table>
</section>
