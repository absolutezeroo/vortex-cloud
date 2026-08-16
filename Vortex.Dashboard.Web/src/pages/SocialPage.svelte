<script>

  // The social graph and the guild forums. Friendship rows are stored in both directions, so the
  // API halves them — the raw row count is shown next to it so the two numbers never look like a
  // discrepancy.
  import { onMount } from 'svelte';
  import { apiGet } from '../lib/api.js';
  import { formatNumber, formatDate } from '../lib/format.js';
  import { isPermissionDeniedError } from '../lib/permissions.js';
  import { openPlayer } from '../lib/session.js';
  import AccessDeniedNotice from '../components/AccessDeniedNotice.svelte';
  import AssetImage from '../components/AssetImage.svelte';
  import EntityLink from '../components/EntityLink.svelte';
  import LineChart from '../components/LineChart.svelte';
  import StatCard from '../components/StatCard.svelte';
  import { Users, MessageSquare, UserPlus, ShieldOff, MessagesSquare, Shield } from '@lucide/svelte';
  import { t } from '../lib/i18n.js';

  const granularities = ['day', 'month', 'year'];

  function granularityLabel(value, translator) {
    return translator(`common.granularity${value.charAt(0).toUpperCase()}${value.slice(1)}`);
  }

  let since = $state('');
  let until = $state('');
  let granularity = $state('day');
  let loading = $state(false);
  let forbidden = $state(false);
  let error = $state('');
  let data = $state(null);

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
      data = await apiGet(`/api/v1/social/stats?${params}`);
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

  let messageSeries = $derived(data
    ? [
        {
          name: $t('social.messages'),
          color: 'var(--accent)',
          points: (data.timeline || []).map((p) => ({ label: p.label, value: p.messages })),
        },
      ]
    : []);

  onMount(() => {
    setDefaultWindow();
    void refresh();
  });
</script>

<section class="panel">
  <div class="panel-head"><h2>{$t('social.title')}</h2></div>
  <p class="muted">{$t('social.description')}</p>

  <form class="toolbar-grid" onsubmit={(event) => { event.preventDefault(); refresh(); }}>
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
    <AccessDeniedNotice message={$t('social.accessDenied')} />
  {:else if error}
    <p class="empty-state danger">{error}</p>
  {/if}
</section>

{#if data}
  <div class="metric-grid" style="margin-top: 12px;">
    <StatCard
      label={$t('social.friendships')}
      value={formatNumber(data.totals.friendships)}
      sub={$t('social.friendRows', { rows: formatNumber(data.totals.friendRows) })}
    >
      {#snippet icon()}
        <Users size={15} strokeWidth={2} aria-hidden="true" />
      {/snippet}
    </StatCard>
    <StatCard label={$t('social.playersWithFriends')} value={formatNumber(data.totals.playersWithFriends)}>
      {#snippet icon()}
        <Users size={15} strokeWidth={2} aria-hidden="true" />
      {/snippet}
    </StatCard>
    <StatCard label={$t('social.pendingRequests')} value={formatNumber(data.totals.pendingRequests)}>
      {#snippet icon()}
        <UserPlus size={15} strokeWidth={2} aria-hidden="true" />
      {/snippet}
    </StatCard>
    <StatCard
      label={$t('social.blocked')}
      value={formatNumber(data.totals.blockedPairs)}
      sub={$t('social.ignored', { count: formatNumber(data.totals.ignoredPairs) })}
    >
      {#snippet icon()}
        <ShieldOff size={15} strokeWidth={2} aria-hidden="true" />
      {/snippet}
    </StatCard>
    <StatCard
      label={$t('social.messages')}
      value={formatNumber(data.totals.totalMessages)}
      sub={$t('social.undelivered', { count: formatNumber(data.totals.undelivered) })}
    >
      {#snippet icon()}
        <MessageSquare size={15} strokeWidth={2} aria-hidden="true" />
      {/snippet}
    </StatCard>
    <StatCard
      label={$t('social.threads')}
      value={formatNumber(data.totals.threads)}
      sub={$t('social.posts', { count: formatNumber(data.totals.posts) })}
    >
      {#snippet icon()}
        <MessagesSquare size={15} strokeWidth={2} aria-hidden="true" />
      {/snippet}
    </StatCard>
  </div>

  <div class="panel" style="margin-top: 12px;">
    <div class="panel-head"><h2>{$t('social.messageChartTitle', { granularity: granularityLabel(granularity, $t) })}</h2></div>
    <LineChart series={messageSeries} valueFormatter={(v) => formatNumber(v)} />
  </div>

  <div class="split-grid" style="margin-top: 12px;">
    <div class="panel">
      <div class="panel-head"><h2>{$t('social.topSendersTitle')}</h2></div>
      <div class="table-wrap">
        <table>
          <thead><tr><th>{$t('social.colPlayer')}</th><th>{$t('social.colMessages')}</th></tr></thead>
          <tbody>
            {#each data.topSenders || [] as row}
              <tr>
                <td><EntityLink type="player" id={row.playerId} label={row.playerName} {openPlayer} /></td>
                <td>{formatNumber(row.messages)}</td>
              </tr>
            {:else}
              <tr><td colspan="2" class="muted">{$t('social.noMessages')}</td></tr>
            {/each}
          </tbody>
        </table>
      </div>
    </div>

    <div class="panel">
      <div class="panel-head"><h2>{$t('social.topFriendedTitle')}</h2></div>
      <div class="table-wrap">
        <table>
          <thead><tr><th>{$t('social.colPlayer')}</th><th>{$t('social.colFriends')}</th></tr></thead>
          <tbody>
            {#each data.topFriended || [] as row}
              <tr>
                <td><EntityLink type="player" id={row.playerId} label={row.playerName} {openPlayer} /></td>
                <td>{formatNumber(row.friends)}</td>
              </tr>
            {:else}
              <tr><td colspan="2" class="muted">{$t('social.noFriends')}</td></tr>
            {/each}
          </tbody>
        </table>
      </div>
    </div>
  </div>

  <div class="panel" style="margin-top: 12px;">
    <div class="panel-head"><h2>{$t('social.forumActivityTitle')}</h2></div>
    <div class="split-grid">
      <div class="table-wrap">
        <table>
          <thead><tr><th>{$t('social.colThreadState')}</th><th>{$t('social.colCount')}</th></tr></thead>
          <tbody>
            {#each data.forums.threadsByState || [] as row}
              <tr><td>{row.state}</td><td>{formatNumber(row.count)}</td></tr>
            {:else}
              <tr><td colspan="2" class="muted">{$t('social.noThreads')}</td></tr>
            {/each}
          </tbody>
        </table>
      </div>
      <div class="table-wrap">
        <table>
          <thead><tr><th>{$t('social.colPostState')}</th><th>{$t('social.colCount')}</th></tr></thead>
          <tbody>
            {#each data.forums.postsByState || [] as row}
              <tr><td>{row.state}</td><td>{formatNumber(row.count)}</td></tr>
            {:else}
              <tr><td colspan="2" class="muted">{$t('social.noPosts')}</td></tr>
            {/each}
          </tbody>
        </table>
      </div>
    </div>
  </div>

  <div class="panel" style="margin-top: 12px;">
    <div class="panel-head"><h2>{$t('social.topForumsTitle')}</h2></div>
    <div class="table-wrap">
      <table>
        <thead>
          <tr>
            <th>{$t('social.colGuild')}</th>
            <th>{$t('social.colThreads')}</th>
            <th>{$t('social.colPosts')}</th>
            <th>{$t('social.colLastPost')}</th>
          </tr>
        </thead>
        <tbody>
          {#each data.forums.topGroups || [] as row}
            <tr>
              <td>
                <span class="guild-cell">
                  <AssetImage src={row.badgeUrl} alt="" size={28} fallbackIcon={Shield} />
                  <span>{row.groupName || `#${row.groupId}`}</span>
                </span>
              </td>
              <td>{formatNumber(row.threads)}</td>
              <td>{formatNumber(row.postCount)}</td>
              <td>{row.lastPostAt ? formatDate(row.lastPostAt) : '—'}</td>
            </tr>
          {:else}
            <tr><td colspan="4" class="muted">{$t('social.noThreads')}</td></tr>
          {/each}
        </tbody>
      </table>
    </div>
  </div>

  <div class="panel" style="margin-top: 12px;">
    <div class="panel-head"><h2>{$t('social.recentThreadsTitle')}</h2></div>
    <div class="table-wrap">
      <table>
        <thead>
          <tr>
            <th>{$t('social.colSubject')}</th>
            <th>{$t('social.colGuild')}</th>
            <th>{$t('social.colAuthor')}</th>
            <th>{$t('social.colThreadState')}</th>
            <th>{$t('social.colPosts')}</th>
            <th>{$t('social.colLastPost')}</th>
          </tr>
        </thead>
        <tbody>
          {#each data.forums.recentThreads || [] as row}
            <tr>
              <td>
                {row.subject}
                {#if row.isPinned}<span class="status-badge status-badge--ok">{$t('social.pinned')}</span>{/if}
              </td>
              <td>
                <span class="guild-cell">
                  <AssetImage src={row.badgeUrl} alt="" size={24} fallbackIcon={Shield} />
                  <span>{row.groupName || `#${row.groupId}`}</span>
                </span>
              </td>
              <td><EntityLink type="player" id={row.authorId} label={row.authorName} {openPlayer} /></td>
              <td>{row.state}</td>
              <td>{formatNumber(row.postCount)}</td>
              <td>{row.lastPostAt ? formatDate(row.lastPostAt) : formatDate(row.createdAt)}</td>
            </tr>
          {:else}
            <tr><td colspan="6" class="muted">{$t('social.noThreads')}</td></tr>
          {/each}
        </tbody>
      </table>
    </div>
  </div>
{/if}

<style>
  .guild-cell {
    display: inline-flex;
    align-items: center;
    gap: 8px;
  }
</style>
