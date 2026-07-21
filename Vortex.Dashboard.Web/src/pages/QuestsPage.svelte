<script>
  import OpResult from '../components/OpResult.svelte';
  import { onMount } from 'svelte';
  import {
    Award,
    CircleCheck,
    Clock,
    EyeOff,
    Flag,
    Gift,
    ListChecks,
    Pencil,
    Plus,
    Target,
    Trash2,
    Users,
  } from '@lucide/svelte';
  import { apiGet, apiPost } from '../lib/api.js';
  import { formatDate, formatDuration, formatNumber } from '../lib/format.js';
  import { isPermissionDeniedError, hasDashboardCapability } from '../lib/permissions.js';
  import { CAPABILITIES } from '../lib/dashboardPermissions.js';
  import { reasonOk } from '../lib/validation.js';
  import { rememberReason } from '../lib/reasonHistory.js';
  import AccessDeniedNotice from '../components/AccessDeniedNotice.svelte';
  import ConfirmReasonModal from '../components/ConfirmReasonModal.svelte';
  import { identity } from '../lib/session.js';
  import { t, translate } from '../lib/i18n.js';

  // Reward is encoded in a single wire int: negative => Credits, otherwise the activity-point
  // currency type granted on completion (0 = Duckets). The form splits that back out into a friendly
  // "kind" select + an optional point-type number, then folds it back to the int when submitting.
  function emptyQuestForm() {
    return {
      campaignCode: '', chainCode: '', localizationCode: '', questType: '',
      totalSteps: 1, rewardKind: 'activityPoints', rewardPointType: 0, rewardAmount: 0,
      targetType: '', targetValue: '', enabled: true,
      catalogPageName: '', imageVersion: '', sortOrder: 0, easy: false,
      seasonal: false, seasonalSeconds: 0, endsAt: '', reason: '',
    };
  }

  // A few operator-friendly timer presets — the durations the game actually uses for seasonal quests
  // (9-minute mini, 1h, daily, and the 14-day campaign window).
  const seasonalPresets = [
    { seconds: 540, key: 'quests.preset9min' },
    { seconds: 3600, key: 'quests.preset1hour' },
    { seconds: 86400, key: 'quests.preset1day' },
    { seconds: 1209600, key: 'quests.preset14days' },
  ];

  // The API round-trips EndsAt as an ISO instant (or null); <input type="datetime-local"> wants a
  // local `yyyy-MM-ddThh:mm` string with no zone. These bridge the pair -- an empty field means
  // "no absolute end" (null on the wire), matching the nullable DateTime? server-side.
  function toDateTimeLocal(iso) {
    if (!iso) return '';
    const date = new Date(iso);
    if (Number.isNaN(date.getTime())) return '';
    const pad = (n) => String(n).padStart(2, '0');
    return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}T${pad(date.getHours())}:${pad(date.getMinutes())}`;
  }

  function fromDateTimeLocal(value) {
    if (!value) return null;
    const date = new Date(value);
    return Number.isNaN(date.getTime()) ? null : date.toISOString();
  }

  // Compact reward summary for a list row: "50 Credits" / "25 Duckets" / "10 pts(type 3)".
  function rewardChip(quest, translator) {
    const amount = formatNumber(quest.rewardAmount);
    if (Number(quest.rewardType) < 0) {
      return translator('quests.rewardCredits', { amount });
    }
    if (Number(quest.rewardType) === 0) {
      return translator('quests.rewardDuckets', { amount });
    }
    return translator('quests.rewardPoints', { amount, type: quest.rewardType });
  }

  let quests = [];
  // Campaigns power the filter dropdown. The list endpoint only reports the campaigns present in the
  // *filtered* rows, so once a filter is active it would collapse to a single option -- keep the full
  // set from the last unfiltered load so the dropdown stays complete.
  let campaigns = [];
  let campaignFilter = '';
  // Valid objective types from the backend (name + whether a trigger actually advances it today).
  // Loaded best-effort: if the fetch fails we fall back to a free-text questType input.
  let questTypes = [];
  let loading = false;
  let error = '';
  let forbidden = false;

  let newQuestOpen = false;
  let newQuest = emptyQuestForm();
  let editQuestId = null;
  let editQuestForm = null;

  // Deletes go through the shared reason modal (ConfirmReasonModal); deleteTarget describes the
  // pending delete and the modal supplies the reason.
  let deleteTarget = null;
  let deleteBusy = false;
  let deleteError = '';

  let results = {};
  let errors = {};
  let busy = {};
  let pending = null;

  $: canManage = hasDashboardCapability($identity, CAPABILITIES.opsQuestsManage);
  $: questTypeNames = questTypes.map((it) => it.name);

  async function loadQuests() {
    loading = true;
    error = '';
    forbidden = false;

    try {
      const params = campaignFilter ? `?campaign=${encodeURIComponent(campaignFilter)}` : '';
      const data = await apiGet(`/api/quests${params}`);
      quests = data.items || [];
      if (!campaignFilter) {
        campaigns = data.campaigns || [];
      }
    } catch (err) {
      if (isPermissionDeniedError(err)) {
        forbidden = true;
        quests = [];
        return;
      }

      error = err.message;
      quests = [];
    } finally {
      loading = false;
    }
  }

  function stage(id, title, endpoint, valid, body, summary, onSuccess) {
    errors = { ...errors, [id]: '' };

    if (!valid) {
      errors = { ...errors, [id]: translate('quests.fillFields') };
      return;
    }

    pending = { id, title, endpoint, body, summary, reason: body.reason, onSuccess };
  }

  function cancelPending() {
    pending = null;
  }

  async function confirmPending() {
    if (!pending) return;

    const { id, endpoint, body, reason, onSuccess } = pending;
    pending = null;

    busy = { ...busy, [id]: true };
    errors = { ...errors, [id]: '' };
    results = { ...results, [id]: null };

    try {
      const result = await apiPost(endpoint, body);
      results = { ...results, [id]: result };

      if (result.ok) {
        rememberReason(reason);
        await onSuccess?.();
      }
    } catch (err) {
      errors = {
        ...errors,
        [id]: isPermissionDeniedError(err) ? translate('common.insufficientRightsAction') : err.code || err.message,
      };
    } finally {
      busy = { ...busy, [id]: false };
    }
  }

  // Create and update take the same field set; update additionally carries the questId. The reward
  // int and the datetime are folded here so both call sites stay identical.
  function buildQuestBody(form, questId) {
    const body = {
      campaignCode: form.campaignCode.trim(),
      chainCode: form.chainCode.trim(),
      localizationCode: form.localizationCode.trim(),
      questType: form.questType.trim(),
      totalSteps: Number(form.totalSteps) || 1,
      rewardType: form.rewardKind === 'credits' ? -1 : Number(form.rewardPointType) || 0,
      rewardAmount: Number(form.rewardAmount) || 0,
      targetType: form.targetType.trim(),
      targetValue: form.targetValue.trim(),
      enabled: form.enabled,
      catalogPageName: form.catalogPageName.trim(),
      imageVersion: form.imageVersion.trim(),
      sortOrder: Number(form.sortOrder) || 0,
      easy: form.easy,
      seasonal: form.seasonal,
      seasonalSeconds: Number(form.seasonalSeconds) || 0,
      endsAt: fromDateTimeLocal(form.endsAt),
      reason: form.reason.trim(),
    };

    return questId === null ? body : { questId, ...body };
  }

  function formValid(form) {
    return (
      Boolean(form.campaignCode.trim()) &&
      Boolean(form.localizationCode.trim()) &&
      Boolean(form.questType.trim()) &&
      reasonOk(form.reason)
    );
  }

  function stageCreateQuest() {
    if (!canManage) return;

    stage(
      'createQuest',
      translate('quests.newQuest'),
      '/api/operations/quests',
      formValid(newQuest),
      buildQuestBody(newQuest, null),
      translate('quests.createQuestSummary', { name: newQuest.localizationCode.trim() || newQuest.campaignCode.trim() }),
      async () => {
        newQuestOpen = false;
        newQuest = emptyQuestForm();
        await loadQuests();
      },
    );
  }

  // List rows omit catalogPageName/imageVersion (only the detail endpoint carries them), so the edit
  // form is populated from a fresh detail fetch rather than the list row.
  async function startEditQuest(quest) {
    editQuestId = quest.id;
    editQuestForm = null;
    errors = { ...errors, updateQuest: '' };

    try {
      const detail = await apiGet(`/api/quests/${quest.id}`);
      editQuestForm = {
        campaignCode: detail.campaignCode || '',
        chainCode: detail.chainCode || '',
        localizationCode: detail.localizationCode || '',
        questType: detail.questType || '',
        totalSteps: detail.totalSteps ?? 1,
        rewardKind: Number(detail.rewardType) < 0 ? 'credits' : 'activityPoints',
        rewardPointType: Number(detail.rewardType) < 0 ? 0 : Number(detail.rewardType) || 0,
        rewardAmount: detail.rewardAmount ?? 0,
        targetType: detail.targetType || '',
        targetValue: detail.targetValue || '',
        enabled: detail.enabled ?? true,
        catalogPageName: detail.catalogPageName || '',
        imageVersion: detail.imageVersion || '',
        sortOrder: detail.sortOrder ?? 0,
        easy: detail.easy,
        seasonal: detail.seasonal,
        seasonalSeconds: detail.seasonalSeconds ?? 0,
        endsAt: toDateTimeLocal(detail.endsAt),
        reason: '',
      };
    } catch (err) {
      editQuestId = null;
      errors = {
        ...errors,
        updateQuest: isPermissionDeniedError(err) ? translate('common.insufficientRights') : err.code || err.message,
      };
    }
  }

  function stageUpdateQuest() {
    if (!canManage || !editQuestForm || editQuestId === null) return;

    stage(
      'updateQuest',
      translate('quests.edit'),
      '/api/operations/quests/update',
      formValid(editQuestForm),
      buildQuestBody(editQuestForm, editQuestId),
      translate('quests.updateQuestSummary', { id: editQuestId }),
      async () => {
        editQuestId = null;
        editQuestForm = null;
        await loadQuests();
      },
    );
  }

  function openDeleteQuest(quest) {
    if (!canManage) return;
    deleteError = '';
    deleteTarget = {
      endpoint: '/api/operations/quests/delete',
      body: { questId: quest.id },
      resultId: 'deleteQuest',
      title: translate('quests.deleteQuest'),
      summary: translate('quests.deleteQuestSummary', {
        id: quest.id,
        name: quest.localizationCode || quest.campaignCode,
      }),
      onSuccess: async () => {
        await loadQuests();
      },
    };
  }

  function cancelDelete() {
    deleteTarget = null;
    deleteError = '';
  }

  // The reason comes from the shared modal; on failure we keep the modal open and show the server's
  // message (e.g. quest_has_progress) so the operator can react without re-opening it.
  async function confirmDelete(reason) {
    if (!deleteTarget) return;
    const target = deleteTarget;

    deleteBusy = true;
    deleteError = '';
    try {
      const result = await apiPost(target.endpoint, { ...target.body, reason });
      results = { ...results, [target.resultId]: result };

      if (result.ok) {
        rememberReason(reason);
        deleteTarget = null;
        await target.onSuccess?.();
      } else {
        deleteError = result.message || translate('quests.fillFields');
      }
    } catch (err) {
      deleteError = isPermissionDeniedError(err)
        ? translate('common.insufficientRightsAction')
        : err.code || err.message;
    } finally {
      deleteBusy = false;
    }
  }

  // Best-effort: the type picker degrades to a free-text input if this fails, so errors are swallowed.
  async function loadQuestTypes() {
    try {
      const data = await apiGet('/api/quests/types');
      questTypes = data.items || [];
    } catch {
      questTypes = [];
    }
  }

  onMount(() => {
    loadQuests();
    loadQuestTypes();
  });
</script>

<section class="panel">
  <div class="panel-head">
    <h2>{$t('quests.title')}</h2>
    <div class="head-actions">
      <label class="filter-field">
        {$t('quests.campaignFilter')}
        <select bind:value={campaignFilter} on:change={loadQuests}>
          <option value="">{$t('quests.allCampaigns')}</option>
          {#each campaigns as campaign}
            <option value={campaign}>{campaign}</option>
          {/each}
        </select>
      </label>
      <button type="button" class="ghost-button" on:click={loadQuests} disabled={loading}>{$t('common.refresh')}</button>
    </div>
  </div>
  <p class="muted">{$t('quests.description')}</p>
</section>

{#if forbidden}
  <AccessDeniedNotice message={$t('quests.accessDenied')} />
{:else}
  <section class="panel">
    <div class="panel-head">
      <h2><Award size={17} strokeWidth={2} aria-hidden="true" /> {$t('quests.questsHeading')}</h2>
      {#if canManage}
        <button type="button" class="ghost-button" on:click={() => (newQuestOpen = !newQuestOpen)}>
          <Plus size={14} strokeWidth={2} aria-hidden="true" /> {newQuestOpen ? $t('quests.cancel') : $t('quests.newQuest')}
        </button>
      {/if}
    </div>

    {#if newQuestOpen}
      <div class="catalog-card-detail">
        <div class="op-field">
          <label for="new-quest-campaign">{$t('quests.campaignCodeRequired')}</label>
          <input id="new-quest-campaign" bind:value={newQuest.campaignCode} placeholder={$t('quests.campaignPlaceholder')} />
        </div>
        <div class="op-field">
          <label for="new-quest-chain">{$t('quests.chainCode')}</label>
          <input id="new-quest-chain" bind:value={newQuest.chainCode} />
        </div>
        <div class="op-field">
          <label for="new-quest-localization">{$t('quests.localizationCodeRequired')}</label>
          <input id="new-quest-localization" bind:value={newQuest.localizationCode} placeholder={$t('quests.localizationPlaceholder')} />
        </div>
        <div class="op-field">
          <label for="new-quest-type">{$t('quests.questTypeRequired')}</label>
          {#if questTypes.length > 0}
            <select id="new-quest-type" bind:value={newQuest.questType}>
              <option value="">{$t('quests.questTypeSelect')}</option>
              {#if newQuest.questType && !questTypeNames.includes(newQuest.questType)}
                <option value={newQuest.questType}>{$t('quests.questTypeLegacy', { name: newQuest.questType })}</option>
              {/if}
              {#each questTypes as questType (questType.name)}
                <option value={questType.name}>{questType.wired ? questType.name : $t('quests.questTypeNoTrigger', { name: questType.name })}</option>
              {/each}
            </select>
          {:else}
            <input id="new-quest-type" bind:value={newQuest.questType} placeholder={$t('quests.questTypePlaceholder')} />
          {/if}
        </div>
        <div class="op-field">
          <label for="new-quest-steps">{$t('quests.totalSteps')}</label>
          <input id="new-quest-steps" type="number" min="1" bind:value={newQuest.totalSteps} />
          <small class="muted">{$t('quests.objectiveHint')}</small>
        </div>

        <fieldset class="op-subgroup">
          <legend><Target size={13} strokeWidth={2} aria-hidden="true" /> {$t('quests.targetLegend')}</legend>
          <div class="op-field">
            <label for="new-quest-target-type">{$t('quests.targetType')}</label>
            <input id="new-quest-target-type" bind:value={newQuest.targetType} placeholder={$t('quests.targetTypePlaceholder')} />
          </div>
          <div class="op-field">
            <label for="new-quest-target-value">{$t('quests.targetValue')}</label>
            <input id="new-quest-target-value" bind:value={newQuest.targetValue} placeholder={$t('quests.targetValuePlaceholder')} />
          </div>
          <small class="muted">{$t('quests.targetHint')}</small>
        </fieldset>

        <fieldset class="op-subgroup">
          <legend><Gift size={13} strokeWidth={2} aria-hidden="true" /> {$t('quests.rewardLegend')}</legend>
          <div class="op-field">
            <label for="new-quest-reward-kind">{$t('quests.rewardKind')}</label>
            <select id="new-quest-reward-kind" bind:value={newQuest.rewardKind}>
              <option value="credits">{$t('quests.rewardKindCredits')}</option>
              <option value="activityPoints">{$t('quests.rewardKindActivityPoints')}</option>
            </select>
          </div>
          {#if newQuest.rewardKind === 'activityPoints'}
            <div class="op-field">
              <label for="new-quest-point-type">{$t('quests.rewardPointType')}</label>
              <input id="new-quest-point-type" type="number" min="0" bind:value={newQuest.rewardPointType} />
              <small class="muted">{$t('quests.rewardPointTypeHint')}</small>
            </div>
          {/if}
          <div class="op-field">
            <label for="new-quest-reward-amount">{$t('quests.rewardAmount')}</label>
            <input id="new-quest-reward-amount" type="number" min="0" bind:value={newQuest.rewardAmount} />
          </div>
        </fieldset>

        <fieldset class="op-subgroup">
          <legend><Clock size={13} strokeWidth={2} aria-hidden="true" /> {$t('quests.timerLegend')}</legend>
          <div class="op-field">
            <label><input type="checkbox" bind:checked={newQuest.seasonal} /> {$t('quests.seasonalLabel')}</label>
          </div>
          {#if newQuest.seasonal}
            <div class="op-field">
              <label for="new-quest-seconds">{$t('quests.seasonalSeconds')}</label>
              <input id="new-quest-seconds" type="number" min="0" bind:value={newQuest.seasonalSeconds} />
              <div class="preset-row">
                {#each seasonalPresets as preset}
                  <button type="button" class="ghost-button preset" on:click={() => { newQuest.seasonalSeconds = preset.seconds; newQuest = newQuest; }}>{$t(preset.key)}</button>
                {/each}
              </div>
            </div>
            <div class="op-field">
              <label for="new-quest-ends">{$t('quests.endsAt')}</label>
              <input id="new-quest-ends" type="datetime-local" bind:value={newQuest.endsAt} />
              <small class="muted">{$t('quests.timerHint')}</small>
            </div>
          {/if}
        </fieldset>

        <div class="op-field">
          <label for="new-quest-catalog">{$t('quests.catalogPageName')}</label>
          <input id="new-quest-catalog" bind:value={newQuest.catalogPageName} />
        </div>
        <div class="op-field">
          <label for="new-quest-image">{$t('quests.imageVersion')}</label>
          <input id="new-quest-image" bind:value={newQuest.imageVersion} />
        </div>
        <div class="op-field">
          <label for="new-quest-sort">{$t('quests.sortOrder')}</label>
          <input id="new-quest-sort" type="number" bind:value={newQuest.sortOrder} />
        </div>
        <div class="op-field">
          <label><input type="checkbox" bind:checked={newQuest.enabled} /> {$t('quests.enabledLabel')}</label>
        </div>
        <div class="op-field">
          <label><input type="checkbox" bind:checked={newQuest.easy} /> {$t('quests.easyLabel')}</label>
        </div>
        <div class="op-field">
          <label for="new-quest-reason">{$t('common.reasonRequired')}</label>
          <input id="new-quest-reason" bind:value={newQuest.reason} placeholder={$t('quests.reasonPlaceholder')} list="reason-history" />
        </div>
        <div class="op-actions">
          <button type="button" on:click={stageCreateQuest} disabled={busy.createQuest}>{$t('quests.create')}</button>
        </div>
        {#if errors.createQuest}<p class="empty-state danger">{errors.createQuest}</p>{/if}
        {#if results.createQuest}
          <OpResult result={results.createQuest} />
        {/if}
      </div>
    {/if}

    {#if loading}
      <p class="muted">{$t('common.loading')}</p>
    {:else if error}
      <p class="empty-state danger">{error}</p>
    {:else if quests.length === 0}
      <p class="empty-state">{$t('quests.noQuests')}</p>
    {:else}
      <div class="catalog-list">
        {#each quests as quest (quest.id)}
          <div class="catalog-card">
            <div class="offer-head">
              <span class="quest-icon"><Award size={20} strokeWidth={2} aria-hidden="true" /></span>
              <span class="catalog-row-main">
                <strong>{quest.localizationCode || quest.campaignCode}</strong>
                <small class="muted">{quest.campaignCode}{quest.chainCode ? ` - ${quest.chainCode}` : ''} - #{quest.id} - {quest.questType}</small>
              </span>
              <div class="op-actions offer-actions">
                {#if canManage}
                  <button type="button" class="ghost-button" on:click={() => startEditQuest(quest)}>
                    <Pencil size={14} strokeWidth={2} aria-hidden="true" /> {$t('quests.edit')}
                  </button>
                {/if}
              </div>
            </div>
            <div class="offer-meta">
              <span class="cost-chip"><Gift size={12} strokeWidth={2} aria-hidden="true" /> {rewardChip(quest, $t)}</span>
              <span class="op-chip" title={$t('quests.totalSteps')}><ListChecks size={12} strokeWidth={2} aria-hidden="true" /> {$t('quests.stepsChip', { count: quest.totalSteps })}</span>
              <span class="op-chip" title={$t('quests.accepted')}><Users size={12} strokeWidth={2} aria-hidden="true" /> {formatNumber(quest.acceptedCount)}</span>
              <span class="op-chip" title={$t('quests.completed')}><CircleCheck size={12} strokeWidth={2} aria-hidden="true" /> {formatNumber(quest.completedCount)}</span>
              {#if quest.targetType}
                <span class="op-chip" title={$t('quests.targetLegend')}><Target size={12} strokeWidth={2} aria-hidden="true" /> {quest.targetType}{quest.targetValue ? `=${quest.targetValue}` : ''}</span>
              {/if}
              {#if !quest.enabled}
                <span class="status-badge status-badge--bad"><EyeOff size={12} strokeWidth={2} aria-hidden="true" /> {$t('quests.disabledLabel')}</span>
              {/if}
              {#if quest.easy}
                <span class="status-badge status-badge--ok"><Flag size={12} strokeWidth={2} aria-hidden="true" /> {$t('quests.easyLabel')}</span>
              {/if}
              {#if quest.seasonal}
                <span class="op-chip" title={$t('quests.timerLegend')}><Clock size={12} strokeWidth={2} aria-hidden="true" /> {formatDuration(quest.seasonalSeconds)}{quest.endsAt ? ` - ${formatDate(quest.endsAt)}` : ''}</span>
              {/if}
              {#if quest.expired}
                <span class="status-badge status-badge--warn"><Clock size={12} strokeWidth={2} aria-hidden="true" /> {$t('quests.expiredLabel')}</span>
              {/if}
            </div>

            {#if editQuestId === quest.id}
              {#if editQuestForm}
                <div class="catalog-card-detail">
                  <div class="op-field">
                    <label for={`edit-quest-campaign-${quest.id}`}>{$t('quests.campaignCodeRequired')}</label>
                    <input id={`edit-quest-campaign-${quest.id}`} bind:value={editQuestForm.campaignCode} />
                  </div>
                  <div class="op-field">
                    <label for={`edit-quest-chain-${quest.id}`}>{$t('quests.chainCode')}</label>
                    <input id={`edit-quest-chain-${quest.id}`} bind:value={editQuestForm.chainCode} />
                  </div>
                  <div class="op-field">
                    <label for={`edit-quest-localization-${quest.id}`}>{$t('quests.localizationCodeRequired')}</label>
                    <input id={`edit-quest-localization-${quest.id}`} bind:value={editQuestForm.localizationCode} />
                  </div>
                  <div class="op-field">
                    <label for={`edit-quest-type-${quest.id}`}>{$t('quests.questTypeRequired')}</label>
                    {#if questTypes.length > 0}
                      <select id={`edit-quest-type-${quest.id}`} bind:value={editQuestForm.questType}>
                        <option value="">{$t('quests.questTypeSelect')}</option>
                        {#if editQuestForm.questType && !questTypeNames.includes(editQuestForm.questType)}
                          <option value={editQuestForm.questType}>{$t('quests.questTypeLegacy', { name: editQuestForm.questType })}</option>
                        {/if}
                        {#each questTypes as questType (questType.name)}
                          <option value={questType.name}>{questType.wired ? questType.name : $t('quests.questTypeNoTrigger', { name: questType.name })}</option>
                        {/each}
                      </select>
                    {:else}
                      <input id={`edit-quest-type-${quest.id}`} bind:value={editQuestForm.questType} />
                    {/if}
                  </div>
                  <div class="op-field">
                    <label for={`edit-quest-steps-${quest.id}`}>{$t('quests.totalSteps')}</label>
                    <input id={`edit-quest-steps-${quest.id}`} type="number" min="1" bind:value={editQuestForm.totalSteps} />
                    <small class="muted">{$t('quests.objectiveHint')}</small>
                  </div>

                  <fieldset class="op-subgroup">
                    <legend><Target size={13} strokeWidth={2} aria-hidden="true" /> {$t('quests.targetLegend')}</legend>
                    <div class="op-field">
                      <label for={`edit-quest-target-type-${quest.id}`}>{$t('quests.targetType')}</label>
                      <input id={`edit-quest-target-type-${quest.id}`} bind:value={editQuestForm.targetType} placeholder={$t('quests.targetTypePlaceholder')} />
                    </div>
                    <div class="op-field">
                      <label for={`edit-quest-target-value-${quest.id}`}>{$t('quests.targetValue')}</label>
                      <input id={`edit-quest-target-value-${quest.id}`} bind:value={editQuestForm.targetValue} placeholder={$t('quests.targetValuePlaceholder')} />
                    </div>
                    <small class="muted">{$t('quests.targetHint')}</small>
                  </fieldset>

                  <fieldset class="op-subgroup">
                    <legend><Gift size={13} strokeWidth={2} aria-hidden="true" /> {$t('quests.rewardLegend')}</legend>
                    <div class="op-field">
                      <label for={`edit-quest-reward-kind-${quest.id}`}>{$t('quests.rewardKind')}</label>
                      <select id={`edit-quest-reward-kind-${quest.id}`} bind:value={editQuestForm.rewardKind}>
                        <option value="credits">{$t('quests.rewardKindCredits')}</option>
                        <option value="activityPoints">{$t('quests.rewardKindActivityPoints')}</option>
                      </select>
                    </div>
                    {#if editQuestForm.rewardKind === 'activityPoints'}
                      <div class="op-field">
                        <label for={`edit-quest-point-type-${quest.id}`}>{$t('quests.rewardPointType')}</label>
                        <input id={`edit-quest-point-type-${quest.id}`} type="number" min="0" bind:value={editQuestForm.rewardPointType} />
                        <small class="muted">{$t('quests.rewardPointTypeHint')}</small>
                      </div>
                    {/if}
                    <div class="op-field">
                      <label for={`edit-quest-reward-amount-${quest.id}`}>{$t('quests.rewardAmount')}</label>
                      <input id={`edit-quest-reward-amount-${quest.id}`} type="number" min="0" bind:value={editQuestForm.rewardAmount} />
                    </div>
                  </fieldset>

                  <fieldset class="op-subgroup">
                    <legend><Clock size={13} strokeWidth={2} aria-hidden="true" /> {$t('quests.timerLegend')}</legend>
                    <div class="op-field">
                      <label><input type="checkbox" bind:checked={editQuestForm.seasonal} /> {$t('quests.seasonalLabel')}</label>
                    </div>
                    {#if editQuestForm.seasonal}
                      <div class="op-field">
                        <label for={`edit-quest-seconds-${quest.id}`}>{$t('quests.seasonalSeconds')}</label>
                        <input id={`edit-quest-seconds-${quest.id}`} type="number" min="0" bind:value={editQuestForm.seasonalSeconds} />
                        <div class="preset-row">
                          {#each seasonalPresets as preset}
                            <button type="button" class="ghost-button preset" on:click={() => { editQuestForm.seasonalSeconds = preset.seconds; editQuestForm = editQuestForm; }}>{$t(preset.key)}</button>
                          {/each}
                        </div>
                      </div>
                      <div class="op-field">
                        <label for={`edit-quest-ends-${quest.id}`}>{$t('quests.endsAt')}</label>
                        <input id={`edit-quest-ends-${quest.id}`} type="datetime-local" bind:value={editQuestForm.endsAt} />
                        <small class="muted">{$t('quests.timerHint')}</small>
                      </div>
                    {/if}
                  </fieldset>

                  <div class="op-field">
                    <label for={`edit-quest-catalog-${quest.id}`}>{$t('quests.catalogPageName')}</label>
                    <input id={`edit-quest-catalog-${quest.id}`} bind:value={editQuestForm.catalogPageName} />
                  </div>
                  <div class="op-field">
                    <label for={`edit-quest-image-${quest.id}`}>{$t('quests.imageVersion')}</label>
                    <input id={`edit-quest-image-${quest.id}`} bind:value={editQuestForm.imageVersion} />
                  </div>
                  <div class="op-field">
                    <label for={`edit-quest-sort-${quest.id}`}>{$t('quests.sortOrder')}</label>
                    <input id={`edit-quest-sort-${quest.id}`} type="number" bind:value={editQuestForm.sortOrder} />
                  </div>
                  <div class="op-field">
                    <label><input type="checkbox" bind:checked={editQuestForm.enabled} /> {$t('quests.enabledLabel')}</label>
                  </div>
                  <div class="op-field">
                    <label><input type="checkbox" bind:checked={editQuestForm.easy} /> {$t('quests.easyLabel')}</label>
                  </div>
                  <div class="op-field">
                    <label for={`edit-quest-reason-${quest.id}`}>{$t('common.reasonRequired')}</label>
                    <input id={`edit-quest-reason-${quest.id}`} bind:value={editQuestForm.reason} placeholder={$t('common.reasonPlaceholderChange')} list="reason-history" />
                  </div>
                  <div class="op-actions">
                    <button type="button" on:click={stageUpdateQuest} disabled={busy.updateQuest}>{$t('quests.save')}</button>
                    <button class="ghost-button" type="button" on:click={() => { editQuestId = null; editQuestForm = null; }}>{$t('quests.cancel')}</button>
                  </div>
                  {#if errors.updateQuest}<p class="empty-state danger">{errors.updateQuest}</p>{/if}
                  {#if results.updateQuest}
                    <OpResult result={results.updateQuest} />
                  {/if}
                </div>
              {:else if errors.updateQuest}
                <div class="catalog-card-detail"><p class="empty-state danger">{errors.updateQuest}</p></div>
              {/if}
            {/if}

            {#if canManage}
              <div class="catalog-card-detail delete-bar">
                <button type="button" class="ghost-button danger" on:click={() => openDeleteQuest(quest)}>
                  <Trash2 size={14} strokeWidth={2} aria-hidden="true" /> {$t('quests.deleteQuest')}
                </button>
              </div>
            {/if}
          </div>
        {/each}
      </div>
    {/if}
    {#if errors.deleteQuest}<p class="empty-state danger">{errors.deleteQuest}</p>{/if}
    {#if results.deleteQuest}
      <OpResult result={results.deleteQuest} />
    {/if}
  </section>
{/if}

{#if pending}
  <div class="modal-layer">
    <button class="modal-backdrop" type="button" aria-label="Cancel" on:click={cancelPending}></button>
    <section class="modal-panel" role="dialog" aria-modal="true" style="width: min(460px, 100%)">
      <header class="modal-header">
        <div>
          <p class="eyebrow">{$t('quests.confirmEyebrow')}</p>
          <h2>{pending.title}</h2>
        </div>
      </header>
      <p>{pending.summary}</p>
      <p class="muted">{$t('quests.reasonLabel', { reason: pending.reason })}</p>
      <div class="op-actions">
        <button type="button" on:click={confirmPending}>{$t('common.confirm')}</button>
        <button class="ghost-button" type="button" on:click={cancelPending}>{$t('quests.cancel')}</button>
      </div>
    </section>
  </div>
{/if}

<ConfirmReasonModal
  open={Boolean(deleteTarget)}
  title={deleteTarget?.title ?? ''}
  summary={deleteTarget?.summary ?? ''}
  confirmLabel={deleteTarget?.title ?? $t('common.confirm')}
  busy={deleteBusy}
  error={deleteError}
  on:confirm={(e) => confirmDelete(e.detail)}
  on:cancel={cancelDelete}
/>

<style>
  .head-actions {
    display: flex;
    align-items: center;
    gap: 12px;
    flex-wrap: wrap;
  }

  .filter-field {
    display: inline-flex;
    align-items: center;
    gap: 6px;
    font-size: 0.85rem;
    color: var(--muted);
    white-space: nowrap;
  }

  .ghost-button.danger {
    color: var(--danger);
    border-color: rgba(var(--danger-rgb), 0.4);
  }

  .ghost-button,
  .op-actions button {
    display: inline-flex;
    align-items: center;
    gap: 6px;
  }

  /* Quest card laid out as a column: a header line (icon + title + actions) with the reward/stat
     chips on their own line beneath. Mirrors the targeted-offer card. */
  .offer-head {
    display: flex;
    align-items: center;
    gap: 12px;
    padding: 10px 12px;
    flex-wrap: wrap;
  }

  .offer-head .catalog-row-main {
    flex: 1 1 160px;
    min-width: 120px;
  }

  .quest-icon {
    width: 44px;
    height: 44px;
    flex: 0 0 auto;
    display: grid;
    place-items: center;
    border: 1px solid var(--line-strong);
    border-radius: 9px;
    background: var(--input-bg);
    color: var(--accent);
  }

  .offer-actions {
    margin-left: auto;
    flex-wrap: wrap;
  }

  .offer-meta {
    display: flex;
    flex-wrap: wrap;
    align-items: center;
    gap: 6px;
    padding: 0 12px 10px 68px;
  }

  .offer-meta > .op-chip,
  .offer-meta > .status-badge,
  .offer-meta > .cost-chip {
    height: 24px;
    box-sizing: border-box;
  }

  .delete-bar {
    display: flex;
    justify-content: flex-end;
  }

  .panel-head {
    flex-wrap: wrap;
    row-gap: 8px;
  }

  .panel-head h2 {
    display: flex;
    align-items: center;
    gap: 8px;
  }

  .op-subgroup {
    border: 1px solid var(--line);
    border-radius: 10px;
    padding: 10px 12px 4px;
    margin: 4px 0 8px;
    display: grid;
    gap: 0;
  }

  .op-subgroup legend {
    display: inline-flex;
    align-items: center;
    gap: 6px;
    padding: 0 6px;
    font-size: 0.8rem;
    font-weight: 700;
    color: var(--muted);
  }

  .preset-row {
    display: flex;
    flex-wrap: wrap;
    gap: 6px;
    margin-top: 6px;
  }

  .ghost-button.preset {
    padding: 3px 9px;
    font-size: 0.78rem;
  }

  .catalog-list {
    display: grid;
    gap: 8px;
    margin-top: 10px;
  }

  .catalog-card {
    border: 1px solid var(--line);
    border-radius: 12px;
    overflow: hidden;
    background: var(--surface-strong);
  }

  .catalog-row-main {
    display: grid;
    gap: 2px;
    min-width: 120px;
    flex: 1 1 160px;
  }

  .catalog-row-main strong {
    overflow: hidden;
    text-overflow: ellipsis;
    white-space: nowrap;
  }

  .cost-chip {
    display: inline-flex;
    align-items: center;
    gap: 5px;
    border: 1px solid var(--warning-border);
    background: var(--warning-bg);
    color: var(--warning);
    border-radius: 999px;
    padding: 0 9px;
    font-size: 0.78rem;
    font-weight: 700;
    white-space: nowrap;
  }

  .catalog-card-detail {
    border-top: 1px solid var(--line);
    padding: 12px;
  }
</style>
