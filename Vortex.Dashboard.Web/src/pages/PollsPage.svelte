<script>
  import ConfirmStagedModal from '../components/ConfirmStagedModal.svelte';
  import { onMount } from 'svelte';
  import {
    ChartColumn,
    CircleCheck,
    CircleHelp,
    House,
    ListChecks,
    MessageSquare,
    Pencil,
    Plus,
    Split,
    ThumbsDown,
    Trash2,
  } from '@lucide/svelte';
  import AccessDeniedNotice from '../components/AccessDeniedNotice.svelte';
  import Drawer from '../components/Drawer.svelte';
  import AssetImage from '../components/AssetImage.svelte';
  import ConfirmReasonModal from '../components/ConfirmReasonModal.svelte';
  import OpResult from '../components/OpResult.svelte';
  import PickerModal from '../components/PickerModal.svelte';
  import { apiGet } from '../lib/api.js';
  import { createWriteOps } from '../lib/writeOps.js';
  import { formatDate, formatNumber } from '../lib/format.js';
  import { CAPABILITIES } from '../lib/dashboardPermissions.js';
  import { hasDashboardCapability, isPermissionDeniedError } from '../lib/permissions.js';
  import { identity } from '../lib/session.js';
  import { t, translate } from '../lib/i18n.js';

  // The client's own question types. 1/2 take a choice list; 3/4 are free text. 5 and 6 exist in
  // the client enum but its survey dialog skips them outright, so the server rejects them too --
  // the picker only ever offers what a player can actually answer.
  const CHOICE_TYPES = [1, 2];

  function emptyPollForm() {
    return {
      code: '', pollType: '', headline: '', summary: '',
      startMessage: '', endMessage: '',
      npsPoll: false, enabled: true, offerOnRoomEntry: true,
      roomId: null, roomName: '', sortOrder: 0,
    };
  }

  function emptyQuestionForm(pollId, parentQuestionId = null) {
    return {
      pollId,
      parentQuestionId,
      sortOrder: 0,
      questionType: 1,
      questionText: '',
      questionCategory: 0,
      questionAnswerType: 0,
      choices: [emptyChoice()],
    };
  }

  function emptyChoice() {
    return { value: '', choiceText: '', choiceType: 0, sortOrder: 0 };
  }

  let polls = $state([]);
  let questionTypes = $state([]);
  let enabledOnly = $state(false);
  let loading = $state(false);
  let error = $state('');
  let forbidden = $state(false);

  // One survey is expanded at a time: the detail (question tree) and the results are separate reads,
  // both keyed to the open poll so switching rows never shows one survey's answers under another's.
  let expandedId = $state(null);
  let detail = $state(null);
  let detailError = $state('');
  let results = $state(null);
  let resultsError = $state('');
  let showResults = $state(false);

  let newPollOpen = $state(false);
  let newPoll = $state(emptyPollForm());
  let editPollForm = $state(null);

  let questionForm = $state(null);
  let editingQuestionId = $state(null);

  let roomPickerFor = $state(null);

  // Deletes get their own store: they collect the reason in the shared modal (the edits carry it in
  // the form), so the two flows must be able to be staged independently without one modal's pending
  // write opening the other's dialog.
  const deleteOps = createWriteOps();

  // The edits carry their reason as a field of the form itself (the confirm step below only re-reads
  // it back), so `ask` is given the reason instead of collecting it -- but the posting, the reason
  // history and the per-form busy/error/result bookkeeping are the shared ones. Each form passes a
  // `key` so its outcome renders next to it rather than in one banner for the whole page.
  const ops = createWriteOps();

  let canManage = $derived(hasDashboardCapability($identity, CAPABILITIES.opsPollsManage));
  let choiceTypeSelected = $derived(questionForm && CHOICE_TYPES.includes(Number(questionForm.questionType)));

  async function loadPolls() {
    loading = true;
    error = '';
    forbidden = false;

    try {
      const data = await apiGet(`/api/v1/polls${enabledOnly ? '?enabled=true' : ''}`);
      polls = data.items || [];
    } catch (err) {
      if (isPermissionDeniedError(err)) {
        forbidden = true;
        polls = [];
        return;
      }

      error = err.message;
      polls = [];
    } finally {
      loading = false;
    }
  }

  // Best-effort: the form falls back to its own constant list if this read fails, so a picker is
  // never empty just because one endpoint hiccuped.
  async function loadQuestionTypes() {
    try {
      const data = await apiGet('/api/v1/polls/question-types');
      questionTypes = (data.items || []).filter((it) => it.supported);
    } catch {
      questionTypes = [];
    }
  }

  async function openPoll(poll) {
    if (expandedId === poll.id) {
      expandedId = null;
      detail = null;
      results = null;
      editPollForm = null;
      questionForm = null;
      return;
    }

    expandedId = poll.id;
    detail = null;
    results = null;
    showResults = false;
    editPollForm = null;
    questionForm = null;
    detailError = '';

    await loadDetail(poll.id);
  }

  async function loadDetail(pollId) {
    try {
      detail = await apiGet(`/api/v1/polls/${pollId}`);
    } catch (err) {
      detailError = err.message;
      detail = null;
    }
  }

  async function loadResults(pollId) {
    resultsError = '';

    try {
      results = await apiGet(`/api/v1/polls/${pollId}/results`);
    } catch (err) {
      resultsError = err.message;
      results = null;
    }
  }

  async function toggleResults(pollId) {
    showResults = true;

    if (showResults && !results) {
      await loadResults(pollId);
    }
  }

  const stage = (id, title, endpoint, valid, body, summary, onSuccess) =>
    ops.ask(endpoint, body, title, summary, {
      key: id,
      valid,
      invalidMessage: translate('polls.fillFields'),
      onSuccess,
    });

  function pollBody(form, pollId) {
    const body = {
      code: form.code.trim(),
      pollType: form.pollType.trim(),
      headline: form.headline.trim(),
      summary: form.summary.trim(),
      startMessage: form.startMessage.trim(),
      endMessage: form.endMessage.trim(),
      npsPoll: form.npsPoll,
      enabled: form.enabled,
      offerOnRoomEntry: form.offerOnRoomEntry,
      roomId: form.roomId ?? null,
      sortOrder: Number(form.sortOrder) || 0,
    };

    return pollId === null ? body : { pollId, ...body };
  }

  function pollFormValid(form) {
    return (
      Boolean(form.code.trim()) &&
      Boolean(form.headline.trim()) &&
      Boolean(form.summary.trim())
    );
  }

  function stageCreatePoll() {
    if (!canManage) return;

    stage(
      'createPoll',
      translate('polls.newPoll'),
      '/api/v1/operations/polls',
      pollFormValid(newPoll),
      pollBody(newPoll, null),
      translate('polls.createPollSummary', { code: newPoll.code.trim() }),
      async () => {
        newPollOpen = false;
        newPoll = emptyPollForm();
        await loadPolls();
      },
    );
  }

  function startEditPoll(poll) {
    editPollForm = {
      code: poll.code || '',
      pollType: poll.pollType || '',
      headline: poll.headline || '',
      summary: poll.summary || '',
      startMessage: poll.startMessage || '',
      endMessage: poll.endMessage || '',
      npsPoll: Boolean(poll.npsPoll),
      enabled: Boolean(poll.enabled),
      offerOnRoomEntry: Boolean(poll.offerOnRoomEntry),
      roomId: poll.roomId ?? null,
      roomName: poll.roomName || '',
      sortOrder: poll.sortOrder ?? 0,
    };
  }

  function stageUpdatePoll(pollId) {
    if (!canManage || !editPollForm) return;

    stage(
      `updatePoll:${pollId}`,
      translate('polls.editPoll'),
      '/api/v1/operations/polls/update',
      pollFormValid(editPollForm),
      pollBody(editPollForm, pollId),
      translate('polls.updatePollSummary', { code: editPollForm.code.trim() }),
      async () => {
        editPollForm = null;
        await loadPolls();
        await loadDetail(pollId);
      },
    );
  }

  function questionBody(form, questionId) {
    const takesChoices = CHOICE_TYPES.includes(Number(form.questionType));

    const body = {
      pollId: form.pollId,
      parentQuestionId: form.parentQuestionId ?? null,
      sortOrder: Number(form.sortOrder) || 0,
      questionType: Number(form.questionType),
      questionText: form.questionText.trim(),
      questionCategory: Number(form.questionCategory) || 0,
      questionAnswerType: Number(form.questionAnswerType) || 0,
      // A text question carries no choices: sending the half-filled rows the operator may have typed
      // before switching the type would only be stored and never shown.
      choices: takesChoices
        ? form.choices
            .filter((c) => c.value.trim() || c.choiceText.trim())
            .map((c, index) => ({
              value: c.value.trim(),
              choiceText: c.choiceText.trim(),
              choiceType: Number(c.choiceType) || 0,
              sortOrder: index,
            }))
        : [],
    };

    return questionId === null ? body : { questionId, ...body };
  }

  function questionFormValid(form) {
    if (!form.questionText.trim()) return false;

    if (!CHOICE_TYPES.includes(Number(form.questionType))) return true;

    const filled = form.choices.filter((c) => c.value.trim() && c.choiceText.trim());

    return filled.length > 0 && filled.length === form.choices.filter((c) => c.value.trim() || c.choiceText.trim()).length;
  }

  function startNewQuestion(pollId, parentQuestionId = null) {
    editingQuestionId = null;
    questionForm = emptyQuestionForm(pollId, parentQuestionId);
  }

  function startEditQuestion(pollId, question) {
    editingQuestionId = question.id;
    questionForm = {
      pollId,
      parentQuestionId: question.parentQuestionId ?? null,
      sortOrder: question.sortOrder ?? 0,
      questionType: question.questionType,
      questionText: question.questionText || '',
      questionCategory: question.questionCategory ?? 0,
      questionAnswerType: question.questionAnswerType ?? 0,
      choices:
        question.choices?.length > 0
          ? question.choices.map((c) => ({
              value: c.value,
              choiceText: c.choiceText,
              choiceType: c.choiceType,
              sortOrder: c.sortOrder,
            }))
          : [emptyChoice()],
    };
  }

  // Editing a follow-up needs its parent id, which the tree carries structurally rather than on the
  // child row -- so it is threaded in here when the edit starts.
  function startEditFollowUp(pollId, parentId, child) {
    startEditQuestion(pollId, { ...child, parentQuestionId: parentId });
  }

  function stageSaveQuestion() {
    if (!canManage || !questionForm) return;

    const isEdit = editingQuestionId !== null;

    stage(
      isEdit ? `updateQuestion:${editingQuestionId}` : 'createQuestion',
      isEdit ? translate('polls.editQuestion') : translate('polls.newQuestion'),
      isEdit ? '/api/v1/operations/polls/questions/update' : '/api/v1/operations/polls/questions',
      questionFormValid(questionForm),
      questionBody(questionForm, editingQuestionId),
      questionForm.questionText.trim(),
      async () => {
        const pollId = questionForm.pollId;
        questionForm = null;
        editingQuestionId = null;
        await loadDetail(pollId);
        await loadPolls();
      },
    );
  }

  // Deletes go straight through the shared reason modal rather than the two-step stage/confirm the
  // edits use: the modal already captures the reason and confirms, so staging would ask twice.
  // poll_has_answers / question_has_answers are the two refusals an operator hits in practice, and
  // both mean "disable it instead" -- createWriteOps keeps the modal open on them with the code
  // visible rather than closing over the failure.
  function askDelete(target) {
    if (!canManage) return;

    const isPoll = target.kind === 'poll';

    deleteOps.ask(
      isPoll ? '/api/v1/operations/polls/delete' : '/api/v1/operations/polls/questions/delete',
      isPoll ? { pollId: target.id } : { questionId: target.id },
      isPoll ? translate('polls.deletePoll') : translate('polls.deleteQuestion'),
      target.label,
      {
        danger: true,
        onSuccess: async () => {
          if (isPoll) {
            expandedId = null;
            detail = null;
            results = null;
          } else {
            await loadDetail(target.pollId);
          }

          await loadPolls();
        },
      },
    );
  }

  function pickRoom(item) {
    if (roomPickerFor === 'new') {
      newPoll.roomId = item.id;
      newPoll.roomName = item.name;
    } else if (roomPickerFor === 'edit' && editPollForm) {
      editPollForm.roomId = item.id;
      editPollForm.roomName = item.name;
    }
  }

  function clearRoom(form) {
    form.roomId = null;
    form.roomName = '';
    return form;
  }

  function typeLabel(questionType) {
    const known = questionTypes.find((it) => it.id === Number(questionType));
    return known ? known.name : `#${questionType}`;
  }

  onMount(() => {
    loadPolls();
    loadQuestionTypes();
  });
</script>

<section class="panel">
  <div class="panel-head">
    <h2>{$t('polls.title')}</h2>
    
    <div class="head-actions">
      <label class="filter-field">
        <input autocomplete="off" spellcheck="false" type="checkbox" bind:checked={enabledOnly} onchange={loadPolls} />
        {$t('polls.enabledOnly')}
      </label>
      <button type="button" class="warning" onclick={loadPolls} disabled={loading}>
        {$t('common.refresh')}
      </button>
    </div>
  </div>
  <p class="muted">{$t('polls.description')}</p>
</section>

{#if forbidden}
  <AccessDeniedNotice message={$t('polls.accessDenied')} />
{:else}
  <section class="panel">
    <div class="panel-head">
      <h2><ListChecks size={17} strokeWidth={2} aria-hidden="true" /> {$t('polls.pollsHeading')}</h2>
      {#if canManage}
        <button type="button" class="success" onclick={() => (newPollOpen = true)}>
          {$t('polls.newPoll')}
        </button>
      {/if}
    </div>


    {#if error}
      <p class="empty-state danger" role="alert">{error}</p>
    {:else if loading}
      <p class="empty-state">{$t('common.loading')}</p>
    {/if}

    <div class="catalog-list">
      {#each polls as poll (poll.id)}
        <article class="catalog-card">
          <button type="button" class="poll-row" onclick={() => openPoll(poll)}>
            <span class="catalog-row-main">
              <strong>{poll.headline}</strong>
              <small>{poll.code} - {$t('polls.questionCount', { roots: poll.rootQuestionCount, followUps: poll.followUpCount })}</small>
            </span>
            <span class="chip-row">
              {#if !poll.enabled}<span class="chip off">{$t('polls.chipDisabled')}</span>{/if}
              {#if poll.npsPoll}<span class="chip">{$t('polls.chipNps')}</span>{/if}
              {#if poll.roomId}
                <span class="chip" class:warn={poll.roomMissing}>
                  {poll.roomMissing ? $t('polls.chipRoomMissing', { id: poll.roomId }) : poll.roomName}
                </span>
              {/if}
              {#if poll.enabled && !poll.offerable}
                <span class="chip warn">{$t('polls.chipNoQuestions')}</span>
              {/if}
            </span>
            <span class="funnel">
              <span title={$t('polls.offered')}>{formatNumber(poll.offeredCount)}</span>
              <span class="funnel-sep">/</span>
              <span class="ok" title={$t('polls.completed')}>{formatNumber(poll.completedCount)}</span>
              <small>{poll.completionRate}%</small>
            </span>
          </button>

          {#if expandedId === poll.id}
            <div class="catalog-card-detail">
              {#if detailError}
                <p class="empty-state danger" role="alert">{detailError}</p>
              {:else if !detail}
                <p class="empty-state">{$t('common.loading')}</p>
              {:else}
                <div class="detail-actions">
                  {#if canManage}
                    <button type="button" class="ghost-button" onclick={() => (editPollForm ? (editPollForm = null) : startEditPoll(detail))}>
                      {editPollForm ? $t('polls.cancel') : $t('polls.editPoll')}
                    </button>
                  {/if}
                  <button type="button" class="ghost-button" onclick={() => toggleResults(poll.id)}>
                    {$t('polls.showResults')}
                  </button>
                  {#if canManage}
                    <button
                      type="button"
                      class="ghost-button danger"
                      onclick={() => askDelete({ kind: 'poll', id: poll.id, pollId: poll.id, label: poll.code })}
                    >
                      {$t('polls.deletePoll')}
                    </button>
                  {/if}
                </div>


                <h3 class="section-title">
                  <CircleHelp size={14} strokeWidth={2} aria-hidden="true" /> {$t('polls.questionsHeading')}
                </h3>

                {#if detail.questions.length === 0}
                  <p class="empty-state">{$t('polls.noQuestions')}</p>
                {/if}

                <div class="question-list">
                  {#each detail.questions as question (question.id)}
                    <div class="question-card">
                      <div class="question-head">
                        <span class="catalog-row-main">
                          <strong>{question.questionText}</strong>
                          <small>
                            {typeLabel(question.questionType)} -
                            {$t('polls.answersRecorded', { count: formatNumber(question.answerCount) })}
                          </small>
                        </span>
                        {#if canManage}
                          <span class="row-actions">
                            <button type="button" class="ghost-button" onclick={() => startEditQuestion(poll.id, question)}>
                              {$t('common.edit')}</button>
                            {#if detail.npsPoll}
                              <button type="button" class="success" onclick={() => startNewQuestion(poll.id, question.id)}>
                                {$t('polls.addFollowUp')}
                              </button>
                            {/if}
                            <button
                              type="button"
                              class="ghost-button danger"
                              onclick={() => askDelete({ kind: 'question', id: question.id, pollId: poll.id, label: question.questionText })}
                            >
                              {$t('common.delete')}</button>
                          </span>
                        {/if}
                      </div>

                      {#if question.choices.length > 0}
                        <ul class="choice-list">
                          {#each question.choices as choice (choice.id)}
                            <li>
                              <code>{choice.value}</code> {choice.choiceText}
                              {#if choice.choiceType > 0}
                                <span class="chip small"><Split size={11} strokeWidth={2} aria-hidden="true" /> {choice.choiceType}</span>
                              {/if}
                            </li>
                          {/each}
                        </ul>
                      {/if}

                      {#each question.children as child (child.id)}
                        <div class="follow-up">
                          <span class="catalog-row-main">
                            <strong>{child.questionText}</strong>
                            <small>
                              {typeLabel(child.questionType)} -
                              {$t('polls.followUpFor', { category: child.questionCategory })}
                            </small>
                          </span>
                          {#if canManage}
                            <span class="row-actions">
                              <button type="button" class="ghost-button" onclick={() => startEditFollowUp(poll.id, question.id, child)}>
                                {$t('common.edit')}</button>
                              <button
                                type="button"
                                class="ghost-button danger"
                                onclick={() => askDelete({ kind: 'question', id: child.id, pollId: poll.id, label: child.questionText })}
                              >
                                {$t('common.delete')}</button>
                            </span>
                          {/if}
                        </div>
                      {/each}
                    </div>
                  {/each}
                </div>

                {#if canManage && !questionForm}
                  <button type="button" class="success" onclick={() => startNewQuestion(poll.id)}>
                    {$t('polls.newQuestion')}
                  </button>
                {/if}


              {/if}
            </div>
          {/if}
        </article>
      {:else}
        {#if !loading}<p class="empty-state">{$t('polls.noPolls')}</p>{/if}
      {/each}
    </div>
  </section>
{/if}

{#if roomPickerFor}
  <PickerModal
    kind="room"
    title={$t('polls.pickRoom')}
    onSelect={pickRoom}
    onClose={() => (roomPickerFor = null)}
  />
{/if}

<ConfirmStagedModal {ops} eyebrow={$t('polls.confirmEyebrow')} />

{#if newPollOpen}
  <Drawer title={$t('polls.newPoll')} eyebrow={$t('polls.title')} onclose={() => { newPollOpen = false; }}>
    <div class="catalog-card-detail">
      <div class="op-field">
        <label for="new-poll-code">{$t('polls.codeRequired')}</label>
        <input autocomplete="off" spellcheck="false" id="new-poll-code" bind:value={newPoll.code} placeholder={$t('polls.codePlaceholder')} />
        <small class="muted">{$t('polls.codeHint')}</small>
      </div>
      <div class="op-field">
        <label for="new-poll-headline">{$t('polls.headlineRequired')}</label>
        <input autocomplete="off" spellcheck="false" id="new-poll-headline" bind:value={newPoll.headline} />
      </div>
      <div class="op-field">
        <label for="new-poll-summary">{$t('polls.summaryRequired')}</label>
        <input autocomplete="off" spellcheck="false" id="new-poll-summary" bind:value={newPoll.summary} />
        <small class="muted">{$t('polls.offerHint')}</small>
      </div>
      <div class="op-field">
        <label for="new-poll-start">{$t('polls.startMessage')}</label>
        <input autocomplete="off" spellcheck="false" id="new-poll-start" bind:value={newPoll.startMessage} />
      </div>
      <div class="op-field">
        <label for="new-poll-end">{$t('polls.endMessage')}</label>
        <input autocomplete="off" spellcheck="false" id="new-poll-end" bind:value={newPoll.endMessage} />
      </div>
      <div class="op-field">
        <label for="new-poll-type">{$t('polls.pollType')}</label>
        <input autocomplete="off" spellcheck="false" id="new-poll-type" bind:value={newPoll.pollType} placeholder={$t('polls.pollTypePlaceholder')} />
      </div>
      <div class="op-field">
        <label><input autocomplete="off" spellcheck="false" type="checkbox" bind:checked={newPoll.npsPoll} /> {$t('polls.npsLabel')}</label>
        <small class="muted">{$t('polls.npsHint')}</small>
      </div>
      <div class="op-field">
        <label><input autocomplete="off" spellcheck="false" type="checkbox" bind:checked={newPoll.offerOnRoomEntry} /> {$t('polls.offerOnRoomEntry')}</label>
      </div>
      <div class="op-field">
        <span class="field-label">{$t('polls.roomPin')}</span>
        <div class="picker-row">
          <button type="button" class="ghost-button" onclick={() => (roomPickerFor = 'new')}>
            {newPoll.roomName || $t('polls.anyRoom')}
          </button>
          {#if newPoll.roomId}
            <button type="button" class="ghost-button" onclick={() => (newPoll = clearRoom(newPoll))}>
              {$t('polls.clearRoom')}
            </button>
          {/if}
        </div>
        <small class="muted">{$t('polls.roomPinHint')}</small>
      </div>
      <div class="op-field">
        <label for="new-poll-sort">{$t('polls.sortOrder')}</label>
        <input autocomplete="off" spellcheck="false" id="new-poll-sort" type="number" bind:value={newPoll.sortOrder} />
      </div>
      <div class="op-field">
        <label><input autocomplete="off" spellcheck="false" type="checkbox" bind:checked={newPoll.enabled} /> {$t('polls.enabledLabel')}</label>
      </div>
      <OpResult result={$ops.results.createPoll} error={$ops.errors.createPoll} />
    </div>

    {#snippet actions()}
      <button type="button" onclick={stageCreatePoll} disabled={$ops.busyKeys.createPoll || !canManage} class="success">
        {$t('polls.create')}
      </button>
      <button type="button" class="ghost-button" onclick={() => (newPollOpen = false)}>{$t('polls.cancel')}</button>
    {/snippet}
  </Drawer>
{/if}

{#if editPollForm}
  <Drawer title={$t('polls.editPoll')} eyebrow={$t('polls.title')} onclose={() => { editPollForm = null; }}>
    <div class="op-field">
      <label for="edit-poll-code">{$t('polls.codeRequired')}</label>
      <input autocomplete="off" spellcheck="false" id="edit-poll-code" bind:value={editPollForm.code} />
    </div>
    <div class="op-field">
      <label for="edit-poll-headline">{$t('polls.headlineRequired')}</label>
      <input autocomplete="off" spellcheck="false" id="edit-poll-headline" bind:value={editPollForm.headline} />
    </div>
    <div class="op-field">
      <label for="edit-poll-summary">{$t('polls.summaryRequired')}</label>
      <input autocomplete="off" spellcheck="false" id="edit-poll-summary" bind:value={editPollForm.summary} />
    </div>
    <div class="op-field">
      <label for="edit-poll-start">{$t('polls.startMessage')}</label>
      <input autocomplete="off" spellcheck="false" id="edit-poll-start" bind:value={editPollForm.startMessage} />
    </div>
    <div class="op-field">
      <label for="edit-poll-end">{$t('polls.endMessage')}</label>
      <input autocomplete="off" spellcheck="false" id="edit-poll-end" bind:value={editPollForm.endMessage} />
    </div>
    <div class="op-field">
      <label for="edit-poll-type">{$t('polls.pollType')}</label>
      <input autocomplete="off" spellcheck="false" id="edit-poll-type" bind:value={editPollForm.pollType} />
    </div>
    <div class="op-field">
      <label><input autocomplete="off" spellcheck="false" type="checkbox" bind:checked={editPollForm.npsPoll} /> {$t('polls.npsLabel')}</label>
    </div>
    <div class="op-field">
      <label><input autocomplete="off" spellcheck="false" type="checkbox" bind:checked={editPollForm.offerOnRoomEntry} /> {$t('polls.offerOnRoomEntry')}</label>
    </div>
    <div class="op-field">
      <span class="field-label">{$t('polls.roomPin')}</span>
      <div class="picker-row">
        <button type="button" class="ghost-button" onclick={() => (roomPickerFor = 'edit')}>
          {editPollForm.roomName || $t('polls.anyRoom')}
        </button>
        {#if editPollForm.roomId}
          <button type="button" class="ghost-button" onclick={() => (editPollForm = clearRoom(editPollForm))}>
            {$t('polls.clearRoom')}
          </button>
        {/if}
      </div>
    </div>
    <div class="op-field">
      <label for="edit-poll-sort">{$t('polls.sortOrder')}</label>
      <input autocomplete="off" spellcheck="false" id="edit-poll-sort" type="number" bind:value={editPollForm.sortOrder} />
    </div>
    <div class="op-field">
      <label><input autocomplete="off" spellcheck="false" type="checkbox" bind:checked={editPollForm.enabled} /> {$t('polls.enabledLabel')}</label>
    </div>
    <OpResult result={$ops.results[`updatePoll:${editPollForm.id}`]} error={$ops.errors[`updatePoll:${editPollForm.id}`]} />

    {#snippet actions()}
      <button type="button" onclick={() => stageUpdatePoll(editPollForm.id)} disabled={$ops.busyKeys[`updatePoll:${editPollForm.id}`]}>
        {$t('polls.save')}
      </button>
      <button type="button" class="ghost-button" onclick={() => (editPollForm = null)}>{$t('polls.cancel')}</button>
    {/snippet}
  </Drawer>
{/if}

{#if questionForm}
  <Drawer title={editingQuestionId ? $t('polls.editQuestion') : $t('polls.newQuestion')} eyebrow={$t('polls.title')} onclose={() => { questionForm = null; editingQuestionId = null; }}>
    <div class="question-form">
      {#if questionForm.parentQuestionId}
        <p><span class="chip small">{$t('polls.chipFollowUp')}</span></p>
      {/if}
      <div class="op-field">
        <label for="question-text">{$t('polls.questionTextRequired')}</label>
        <input autocomplete="off" spellcheck="false" id="question-text" bind:value={questionForm.questionText} />
      </div>
      <div class="op-field">
        <label for="question-type">{$t('polls.questionType')}</label>
        <select id="question-type" bind:value={questionForm.questionType}>
          {#each questionTypes as type (type.id)}
            <option value={type.id}>{type.name}</option>
          {/each}
          {#if questionTypes.length === 0}
            <option value={1}>SingleChoice</option>
            <option value={2}>MultipleChoice</option>
            <option value={3}>TextLine</option>
            <option value={4}>TextArea</option>
          {/if}
        </select>
      </div>
      {#if questionForm.parentQuestionId}
        <div class="op-field">
          <label for="question-category">{$t('polls.questionCategory')}</label>
          <input autocomplete="off" spellcheck="false" id="question-category" type="number" min="0" bind:value={questionForm.questionCategory} />
          <small class="muted">{$t('polls.questionCategoryHint')}</small>
        </div>
      {/if}
      <div class="op-field">
        <label for="question-sort">{$t('polls.sortOrder')}</label>
        <input autocomplete="off" spellcheck="false" id="question-sort" type="number" bind:value={questionForm.sortOrder} />
      </div>

      {#if choiceTypeSelected}
        <fieldset class="op-subgroup">
          <legend>{$t('polls.choicesLegend')}</legend>
          {#each questionForm.choices as choice, index}
            <div class="choice-row">
              <input autocomplete="off" spellcheck="false" placeholder={$t('polls.choiceValue')} bind:value={choice.value} />
              <input autocomplete="off" spellcheck="false" placeholder={$t('polls.choiceText')} bind:value={choice.choiceText} />
              <input autocomplete="off" spellcheck="false"
                type="number"
                min="0"
                title={$t('polls.choiceTypeHint')}
                bind:value={choice.choiceType}
                disabled={!detail.npsPoll}
              />
              <button
                type="button"
                class="ghost-button danger"
                onclick={() => {
                  questionForm.choices = questionForm.choices.filter((_, i) => i !== index);
                  if (questionForm.choices.length === 0) questionForm.choices = [emptyChoice()];
                }}
              >
                {$t('common.delete')}</button>
            </div>
          {/each}
          <button
      type="button"
      class="success"
      onclick={() => (questionForm.choices = [...questionForm.choices, emptyChoice()])}
          >
            {$t('polls.addChoice')}
          </button>
          <small class="muted">{$t('polls.choicesHint')}</small>
        </fieldset>
      {/if}

      <OpResult
        result={$ops.results[editingQuestionId ? `updateQuestion:${editingQuestionId}` : 'createQuestion']}
        error={$ops.errors[editingQuestionId ? `updateQuestion:${editingQuestionId}` : 'createQuestion']}
      />
    </div>

    {#snippet actions()}
      <button type="button" onclick={stageSaveQuestion}>{$t('polls.save')}</button>
      <button type="button" class="ghost-button" onclick={() => { questionForm = null; editingQuestionId = null; }}>
        {$t('polls.cancel')}
      </button>
    {/snippet}
  </Drawer>
{/if}

<ConfirmReasonModal
  open={Boolean($deleteOps.pending)}
  title={$deleteOps.pending?.title ?? ''}
  changes={$deleteOps.pending?.changes ?? []}
  noteOnly={$deleteOps.pending?.noteOnly ?? false}
  summary={$deleteOps.pending?.summary ?? ''}
  confirmLabel={$t('common.confirm')}
  busy={$deleteOps.busy}
  error={$deleteOps.error}
  danger={$deleteOps.pending?.danger ?? false}
  onconfirm={deleteOps.confirm}
  oncancel={() => deleteOps.cancel()}
/>

<style>
  .poll-row {
    display: flex;
    align-items: center;
    gap: 12px;
    width: 100%;
    text-align: left;
    background: none;
    border: 0;
    color: var(--ink);
    padding: 11px 12px;
  }

  .poll-row:hover {
    background: var(--surface-hover);
  }

  .catalog-row-main small {
    color: var(--muted);
  }

  .chip-row {
    display: flex;
    flex-wrap: wrap;
    gap: 5px;
  }

  .chip {
    display: inline-flex;
    align-items: center;
    gap: 4px;
    border: 1px solid var(--line-strong);
    border-radius: 999px;
    padding: 0 9px;
    font-size: 0.76rem;
    font-weight: 700;
    white-space: nowrap;
  }

  .chip.small {
    font-size: 0.7rem;
    padding: 0 7px;
  }

  .chip.off {
    border-color: var(--line);
    color: var(--muted);
  }

  .chip.warn {
    border-color: var(--warning-border);
    background: var(--warning-bg);
    color: var(--warning);
  }

  .funnel {
    display: flex;
    align-items: baseline;
    gap: 4px;
    font-weight: 700;
    white-space: nowrap;
  }

  .funnel .ok {
    color: var(--ok);
  }

  .funnel-sep,
  .funnel small {
    color: var(--muted);
    font-weight: 500;
  }

  .detail-actions,
  .picker-row,
  .row-actions {
    display: flex;
    flex-wrap: wrap;
    gap: 6px;
    align-items: center;
  }

  .detail-actions {
    margin-bottom: 10px;
  }

  .section-title {
    display: flex;
    align-items: center;
    gap: 7px;
    margin: 14px 0 8px;
    font-size: 0.95rem;
  }

  .question-list {
    display: grid;
    gap: 8px;
    margin-bottom: 10px;
  }

  .question-card {
    border: 1px solid var(--line);
    border-radius: 8px;
    padding: 10px 11px;
    background: var(--surface);
  }

  .question-head {
    display: flex;
    align-items: center;
    gap: 10px;
  }

  .choice-list {
    list-style: none;
    margin: 8px 0 0;
    padding: 0;
    display: grid;
    gap: 3px;
    font-size: 0.84rem;
    color: var(--muted);
  }

  .choice-list code {
    color: var(--accent);
  }

  .follow-up {
    display: flex;
    align-items: center;
    gap: 10px;
    margin-top: 8px;
    margin-left: 14px;
    padding: 8px 10px;
    border-left: 2px solid var(--line-strong);
    background: var(--surface-strong);
    border-radius: 0 8px 8px 0;
  }

  .question-form {
    border: 1px solid var(--line-strong);
    border-radius: 8px;
    padding: 11px 12px;
    margin-top: 8px;
  }

  .question-form h4 {
    display: flex;
    align-items: center;
    gap: 7px;
    margin: 0 0 8px;
  }

  .op-subgroup {
    border: 1px solid var(--line);
    border-radius: 8px;
    padding: 10px 12px;
    margin: 4px 0 8px;
  }

  .op-subgroup legend {
    padding: 0 6px;
    font-size: 0.8rem;
    font-weight: 700;
    color: var(--muted);
  }

  .choice-row {
    display: grid;
    grid-template-columns: 1fr 1.4fr 78px auto;
    gap: 6px;
    margin-bottom: 6px;
  }

  .funnel-grid {
    display: grid;
    grid-template-columns: repeat(auto-fit, minmax(120px, 1fr));
    gap: 8px;
    margin-bottom: 12px;
  }

  .stat {
    border: 1px solid var(--line);
    border-radius: 8px;
    padding: 8px 10px;
    background: var(--surface);
    display: grid;
    gap: 2px;
  }

  .stat small {
    display: flex;
    align-items: center;
    gap: 5px;
    color: var(--muted);
  }

  .result-card {
    border: 1px solid var(--line);
    border-radius: 8px;
    padding: 10px 11px;
    margin-bottom: 8px;
    background: var(--surface);
  }

  .tally {
    list-style: none;
    margin: 8px 0 0;
    padding: 0;
    display: grid;
    gap: 6px;
  }

  .tally li {
    display: grid;
    grid-template-columns: minmax(90px, 1fr) minmax(60px, 2fr) auto;
    align-items: center;
    gap: 9px;
    font-size: 0.86rem;
  }

  .tally-label {
    display: flex;
    align-items: center;
    gap: 5px;
  }

  .bar {
    display: block;
    height: 8px;
    border-radius: 999px;
    background: var(--input-bg);
    overflow: hidden;
  }

  .bar-fill {
    display: block;
    height: 100%;
    background: var(--accent);
  }

  .tally-count {
    font-weight: 700;
    white-space: nowrap;
  }

  .tally-count small {
    color: var(--muted);
    font-weight: 500;
  }

  .free-text {
    list-style: none;
    margin: 8px 0 0;
    padding: 0;
    display: grid;
    gap: 6px;
  }

  .free-text li {
    display: flex;
    align-items: center;
    gap: 9px;
  }

  .field-label {
    font-size: 0.85rem;
    font-weight: 600;
  }

  .filter-field {
    display: inline-flex;
    align-items: center;
    gap: 6px;
  }

</style>

{#if showResults}
  <Drawer
    title={$t('polls.resultsHeading')}
    eyebrow={detail?.code || $t('polls.title')}
    width={720}
    onclose={() => (showResults = false)}
  >
    {#if resultsError}
      <p class="empty-state danger" role="alert">{resultsError}</p>
    {:else if !results}
      <p class="empty-state">{$t('common.loading')}</p>
    {:else}
      <div class="funnel-grid">
        <div class="stat"><small>{$t('polls.offered')}</small><strong>{formatNumber(results.funnel.offered)}</strong></div>
        <div class="stat"><small>{$t('polls.pendingOffers')}</small><strong>{formatNumber(results.funnel.pending)}</strong></div>
        <div class="stat"><small>{$t('polls.started')}</small><strong>{formatNumber(results.funnel.started)}</strong></div>
        <div class="stat">
          <small><CircleCheck size={12} strokeWidth={2} aria-hidden="true" /> {$t('polls.completed')}</small>
          <strong>{formatNumber(results.funnel.completed)} <span class="muted">({results.funnel.completionRate}%)</span></strong>
        </div>
        <div class="stat">
          <small><ThumbsDown size={12} strokeWidth={2} aria-hidden="true" /> {$t('polls.rejected')}</small>
          <strong>{formatNumber(results.funnel.rejected)} <span class="muted">({results.funnel.rejectionRate}%)</span></strong>
        </div>
      </div>

      {#each results.questions as question (question.id)}
        <div class="result-card">
          <div class="catalog-row-main">
            <strong>{question.questionText}</strong>
            <small>
              {#if question.isFollowUp}<span class="chip small">{$t('polls.chipFollowUp')}</span>{/if}
              {$t('polls.respondents', { count: formatNumber(question.respondents) })}
            </small>
          </div>

          {#if question.tally.length > 0}
            <ul class="tally">
              {#each question.tally as row}
                <li>
                  <span class="tally-label">
                    {row.text}
                    {#if row.retired}<span class="chip small warn">{$t('polls.chipRetiredChoice')}</span>{/if}
                  </span>
                  <span class="bar"><span class="bar-fill" style={`width:${row.share}%`}></span></span>
                  <span class="tally-count">{formatNumber(row.count)} <small>{row.share}%</small></span>
                </li>
              {/each}
            </ul>
          {:else if question.freeText.length > 0}
            <ul class="free-text">
              {#each question.freeText as answer}
                <li>
                  <AssetImage src={answer.avatarUrl} alt={answer.playerName ?? ''} size={28} fallbackIcon={MessageSquare} />
                  <span class="catalog-row-main">
                    <strong>{answer.answer}</strong>
                    <small>{answer.playerName ?? `#${answer.playerId}`} - {formatDate(answer.answeredAt)}</small>
                  </span>
                </li>
              {/each}
            </ul>
            {#if question.freeTextTruncated}
              <small class="muted">{$t('polls.freeTextTruncated')}</small>
            {/if}
          {:else}
            <p class="empty-state">{$t('polls.noAnswersYet')}</p>
          {/if}
        </div>
      {/each}
    {/if}
  </Drawer>
{/if}
