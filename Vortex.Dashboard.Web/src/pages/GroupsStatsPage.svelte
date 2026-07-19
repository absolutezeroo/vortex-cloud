<script>
  import { onMount } from 'svelte';
  import { apiGet } from '../lib/api.js';
  import { formatNumber, formatDate } from '../lib/format.js';
  import { isPermissionDeniedError } from '../lib/permissions.js';
  import { openPlayer } from '../lib/session.js';
  import AccessDeniedNotice from '../components/AccessDeniedNotice.svelte';
  import EntityLink from '../components/EntityLink.svelte';
  import LineChart from '../components/LineChart.svelte';
  import { t } from '../lib/i18n.js';

  const granularities = ['day', 'month', 'year'];

  function granularityLabel(value, translator) {
    return translator(`common.granularity${value.charAt(0).toUpperCase()}${value.slice(1)}`);
  }

  function resultLabel(value, translator) {
    if (value === 'Success') return translator('common.resultSuccess');
    if (value === 'Denied') return translator('common.resultDenied');
    if (value === 'Failed') return translator('common.resultFailed');
    return value;
  }

  let since = '';
  let until = '';
  let granularity = 'day';
  let loading = false;
  let forbidden = false;
  let error = '';
  let data = null;

  function toLocalDateValue(value) {
    const date = new Date(value);
    return Number.isNaN(date.getTime()) ? '' : date.toISOString().slice(0, 10);
  }

  function setDefaultWindow() {
    const end = new Date();
    const start = new Date(end.getTime() - 30 * 24 * 60 * 60 * 1000);
    since = toLocalDateValue(start);
    until = toLocalDateValue(end);
  }

  async function refresh() {
    loading = true;
    error = '';
    forbidden = false;

    const params = new URLSearchParams({ granularity });
    if (since) params.set('since', new Date(`${since}T00:00:00`).toISOString());
    if (until) params.set('until', new Date(`${until}T23:59:59`).toISOString());

    try {
      data = await apiGet(`/api/v1/groups/stats?${params}`);
    } catch (err) {
      if (isPermissionDeniedError(err)) {
        forbidden = true;
        data = null;
        return;
      }

      error = err.message;
      data = null;
    } finally {
      loading = false;
    }
  }

  $: growthSeries = data
    ? [
        {
          name: $t('groupsStats.totalGroups'),
          color: 'var(--accent)',
          points: (data.growth || []).map((p) => ({ label: p.label, value: p.groupsCreated })),
        },
      ]
    : [];

  onMount(() => {
    setDefaultWindow();
    void refresh();
  });
</script>

<section class="panel">
  <div class="panel-head"><h2>{$t('groupsStats.title')}</h2></div>
  <p class="muted">{$t('groupsStats.description')}</p>

  <form class="toolbar-grid" on:submit|preventDefault={refresh}>
    <label>
      {$t('common.since')}
      <input type="date" bind:value={since} />
    </label>
    <label>
      {$t('common.until')}
      <input type="date" bind:value={until} />
    </label>
    <label>
      {$t('common.granularity')}
      <select bind:value={granularity}>
        {#each granularities as g}
          <option value={g}>{granularityLabel(g, $t)}</option>
        {/each}
      </select>
    </label>
    <button type="submit" disabled={loading}>{$t('common.refresh')}</button>
  </form>

  {#if loading}
    <p class="muted">{$t('common.loading')}</p>
  {:else if forbidden}
    <AccessDeniedNotice message={$t('groupsStats.accessDenied')} />
  {:else if error}
    <p class="empty-state danger">{error}</p>
  {/if}
</section>

{#if data}
  <div class="metric-grid" style="margin-top: 12px;">
    <article><span>{$t('groupsStats.totalGroups')}</span><strong>{formatNumber(data.totals.totalGroups)}</strong></article>
    <article><span>{$t('groupsStats.totalMembers')}</span><strong>{formatNumber(data.totals.totalMembers)}</strong></article>
    <article><span>{$t('groupsStats.totalThreads')}</span><strong>{formatNumber(data.totals.totalThreads)}</strong></article>
    <article><span>{$t('groupsStats.totalPosts')}</span><strong>{formatNumber(data.totals.totalPosts)}</strong></article>
    <article><span>{$t('groupsStats.avgMembersPerGroup')}</span><strong>{data.totals.avgMembersPerGroup}</strong></article>
  </div>

  <div class="panel" style="margin-top: 12px;">
    <div class="panel-head"><h2>{$t('groupsStats.growthChartTitle', { granularity: granularityLabel(granularity, $t) })}</h2></div>
    <LineChart series={growthSeries} valueFormatter={(v) => formatNumber(v)} />
  </div>

  <div class="panel" style="margin-top: 12px;">
    <div class="panel-head"><h2>{$t('groupsStats.topByMembersTitle')}</h2></div>
    <div class="table-wrap">
      <table>
        <thead>
          <tr>
            <th>{$t('groupsStats.colName')}</th>
            <th>{$t('groupsStats.colBadge')}</th>
            <th>{$t('groupsStats.colOwner')}</th>
            <th>{$t('groupsStats.colMembers')}</th>
            <th>{$t('groupsStats.colRoom')}</th>
          </tr>
        </thead>
        <tbody>
          {#each data.topGroupsByMembers || [] as g}
            <tr>
              <td>{g.name}</td>
              <td>{g.badge}</td>
              <td><EntityLink type="player" id={g.ownerId} label={g.ownerName} {openPlayer} /></td>
              <td>{formatNumber(g.memberCount)}</td>
              <td>{g.roomId}</td>
            </tr>
          {:else}
            <tr><td colspan="5" class="muted">{$t('groupsStats.noGroups')}</td></tr>
          {/each}
        </tbody>
      </table>
    </div>
  </div>

  <div class="panel" style="margin-top: 12px;">
    <div class="panel-head"><h2>{$t('groupsStats.topByForumTitle')}</h2></div>
    <div class="table-wrap">
      <table>
        <thead>
          <tr>
            <th>{$t('groupsStats.colName')}</th>
            <th>{$t('groupsStats.colThreads')}</th>
            <th>{$t('groupsStats.colPosts')}</th>
          </tr>
        </thead>
        <tbody>
          {#each data.topGroupsByForumActivity || [] as g}
            <tr>
              <td>{g.name}</td>
              <td>{formatNumber(g.threadCount)}</td>
              <td>{formatNumber(g.postCount)}</td>
            </tr>
          {:else}
            <tr><td colspan="3" class="muted">{$t('groupsStats.noForumActivity')}</td></tr>
          {/each}
        </tbody>
      </table>
    </div>
  </div>

  <div class="panel" style="margin-top: 12px;">
    <div class="panel-head"><h2>{$t('groupsStats.recentActivityTitle')}</h2></div>
    <div class="table-wrap">
      <table>
        <thead>
          <tr>
            <th>{$t('groupsStats.colWhen')}</th>
            <th>{$t('groupsStats.colAction')}</th>
            <th>{$t('groupsStats.colActor')}</th>
            <th>{$t('groupsStats.colResult')}</th>
          </tr>
        </thead>
        <tbody>
          {#each data.recentActivity || [] as a}
            <tr>
              <td>{formatDate(a.occurredAt)}</td>
              <td><code>{a.action}</code></td>
              <td><EntityLink type="player" id={a.actorPlayerId} label={a.actorPlayerName} {openPlayer} /></td>
              <td>{resultLabel(a.result, $t)}</td>
            </tr>
          {:else}
            <tr><td colspan="4" class="muted">{$t('groupsStats.noActivity')}</td></tr>
          {/each}
        </tbody>
      </table>
    </div>
  </div>
{/if}
