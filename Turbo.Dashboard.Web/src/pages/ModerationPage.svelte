<script>
  import { onMount } from 'svelte';
  import { apiGet } from '../lib/api.js';
  import { formatDate, formatDuration, formatNumber } from '../lib/format.js';
  import { isPermissionDeniedError } from '../lib/permissions.js';
  import AccessDeniedNotice from '../components/AccessDeniedNotice.svelte';
  import EntityLink from '../components/EntityLink.svelte';
  import { openPlayer, openItem } from '../lib/session.js';

  const actionOptions = [
    '',
    'moderation.kick',
    'moderation.mute',
    'moderation.ban',
    'moderation.alert',
    'moderation.denied',
  ];

  const resultOptions = ['', 'Success', 'Failed', 'Denied'];

  const actionColors = {
    'moderation.kick': 'var(--accent)',
    'moderation.mute': 'var(--ok)',
    'moderation.ban': 'var(--danger)',
    'moderation.alert': 'var(--warning)',
    'moderation.denied': '#9f6ce1',
    other: '#64748b',
  };

  let since = '';
  let until = '';
  let actor = '';
  let target = '';
  let room = '';
  let action = '';
  let result = '';
  let limit = '80';
  let loading = false;
  let error = '';
  let forbidden = false;
  let data = null;

  let filterSummary = 'No data loaded.';

  function toLocalInputValue(value) {
    if (!value) {
      return '';
    }

    const date = new Date(value);
    if (Number.isNaN(date.getTime())) {
      return '';
    }

    const adjusted = new Date(date.getTime() - date.getTimezoneOffset() * 60000);
    return adjusted.toISOString().slice(0, 16);
  }

  function currentIso(value) {
    if (!value) {
      return '';
    }

    const date = new Date(value);
    if (Number.isNaN(date.getTime())) {
      return '';
    }

    return date.toISOString();
  }

  function setDefaults() {
    const end = new Date();
    const start = new Date(end.getTime() - 24 * 60 * 60 * 1000);
    since = toLocalInputValue(start);
    until = toLocalInputValue(end);
  }

  function csvEscape(value) {
    const text = value === null || value === undefined ? '' : String(value);
    if (text.includes('"') || text.includes(',') || text.includes('\n')) {
      return `"${text.replace(/"/g, '""')}"`;
    }

    return text;
  }

  function buildParams() {
    const params = new URLSearchParams();

    if (since) {
      const iso = currentIso(since);
      if (iso) params.set('since', iso);
    }

    if (until) {
      const iso = currentIso(until);
      if (iso) params.set('until', iso);
    }

    if (actor.trim()) {
      params.set('actor', actor.trim());
    }

    if (target.trim()) {
      params.set('target', target.trim());
    }

    if (room.trim()) {
      params.set('room', room.trim());
    }

    if (action) {
      params.set('action', action);
    }

    if (result) {
      params.set('result', result);
    }

    if (limit) {
      params.set('limit', limit);
    }

    return params;
  }

  function updateSummary() {
    const total = data?.totals?.total || 0;
    const from = data?.window?.since || '';
    const to = data?.window?.until || '';

    filterSummary =
      `${total} moderation action(s) found${from ? ` between ${formatDate(from)} and ${formatDate(to)}.` : '.'}`;
  }

  async function refresh() {
    loading = true;
    error = '';
    forbidden = false;

    try {
      const params = buildParams();
      data = await apiGet(`/api/v1/forensics/moderation/stats?${params.toString()}`);
      updateSummary();
    } catch (err) {
      if (isPermissionDeniedError(err)) {
        forbidden = true;
        error = '';
        data = null;
        return;
      }

      error = err.message;
      data = null;
    } finally {
      loading = false;
    }
  }

  function exportCsv() {
    const rows = data?.rows || [];
    if (!rows.length) {
      return;
    }

    const headers = [
      'OccurredAt',
      'Action',
      'Result',
      'ActorId',
      'ActorName',
      'TargetId',
      'TargetName',
      'RoomId',
      'RoomName',
      'DurationSeconds',
      'Duration',
      'Reason',
      'IsRenewal',
      'CorrelationId',
    ];

    const lines = rows.map((row) =>
      [
        row.occurredAt,
        row.action,
        row.result,
        row.actorPlayerId,
        row.actorName,
        row.targetPlayerId,
        row.targetName,
        row.roomId,
        row.roomName,
        row.durationSeconds,
        row.duration,
        row.reason,
        row.isRenewal,
        row.correlationId,
      ].map(csvEscape).join(',')
    );

    const csv = `${headers.map(csvEscape).join(',')}\n${lines.join('\n')}`;
    const blob = new Blob([csv], { type: 'text/csv;charset=utf-8;' });
    const link = document.createElement('a');
    const url = URL.createObjectURL(blob);

    link.href = url;
    link.download = `moderation-stats-${new Date().toISOString().slice(0, 10)}.csv`;
    document.body.appendChild(link);
    link.click();
    link.remove();
    URL.revokeObjectURL(url);
  }

  $: actionDistribution = data?.distribution?.byAction || [];
  $: timelineRows = data?.timeline || [];
  $: rowMax = timelineRows.reduce((max, row) => Math.max(max, row.count || 0), 0);
  $: totalForPie = actionDistribution.reduce((sum, item) => sum + (item.count || 0), 0);
  $: pieSegments = (() => {
    let cursor = 0;
    const segments = [];

    for (const item of actionDistribution) {
      const count = item.count || 0;
      const size = totalForPie > 0 ? (count / totalForPie) * 360 : 0;
      const from = cursor;
      const to = cursor + size;
      cursor = to;

      segments.push({
        action: item.action,
        count,
        color: actionColors[item.action] || actionColors.other,
        from,
        to,
      });
    }

    return segments;
  })();

  function pieStyle() {
    if (!pieSegments.length || totalForPie === 0) {
      return 'conic-gradient(var(--line) 0deg 360deg)';
    }

    return `conic-gradient(${pieSegments.map((item) => `${item.color} ${item.from}deg ${item.to}deg`).join(', ')})`;
  }

  onMount(() => {
    setDefaults();
    void refresh();
  });
</script>

<section class="panel">
  <div class="panel-head">
    <h2>Moderation statistics</h2>
    <button type="button" on:click={exportCsv}>Export CSV</button>
  </div>

  <form class="toolbar-grid" on:submit|preventDefault={refresh}>
    <label>
      Since
      <input type="datetime-local" bind:value={since} />
    </label>
    <label>
      Until
      <input type="datetime-local" bind:value={until} />
    </label>
    <label>
      Actor
      <input type="text" bind:value={actor} placeholder="player id" />
    </label>
    <label>
      Target
      <input type="text" bind:value={target} placeholder="player id" />
    </label>
    <label>
      Room
      <input type="text" bind:value={room} placeholder="room id" />
    </label>
    <label>
      Action
      <select bind:value={action}>
        {#each actionOptions as option}
          <option value={option}>{option || 'all actions'}</option>
        {/each}
      </select>
    </label>
    <label>
      Result
      <select bind:value={result}>
        {#each resultOptions as option}
          <option value={option}>{option || 'all results'}</option>
        {/each}
      </select>
    </label>
    <label>
      Limit
      <input type="number" min="10" max="500" bind:value={limit} />
    </label>

    <button type="submit">Refresh</button>
  </form>

  <p class="muted">{filterSummary}</p>

  {#if loading}
    <p class="muted">Loading moderation window...</p>
  {:else if forbidden}
    <AccessDeniedNotice message="Vous n'avez pas l'autorisation d'accéder aux statistiques de modération." />
  {:else if error}
    <p class="empty-state danger">{error}</p>
  {/if}

  <div class="metric-grid compact">
    <article><span>Total actions</span><strong>{formatNumber(data?.totals?.total || 0)}</strong></article>
    <article><span>Successful</span><strong>{formatNumber(data?.totals?.success || 0)}</strong></article>
    <article><span>Denied</span><strong>{formatNumber(data?.totals?.denied || 0)}</strong></article>
    <article><span>Failed</span><strong>{formatNumber(data?.totals?.failed || 0)}</strong></article>
    <article><span>Renewals</span><strong>{formatNumber(data?.totals?.renewalCount || 0)}</strong></article>
    <article><span>Avg duration</span><strong>{formatDuration(data?.totals?.averageDurationSeconds || 0)}</strong></article>
    <article><span>Active bans</span><strong>{formatNumber(data?.totals?.activeBans || 0)}</strong></article>
    <article><span>Bans retention</span><strong>{((data?.totals?.retentionRate || 0) * 100).toFixed(2)}%</strong></article>
  </div>

  <div class="split-grid" style="margin-top: 14px;">
    <article class="panel">
      <h3>Actions distribution</h3>
      <div class="chart-wrap">
        <div class="donut" style={`background: ${pieStyle()};`}></div>
        <div class="donut-legend">
          {#each actionDistribution as entry}
            <div>
              <span class="legend-dot" style={`background: ${actionColors[entry.action] || actionColors.other};`}></span>
              <span>{entry.action || 'unknown'} — {formatNumber(entry.count)}</span>
            </div>
          {/each}
        </div>
      </div>
    </article>

    <article class="panel">
      <h3>Trend</h3>
      <div class="bar-chart">
        {#each timelineRows as bucket}
          <div class="bar-row">
            <div class="bar-label">{bucket.label}</div>
            <div class="bar-track">
              <div class="bar-fill" style={`width:${rowMax > 0 ? `${(bucket.count / rowMax) * 100}%` : '0%'}`}></div>
            </div>
            <span class="muted">{bucket.count}</span>
          </div>
        {:else}
          <p class="muted">No buckets in selected window.</p>
        {/each}
      </div>
    </article>
  </div>

  <section class="split-grid" style="margin-top: 14px;">
    <div class="panel">
      <h3>Top actors</h3>
      <table>
        <thead><tr><th>Actor</th><th>Rows</th></tr></thead>
        <tbody>
          {#each data?.topActors || [] as row}
            <tr>
              <td>
                <EntityLink id={row.actorPlayerId} label={row.actorName || `player #${row.actorPlayerId}`} {openPlayer} {openItem} />
              </td>
              <td>{formatNumber(row.count)}</td>
            </tr>
          {:else}
            <tr><td colspan="2" class="muted">No actor activity.</td></tr>
          {/each}
        </tbody>
      </table>
    </div>

    <div class="panel">
      <h3>Top targets</h3>
      <table>
        <thead><tr><th>Target</th><th>Rows</th></tr></thead>
        <tbody>
          {#each data?.topTargets || [] as row}
            <tr>
              <td>
                <EntityLink id={row.targetPlayerId} label={row.targetName || `player #${row.targetPlayerId}`} {openPlayer} {openItem} />
              </td>
              <td>{formatNumber(row.count)}</td>
            </tr>
          {:else}
            <tr><td colspan="2" class="muted">No target activity.</td></tr>
          {/each}
        </tbody>
      </table>
    </div>
  </section>

  <div class="panel" style="margin-top: 14px;">
    <h3>Top rooms</h3>
    <table>
      <thead><tr><th>Room</th><th>Name</th><th>Rows</th></tr></thead>
      <tbody>
        {#each data?.topRooms || [] as row}
          <tr>
            <td>room #{row.roomId}</td>
            <td>{row.roomName || '-'}</td>
            <td>{formatNumber(row.count)}</td>
          </tr>
        {:else}
          <tr><td colspan="3" class="muted">No room activity.</td></tr>
        {/each}
      </tbody>
    </table>
  </div>

  <div class="panel" style="margin-top: 14px;">
    <div class="panel-head">
      <h3>Recent moderation events</h3>
      <span class="muted">page {data?.totals?.page || 1} · page size {data?.totals?.limit || 0}</span>
    </div>
    <table>
      <thead>
        <tr>
          <th>Time</th>
          <th>Action</th>
          <th>Actor</th>
          <th>Target</th>
          <th>Room</th>
          <th>Duration</th>
          <th>Result</th>
          <th>Renewal</th>
          <th>Reason</th>
        </tr>
      </thead>
      <tbody>
        {#each data?.rows || [] as row}
          <tr>
            <td>{formatDate(row.occurredAt)}</td>
            <td><code>{row.action}</code></td>
            <td>
              <EntityLink id={row.actorPlayerId} label={row.actorName || `player #${row.actorPlayerId || ''}`} {openPlayer} {openItem} />
            </td>
            <td>
              <EntityLink id={row.targetPlayerId} label={row.targetName || `player #${row.targetPlayerId || ''}`} {openPlayer} {openItem} />
            </td>
            <td>{row.roomName ? `${row.roomName} (#${row.roomId})` : row.roomId ? `#${row.roomId}` : '-'}</td>
            <td>{row.duration ? row.duration : (row.durationSeconds ? `${row.durationSeconds}s` : '-')}</td>
            <td>{row.result}</td>
            <td>
              {#if row.isRenewal}
                <span class="positive">Yes</span>
              {:else}
                No
              {/if}
            </td>
            <td class="truncate">{row.reason || '-'}</td>
          </tr>
        {:else}
          <tr><td colspan="9" class="muted">No moderation events for this filter.</td></tr>
        {/each}
      </tbody>
    </table>
  </div>
</section>

