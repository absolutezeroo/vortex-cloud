<script lang="ts">
  import { readNumberParam, writeParams } from '../lib/urlState';

  import { onMount } from 'svelte';
  import { CheckCircle2, Clock, ListChecks, Trophy } from '@lucide/svelte';
  import AccessDeniedNotice from '../components/AccessDeniedNotice.svelte';
  import AssetImage from '../components/AssetImage.svelte';
  import EmptyState from '../components/EmptyState.svelte';
  import PageHeader from '../components/PageHeader.svelte';
  import Pagination from '../components/Pagination.svelte';
  import PlayerCell from '../components/PlayerCell.svelte';
  import StatCard from '../components/StatCard.svelte';
  import Tabs from '../components/Tabs.svelte';
  import { apiGet } from '../lib/api';
  import { formatDate, formatNumber } from '../lib/format';
  import { isPermissionDeniedError } from '../lib/permissions';
  import { t } from '../lib/i18n';
  import type {
    AchievementResolutions,
    ResolutionChallenge,
    ResolutionOffer,
    ResolutionTotals,
  } from '../lib/apiTypes';

  // Read-only, and under the achievements capability rather than one of its own: the statue is a
  // view onto achievement progress, so anyone allowed to read that has no reason to be kept out.
  // Editing the offer list is still a SQL job -- there is no operations half here yet.

  const PAGE_SIZE = 25;

  let offers = $state<ResolutionOffer[]>([]);
  let challenges = $state<ResolutionChallenge[]>([]);
  let totals = $state<ResolutionTotals | null>(null);
  let truncated = $state(false);
  let loading = $state(false);
  let error = $state('');
  let forbidden = $state(false);

  let tab = $state('offers');
  let stateFilter = $state('');
  let search = $state('');
  let page = $state(readNumberParam('page', 1));

  $effect(() => {
    writeParams({ page: page > 1 ? page : '' });
  });

  async function load() {
    loading = true;
    error = '';
    forbidden = false;

    try {
      const query = stateFilter ? `?state=${encodeURIComponent(stateFilter)}` : '';
      const data = await apiGet<AchievementResolutions>(
        `/api/v1/achievements/resolutions${query}`,
      );

      offers = data.offers || [];
      challenges = data.challenges || [];
      totals = data.totals || null;
      truncated = Boolean(data.truncated);
    } catch (err) {
      if (isPermissionDeniedError(err)) {
        forbidden = true;
        offers = [];
        challenges = [];
        totals = null;
        return;
      }

      error = (err as Error).message;
    } finally {
      loading = false;
    }
  }

  // The state filter runs server-side because it decides what the 200-row cap keeps; the text
  // search is client-side over whatever came back.
  function onStateChange(value: string) {
    stateFilter = value;
    page = 1;
    load();
  }

  let filtered = $derived(challenges.filter((c) => {
    if (!search.trim()) return true;

    const needle = search.trim().toLowerCase();

    return (
      (c.playerName || '').toLowerCase().includes(needle) ||
      (c.achievementName || '').toLowerCase().includes(needle) ||
      String(c.itemId).includes(needle)
    );
  }));

  let pageCount = $derived(Math.max(1, Math.ceil(filtered.length / PAGE_SIZE)));
  let safePage = $derived(Math.min(page, pageCount));
  let pageRows = $derived(filtered.slice((safePage - 1) * PAGE_SIZE, safePage * PAGE_SIZE));
  // Reading `search` is what registers the dependency; the assignment is the effect.
  $effect(() => {
    void search;
    page = 1;
  });

  onMount(load);
</script>

<section class="panel">
  <PageHeader title={$t('achievementResolutions.title')} description={$t('achievementResolutions.lede')}>
    {#snippet actions()}
      <button type="button" onclick={load} class="warning">{$t('common.refresh')}</button>
    {/snippet}
  </PageHeader>

  {#if forbidden}
    <AccessDeniedNotice />
  {:else if error}
    <EmptyState kind="error" message={error} />
  {/if}
</section>

{#if !forbidden && !error}
  {#if totals}
    <div class="stats">
      <StatCard
        label={$t('achievementResolutions.statOffers')}
        value={formatNumber(totals.enabledOffers)}
        sub={$t('achievementResolutions.statOffersSub', { total: totals.offers })}
      />
      <StatCard
        label={$t('achievementResolutions.statTaken')}
        value={formatNumber(totals.taken)}
        sub={$t('achievementResolutions.statTakenSub', { players: totals.players })}
      />
      <StatCard
        label={$t('achievementResolutions.statCompleted')}
        value={formatNumber(totals.completed)}
        sub={`${totals.completionRate}%`}
        accent
      />
      <StatCard
        label={$t('achievementResolutions.statLive')}
        value={formatNumber(totals.live)}
        sub={$t('achievementResolutions.statLiveSub', { expired: totals.expired })}
      />
    </div>

    {#if totals.orphanedOffers > 0}
      <!-- Worth its own line: the grain drops these silently, so the picker is quietly shorter
           than the table says and nothing anywhere logs it. -->
      <p class="warn">
        {$t('achievementResolutions.orphanWarning', { count: totals.orphanedOffers })}
      </p>
    {/if}
  {/if}

  <Tabs
    bind:active={tab}
    storageKey="achievementResolutions"
    tabs={[
      {
        id: 'offers',
        label: $t('achievementResolutions.tabOffers'),
        icon: ListChecks,
        count: offers.length,
      },
      {
        id: 'challenges',
        label: $t('achievementResolutions.tabChallenges'),
        icon: Trophy,
        count: challenges.length,
      },
    ]}
  />

  {#if loading}
    <section class="panel">
      <EmptyState kind="loading" message={$t('common.loading')} />
    </section>
  {:else if tab === 'offers'}
    <section class="panel">
      <div class="panel-head"><h2>{$t('achievementResolutions.tabOffers')}</h2></div>
      {#if offers.length === 0}
        <EmptyState message={$t('achievementResolutions.emptyOffers')} />
      {:else}
        <div class="table-wrap">
          <table>
            <thead>
              <tr>
                <th>{$t('achievementResolutions.colAchievement')}</th>
                <th>{$t('achievementResolutions.colCategory')}</th>
                <th class="num">{$t('achievementResolutions.colLevels')}</th>
                <th class="num">{$t('achievementResolutions.colOffset')}</th>
                <th class="num">{$t('achievementResolutions.colTaken')}</th>
                <th class="num">{$t('achievementResolutions.colCompleted')}</th>
                <th class="num">{$t('achievementResolutions.colRate')}</th>
                <th>{$t('achievementResolutions.colEnabled')}</th>
              </tr>
            </thead>
            <tbody>
              {#each offers as offer (offer.id)}
                <tr class:orphan={offer.orphaned}>
                  <td>
                    {offer.achievementName || `#${offer.achievementId}`}
                    {#if offer.orphaned}
                      <span class="pill danger">{$t('achievementResolutions.orphaned')}</span>
                    {/if}
                  </td>
                  <td class="muted">{offer.category || '—'}</td>
                  <td class="num">{offer.levelCount}</td>
                  <td class="num">+{offer.targetLevelOffset}</td>
                  <td class="num">{formatNumber(offer.taken)}</td>
                  <td class="num">{formatNumber(offer.completed)}</td>
                  <td class="num">{offer.completionRate}%</td>
                  <td>
                    {#if offer.enabled}
                      <span class="pill ok">{$t('common.yes')}</span>
                    {:else}
                      <span class="pill">{$t('common.no')}</span>
                    {/if}
                  </td>
                </tr>
              {/each}
            </tbody>
          </table>
        </div>
      {/if}
    </section>
  {:else}
    <section class="panel">
      <div class="panel-head"><h2>{$t('achievementResolutions.tabChallenges')}</h2></div>
      <div class="filters">
        <input autocomplete="off" spellcheck="false"
          type="search"
          bind:value={search}
          placeholder={$t('achievementResolutions.searchPlaceholder')}
        />
        <select value={stateFilter} onchange={(e) => onStateChange(e.currentTarget.value)}>
          <option value="">{$t('achievementResolutions.stateAll')}</option>
          <option value="live">{$t('achievementResolutions.stateLive')}</option>
          <option value="completed">{$t('achievementResolutions.stateCompleted')}</option>
          <option value="expired">{$t('achievementResolutions.stateExpired')}</option>
        </select>
      </div>

      {#if truncated}
        <p class="warn">{$t('achievementResolutions.truncated')}</p>
      {/if}

      {#if filtered.length === 0}
        <EmptyState message={$t('achievementResolutions.emptyChallenges')} />
      {:else}
        <div class="table-wrap">
          <table>
            <thead>
              <tr>
                <th>{$t('achievementResolutions.colPlayer')}</th>
                <th>{$t('achievementResolutions.colAchievement')}</th>
                <th class="num">{$t('achievementResolutions.colProgress')}</th>
                <th>{$t('achievementResolutions.colStarted')}</th>
                <th>{$t('achievementResolutions.colDeadline')}</th>
                <th>{$t('achievementResolutions.colState')}</th>
                <th>{$t('achievementResolutions.colBadge')}</th>
              </tr>
            </thead>
            <tbody>
              {#each pageRows as row (row.id)}
                <tr>
                  <td><PlayerCell name={row.playerName} /></td>
                  <td>
                    {row.achievementName || `#${row.achievementId}`}
                    <span class="muted">· #{row.itemId}</span>
                  </td>
                  <td class="num">{row.reachedLevel}/{row.targetLevel}</td>
                  <td class="muted">{formatDate(row.startedAt)}</td>
                  <td class="muted">{formatDate(row.endsAt)}</td>
                  <td>
                    {#if row.state === 'completed'}
                      <span class="pill ok">
                        <CheckCircle2 size={12} aria-hidden="true" />
                        {$t('achievementResolutions.stateCompleted')}
                      </span>
                    {:else if row.state === 'live'}
                      <span class="pill live">
                        <Clock size={12} aria-hidden="true" />
                        {$t('achievementResolutions.stateLive')}
                      </span>
                    {:else}
                      <span class="pill">{$t('achievementResolutions.stateExpired')}</span>
                    {/if}
                  </td>
                  <td>
                    {#if row.badgeUrl}
                      <AssetImage src={row.badgeUrl} alt={row.badgeCode} size={28} />
                    {:else}
                      <span class="muted">—</span>
                    {/if}
                  </td>
                </tr>
              {/each}
            </tbody>
          </table>
        </div>

        <Pagination
          page={safePage}
          onchange={(next) => (page = next)}
          {pageCount}
          total={filtered.length}
          pageSize={PAGE_SIZE}
          label={$t('achievementResolutions.paginationLabel')}
          disabled={loading}
        />
      {/if}
    </section>
  {/if}
{/if}

<style>
  /* Header, tiles, tab strip and each tab's table are separate blocks: the panel is the unit an
     operator reads, so one panel holding all four read as a single undifferentiated wall.
     Tabs already carries its own 10px/14px margins, so the tab panels need none of their own. */
  .stats {
    margin: 12px 0;
  }

  .filters {
    display: flex;
    flex-wrap: wrap;
    gap: 8px;
    margin-bottom: 10px;
  }

  .filters input {
    flex: 1 1 220px;
  }

  .warn {
    margin: 0 0 10px;
    padding: 8px 12px;
    border: 1px solid var(--line);
    border-radius: 8px;
    background: var(--surface-strong);
    color: var(--muted);
    font-size: 0.86rem;
  }

  /* Wide tables scroll inside their own box rather than pushing the page sideways. */
  .table-wrap {
    overflow-x: auto;
  }

  table {
    width: 100%;
    border-collapse: collapse;
    font-size: 0.88rem;
  }

  th,
  td {
    padding: 8px 10px;
    border-bottom: 1px solid var(--line);
    text-align: left;
    white-space: nowrap;
  }

  th {
    color: var(--muted);
    font-size: 0.78rem;
    text-transform: uppercase;
    letter-spacing: 0.03em;
  }

  .num {
    text-align: right;
    font-variant-numeric: tabular-nums;
  }

  .muted {
    color: var(--muted);
  }

  tr.orphan {
    background: rgba(var(--danger-rgb, 220, 60, 60), 0.06);
  }

  .pill {
    display: inline-flex;
    align-items: center;
    gap: 4px;
    border-radius: 999px;
    padding: 2px 9px;
    background: rgba(var(--muted-rgb), 0.18);
    font-size: 0.76rem;
    font-weight: 700;
  }

  .pill.ok {
    background: rgba(var(--ok-rgb, 40, 160, 90), 0.2);
  }

  .pill.live {
    background: rgba(var(--accent-rgb, 210, 170, 60), 0.22);
  }

  .pill.danger {
    background: rgba(var(--danger-rgb, 220, 60, 60), 0.22);
  }
</style>
