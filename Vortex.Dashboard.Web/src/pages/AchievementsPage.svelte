<script>
  // Achievement definitions are seeded, not authored here, so this page answers three questions an
  // operator cannot get from the tables: which ladders can advance at all (a definition no trigger
  // feeds is dead weight that still renders in the client), how far the hotel has climbed each one,
  // and who is ahead.
  import { onMount } from 'svelte';
  import { apiGet } from '../lib/api.js';
  import { formatNumber } from '../lib/format.js';
  import { isPermissionDeniedError } from '../lib/permissions.js';
  import { openPlayer } from '../lib/session.js';
  import AccessDeniedNotice from '../components/AccessDeniedNotice.svelte';
  import EmptyState from '../components/EmptyState.svelte';
  import EntityLink from '../components/EntityLink.svelte';
  import StatCard from '../components/StatCard.svelte';
  import { Trophy, Award, Zap, ZapOff, Users } from '@lucide/svelte';
  import { t } from '../lib/i18n.js';

  let category = '';
  let loading = false;
  let forbidden = false;
  let error = '';
  let list = null;
  let stats = null;
  let selected = null;
  let detail = null;
  let detailLoading = false;

  async function refresh() {
    loading = true;
    error = '';
    forbidden = false;

    const params = new URLSearchParams();
    if (category) params.set('category', category);

    try {
      const [listResult, statsResult] = await Promise.all([
        apiGet(`/api/v1/achievements?${params}`),
        apiGet('/api/v1/achievements/stats'),
      ]);
      list = listResult;
      stats = statsResult;
    } catch (err) {
      if (isPermissionDeniedError(err)) {
        forbidden = true;
        list = null;
        stats = null;
        return;
      }

      error = err.message;
      list = null;
      stats = null;
    } finally {
      loading = false;
    }
  }

  async function select(row) {
    if (selected === row.id) {
      selected = null;
      detail = null;
      return;
    }

    selected = row.id;
    detail = null;
    detailLoading = true;

    try {
      detail = await apiGet(`/api/v1/achievements/${row.id}`);
    } catch (err) {
      error = err.message;
    } finally {
      detailLoading = false;
    }
  }

  function rewardLabel(level) {
    if (!level.rewardAmount) return '—';
    return level.rewardKind === 'credits'
      ? $t('achievements.rewardCredits', { amount: formatNumber(level.rewardAmount) })
      : $t('achievements.rewardPoints', {
          amount: formatNumber(level.rewardAmount),
          type: level.rewardType,
        });
  }

  onMount(() => {
    void refresh();
  });
</script>

<section class="panel">
  <div class="panel-head"><h2>{$t('achievements.title')}</h2></div>
  <p class="muted">{$t('achievements.description')}</p>

  <form class="toolbar-grid" on:submit|preventDefault={refresh}>
    <label>
      {$t('achievements.category')}
      <select bind:value={category}>
        <option value="">{$t('achievements.allCategories')}</option>
        {#each list?.categories || [] as c}
          <option value={c}>{c}</option>
        {/each}
      </select>
    </label>
    <button type="submit" disabled={loading}>{$t('common.refresh')}</button>
  </form>

  {#if loading}
    <p class="muted">{$t('common.loading')}</p>
  {:else if forbidden}
    <AccessDeniedNotice message={$t('achievements.accessDenied')} />
  {:else if error}
    <p class="empty-state danger">{error}</p>
  {/if}
</section>

{#if stats}
  <div class="metric-grid" style="margin-top: 12px;">
    <StatCard label={$t('achievements.totalAchievements')} value={formatNumber(stats.totals.totalAchievements)}>
      <Trophy slot="icon" size={15} strokeWidth={2} aria-hidden="true" />
    </StatCard>
    <StatCard label={$t('achievements.totalLevels')} value={formatNumber(stats.totals.totalLevels)}>
      <Award slot="icon" size={15} strokeWidth={2} aria-hidden="true" />
    </StatCard>
    <StatCard label={$t('achievements.triggered')} value={formatNumber(stats.totals.triggeredCount)}>
      <Zap slot="icon" size={15} strokeWidth={2} aria-hidden="true" />
    </StatCard>
    <StatCard label={$t('achievements.untriggered')} value={formatNumber(stats.totals.untriggeredCount)}>
      <ZapOff slot="icon" size={15} strokeWidth={2} aria-hidden="true" />
    </StatCard>
    <StatCard label={$t('achievements.badgesAwarded')} value={formatNumber(stats.totals.badgesAwarded)}>
      <Award slot="icon" size={15} strokeWidth={2} aria-hidden="true" />
    </StatCard>
    <StatCard label={$t('achievements.playersWithProgress')} value={formatNumber(stats.totals.playersWithProgress)}>
      <Users slot="icon" size={15} strokeWidth={2} aria-hidden="true" />
    </StatCard>
  </div>
{/if}

{#if list}
  <div class="panel" style="margin-top: 12px;">
    <div class="panel-head"><h2>{$t('achievements.definitionsTitle')}</h2></div>
    <div class="table-wrap">
      <table>
        <thead>
          <tr>
            <th>{$t('achievements.colName')}</th>
            <th>{$t('achievements.colCategory')}</th>
            <th>{$t('achievements.colTrigger')}</th>
            <th>{$t('achievements.colLevels')}</th>
            <th>{$t('achievements.colFinalRequirement')}</th>
            <th>{$t('achievements.colScore')}</th>
            <th>{$t('achievements.colStarted')}</th>
            <th>{$t('achievements.colCompleted')}</th>
            <th>{$t('achievements.colBadges')}</th>
          </tr>
        </thead>
        <tbody>
          {#each list.items || [] as row}
            <tr class:selected={selected === row.id} on:click={() => select(row)} style="cursor: pointer;">
              <td>{row.name}</td>
              <td>{row.category}</td>
              <td>
                {#if row.triggered}
                  <span class="status-badge status-badge--ok">{$t('achievements.triggerLive')}</span>
                {:else}
                  <span class="status-badge status-badge--warn">{$t('achievements.triggerMissing')}</span>
                {/if}
              </td>
              <td>{row.levelCount}</td>
              <td>{formatNumber(row.finalRequirement)}</td>
              <td>{formatNumber(row.totalScore)}</td>
              <td>{formatNumber(row.playersStarted)}</td>
              <td>{formatNumber(row.playersCompleted)}</td>
              <td>{formatNumber(row.badgesAwarded)}</td>
            </tr>
            {#if selected === row.id}
              <tr>
                <td colspan="9">
                  {#if detailLoading}
                    <p class="muted">{$t('common.loading')}</p>
                  {:else if detail}
                    <div class="table-wrap">
                      <table>
                        <thead>
                          <tr>
                            <th>{$t('achievements.colLevel')}</th>
                            <th>{$t('achievements.colBadgeCode')}</th>
                            <th>{$t('achievements.colRequirement')}</th>
                            <th>{$t('achievements.colReward')}</th>
                            <th>{$t('achievements.colLevelScore')}</th>
                            <th>{$t('achievements.colPlayersAtLevel')}</th>
                          </tr>
                        </thead>
                        <tbody>
                          {#each detail.ladder || [] as level}
                            <tr>
                              <td>{level.level}</td>
                              <td><code>{level.badgeCode}</code></td>
                              <td>{formatNumber(level.progressRequirement)}</td>
                              <td>{rewardLabel(level)}</td>
                              <td>{formatNumber(level.scorePoints)}</td>
                              <td>
                                {formatNumber(
                                  (detail.levelDistribution || []).find((d) => d.level === level.level)?.players || 0
                                )}
                              </td>
                            </tr>
                          {/each}
                        </tbody>
                      </table>
                    </div>

                    {#if (detail.topPlayers || []).length > 0}
                      <h3 class="subhead">{$t('achievements.topHoldersTitle')}</h3>
                      <div class="table-wrap">
                        <table>
                          <thead>
                            <tr>
                              <th>{$t('achievements.colPlayer')}</th>
                              <th>{$t('achievements.colLevel')}</th>
                              <th>{$t('achievements.colProgress')}</th>
                            </tr>
                          </thead>
                          <tbody>
                            {#each detail.topPlayers as holder}
                              <tr>
                                <td>
                                  <EntityLink
                                    type="player"
                                    id={holder.playerId}
                                    label={holder.playerName}
                                    {openPlayer}
                                  />
                                </td>
                                <td>{holder.level}</td>
                                <td>{formatNumber(holder.progress)}</td>
                              </tr>
                            {/each}
                          </tbody>
                        </table>
                      </div>
                    {/if}
                  {/if}
                </td>
              </tr>
            {/if}
          {:else}
            <tr><td colspan="9" class="muted">{$t('achievements.noDefinitions')}</td></tr>
          {/each}
        </tbody>
      </table>
    </div>
  </div>
{/if}

{#if stats}
  <div class="panel" style="margin-top: 12px;">
    <div class="panel-head"><h2>{$t('achievements.byCategoryTitle')}</h2></div>
    <div class="table-wrap">
      <table>
        <thead>
          <tr>
            <th>{$t('achievements.colCategory')}</th>
            <th>{$t('achievements.colAchievements')}</th>
            <th>{$t('achievements.colLevels')}</th>
            <th>{$t('achievements.colBadges')}</th>
          </tr>
        </thead>
        <tbody>
          {#each stats.byCategory || [] as row}
            <tr>
              <td>{row.category}</td>
              <td>{formatNumber(row.achievements)}</td>
              <td>{formatNumber(row.levels)}</td>
              <td>{formatNumber(row.badgesAwarded)}</td>
            </tr>
          {/each}
        </tbody>
      </table>
    </div>
  </div>

  <div class="panel" style="margin-top: 12px;">
    <div class="panel-head"><h2>{$t('achievements.untouchedTitle')}</h2></div>
    <p class="muted">{$t('achievements.untouchedDescription')}</p>
    {#if (stats.untouched || []).length === 0}
      <EmptyState message={$t('achievements.untouchedNone')} />
    {:else}
      <div class="table-wrap">
        <table>
          <thead>
            <tr>
              <th>{$t('achievements.colName')}</th>
              <th>{$t('achievements.colCategory')}</th>
              <th>{$t('achievements.colTrigger')}</th>
              <th>{$t('achievements.colLevels')}</th>
            </tr>
          </thead>
          <tbody>
            {#each stats.untouched as row}
              <tr>
                <td>{row.name}</td>
                <td>{row.category}</td>
                <td>
                  {#if row.triggered}
                    <span class="status-badge status-badge--ok">{$t('achievements.triggerLive')}</span>
                  {:else}
                    <span class="status-badge status-badge--warn">{$t('achievements.triggerMissing')}</span>
                  {/if}
                </td>
                <td>{row.levels}</td>
              </tr>
            {/each}
          </tbody>
        </table>
      </div>
    {/if}
  </div>

  <div class="panel" style="margin-top: 12px;">
    <div class="panel-head"><h2>{$t('achievements.leaderboardTitle')}</h2></div>
    <div class="table-wrap">
      <table>
        <thead>
          <tr>
            <th>{$t('achievements.colPlayer')}</th>
            <th>{$t('achievements.colPlayerScore')}</th>
            <th>{$t('achievements.colBadges')}</th>
          </tr>
        </thead>
        <tbody>
          {#each stats.topPlayers || [] as row}
            <tr>
              <td><EntityLink type="player" id={row.playerId} label={row.playerName} {openPlayer} /></td>
              <td>{formatNumber(row.score)}</td>
              <td>{formatNumber(row.badges)}</td>
            </tr>
          {:else}
            <tr><td colspan="3" class="muted">{$t('achievements.noProgress')}</td></tr>
          {/each}
        </tbody>
      </table>
    </div>
  </div>
{/if}

<style>
  tr.selected {
    background: var(--surface-raised, rgba(255, 255, 255, 0.04));
  }

  .subhead {
    margin: 16px 0 8px;
    font-size: 0.95rem;
  }
</style>
