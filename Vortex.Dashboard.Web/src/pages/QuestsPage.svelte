<script>
  import ConfirmStagedModal from '../components/ConfirmStagedModal.svelte';
  import Modal from '../components/Modal.svelte';
  import OpResult from '../components/OpResult.svelte';
  import AssetImage from '../components/AssetImage.svelte';
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
  import { apiGet } from '../lib/api.js';
  import { createWriteOps } from '../lib/writeOps.js';
  import { formatDate, formatDuration, formatNumber } from '../lib/format.js';
  import { isPermissionDeniedError, hasDashboardCapability } from '../lib/permissions.js';
  import { CAPABILITIES } from '../lib/dashboardPermissions.js';
  import { reasonOk } from '../lib/validation.js';
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

  // One modal serves both create and edit: the fields are the same nineteen either way, and the two
  // used to be a copy-paste pair unfolding inside the panel. `id === null` is a create.
  // Editing in place pushed the rest of the list off-screen and read as part of the row it belonged
  // to, which is the case every design system reserves a dialog for -- a multi-field form with mixed
  // input types. It also means the save commits straight away: a confirm dialog on top of a dialog
  // is the one thing the same guidance rules out, and the modal is itself the deliberate step.
  let questModal = null;

  // The edits carry their reason as a field of the form itself; the deletes collect it in the shared
  // ConfirmReasonModal. Two stores rather than one so staging an edit cannot open the delete dialog
  // (and vice versa) -- both post, remember the reason and track busy/error/result the same way.
  const ops = createWriteOps();
  const deleteOps = createWriteOps();

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

  function openCreateQuest() {
    if (!canManage) return;

    ops.clear('questForm');
    questModal = { id: null, form: emptyQuestForm() };
  }

  function closeQuestModal() {
    questModal = null;
    ops.clear('questForm');
  }

  function saveQuest() {
    if (!canManage || !questModal) return;

    const { id, form } = questModal;
    const staged = ops.ask(
      id === null ? '/api/operations/quests' : '/api/operations/quests/update',
      buildQuestBody(form, id),
      id === null ? translate('quests.newQuest') : translate('quests.edit'),
      '',
      {
        key: 'questForm',
        valid: formValid(form),
        invalidMessage: translate('quests.fillFields'),
        reason: form.reason.trim(),
        onSuccess: async () => {
          questModal = null;
          await loadQuests();
        },
      },
    );

    if (staged) void ops.confirm();
  }

  // List rows omit catalogPageName/imageVersion (only the detail endpoint carries them), so the edit
  // form is populated from a fresh detail fetch rather than the list row.
  async function startEditQuest(quest) {
    if (!canManage) return;

    ops.clear('questForm');

    try {
      const detail = await apiGet(`/api/quests/${quest.id}`);

      questModal = {
        id: quest.id,
        form: {
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
        },
      };
    } catch (err) {
      ops.fail(
        'questForm',
        isPermissionDeniedError(err) ? translate('common.insufficientRights') : err.code || err.message,
      );
    }
  }

  // The reason comes from the shared modal; on a server refusal (quest_has_progress) createWriteOps
  // keeps it open with the message so the operator can react without re-opening it.
  function openDeleteQuest(quest) {
    if (!canManage) return;

    deleteOps.ask(
      '/api/operations/quests/delete',
      { questId: quest.id },
      translate('quests.deleteQuest'),
      translate('quests.deleteQuestSummary', {
        id: quest.id,
        name: quest.localizationCode || quest.campaignCode,
      }),
      { key: 'deleteQuest', danger: true, onSuccess: loadQuests },
    );
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
        <button type="button" class="ghost-button" on:click={openCreateQuest}>
          <Plus size={14} strokeWidth={2} aria-hidden="true" /> {$t('quests.newQuest')}
        </button>
      {/if}
    </div>

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
              <span class="quest-icon">
                <AssetImage src={quest.imageUrl} alt="" size={34} fallbackIcon={Award} />
              </span>
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
    {#if $deleteOps.errors.deleteQuest}<p class="empty-state danger">{$deleteOps.errors.deleteQuest}</p>{/if}
    {#if $deleteOps.results.deleteQuest}
      <OpResult result={$deleteOps.results.deleteQuest} />
    {/if}
  </section>
{/if}

{#if questModal}
  <Modal
    title={questModal.id === null ? $t('quests.newQuest') : $t('quests.editQuest')}
    eyebrow={$t('quests.questsHeading')}
    width={720}
    labelledBy="quest-form-title"
    on:close={closeQuestModal}
  >
    <div class="op-field">
      <label for="quest-campaign">{$t('quests.campaignCodeRequired')}</label>
      <input id="quest-campaign" bind:value={questModal.form.campaignCode} placeholder={$t('quests.campaignPlaceholder')} />
    </div>
    <div class="op-field">
      <label for="quest-chain">{$t('quests.chainCode')}</label>
      <input id="quest-chain" bind:value={questModal.form.chainCode} />
    </div>
    <div class="op-field">
      <label for="quest-localization">{$t('quests.localizationCodeRequired')}</label>
      <input id="quest-localization" bind:value={questModal.form.localizationCode} placeholder={$t('quests.localizationPlaceholder')} />
    </div>
    <div class="op-field">
      <label for="quest-type">{$t('quests.questTypeRequired')}</label>
      {#if questTypes.length > 0}
        <select id="quest-type" bind:value={questModal.form.questType}>
          <option value="">{$t('quests.questTypeSelect')}</option>
          {#if questModal.form.questType && !questTypeNames.includes(questModal.form.questType)}
            <option value={questModal.form.questType}>{$t('quests.questTypeLegacy', { name: questModal.form.questType })}</option>
          {/if}
          {#each questTypes as questType (questType.name)}
            <option value={questType.name}>{questType.wired ? questType.name : $t('quests.questTypeNoTrigger', { name: questType.name })}</option>
          {/each}
        </select>
      {:else}
        <input id="quest-type" bind:value={questModal.form.questType} placeholder={$t('quests.questTypePlaceholder')} />
      {/if}
    </div>
    <div class="op-field">
      <label for="quest-steps">{$t('quests.totalSteps')}</label>
      <input id="quest-steps" type="number" min="1" bind:value={questModal.form.totalSteps} />
      <small class="muted">{$t('quests.objectiveHint')}</small>
    </div>

    <fieldset class="op-subgroup">
      <legend><Target size={13} strokeWidth={2} aria-hidden="true" /> {$t('quests.targetLegend')}</legend>
      <div class="op-field">
        <label for="quest-target-type">{$t('quests.targetType')}</label>
        <input id="quest-target-type" bind:value={questModal.form.targetType} placeholder={$t('quests.targetTypePlaceholder')} />
      </div>
      <div class="op-field">
        <label for="quest-target-value">{$t('quests.targetValue')}</label>
        <input id="quest-target-value" bind:value={questModal.form.targetValue} placeholder={$t('quests.targetValuePlaceholder')} />
      </div>
      <small class="muted">{$t('quests.targetHint')}</small>
    </fieldset>

    <fieldset class="op-subgroup">
      <legend><Gift size={13} strokeWidth={2} aria-hidden="true" /> {$t('quests.rewardLegend')}</legend>
      <div class="op-field">
        <label for="quest-reward-kind">{$t('quests.rewardKind')}</label>
        <select id="quest-reward-kind" bind:value={questModal.form.rewardKind}>
          <option value="credits">{$t('quests.rewardKindCredits')}</option>
          <option value="activityPoints">{$t('quests.rewardKindActivityPoints')}</option>
        </select>
      </div>
      {#if questModal.form.rewardKind === 'activityPoints'}
        <div class="op-field">
          <label for="quest-point-type">{$t('quests.rewardPointType')}</label>
          <input id="quest-point-type" type="number" min="0" bind:value={questModal.form.rewardPointType} />
          <small class="muted">{$t('quests.rewardPointTypeHint')}</small>
        </div>
      {/if}
      <div class="op-field">
        <label for="quest-reward-amount">{$t('quests.rewardAmount')}</label>
        <input id="quest-reward-amount" type="number" min="0" bind:value={questModal.form.rewardAmount} />
      </div>
    </fieldset>

    <fieldset class="op-subgroup">
      <legend><Clock size={13} strokeWidth={2} aria-hidden="true" /> {$t('quests.timerLegend')}</legend>
      <div class="op-field">
        <label><input type="checkbox" bind:checked={questModal.form.seasonal} /> {$t('quests.seasonalLabel')}</label>
      </div>
      {#if questModal.form.seasonal}
        <div class="op-field">
          <label for="quest-seconds">{$t('quests.seasonalSeconds')}</label>
          <input id="quest-seconds" type="number" min="0" bind:value={questModal.form.seasonalSeconds} />
          <div class="preset-row">
            {#each seasonalPresets as preset}
              <button type="button" class="ghost-button preset" on:click={() => { questModal.form.seasonalSeconds = preset.seconds; questModal = questModal; }}>{$t(preset.key)}</button>
            {/each}
          </div>
        </div>
        <div class="op-field">
          <label for="quest-ends">{$t('quests.endsAt')}</label>
          <input id="quest-ends" type="datetime-local" bind:value={questModal.form.endsAt} />
          <small class="muted">{$t('quests.timerHint')}</small>
        </div>
      {/if}
    </fieldset>

    <div class="op-field">
      <label for="quest-catalog">{$t('quests.catalogPageName')}</label>
      <input id="quest-catalog" bind:value={questModal.form.catalogPageName} />
    </div>
    <div class="op-field">
      <label for="quest-image">{$t('quests.imageVersion')}</label>
      <input id="quest-image" bind:value={questModal.form.imageVersion} />
    </div>
    <div class="op-field">
      <label for="quest-sort">{$t('quests.sortOrder')}</label>
      <input id="quest-sort" type="number" bind:value={questModal.form.sortOrder} />
    </div>
    <div class="op-field">
      <label><input type="checkbox" bind:checked={questModal.form.enabled} /> {$t('quests.enabledLabel')}</label>
    </div>
    <div class="op-field">
      <label><input type="checkbox" bind:checked={questModal.form.easy} /> {$t('quests.easyLabel')}</label>
    </div>
    <div class="op-field">
      <label for="quest-reason">{$t('common.reasonRequired')}</label>
      <input id="quest-reason" bind:value={questModal.form.reason} placeholder={$t('quests.reasonPlaceholder')} list="reason-history" />
    </div>

    {#if $ops.errors.questForm}<p class="empty-state danger">{$ops.errors.questForm}</p>{/if}

    <svelte:fragment slot="actions">
      <button type="button" on:click={saveQuest} disabled={$ops.busyKeys.questForm}>
        {questModal.id === null ? $t('quests.create') : $t('quests.save')}
      </button>
      <button class="ghost-button" type="button" on:click={closeQuestModal}>{$t('quests.cancel')}</button>
    </svelte:fragment>
  </Modal>
{/if}

<ConfirmStagedModal {ops} eyebrow={$t('quests.confirmEyebrow')} />

<ConfirmReasonModal
  open={Boolean($deleteOps.pending)}
  title={$deleteOps.pending?.title ?? ''}
  summary={$deleteOps.pending?.summary ?? ''}
  confirmLabel={$deleteOps.pending?.title ?? $t('common.confirm')}
  busy={$deleteOps.busy}
  error={$deleteOps.error}
  danger={$deleteOps.pending?.danger ?? false}
  on:confirm={(e) => deleteOps.confirm(e.detail)}
  on:cancel={() => deleteOps.cancel()}
/>

<style>
  .ghost-button,
  /* Quest card laid out as a column: a header line (icon + title + actions) with the reward/stat
     chips on their own line beneath. Mirrors the targeted-offer card. */
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

</style>
