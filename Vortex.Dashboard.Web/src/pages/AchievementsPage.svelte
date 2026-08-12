<script>
  // Achievement definitions are seeded, not authored here, so this page answers three questions an
  // operator cannot get from the tables: which ladders can advance at all (a definition no trigger
  // feeds is dead weight that still renders in the client), how far the hotel has climbed each one,
  // and who is ahead.
  import { onMount } from 'svelte';
  import { apiGet } from '../lib/api.js';
  import { createWriteOps } from '../lib/writeOps.js';
  import { hasDashboardCapability } from '../lib/permissions.js';
  import { CAPABILITIES } from '../lib/dashboardPermissions.js';
  import { identity } from '../lib/session.js';
  import ConfirmReasonModal from '../components/ConfirmReasonModal.svelte';
  import OpResult from '../components/OpResult.svelte';
  import { formatNumber } from '../lib/format.js';
  import { isPermissionDeniedError } from '../lib/permissions.js';
  import { openPlayer } from '../lib/session.js';
  import AccessDeniedNotice from '../components/AccessDeniedNotice.svelte';
  import AssetImage from '../components/AssetImage.svelte';
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

  const ops = createWriteOps(async () => {
    await refresh();
    if (selected) await reloadDetail(selected);
  });

  $: canManage = hasDashboardCapability($identity, CAPABILITIES.opsContentManage);

  const emptyAchievement = () => ({ id: 0, name: '', category: '', displayMethod: 0 });
  const emptyLevel = () => ({
    level: 1,
    badgeCode: '',
    progressRequirement: 1,
    rewardAmount: 0,
    rewardType: -1,
    scorePoints: 0,
  });

  let achievementForm = emptyAchievement();
  let levelForm = emptyLevel();

  // The badge file is named after the code, so the preview is the honest test of a typed one: a
  // wrong code shows the fallback here exactly as it would show nothing in the client. Built from
  // the template so a rung that does not exist yet still previews.
  $: badgePreviewUrl =
    levelForm.badgeCode.trim() && stats?.badgeImageTemplate
      ? stats.badgeImageTemplate.replace('{badge}', encodeURIComponent(levelForm.badgeCode.trim()))
      : null;

  async function reloadDetail(id) {
    try {
      detail = await apiGet(`/api/v1/achievements/${id}`);
    } catch {
      detail = null;
    }
  }

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
            {#if canManage}<th></th>{/if}
          </tr>
        </thead>
        <tbody>
          {#each list.items || [] as row}
            <tr class:selected={selected === row.id} on:click={() => select(row)} style="cursor: pointer;">
              <td>
                <span class="badge-cell">
                  <AssetImage src={row.badgeUrl} alt={row.name} size={32} fallbackIcon={Trophy} />
                  <span>{row.name}</span>
                </span>
              </td>
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
              {#if canManage}
                <td class="row-actions">
                  <button
                    type="button"
                    class="ghost-button"
                    on:click|stopPropagation={() =>
                      (achievementForm = {
                        id: row.id,
                        name: row.name,
                        category: row.category,
                        displayMethod: row.displayMethod,
                      })}
                  >
                    {$t('achievements.edit')}
                  </button>
                  <button
                    type="button"
                    class="ghost-button danger"
                    on:click|stopPropagation={() =>
                      ops.ask(
                        '/api/v1/operations/content/achievements/delete',
                        { achievementId: row.id },
                        $t('achievements.deleteAchievement'),
                        $t('achievements.deleteAchievementSummary', { name: row.name })
                      )}
                  >
                    {$t('achievements.delete')}
                  </button>
                </td>
              {/if}
            </tr>
            {#if selected === row.id}
              <tr>
                <td colspan={canManage ? 10 : 9}>
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
                            {#if canManage}<th></th>{/if}
                          </tr>
                        </thead>
                        <tbody>
                          {#each detail.ladder || [] as level}
                            <tr>
                              <td>{level.level}</td>
                              <td>
                                <span class="badge-cell">
                                  <AssetImage
                                    src={level.badgeUrl}
                                    alt={level.badgeCode}
                                    size={28}
                                    fallbackIcon={Award}
                                  />
                                  <code>{level.badgeCode}</code>
                                </span>
                              </td>
                              <td>{formatNumber(level.progressRequirement)}</td>
                              <td>{rewardLabel(level)}</td>
                              <td>{formatNumber(level.scorePoints)}</td>
                              <td>
                                {formatNumber(
                                  (detail.levelDistribution || []).find((d) => d.level === level.level)?.players || 0
                                )}
                              </td>
                              {#if canManage}
                                <td class="row-actions">
                                  <button
                                    type="button"
                                    class="ghost-button"
                                    on:click|stopPropagation={() => (levelForm = { ...level })}
                                  >
                                    {$t('achievements.edit')}
                                  </button>
                                  <button
                                    type="button"
                                    class="ghost-button danger"
                                    on:click|stopPropagation={() =>
                                      ops.ask(
                                        '/api/v1/operations/content/achievements/levels/delete',
                                        { levelId: level.id },
                                        $t('achievements.deleteLevel'),
                                        $t('achievements.deleteLevelSummary', { level: level.level })
                                      )}
                                  >
                                    {$t('achievements.delete')}
                                  </button>
                                </td>
                              {/if}
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
            <tr><td colspan={canManage ? 10 : 9} class="muted">{$t('achievements.noDefinitions')}</td></tr>
          {/each}
        </tbody>
      </table>
    </div>
  </div>
{/if}

{#if canManage && list}
  <section class="panel" style="margin-top: 12px;">
    <div class="panel-head"><h2>{$t('achievements.editorTitle')}</h2></div>
    <p class="muted">{$t('achievements.editorHint')}</p>

    <form
      class="inline-form"
      on:submit|preventDefault={() =>
        ops.ask(
          '/api/v1/operations/content/achievements',
          {
            achievementId: Number(achievementForm.id) || 0,
            name: achievementForm.name,
            category: achievementForm.category,
            displayMethod: Number(achievementForm.displayMethod) || 0,
          },
          achievementForm.id ? $t('achievements.updateAchievement') : $t('achievements.addAchievement'),
          $t('achievements.saveSummary', { name: achievementForm.name })
        )}
    >
      <label>
        {$t('achievements.colName')}
        <input bind:value={achievementForm.name} placeholder="RoomEntry" />
      </label>
      <label>
        {$t('achievements.colCategory')}
        <input bind:value={achievementForm.category} placeholder="explore" list="achievement-categories" />
      </label>
      <label>
        {$t('achievements.displayMethod')}
        <input type="number" bind:value={achievementForm.displayMethod} min="0" />
      </label>
      <button type="submit" disabled={!achievementForm.name.trim() || !achievementForm.category.trim()}>
        {achievementForm.id ? $t('achievements.updateAchievement') : $t('achievements.addAchievement')}
      </button>
      {#if achievementForm.id}
        <button type="button" class="ghost-button" on:click={() => (achievementForm = emptyAchievement())}>
          {$t('achievements.newAchievement')}
        </button>
      {/if}
    </form>

    <datalist id="achievement-categories">
      {#each list.categories || [] as c}<option value={c}></option>{/each}
    </datalist>

    {#if selected}
      <h3 class="subhead">{$t('achievements.levelEditorTitle')}</h3>
      <form
        class="inline-form"
        on:submit|preventDefault={() =>
          ops.ask(
            '/api/v1/operations/content/achievements/levels',
            {
              achievementId: selected,
              level: Number(levelForm.level),
              badgeCode: levelForm.badgeCode,
              progressRequirement: Number(levelForm.progressRequirement),
              rewardAmount: Number(levelForm.rewardAmount),
              rewardType: Number(levelForm.rewardType),
              scorePoints: Number(levelForm.scorePoints),
            },
            $t('achievements.saveLevel'),
            $t('achievements.saveLevelSummary', { level: levelForm.level })
          )}
      >
        <label>
          {$t('achievements.colLevel')}
          <input type="number" bind:value={levelForm.level} min="1" />
        </label>
        <label>
          {$t('achievements.colBadgeCode')}
          <span class="badge-cell">
            <input bind:value={levelForm.badgeCode} placeholder="ACH_RoomEntry1" />
            <AssetImage src={badgePreviewUrl} alt={levelForm.badgeCode} size={32} fallbackIcon={Award} />
          </span>
        </label>
        <label>
          {$t('achievements.colRequirement')}
          <input type="number" bind:value={levelForm.progressRequirement} min="1" />
        </label>
        <label>
          {$t('achievements.rewardAmount')}
          <input type="number" bind:value={levelForm.rewardAmount} min="0" />
        </label>
        <label>
          {$t('achievements.rewardType')}
          <input type="number" bind:value={levelForm.rewardType} />
        </label>
        <label>
          {$t('achievements.colLevelScore')}
          <input type="number" bind:value={levelForm.scorePoints} min="0" />
        </label>
        <button type="submit" disabled={!levelForm.badgeCode.trim()}>{$t('achievements.saveLevel')}</button>
      </form>
      <p class="muted">{$t('achievements.rewardTypeHint')}</p>
    {:else}
      <p class="muted">{$t('achievements.pickToEditLevels')}</p>
    {/if}

    {#if $ops.result}
      <OpResult result={$ops.result} />
    {/if}
  </section>
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

<ConfirmReasonModal
  open={Boolean($ops.pending)}
  title={$ops.pending?.title ?? ''}
  summary={$ops.pending?.summary ?? ''}
  confirmLabel={$ops.pending?.title ?? $t('common.confirm')}
  busy={$ops.busy}
  error={$ops.error}
  on:confirm={(e) => ops.confirm(e.detail)}
  on:cancel={() => ops.cancel()}
/>

<style>
  .badge-cell {
    display: inline-flex;
    align-items: center;
    gap: 8px;
  }

  tr.selected {
    background: var(--surface-raised, rgba(255, 255, 255, 0.04));
  }

  .subhead {
    margin: 16px 0 8px;
    font-size: 0.95rem;
  }
</style>
