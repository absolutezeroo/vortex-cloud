<script>
  // Reward-track campaigns.
  //
  // The whole point of the engine is that a campaign is content, so this page is where one gets
  // built: the track, its tasks and their stages, its milestones and what each hands over, and the
  // lifecycle that puts it in front of the hotel. Nothing here needs a rebuild to take effect.
  import { apiGet } from '../lib/api.js';
  import { createResource } from '../lib/resource.js';
  import { createWriteOps } from '../lib/writeOps.js';
  import { hasDashboardCapability } from '../lib/permissions.js';
  import { CAPABILITIES } from '../lib/dashboardPermissions.js';
  import { identity } from '../lib/session.js';
  import { formatDate, formatNumber } from '../lib/format.js';
  import { t, translate } from '../lib/i18n.js';
  import { Route, Users } from '@lucide/svelte';

  import AccessDeniedNotice from '../components/AccessDeniedNotice.svelte';
  import ConfirmReasonModal from '../components/ConfirmReasonModal.svelte';
  import Drawer from '../components/Drawer.svelte';
  import EmptyState from '../components/EmptyState.svelte';
  import OpResult from '../components/OpResult.svelte';
  import PageHeader from '../components/PageHeader.svelte';
  import PickerModal from '../components/PickerModal.svelte';
  import StatCard from '../components/StatCard.svelte';
  import Tabs from '../components/Tabs.svelte';

  // The client's five palettes. Anything else renders blue, so this is a select rather than a text
  // field.
  const THEMES = ['blue', 'orange', 'forest_green', 'red', 'cyan'];

  // Mirrors TaskProgressMode. Four modes cover every task the official track has; the labels say
  // what each counts rather than naming the enum.
  const MODES = [
    { value: 0, key: 'rewardTracks.modeCounter' },
    { value: 1, key: 'rewardTracks.modeDistinct' },
    { value: 2, key: 'rewardTracks.modeAbsolute' },
    { value: 3, key: 'rewardTracks.modeHighest' },
  ];

  const UNLOCK_KINDS = [
    { value: 0, key: 'rewardTracks.unlockAlways' },
    { value: 1, key: 'rewardTracks.unlockTrackCompleted' },
    { value: 2, key: 'rewardTracks.unlockPrizeClaimed' },
    { value: 3, key: 'rewardTracks.unlockBadge' },
    { value: 4, key: 'rewardTracks.unlockAccountAge' },
    { value: 5, key: 'rewardTracks.unlockFeatureFlag' },
  ];

  const COMPLETION_POLICIES = [
    { value: 0, key: 'rewardTracks.policyFreeClaimed' },
    { value: 1, key: 'rewardTracks.policyAllClaimed' },
    { value: 2, key: 'rewardTracks.policyMaxPoints' },
    { value: 3, key: 'rewardTracks.policyAllTasks' },
  ];

  let tab = $state('tracks');
  let search = $state('');
  let expanded = $state(null);

  let player = $state(null);
  let pickingPlayer = $state(false);

  let trackDraft = $state(null);
  let taskDraft = $state(null);
  let prizeDraft = $state(null);
  let cloneDraft = $state(null);

  const ops = createWriteOps();

  let canManage = $derived(hasDashboardCapability($identity, CAPABILITIES.opsRewardTracksManage));

  const tracks = createResource(
    () => ['reward-tracks', search.trim()],
    async () => {
      const params = search.trim() ? `?search=${encodeURIComponent(search.trim())}` : '';
      const [list, actions, kinds] = await Promise.all([
        apiGet(`/api/v1/reward-tracks${params}`),
        apiGet('/api/v1/reward-tracks/actions'),
        apiGet('/api/v1/reward-tracks/reward-kinds'),
      ]);

      return { items: list.items ?? [], actions: actions.items ?? [], kinds: kinds.items ?? [] };
    }
  );

  const progress = createResource(
    () => ['reward-tracks-player', player?.id ?? null],
    () => apiGet(`/api/v1/reward-tracks/players/${player.id}`),
    { enabled: () => player !== null }
  );

  let items = $derived(tracks.data?.items ?? []);
  // NOT `actions`: every Drawer footer here is an `{#snippet actions()}`, and a snippet is hoisted
  // to its enclosing block -- so inside `{#if taskDraft}` the name resolved to the footer snippet
  // instead of this array. Iterating a function yields nothing and throws nothing, which is how the
  // action picker shipped as an empty menu.
  let actionOptions = $derived(tracks.data?.actions ?? []);
  let kinds = $derived(tracks.data?.kinds ?? []);
  let live = $derived(items.filter((it) => it.status === 'Active').length);
  let participants = $derived(items.reduce((sum, it) => sum + it.participants, 0));
  let premiumHolders = $derived(items.reduce((sum, it) => sum + it.premiumHolders, 0));

  function emptyTrack() {
    return {
      trackId: '',
      theme: 'blue',
      sortOrder: 0,
      startsAt: '',
      progressEndsAt: '',
      claimEndsAt: '',
      unlockKind: 0,
      unlockValue: '',
      completionPolicy: 0,
      premiumEnabled: false,
      premiumBoostPerMille: 1200,
      premiumInstantPoints: 0,
      premiumCostCredits: 0,
      premiumCostDiamonds: 25,
      hidden: false,
      campaignCode: '',
    };
  }

  function emptyTask(trackRowId) {
    return {
      trackRowId,
      taskId: '',
      actionCode: actionOptions[0]?.name ?? '',
      parameter: '',
      mode: 0,
      premium: false,
      sortOrder: 0,
      levels: [{ requiredCount: 1, pointsReward: 10, premium: false }],
    };
  }

  function emptyPrize(trackRowId) {
    return {
      trackRowId,
      prizeId: '',
      requiredPoints: 0,
      premium: false,
      sortOrder: 0,
      rewards: [{ kind: 8, rewardTypeId: '0', amount: 100, extraParams: '', sortOrder: 0 }],
    };
  }

  function toLocal(iso) {
    if (!iso) return '';
    const date = new Date(iso);
    if (Number.isNaN(date.getTime())) return '';
    const pad = (n) => String(n).padStart(2, '0');
    return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}T${pad(date.getHours())}:${pad(date.getMinutes())}`;
  }

  function fromLocal(value) {
    if (!value) return null;
    const date = new Date(value);
    return Number.isNaN(date.getTime()) ? null : date.toISOString();
  }

  function unlockKindValue(name) {
    const index = ['Always', 'TrackCompleted', 'PrizeClaimed', 'BadgeOwned', 'AccountAgeDays', 'FeatureFlag'].indexOf(name);
    return index < 0 ? 0 : index;
  }

  function policyValue(name) {
    const index = ['AllFreePrizesClaimed', 'AllPrizesClaimed', 'MaxPointsReached', 'AllTasksCompleted'].indexOf(name);
    return index < 0 ? 0 : index;
  }

  function modeValue(name) {
    const index = ['Counter', 'Distinct', 'Absolute', 'Highest'].indexOf(name);
    return index < 0 ? 0 : index;
  }

  function trackBody(form, rowId) {
    const body = {
      trackId: form.trackId.trim(),
      theme: form.theme,
      sortOrder: Number(form.sortOrder) || 0,
      startsAt: fromLocal(form.startsAt),
      progressEndsAt: fromLocal(form.progressEndsAt),
      claimEndsAt: fromLocal(form.claimEndsAt),
      unlockKind: Number(form.unlockKind) || 0,
      unlockValue: form.unlockValue.trim(),
      completionPolicy: Number(form.completionPolicy) || 0,
      premiumEnabled: form.premiumEnabled,
      premiumBoostPerMille: Number(form.premiumBoostPerMille) || 1000,
      premiumInstantPoints: Number(form.premiumInstantPoints) || 0,
      premiumCostCredits: Number(form.premiumCostCredits) || 0,
      premiumCostDiamonds: Number(form.premiumCostDiamonds) || 0,
      hidden: form.hidden,
      campaignCode: form.campaignCode.trim(),
    };

    return rowId === null ? body : { trackRowId: rowId, ...body };
  }

  function saveTrack() {
    if (!canManage || !trackDraft) return;

    const { id, form } = trackDraft;

    ops.ask(
      id === null ? '/api/v1/operations/reward-tracks' : '/api/v1/operations/reward-tracks/update',
      trackBody(form, id),
      id === null ? $t('rewardTracks.newTrack') : $t('rewardTracks.editTrack'),
      form.trackId,
      {
        key: 'trackForm',
        valid: Boolean(form.trackId.trim()),
        invalidMessage: translate('rewardTracks.trackIdRequired'),
        onSuccess: async () => {
          trackDraft = null;
          await tracks.refresh();
        },
      }
    );
  }

  function saveTask() {
    if (!canManage || !taskDraft) return;

    const form = taskDraft.form;

    ops.ask(
      '/api/v1/operations/reward-tracks/tasks',
      {
        trackRowId: form.trackRowId,
        taskId: form.taskId.trim(),
        actionCode: form.actionCode,
        parameter: form.parameter.trim(),
        mode: Number(form.mode) || 0,
        premium: form.premium,
        sortOrder: Number(form.sortOrder) || 0,
        levels: form.levels.map((l) => ({
          requiredCount: Number(l.requiredCount) || 1,
          pointsReward: Number(l.pointsReward) || 0,
          premium: l.premium,
        })),
      },
      $t('rewardTracks.saveTask'),
      form.taskId,
      {
        key: 'taskForm',
        valid: Boolean(form.taskId.trim()) && form.levels.length > 0,
        invalidMessage: translate('rewardTracks.taskIdRequired'),
        onSuccess: async () => {
          taskDraft = null;
          await tracks.refresh();
        },
      }
    );
  }

  function savePrize() {
    if (!canManage || !prizeDraft) return;

    const form = prizeDraft.form;

    ops.ask(
      '/api/v1/operations/reward-tracks/prizes',
      {
        trackRowId: form.trackRowId,
        prizeId: form.prizeId.trim(),
        requiredPoints: Number(form.requiredPoints) || 0,
        premium: form.premium,
        sortOrder: Number(form.sortOrder) || 0,
        rewards: form.rewards.map((r) => ({
          kind: Number(r.kind),
          rewardTypeId: String(r.rewardTypeId).trim(),
          amount: Number(r.amount) || 1,
          extraParams: r.extraParams ?? '',
          sortOrder: Number(r.sortOrder) || 0,
        })),
      },
      $t('rewardTracks.savePrize'),
      form.prizeId,
      {
        key: 'prizeForm',
        valid: Boolean(form.prizeId.trim()) && form.rewards.length > 0,
        invalidMessage: translate('rewardTracks.prizeIdRequired'),
        onSuccess: async () => {
          prizeDraft = null;
          await tracks.refresh();
        },
      }
    );
  }

  function lifecycle(path, track, title) {
    ops.ask(path, { trackRowId: track.id }, title, track.trackId, {
      onSuccess: () => tracks.refresh(),
    });
  }

  function clone() {
    if (!cloneDraft) return;

    ops.ask(
      '/api/v1/operations/reward-tracks/clone',
      { trackRowId: cloneDraft.id, newTrackId: cloneDraft.newTrackId.trim() },
      $t('rewardTracks.clone'),
      cloneDraft.newTrackId,
      {
        key: 'cloneForm',
        valid: Boolean(cloneDraft.newTrackId.trim()),
        invalidMessage: translate('rewardTracks.trackIdRequired'),
        onSuccess: async () => {
          cloneDraft = null;
          await tracks.refresh();
        },
      }
    );
  }

  function addLevel() {
    const levels = taskDraft.form.levels;
    const last = levels[levels.length - 1];

    levels.push({
      requiredCount: (Number(last?.requiredCount) || 1) * 2,
      pointsReward: Number(last?.pointsReward) || 10,
      premium: false,
    });
  }

  function addReward() {
    prizeDraft.form.rewards.push({
      kind: 8,
      rewardTypeId: '0',
      amount: 100,
      extraParams: '',
      sortOrder: prizeDraft.form.rewards.length,
    });
  }

  function kindHint(kindValue) {
    return kinds.find((k) => k.value === Number(kindValue))?.target ?? '';
  }
</script>

<section class="panel">
  <PageHeader title={$t('rewardTracks.title')} description={$t('rewardTracks.subtitle')}>
    {#snippet actions()}
      <button type="button" class="warning" onclick={tracks.refresh}>{$t('common.refresh')}</button>
    {/snippet}
  </PageHeader>
</section>

{#if tracks.forbidden}
  <AccessDeniedNotice />
{:else}
  <Tabs
    bind:active={tab}
    storageKey="rewardTracks"
    tabs={[
      { id: 'tracks', label: $t('rewardTracks.tabTracks'), icon: Route, count: items.length },
      { id: 'players', label: $t('rewardTracks.tabPlayers'), icon: Users },
    ]}
  />

  {#if tab === 'tracks'}
    <div class="metric-grid">
      <StatCard label={$t('rewardTracks.statTracks')} value={formatNumber(items.length)} />
      <StatCard label={$t('rewardTracks.statLive')} value={formatNumber(live)} />
      <StatCard label={$t('rewardTracks.statParticipants')} value={formatNumber(participants)} />
      <StatCard label={$t('rewardTracks.statPremium')} value={formatNumber(premiumHolders)} />
    </div>

    <div class="panel">
      <div class="panel-head">
        <h2>{$t('rewardTracks.tracks')}</h2>
        {#if canManage}
          <button
            type="button"
            class="success"
            onclick={() => (trackDraft = { id: null, form: emptyTrack() })}
          >
            {$t('rewardTracks.newTrack')}
          </button>
        {/if}
      </div>

      <div class="filters">
        <input
          autocomplete="off"
          spellcheck="false"
          type="search"
          bind:value={search}
          placeholder={$t('rewardTracks.searchPlaceholder')}
        />
      </div>

      {#if tracks.loading}
        <p class="muted">{$t('common.loading')}</p>
      {:else if items.length === 0}
        <EmptyState message={$t('rewardTracks.noTracks')} />
      {:else}
        <div class="table-wrap">
          <table>
            <thead>
              <tr>
                <th>{$t('rewardTracks.trackId')}</th>
                <th>{$t('rewardTracks.status')}</th>
                <th>{$t('rewardTracks.tasks')}</th>
                <th>{$t('rewardTracks.prizes')}</th>
                <th>{$t('rewardTracks.ceiling')}</th>
                <th>{$t('rewardTracks.participants')}</th>
                <th></th>
              </tr>
            </thead>
            <tbody>
              {#each items as track (track.id)}
                <tr>
                  <td>
                    <strong>{track.trackId}</strong>
                    {#if track.hidden}<span class="op-chip">{$t('rewardTracks.hidden')}</span>{/if}
                    <div class="muted small">{track.localizationKey}</div>
                  </td>
                  <td>{track.status}</td>
                  <td>{formatNumber(track.tasks.length)}</td>
                  <td>{formatNumber(track.prizes.length)}</td>
                  <td>
                    {formatNumber(track.freePointCeiling)}
                    {#if track.premiumEnabled}
                      / {formatNumber(track.premiumPointCeiling)}
                    {/if}
                  </td>
                  <td>
                    {formatNumber(track.participants)}
                    {#if track.premiumHolders}
                      <span class="muted small">
                        {$t('rewardTracks.premiumCount', { count: track.premiumHolders })}
                      </span>
                    {/if}
                  </td>
                  <td class="row-actions">
                    <button
                      type="button"
                      class="ghost-button"
                      onclick={() => (expanded = expanded === track.id ? null : track.id)}
                    >
                      {expanded === track.id ? $t('rewardTracks.hide') : $t('rewardTracks.content')}
                    </button>
                    {#if canManage}
                      <button
                        type="button"
                        class="ghost-button"
                        onclick={() =>
                          (trackDraft = {
                            id: track.id,
                            form: {
                              ...track,
                              unlockKind: unlockKindValue(track.unlockKind),
                              completionPolicy: policyValue(track.completionPolicy),
                              startsAt: toLocal(track.startsAt),
                              progressEndsAt: toLocal(track.progressEndsAt),
                              claimEndsAt: toLocal(track.claimEndsAt),
                            },
                          })}
                      >
                        {$t('common.edit')}
                      </button>
                      {#if track.status === 'Draft' || track.status === 'Archived'}
                        <button
                          type="button"
                          class="success"
                          onclick={() =>
                            lifecycle(
                              '/api/v1/operations/reward-tracks/publish',
                              track,
                              translate('rewardTracks.publish')
                            )}
                        >
                          {$t('rewardTracks.publish')}
                        </button>
                      {:else}
                        <button
                          type="button"
                          class="warning"
                          onclick={() =>
                            lifecycle(
                              '/api/v1/operations/reward-tracks/archive',
                              track,
                              translate('rewardTracks.archive')
                            )}
                        >
                          {$t('rewardTracks.archive')}
                        </button>
                      {/if}
                      <button
                        type="button"
                        class="ghost-button"
                        onclick={() => (cloneDraft = { id: track.id, newTrackId: '' })}
                      >
                        {$t('rewardTracks.clone')}
                      </button>
                      <button
                        type="button"
                        class="danger"
                        onclick={() =>
                          lifecycle(
                            '/api/v1/operations/reward-tracks/delete',
                            track,
                            translate('rewardTracks.deleteTrack')
                          )}
                      >
                        {$t('common.delete')}
                      </button>
                    {/if}
                  </td>
                </tr>
                {#if expanded === track.id}
                  <tr>
                    <td colspan="7">
                      <div class="panel-head">
                        <h3>{$t('rewardTracks.tasksOf', { track: track.trackId })}</h3>
                        {#if canManage}
                          <button
                            type="button"
                            class="success"
                            onclick={() => (taskDraft = { form: emptyTask(track.id) })}
                          >
                            {$t('rewardTracks.newTask')}
                          </button>
                        {/if}
                      </div>
                      <table>
                        <thead>
                          <tr>
                            <th>{$t('rewardTracks.taskId')}</th>
                            <th>{$t('rewardTracks.action')}</th>
                            <th>{$t('rewardTracks.mode')}</th>
                            <th>{$t('rewardTracks.stages')}</th>
                            <th></th>
                          </tr>
                        </thead>
                        <tbody>
                          {#each track.tasks as task (task.id)}
                            <tr>
                              <td>
                                {task.taskId}
                                {#if task.premium}<span class="op-chip">{$t('rewardTracks.premium')}</span>{/if}
                              </td>
                              <td>
                                {task.actionCode}
                                {#if !task.wired}
                                  <span class="status-badge status-badge--bad">{$t('rewardTracks.notWired')}</span>
                                {/if}
                              </td>
                              <td>{task.mode}</td>
                              <td>
                                <div class="chips">
                                  {#each task.levels as level (level.levelIndex)}
                                    <span class="op-chip">
                                      {level.requiredCount} → {level.pointsReward}
                                    </span>
                                  {/each}
                                </div>
                              </td>
                              <td class="row-actions">
                                {#if canManage}
                                  <button
                                    type="button"
                                    class="ghost-button"
                                    onclick={() =>
                                      (taskDraft = {
                                        form: {
                                          ...task,
                                          trackRowId: track.id,
                                          mode: modeValue(task.mode),
                                          levels: task.levels.map((l) => ({ ...l })),
                                        },
                                      })}
                                  >
                                    {$t('common.edit')}
                                  </button>
                                  <button
                                    type="button"
                                    class="danger"
                                    onclick={() =>
                                      ops.ask(
                                        '/api/v1/operations/reward-tracks/tasks/delete',
                                        { taskRowId: task.id },
                                        translate('rewardTracks.deleteTask'),
                                        task.taskId,
                                        { onSuccess: () => tracks.refresh() }
                                      )}
                                  >
                                    {$t('common.delete')}
                                  </button>
                                {/if}
                              </td>
                            </tr>
                          {/each}
                        </tbody>
                      </table>

                      <div class="panel-head">
                        <h3>{$t('rewardTracks.prizesOf', { track: track.trackId })}</h3>
                        {#if canManage}
                          <button
                            type="button"
                            class="success"
                            onclick={() => (prizeDraft = { form: emptyPrize(track.id) })}
                          >
                            {$t('rewardTracks.newPrize')}
                          </button>
                        {/if}
                      </div>
                      <table>
                        <thead>
                          <tr>
                            <th>{$t('rewardTracks.prizeId')}</th>
                            <th>{$t('rewardTracks.requiredPoints')}</th>
                            <th>{$t('rewardTracks.rewards')}</th>
                            <th></th>
                          </tr>
                        </thead>
                        <tbody>
                          {#each track.prizes as prize (prize.id)}
                            <tr>
                              <td>
                                {prize.prizeId}
                                {#if prize.premium}<span class="op-chip">{$t('rewardTracks.premium')}</span>{/if}
                              </td>
                              <td>
                                {formatNumber(prize.requiredPoints)}
                                {#if !prize.reachable}
                                  <span class="status-badge status-badge--bad">{$t('rewardTracks.unreachable')}</span>
                                {/if}
                              </td>
                              <td>
                                <div class="chips">
                                  {#each prize.rewards as reward (reward.id)}
                                    <span class="op-chip">
                                      {reward.kind}:{reward.rewardTypeId} ×{reward.amount}
                                    </span>
                                  {/each}
                                </div>
                              </td>
                              <td class="row-actions">
                                {#if canManage}
                                  <button
                                    type="button"
                                    class="ghost-button"
                                    onclick={() =>
                                      (prizeDraft = {
                                        form: {
                                          ...prize,
                                          trackRowId: track.id,
                                          rewards: prize.rewards.map((r) => ({
                                            ...r,
                                            kind: r.kindValue,
                                          })),
                                        },
                                      })}
                                  >
                                    {$t('common.edit')}
                                  </button>
                                  <button
                                    type="button"
                                    class="danger"
                                    onclick={() =>
                                      ops.ask(
                                        '/api/v1/operations/reward-tracks/prizes/delete',
                                        { prizeRowId: prize.id },
                                        translate('rewardTracks.deletePrize'),
                                        prize.prizeId,
                                        { onSuccess: () => tracks.refresh() }
                                      )}
                                  >
                                    {$t('common.delete')}
                                  </button>
                                {/if}
                              </td>
                            </tr>
                          {/each}
                        </tbody>
                      </table>
                    </td>
                  </tr>
                {/if}
              {/each}
            </tbody>
          </table>
        </div>
      {/if}
    </div>
  {/if}

  {#if tab === 'players'}
    <div class="panel">
      <div class="panel-head">
        <h2>{$t('rewardTracks.playerLookup')}</h2>
        <button type="button" class="ghost-button" onclick={() => (pickingPlayer = true)}>
          {player ? player.name : $t('rewardTracks.pickPlayer')}
        </button>
      </div>

      {#if !player}
        <EmptyState message={$t('rewardTracks.pickPlayerHint')} />
      {:else if progress.loading}
        <p class="muted">{$t('common.loading')}</p>
      {:else if (progress.data?.items ?? []).length === 0}
        <EmptyState message={$t('rewardTracks.noProgress')} />
      {:else}
        {#each progress.data.items as row (row.trackId)}
          <div class="panel">
            <div class="panel-head">
              <h3>{row.trackId}</h3>
              {#if canManage}
                <div class="row-actions">
                  {#if !row.premiumUnlocked}
                    <button
                      type="button"
                      class="success"
                      onclick={() =>
                        ops.ask(
                          '/api/v1/operations/reward-tracks/players/grant-premium',
                          { playerId: player.id, trackId: row.trackId },
                          translate('rewardTracks.grantPremium'),
                          `${player.name} · ${row.trackId}`,
                          { onSuccess: () => progress.refresh() }
                        )}
                    >
                      {$t('rewardTracks.grantPremium')}
                    </button>
                  {/if}
                  <button
                    type="button"
                    class="danger"
                    onclick={() =>
                      ops.ask(
                        '/api/v1/operations/reward-tracks/players/reset',
                        { playerId: player.id, trackId: row.trackId },
                        translate('rewardTracks.reset'),
                        `${player.name} · ${row.trackId}`,
                        { onSuccess: () => progress.refresh() }
                      )}
                  >
                    {$t('rewardTracks.reset')}
                  </button>
                </div>
              {/if}
            </div>

            <p class="muted">
              {$t('rewardTracks.playerSummary', {
                points: formatNumber(row.points),
                premium: row.premiumUnlocked ? $t('common.yes') : $t('common.no'),
                completed: row.completedAt ? formatDate(row.completedAt) : '—',
              })}
            </p>

            <table>
              <thead>
                <tr>
                  <th>{$t('rewardTracks.taskId')}</th>
                  <th>{$t('rewardTracks.progress')}</th>
                  <th>{$t('rewardTracks.highestPaid')}</th>
                </tr>
              </thead>
              <tbody>
                {#each row.tasks as task (task.taskId)}
                  <tr>
                    <td>{task.taskId}</td>
                    <td>{formatNumber(task.progressCount)}</td>
                    <td>{task.highestPaidLevelIndex}</td>
                  </tr>
                {/each}
              </tbody>
            </table>

            {#if row.claims.length > 0}
              <table>
                <thead>
                  <tr>
                    <th>{$t('rewardTracks.prizeId')}</th>
                    <th>{$t('rewardTracks.claimedAt')}</th>
                    <th>{$t('rewardTracks.pointsAtClaim')}</th>
                    <th>{$t('rewardTracks.granted')}</th>
                  </tr>
                </thead>
                <tbody>
                  {#each row.claims as claim (claim.prizeId)}
                    <tr>
                      <td>{claim.prizeId}</td>
                      <td>{formatDate(claim.claimedAt)}</td>
                      <td>{formatNumber(claim.pointsAtClaim)}</td>
                      <td class="muted small">{claim.granted}</td>
                    </tr>
                  {/each}
                </tbody>
              </table>
            {/if}
          </div>
        {/each}
      {/if}
    </div>
  {/if}
{/if}

{#if trackDraft}
  <Drawer
    title={trackDraft.id === null ? $t('rewardTracks.newTrack') : $t('rewardTracks.editTrack')}
    eyebrow={$t('rewardTracks.title')}
    onclose={() => (trackDraft = null)}
  >
    <label>
      {$t('rewardTracks.trackId')}
      <input type="text" bind:value={trackDraft.form.trackId} disabled={trackDraft.id !== null} />
    </label>
    <p class="muted small">{$t('rewardTracks.trackIdHint')}</p>
    <label>
      {$t('rewardTracks.theme')}
      <select bind:value={trackDraft.form.theme}>
        {#each THEMES as theme (theme)}
          <option value={theme}>{theme}</option>
        {/each}
      </select>
    </label>
    <label>
      {$t('rewardTracks.campaign')}
      <input type="text" bind:value={trackDraft.form.campaignCode} />
    </label>
    <label>
      {$t('rewardTracks.sortOrder')}
      <input type="number" bind:value={trackDraft.form.sortOrder} />
    </label>
    <label>
      {$t('rewardTracks.startsAt')}
      <input type="datetime-local" bind:value={trackDraft.form.startsAt} />
    </label>
    <label>
      {$t('rewardTracks.progressEndsAt')}
      <input type="datetime-local" bind:value={trackDraft.form.progressEndsAt} />
    </label>
    <label>
      {$t('rewardTracks.claimEndsAt')}
      <input type="datetime-local" bind:value={trackDraft.form.claimEndsAt} />
    </label>
    <p class="muted small">{$t('rewardTracks.windowHint')}</p>
    <label>
      {$t('rewardTracks.unlock')}
      <select bind:value={trackDraft.form.unlockKind}>
        {#each UNLOCK_KINDS as kind (kind.value)}
          <option value={kind.value}>{$t(kind.key)}</option>
        {/each}
      </select>
    </label>
    {#if Number(trackDraft.form.unlockKind) !== 0}
      <label>
        {$t('rewardTracks.unlockValue')}
        <input type="text" bind:value={trackDraft.form.unlockValue} />
      </label>
    {/if}
    <label>
      {$t('rewardTracks.completionPolicy')}
      <select bind:value={trackDraft.form.completionPolicy}>
        {#each COMPLETION_POLICIES as policy (policy.value)}
          <option value={policy.value}>{$t(policy.key)}</option>
        {/each}
      </select>
    </label>
    <label class="checkbox">
      <input type="checkbox" bind:checked={trackDraft.form.hidden} />
      {$t('rewardTracks.hiddenLabel')}
    </label>
    <label class="checkbox">
      <input type="checkbox" bind:checked={trackDraft.form.premiumEnabled} />
      {$t('rewardTracks.premiumEnabled')}
    </label>
    {#if trackDraft.form.premiumEnabled}
      <label>
        {$t('rewardTracks.boost')}
        <input type="number" bind:value={trackDraft.form.premiumBoostPerMille} />
      </label>
      <p class="muted small">{$t('rewardTracks.boostHint')}</p>
      <label>
        {$t('rewardTracks.instantPoints')}
        <input type="number" bind:value={trackDraft.form.premiumInstantPoints} />
      </label>
      <label>
        {$t('rewardTracks.costCredits')}
        <input type="number" bind:value={trackDraft.form.premiumCostCredits} />
      </label>
      <label>
        {$t('rewardTracks.costDiamonds')}
        <input type="number" bind:value={trackDraft.form.premiumCostDiamonds} />
      </label>
    {/if}

    {#snippet actions()}
      <button type="button" onclick={saveTrack}>{$t('common.save')}</button>
      <button type="button" class="ghost-button" onclick={() => (trackDraft = null)}>
        {$t('common.cancel')}
      </button>
    {/snippet}
  </Drawer>
{/if}

{#if taskDraft}
  <Drawer
    title={$t('rewardTracks.saveTask')}
    eyebrow={$t('rewardTracks.title')}
    onclose={() => (taskDraft = null)}
  >
    <label>
      {$t('rewardTracks.taskId')}
      <input type="text" bind:value={taskDraft.form.taskId} />
    </label>
    <p class="muted small">{$t('rewardTracks.taskIdHint')}</p>
    <label>
      {$t('rewardTracks.action')}
      <select bind:value={taskDraft.form.actionCode}>
        {#each actionOptions as action (action.name)}
          <option value={action.name}>
            {action.name}{action.wired ? '' : ` — ${translate('rewardTracks.notWired')}`}
          </option>
        {/each}
      </select>
    </label>
    <p class="muted small">{$t('rewardTracks.notWiredHint')}</p>
    <label>
      {$t('rewardTracks.mode')}
      <select bind:value={taskDraft.form.mode}>
        {#each MODES as mode (mode.value)}
          <option value={mode.value}>{$t(mode.key)}</option>
        {/each}
      </select>
    </label>
    <label>
      {$t('rewardTracks.parameter')}
      <input type="text" bind:value={taskDraft.form.parameter} />
    </label>
    <p class="muted small">{$t('rewardTracks.parameterHint')}</p>
    <label class="checkbox">
      <input type="checkbox" bind:checked={taskDraft.form.premium} />
      {$t('rewardTracks.premiumTask')}
    </label>

    <h4>{$t('rewardTracks.stages')}</h4>
    {#each taskDraft.form.levels as level, index (index)}
      <div class="level-row">
        <input type="number" bind:value={level.requiredCount} placeholder={$t('rewardTracks.required')} />
        <input type="number" bind:value={level.pointsReward} placeholder={$t('rewardTracks.points')} />
        <label class="checkbox">
          <input type="checkbox" bind:checked={level.premium} />
          {$t('rewardTracks.premium')}
        </label>
        <button
          type="button"
          class="danger"
          onclick={() => taskDraft.form.levels.splice(index, 1)}
          disabled={taskDraft.form.levels.length === 1}
        >
          {$t('common.remove')}
        </button>
      </div>
    {/each}
    <button type="button" class="ghost-button" onclick={addLevel}>{$t('rewardTracks.addStage')}</button>

    {#snippet actions()}
      <button type="button" onclick={saveTask}>{$t('common.save')}</button>
      <button type="button" class="ghost-button" onclick={() => (taskDraft = null)}>
        {$t('common.cancel')}
      </button>
    {/snippet}
  </Drawer>
{/if}

{#if prizeDraft}
  <Drawer
    title={$t('rewardTracks.savePrize')}
    eyebrow={$t('rewardTracks.title')}
    onclose={() => (prizeDraft = null)}
  >
    <label>
      {$t('rewardTracks.prizeId')}
      <input type="text" bind:value={prizeDraft.form.prizeId} />
    </label>
    <p class="muted small">{$t('rewardTracks.prizeIdHint')}</p>
    <label>
      {$t('rewardTracks.requiredPoints')}
      <input type="number" bind:value={prizeDraft.form.requiredPoints} />
    </label>
    <label class="checkbox">
      <input type="checkbox" bind:checked={prizeDraft.form.premium} />
      {$t('rewardTracks.premiumPrize')}
    </label>

    <h4>{$t('rewardTracks.rewards')}</h4>
    <p class="muted small">{$t('rewardTracks.bundleHint')}</p>
    {#each prizeDraft.form.rewards as reward, index (index)}
      <div class="reward-row">
        <select bind:value={reward.kind}>
          {#each kinds as kind (kind.value)}
            <option value={kind.value}>{kind.name}</option>
          {/each}
        </select>
        <input type="text" bind:value={reward.rewardTypeId} placeholder={kindHint(reward.kind)} />
        <input type="number" bind:value={reward.amount} placeholder={$t('rewardTracks.amount')} />
        <button
          type="button"
          class="danger"
          onclick={() => prizeDraft.form.rewards.splice(index, 1)}
          disabled={prizeDraft.form.rewards.length === 1}
        >
          {$t('common.remove')}
        </button>
        <span class="muted small">{kindHint(reward.kind)}</span>
      </div>
    {/each}
    <button type="button" class="ghost-button" onclick={addReward}>
      {$t('rewardTracks.addReward')}
    </button>

    {#snippet actions()}
      <button type="button" onclick={savePrize}>{$t('common.save')}</button>
      <button type="button" class="ghost-button" onclick={() => (prizeDraft = null)}>
        {$t('common.cancel')}
      </button>
    {/snippet}
  </Drawer>
{/if}

{#if cloneDraft}
  <Drawer
    title={$t('rewardTracks.clone')}
    eyebrow={$t('rewardTracks.title')}
    onclose={() => (cloneDraft = null)}
  >
    <label>
      {$t('rewardTracks.newTrackId')}
      <input type="text" bind:value={cloneDraft.newTrackId} />
    </label>
    <p class="muted small">{$t('rewardTracks.cloneHint')}</p>

    {#snippet actions()}
      <button type="button" onclick={clone}>{$t('rewardTracks.clone')}</button>
      <button type="button" class="ghost-button" onclick={() => (cloneDraft = null)}>
        {$t('common.cancel')}
      </button>
    {/snippet}
  </Drawer>
{/if}

{#if pickingPlayer}
  <PickerModal
    kind="user"
    title={$t('rewardTracks.pickPlayer')}
    onSelect={(picked) => {
      player = picked;
      pickingPlayer = false;
    }}
    onClose={() => (pickingPlayer = false)}
  />
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

<OpResult result={$ops.result} />

<style>
  /* `.row-actions`, `.table-wrap` and the chips come from styles.css; only what the sheet has no
     opinion about is here. */
  .small {
    font-size: 0.8em;
  }

  .filters {
    display: flex;
    gap: 0.5rem;
  }

  .level-row,
  .reward-row {
    display: flex;
    gap: 0.5rem;
    align-items: center;
    margin-bottom: 0.4rem;
    flex-wrap: wrap;
  }

  /* A row of pills in a table cell. Without this they butt together and "1 → 10" "5 → 20" reads as
     one number: the stage ladder was rendering as "1 → 105 → 20". */
  .chips {
    display: flex;
    gap: 4px;
    flex-wrap: wrap;
  }
</style>
