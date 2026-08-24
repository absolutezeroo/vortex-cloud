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
  import PageHeader from '../components/PageHeader.svelte';
  import Tabs from '../components/Tabs.svelte';
  import AssetImage from '../components/AssetImage.svelte';
  import CurrencySelect from '../components/CurrencySelect.svelte';
  import EmptyState from '../components/EmptyState.svelte';
  import EntityLink from '../components/EntityLink.svelte';
  import Drawer from '../components/Drawer.svelte';
  import StatCard from '../components/StatCard.svelte';
  import { Trophy, Award, Zap, ZapOff, Users, Layers, AlertTriangle } from '@lucide/svelte';
  import { t } from '../lib/i18n.js';

  let category = $state('');
  let loading = $state(false);
  let forbidden = $state(false);
  let error = $state('');
  let list = $state(null);
  let stats = $state(null);
  let selected = $state(null);
  let detail = $state(null);
  let detailLoading = $state(false);

  // The definitions list and its editor stay on ONE tab: they are master and detail, read
  // together, and NN/g is explicit that tabs make that pairing worse. The three read-only breakdowns
  // below them are the ones nobody was reaching.
  let tab = $state('definitions');

  // The category picker: a sentinel option that reveals a text box, so an existing category is
  // chosen and a new one is typed once rather than every time.
  const NEW_CATEGORY = '__new__';
  let categoryChoice = $state('');

  // Called when the editor opens. An achievement whose category is not in the list -- including a
  // brand-new one, whose category is blank -- lands on the "new category" branch with its value kept.
  // Set here rather than in an effect: an effect recomputing the choice from the form would snap
  // "new category" straight back to whatever the form still held.
  function openAchievementEditor(form) {
    achievementForm = form;
    categoryChoice = (list?.categories || []).includes(form.category) ? form.category : NEW_CATEGORY;
  }

  const ops = createWriteOps(async () => {
    achievementForm = null;
    levelForm = null;
    await refresh();
    if (selected) await reloadDetail(selected);
  });

  let canManage = $derived(hasDashboardCapability($identity, CAPABILITIES.opsContentManage));

  const emptyAchievement = () => ({ id: 0, name: '', category: '', displayMethod: 0 });
  const emptyLevel = () => ({
    level: 1,
    badgeCode: '',
    progressRequirement: 1,
    rewardAmount: 0,
    rewardType: -1,
    scorePoints: 0,
  });

  // Both editors used to live in a panel pinned to the bottom of the page: you clicked a row at
  // the top and the form that edited it sat several hundred pixels below, with nothing tying the
  // two together. They are dialogs now -- a multi-field form with mixed inputs is the case a
  // dialog is for, and it opens attached to the row you clicked.
  let achievementForm = $state(null);
  let levelForm = $state(null);

  // The badge file is named after the code, so the preview is the honest test of a typed one: a
  // wrong code shows the fallback here exactly as it would show nothing in the client. Built from
  // the template so a rung that does not exist yet still previews.
  let badgePreviewUrl =
    $derived(levelForm?.badgeCode?.trim() && stats?.badgeImageTemplate
      ? stats.badgeImageTemplate.replace('{badge}', encodeURIComponent(levelForm.badgeCode.trim()))
      : null);

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
  <PageHeader title={$t('achievements.title')} description={$t('achievements.description')}>
    {#snippet actions()}
      <button type="button" onclick={refresh} disabled={loading} class="warning">{$t('common.refresh')}</button>
    {/snippet}
  </PageHeader>
</section>

<section class="panel">

  <form class="toolbar-grid" onsubmit={(event) => { event.preventDefault(); refresh(); }}>
    <label>
      {$t('achievements.category')}
      <select bind:value={category}>
        <option value="">{$t('achievements.allCategories')}</option>
        {#each list?.categories || [] as c}
          <option value={c}>{c}</option>
        {/each}
      </select>
    </label>
  </form>

  {#if loading}
    <p class="muted">{$t('common.loading')}</p>
  {:else if forbidden}
    <AccessDeniedNotice message={$t('achievements.accessDenied')} />
  {:else if error}
    <p class="empty-state danger" role="alert">{error}</p>
  {/if}
</section>

{#if stats}
  <div class="metric-grid" style="margin-top: 12px;">
    <StatCard label={$t('achievements.totalAchievements')} value={formatNumber(stats.totals.totalAchievements)}>
      {#snippet icon()}
        <Trophy size={15} strokeWidth={2} aria-hidden="true" />
      {/snippet}
    </StatCard>
    <StatCard label={$t('achievements.totalLevels')} value={formatNumber(stats.totals.totalLevels)}>
      {#snippet icon()}
        <Award size={15} strokeWidth={2} aria-hidden="true" />
      {/snippet}
    </StatCard>
    <StatCard label={$t('achievements.triggered')} value={formatNumber(stats.totals.triggeredCount)}>
      {#snippet icon()}
        <Zap size={15} strokeWidth={2} aria-hidden="true" />
      {/snippet}
    </StatCard>
    <StatCard label={$t('achievements.untriggered')} value={formatNumber(stats.totals.untriggeredCount)}>
      {#snippet icon()}
        <ZapOff size={15} strokeWidth={2} aria-hidden="true" />
      {/snippet}
    </StatCard>
    <StatCard label={$t('achievements.badgesAwarded')} value={formatNumber(stats.totals.badgesAwarded)}>
      {#snippet icon()}
        <Award size={15} strokeWidth={2} aria-hidden="true" />
      {/snippet}
    </StatCard>
    <StatCard label={$t('achievements.playersWithProgress')} value={formatNumber(stats.totals.playersWithProgress)}>
      {#snippet icon()}
        <Users size={15} strokeWidth={2} aria-hidden="true" />
      {/snippet}
    </StatCard>
  </div>
{/if}

<Tabs
  bind:active={tab}
  storageKey="achievements"
  tabs={[
    { id: 'definitions', label: $t('achievements.definitionsTitle'), icon: Award, count: list?.items?.length },
    { id: 'categories', label: $t('achievements.byCategoryTitle'), icon: Layers },
    { id: 'untouched', label: $t('achievements.untouchedTitle'), icon: AlertTriangle },
    { id: 'leaderboard', label: $t('achievements.leaderboardTitle'), icon: Trophy },
  ]}
/>

{#if tab === 'definitions'}
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
              {#if canManage}<th>{$t('common.actions')}</th>{/if}
            </tr>
          </thead>
          <tbody>
            {#each list.items || [] as row}
              <tr
                class:selected={selected === row.id}
                onclick={() => select(row)}
                onkeydown={(e) => {
                  if (e.key === 'Enter' || e.key === ' ') {
                    e.preventDefault();
                    select(row);
                  }
                }}
                tabindex="0"
                aria-selected={selected === row.id}
                style="cursor: pointer;"
              >
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
                      onclick={(event) => {
                        event.stopPropagation();
                        openAchievementEditor({
                          id: row.id,
                          name: row.name,
                          category: row.category,
                          displayMethod: row.displayMethod,
                        });
                      }}
                    >
                      {$t('achievements.edit')}
                    </button>
                    <button
                      type="button"
                      class="ghost-button danger"
                      onclick={(event) => {
                        event.stopPropagation();
                        ops.ask(
                          '/api/v1/operations/content/achievements/delete',
                          { achievementId: row.id },
                          $t('achievements.deleteAchievement'),
                          $t('achievements.deleteAchievementSummary', { name: row.name })
                        );
                      }}
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
                              {#if canManage}<th>{$t('common.actions')}</th>{/if}
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
                                      onclick={(event) => { event.stopPropagation(); levelForm = { ...level }; }}
                                    >
                                      {$t('achievements.edit')}
                                    </button>
                                    <button
                                      type="button"
                                      class="ghost-button danger"
                                      onclick={(event) => {
                                        event.stopPropagation();
                                        ops.ask(
                                          '/api/v1/operations/content/achievements/levels/delete',
                                          { levelId: level.id },
                                          $t('achievements.deleteLevel'),
                                          $t('achievements.deleteLevelSummary', { level: level.level })
                                        );
                                      }}
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
      <div class="panel-head">
        <h2>{$t('achievements.editorTitle')}</h2>
        <div class="head-actions">
          <button type="button" class="success" onclick={() => openAchievementEditor(emptyAchievement())}>
            {$t('achievements.newAchievement')}
          </button>
          {#if selected}
            <button type="button" class="ghost-button" onclick={() => (levelForm = emptyLevel())}>
              {$t('achievements.levelEditorTitle')}
            </button>
          {/if}
        </div>
      </div>
      <p class="muted">{$t('achievements.editorHint')}</p>
      {#if !selected}
        <p class="muted">{$t('achievements.pickToEditLevels')}</p>
      {/if}

      {#if $ops.result}
        <OpResult result={$ops.result} />
      {/if}
    </section>
  {/if}
{/if}

{#if tab === 'categories' && stats}
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
{/if}

{#if tab === 'untouched' && stats}
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
{/if}

{#if tab === 'leaderboard' && stats}
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

<datalist id="achievement-categories">
  {#each list?.categories || [] as c}<option value={c}></option>{/each}
</datalist>

{#if achievementForm}
  <Drawer
    title={achievementForm.id ? $t('achievements.updateAchievement') : $t('achievements.addAchievement')}
    eyebrow={$t('achievements.editorTitle')}
    width={520}
    labelledBy="achievement-form-title"
    onclose={() => (achievementForm = null)}
  >
    <div class="op-field">
      <label for="achievement-name">{$t('achievements.colName')}</label>
      <input autocomplete="off" spellcheck="false" id="achievement-name" bind:value={achievementForm.name} placeholder="RoomEntry" />
    </div>
    <div class="op-field">
      <label for="achievement-category">{$t('achievements.colCategory')}</label>
      <!-- The categories that exist, rather than a text box: a typo here does not fail, it silently
           creates a category of one. The last option is how a genuinely new one gets made. -->
      <select
        id="achievement-category"
        bind:value={categoryChoice}
        onchange={() => {
          if (categoryChoice !== NEW_CATEGORY) achievementForm.category = categoryChoice;
        }}
      >
        {#each list?.categories || [] as c}<option value={c}>{c}</option>{/each}
        <option value={NEW_CATEGORY}>{$t('achievements.categoryNew')}</option>
      </select>
    </div>
    {#if categoryChoice === NEW_CATEGORY}
      <div class="op-field">
        <label for="achievement-category-new">{$t('achievements.categoryNewLabel')}</label>
        <input autocomplete="off" spellcheck="false" id="achievement-category-new" bind:value={achievementForm.category} placeholder="explore" />
      </div>
    {/if}
    <div class="op-field">
      <label for="achievement-display">{$t('achievements.displayMethod')}</label>
      <!-- The client only ever asks `displayMethod != 1`, and the one thing it decides is whether the
           progress bar is drawn. A number box invited a value that means nothing. -->
      <select id="achievement-display" bind:value={achievementForm.displayMethod}>
        <option value={0}>{$t('achievements.displayMethodProgress')}</option>
        <option value={1}>{$t('achievements.displayMethodNoProgress')}</option>
      </select>
      <small class="muted">{$t('achievements.displayMethodHint')}</small>
    </div>

    {#snippet actions()}

      <button class="success"
        type="button"
        disabled={!achievementForm.name.trim() || !achievementForm.category.trim()}
        onclick={() =>
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
        {achievementForm.id ? $t('achievements.updateAchievement') : $t('achievements.addAchievement')}
      </button>
      <button class="ghost-button" type="button" onclick={() => (achievementForm = null)}>
        {$t('common.cancel')}
      </button>

    {/snippet}
  </Drawer>
{/if}

{#if levelForm && selected}
  <Drawer
    title={$t('achievements.levelEditorTitle')}
    eyebrow={$t('achievements.editorTitle')}
    width={520}
    labelledBy="level-form-title"
    onclose={() => (levelForm = null)}
  >
    <div class="op-field">
      <label for="level-number">{$t('achievements.colLevel')}</label>
      <input autocomplete="off" spellcheck="false" id="level-number" type="number" bind:value={levelForm.level} min="1" />
    </div>
    <div class="op-field">
      <label for="level-badge">{$t('achievements.colBadgeCode')}</label>
      <span class="badge-cell">
        <input autocomplete="off" spellcheck="false" id="level-badge" bind:value={levelForm.badgeCode} placeholder="ACH_RoomEntry1" />
        <AssetImage src={badgePreviewUrl} alt={levelForm.badgeCode} size={32} fallbackIcon={Award} />
      </span>
    </div>
    <div class="op-field">
      <label for="level-requirement">{$t('achievements.colRequirement')}</label>
      <input autocomplete="off" spellcheck="false" id="level-requirement" type="number" bind:value={levelForm.progressRequirement} min="1" />
    </div>
    <div class="op-field">
      <label for="level-reward-amount">{$t('achievements.rewardAmount')}</label>
      <input autocomplete="off" spellcheck="false" id="level-reward-amount" type="number" bind:value={levelForm.rewardAmount} min="0" />
    </div>
    <div class="op-field">
      <label for="level-reward-type">{$t('achievements.rewardType')}</label>
      <CurrencySelect id="level-reward-type" bind:value={levelForm.rewardType} />
    </div>
    <div class="op-field">
      <label for="level-score">{$t('achievements.colLevelScore')}</label>
      <input autocomplete="off" spellcheck="false" id="level-score" type="number" bind:value={levelForm.scorePoints} min="0" />
    </div>

    {#snippet actions()}

      <button
        type="button"
        disabled={!levelForm.badgeCode.trim()}
        onclick={() =>
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
        {$t('achievements.saveLevel')}
      </button>
      <button class="ghost-button" type="button" onclick={() => (levelForm = null)}>
        {$t('common.cancel')}
      </button>

    {/snippet}
  </Drawer>
{/if}

<ConfirmReasonModal
  open={Boolean($ops.pending)}
  title={$ops.pending?.title ?? ''}
  changes={$ops.pending?.changes ?? []}
  noteOnly={$ops.pending?.noteOnly ?? false}
  summary={$ops.pending?.summary ?? ''}
  confirmLabel={$ops.pending?.title ?? $t('common.confirm')}
  busy={$ops.busy}
  error={$ops.error}
  onconfirm={ops.confirm}
  oncancel={() => ops.cancel()}
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
