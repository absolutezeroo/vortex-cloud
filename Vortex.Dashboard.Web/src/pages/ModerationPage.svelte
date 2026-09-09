<script lang="ts">
  import { readNumberParam, writeParams } from '../lib/urlState';
  import FilterBar from '../components/FilterBar.svelte';
  import PageHeader from '../components/PageHeader.svelte';
  import { readFilterValues } from '../lib/filters';
  import type { FilterField } from '../lib/filters';
  import LoadingOverlay from '../components/LoadingOverlay.svelte';

  import { onMount } from 'svelte';
  import { apiGet } from '../lib/api';
  import { formatDate, formatDuration, formatNumber } from '../lib/format';
  import { isPermissionDeniedError } from '../lib/permissions';
  import AccessDeniedNotice from '../components/AccessDeniedNotice.svelte';
  import EntityLink from '../components/EntityLink.svelte';
  import Pagination from '../components/Pagination.svelte';
  import StatCard from '../components/StatCard.svelte';
  import { Hash, Activity, TriangleAlert, Timer } from '@lucide/svelte';
  import { openPlayer, openItem } from '../lib/session';
  import { t, translate } from '../lib/i18n';
  import type { ModerationStats } from '../lib/apiTypes';

  /** One slice of the action pie, already turned into degrees. */
  type PieSegment = {
    action: string;
    count: number;
    color: string;
    from: number;
    to: number;
  };

  const actionOptions = [
    '',
    'moderation.kick',
    'moderation.mute',
    'moderation.ban',
    'moderation.alert',
    'moderation.denied',
  ];

  const resultOptions = ['', 'Success', 'Failed', 'Denied'];

  const actionColors: Record<string, string> = {
    'moderation.kick': 'var(--accent)',
    'moderation.mute': 'var(--ok)',
    'moderation.ban': 'var(--danger)',
    'moderation.alert': 'var(--warning)',
    'moderation.denied': '#9f6ce1',
    other: '#64748b',
  };

  // Seven controls the page drew itself, now declared instead: same questions, plus the address bar
  // and the chips that say which of them are on.
  const FILTERS: FilterField[] = [
    { id: 'since', label: translate('moderation.since'), kind: 'datetime' },
    { id: 'until', label: translate('moderation.until'), kind: 'datetime' },
    { id: 'actor', label: translate('moderation.actor'), kind: 'entity', picker: 'user' },
    { id: 'target', label: translate('moderation.target'), kind: 'entity', picker: 'user' },
    { id: 'room', label: translate('moderation.room'), kind: 'entity', picker: 'room' },
    {
      id: 'action',
      label: translate('moderation.action'),
      kind: 'select',
      anyLabel: translate('moderation.allActions'),
      // The action IS the label, as it was before: these are the audit's own action keys, and the
      // page has never had a translation for them. Inventing one here would have printed the path.
      options: actionOptions.filter(Boolean).map((value) => ({ value, label: value })),
    },
    {
      id: 'result',
      label: translate('moderation.result'),
      kind: 'select',
      anyLabel: translate('moderation.allResults'),
      options: resultOptions.filter(Boolean).map((value) => ({
        value,
        label: translate(`common.result${value}`),
      })),
    },
  ];

  let filters = $state(readFilterValues(FILTERS));
  let limit = $state('80');
  let page = $state(readNumberParam('page', 1));

  $effect(() => {
    writeParams({ page: page > 1 ? page : '' });
  });
  let loading = $state(false);
  let error = $state('');
  let forbidden = $state(false);
  let data = $state<ModerationStats | null>(null);

  let filterSummary = $state('');
  $effect(() => {
    if (!filterSummary) filterSummary = translate('moderation.noDataLoaded');
  });

  function toLocalInputValue(value: Date | string) {
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

  function currentIso(value: string) {
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

    // Only when the URL did not already name a window: arriving from a shared link means somebody
    // chose those dates, and overwriting them with "the last day" would answer a different question
    // than the one they sent.
    if (!filters.since && !filters.until) {
      filters = {
        ...filters,
        since: toLocalInputValue(start),
        until: toLocalInputValue(end),
      };
    }
  }

  function csvEscape(value: unknown) {
    const text = value === null || value === undefined ? '' : String(value);
    if (text.includes('"') || text.includes(',') || text.includes('\n')) {
      return `"${text.replace(/"/g, '""')}"`;
    }

    return text;
  }

  function buildParams() {
    const params = new URLSearchParams();

    if (filters.since) {
      const iso = currentIso(filters.since);
      if (iso) params.set('since', iso);
    }

    if (filters.until) {
      const iso = currentIso(filters.until);
      if (iso) params.set('until', iso);
    }

    if (filters.actor.trim()) {
      params.set('actor', filters.actor.trim());
    }

    if (filters.target.trim()) {
      params.set('target', filters.target.trim());
    }

    if (filters.room.trim()) {
      params.set('room', filters.room.trim());
    }

    if (filters.action) {
      params.set('action', filters.action);
    }

    if (filters.result) {
      params.set('result', filters.result);
    }

    if (limit) {
      params.set('limit', limit);
    }

    params.set('page', String(page));

    return params;
  }

  function updateSummary() {
    const total = data?.totals?.total || 0;
    const from = data?.window?.since || '';
    const to = data?.window?.until || '';

    filterSummary = from
      ? translate('moderation.summaryWithWindow', { count: total, from: formatDate(from), to: formatDate(to) })
      : translate('moderation.summaryNoWindow', { count: total });
  }

  async function refresh() {
    loading = true;
    error = '';
    forbidden = false;

    try {
      const params = buildParams();
      data = await apiGet<ModerationStats>(
        `/api/v1/forensics/moderation/stats?${params.toString()}`,
      );
      updateSummary();
    } catch (err) {
      if (isPermissionDeniedError(err)) {
        forbidden = true;
        error = '';
        data = null;
        return;
      }

      error = (err as Error).message;
      data = null;
    } finally {
      loading = false;
    }
  }

  function applyFilters() {
    page = 1;
    void refresh();
  }

  let totalPages = $derived(Math.max(1, Math.ceil((data?.totals?.total || 0) / Number(limit || 1))));

  function goToPage(next: number) {
    page = Math.min(totalPages, Math.max(1, next));
    void refresh();
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

  let actionDistribution = $derived(data?.distribution?.byAction || []);
  let timelineRows = $derived(data?.timeline || []);
  let rowMax = $derived(timelineRows.reduce((max, row) => Math.max(max, row.count || 0), 0));
  let totalForPie = $derived(actionDistribution.reduce((sum, item) => sum + (item.count || 0), 0));
  let pieSegments = $derived((() => {
    let cursor = 0;
    const segments: PieSegment[] = [];

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
  })());

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
  <PageHeader title={$t('moderation.title')} description={$t('moderation.description')}>
    {#snippet actions()}
      <button type="button" onclick={refresh} class="warning">{$t('common.refresh')}</button>
      <button type="button" class="ghost-button" onclick={exportCsv}>{$t('moderation.exportCsv')}</button>
    {/snippet}
  </PageHeader>
</section>

<section class="panel">

  <FilterBar fields={FILTERS} bind:values={filters} onchange={applyFilters} />

  <!-- See AuditPage: how many rows come back is a setting, not a question about them. -->
  <label class="page-size">
    {$t('moderation.limit')}
    <input
      autocomplete="off"
      spellcheck="false"
      type="number"
      min="10"
      max="500"
      bind:value={limit}
      onchange={applyFilters}
    />
  </label>

</section>

<section class="panel">

  <p class="muted">{filterSummary}</p>

  {#if forbidden}
    <AccessDeniedNotice message={$t('moderation.accessDenied')} />
  {:else if error}
    <p class="empty-state danger" role="alert">{error}</p>
  {/if}

  <div class="metric-grid compact">
    <StatCard label={$t('moderation.totalActions')} value={formatNumber(data?.totals?.total || 0)}>
      {#snippet icon()}
        <Hash size={15} strokeWidth={2} aria-hidden="true" />
      {/snippet}
    </StatCard>
    <StatCard label={$t('moderation.successful')} value={formatNumber(data?.totals?.success || 0)}>
      {#snippet icon()}
        <Activity size={15} strokeWidth={2} aria-hidden="true" />
      {/snippet}
    </StatCard>
    <StatCard label={$t('moderation.denied')} value={formatNumber(data?.totals?.denied || 0)}>
      {#snippet icon()}
        <TriangleAlert size={15} strokeWidth={2} aria-hidden="true" />
      {/snippet}
    </StatCard>
    <StatCard label={$t('moderation.failed')} value={formatNumber(data?.totals?.failed || 0)}>
      {#snippet icon()}
        <TriangleAlert size={15} strokeWidth={2} aria-hidden="true" />
      {/snippet}
    </StatCard>
    <StatCard label={$t('moderation.renewals')} value={formatNumber(data?.totals?.renewalCount || 0)}>
      {#snippet icon()}
        <Hash size={15} strokeWidth={2} aria-hidden="true" />
      {/snippet}
    </StatCard>
    <StatCard label={$t('moderation.avgDuration')} value={formatDuration(data?.totals?.averageDurationSeconds || 0)}>
      {#snippet icon()}
        <Timer size={15} strokeWidth={2} aria-hidden="true" />
      {/snippet}
    </StatCard>
    <StatCard label={$t('moderation.activeBans')} value={formatNumber(data?.totals?.activeBans || 0)}>
      {#snippet icon()}
        <TriangleAlert size={15} strokeWidth={2} aria-hidden="true" />
      {/snippet}
    </StatCard>
    <StatCard label={$t('moderation.bansRetention')}>
      {#snippet icon()}
        <Activity size={15} strokeWidth={2} aria-hidden="true" />
      {/snippet}
      {#snippet value()}
        <span>{((data?.totals?.retentionRate || 0) * 100).toFixed(2)}%</span>
      {/snippet}
    </StatCard>
  </div>
</section>

<!-- No wrapper here. Every block below is already a panel; one more around them draws a border
     around a row of borders, which is what the whole page did before the totals moved out. -->
<div class="split-grid" style="margin-top: 12px;">
    <article class="panel">
      <h3>{$t('moderation.actionsDistribution')}</h3>
      <div class="chart-wrap">
        <div class="donut" style={`background: ${pieStyle()};`}></div>
        <div class="donut-legend">
          {#each actionDistribution as entry}
            <div>
              <span class="legend-dot" style={`background: ${actionColors[entry.action] || actionColors.other};`}></span>
              <span>{entry.action || $t('moderation.unknown')} — {formatNumber(entry.count)}</span>
            </div>
          {/each}
        </div>
      </div>
    </article>

    <article class="panel">
      <h3>{$t('moderation.trend')}</h3>
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
          <p class="muted">{$t('moderation.noBuckets')}</p>
        {/each}
      </div>
    </article>
  </div>

<section class="split-grid" style="margin-top: 14px;">
    <div class="panel">
      <h3>{$t('moderation.topActors')}</h3>
      <table>
        <thead><tr><th>{$t('moderation.colActor')}</th><th>{$t('moderation.colRows')}</th></tr></thead>
        <tbody>
          {#each data?.topActors || [] as row}
            <tr>
              <td>
                <EntityLink id={row.actorPlayerId} label={row.actorName || `player #${row.actorPlayerId}`} {openPlayer} {openItem} />
              </td>
              <td>{formatNumber(row.count)}</td>
            </tr>
          {:else}
            <tr><td colspan="2" class="muted">{$t('moderation.noActorActivity')}</td></tr>
          {/each}
        </tbody>
      </table>
    </div>

    <div class="panel">
      <h3>{$t('moderation.topTargets')}</h3>
      <table>
        <thead><tr><th>{$t('moderation.colTarget')}</th><th>{$t('moderation.colRows')}</th></tr></thead>
        <tbody>
          {#each data?.topTargets || [] as row}
            <tr>
              <td>
                <EntityLink id={row.targetPlayerId} label={row.targetName || `player #${row.targetPlayerId}`} {openPlayer} {openItem} />
              </td>
              <td>{formatNumber(row.count)}</td>
            </tr>
          {:else}
            <tr><td colspan="2" class="muted">{$t('moderation.noTargetActivity')}</td></tr>
          {/each}
        </tbody>
      </table>
    </div>
  </section>

  <div class="panel" style="margin-top: 14px;">
    <h3>{$t('moderation.topRooms')}</h3>
    <table>
      <thead><tr><th>{$t('moderation.colRoom')}</th><th>{$t('moderation.colName')}</th><th>{$t('moderation.colRows')}</th></tr></thead>
      <tbody>
        {#each data?.topRooms || [] as row}
          <tr>
            <td>room #{row.roomId}</td>
            <td>{row.roomName || '-'}</td>
            <td>{formatNumber(row.count)}</td>
          </tr>
        {:else}
          <tr><td colspan="3" class="muted">{$t('moderation.noRoomActivity')}</td></tr>
        {/each}
      </tbody>
    </table>
  </div>

  <div class="panel" style="margin-top: 14px;">
    <div class="panel-head">
      <h3>{$t('moderation.recentEvents')}</h3>
      <span class="muted">{$t('moderation.rowsPage', { count: data?.totals?.total || 0, page: data?.totals?.page || 1, totalPages })}</span>
    </div>
    <div class="table-wrap">
      <table>
        <thead>
          <tr>
            <th>{$t('moderation.colTime')}</th>
            <th>{$t('moderation.colAction')}</th>
            <th>{$t('moderation.colActor')}</th>
            <th>{$t('moderation.colTarget')}</th>
            <th>{$t('moderation.colRoom')}</th>
            <th>{$t('moderation.colDuration')}</th>
            <th>{$t('moderation.colResult')}</th>
            <th>{$t('moderation.colRenewal')}</th>
            <th>{$t('moderation.colReason')}</th>
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
                  <span class="positive">{$t('common.yes')}</span>
                {:else}
                  {$t('common.no')}
                {/if}
              </td>
              <td class="truncate">{row.reason || '-'}</td>
            </tr>
          {:else}
            <tr><td colspan="9" class="muted">{$t('moderation.noEvents')}</td></tr>
          {/each}
        </tbody>
      </table>
    </div>

    {#if (data?.totals?.total || 0) > 0}
      <Pagination
        page={page}
        pageCount={totalPages}
        pageWord={$t('common.page')}
        prevLabel={$t('common.prev')}
        nextLabel={$t('common.next')}
        disabled={loading}
        onchange={goToPage}
      />
    {/if}
  </div>

<LoadingOverlay show={loading} label={$t('moderation.loadingWindow')} />

<style>
  /* See AuditPage: the row count is a setting, so it reads quieter than the filters above it. */
  .page-size {
    display: inline-flex;
    align-items: center;
    gap: 8px;
    margin-top: 10px;
    color: var(--muted);
    font-size: 0.82rem;
  }

  .page-size input {
    width: 96px;
  }
</style>
