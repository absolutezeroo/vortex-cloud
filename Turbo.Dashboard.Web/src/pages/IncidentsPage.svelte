<script>
  import { onMount } from 'svelte';
  import { apiGet } from '../lib/api.js';
  import { formatDate, formatNumber } from '../lib/format.js';
  import AccessDeniedNotice from '../components/AccessDeniedNotice.svelte';
  import { isPermissionDeniedError } from '../lib/permissions.js';

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
    { severity: 'critical', label: 'Critical', count: criticalCount, color: 'var(--danger)' },
    { severity: 'degraded', label: 'Degraded', count: degradedCount, color: 'var(--warning)' },
    { severity: 'healthy', label: 'Healthy', count: healthyCount, color: 'var(--ok)' },
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
    <h2>Incident signals</h2>
    <button type="button" on:click={refresh}>Refresh</button>
  </div>

  {#if forbidden}
    <AccessDeniedNotice message="Vous n'avez pas l'autorisation d'accéder aux incidents." />
  {:else if error}
    <p class="empty-state danger">{error}</p>
  {/if}

  <div class="incident-banner">
    <div>
      <span class="muted">Overall severity</span>
      <strong class={severityClass(data?.overall)}>{data?.overall || '-'}</strong>
    </div>
    <span>{formatDate(data?.generatedAt)}</span>
  </div>

  <div class="metric-grid compact" style="margin-top: 12px;">
    <article>
      <span>Active signals</span>
      <strong>{formatNumber(signals.length)}</strong>
      <small>in latest snapshot</small>
    </article>
    <article>
      <span>Error spikes</span>
      <strong>{formatRate(data?.errorSpikesPerMinute)}</strong>
      <small>runtime errors / minute</small>
    </article>
    <article>
      <span>Login failed spikes</span>
      <strong>{formatRate(data?.loginFailedSpikesPerMinute)}</strong>
      <small>auth failures / minute</small>
    </article>
    <article>
      <span>Top error groups</span>
      <strong>{formatNumber(topErrorGroups.length)}</strong>
      <small>window aggregation</small>
    </article>
  </div>
</section>

<section class="split-grid">
  <article class="panel">
    <h3>Severity distribution</h3>
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
    <h3>Top error groups</h3>
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
              style={`background: linear-gradient(90deg, rgba(223, 111, 123, 0.95), rgba(223, 111, 123, 0.55)); width: ${topErrorScale > 0 ? (entry.totalOccurrences / topErrorScale) * 100 : 0}%;`}
            ></div>
          </div>
          <span class="muted">{formatNumber(entry.totalOccurrences)}</span>
        </div>
      {:else}
        <p class="muted">No aggregated error groups in this window.</p>
      {/each}
    </div>
  </article>
</section>

<section class="panel" style="margin-top: 12px;">
  <h3>Active incident signals</h3>
  <div class="incident-list">
    {#each signals as signal}
      <article>
        <div class="split-grid" style="grid-template-columns: 1fr auto; align-items: center; gap: 10px;">
          <strong>{signal.title || signal.code || signal.name}</strong>
          <span class={severityClass(signal.severity)}>{signal.severity || 'healthy'}</span>
        </div>
        <p>{signal.summary}</p>
        <p class="muted">
          observed {formatNumber(signal.observed, 2)} / threshold {formatNumber(signal.threshold, 2)}
          • detected {formatDate(signal.detectedAt)}
        </p>
      </article>
    {:else}
      <p class="muted">No active incident signal.</p>
    {/each}
  </div>
</section>

<section class="panel" style="margin-top: 12px;">
  <h3>Top error group details</h3>
  <table>
    <thead>
      <tr>
        <th>Fingerprint</th>
        <th>Source</th>
        <th>Operation</th>
        <th>Exception</th>
        <th>Occurrences</th>
        <th>Actor</th>
        <th>Room</th>
        <th>Last seen</th>
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
        <tr><td colspan="8" class="muted">No grouped error data for this window.</td></tr>
      {/each}
    </tbody>
  </table>
</section>
