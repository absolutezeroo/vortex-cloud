<script>
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
  import { reasonOk } from '../lib/validation.js';

  // The client's own question types. 1/2 take a choice list; 3/4 are free text. 5 and 6 exist in
  // the client enum but its survey dialog skips them outright, so the server rejects them too --
  // the picker only ever offers what a player can actually answer.
  const CHOICE_TYPES = [1, 2];

  function emptyPollForm() {
    return {
      code: '', pollType: '', headline: '', summary: '',
      startMessage: '', endMessage: '',
      npsPoll: false, enabled: true, offerOnRoomEntry: true,
      roomId: null, roomName: '', sortOrder: 0, reason: '',
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
      reason: '',
    };
  }

  function emptyChoice() {
    return { value: '', choiceText: '', choiceType: 0, sortOrder: 0 };
  }

  let polls = [];
  let questionTypes = [];
  let enabledOnly = false;
  let loading = false;
  let error = '';
  let forbidden = false;

  // One survey is expanded at a time: the detail (question tree) and the results are separate reads,
  // both keyed to the open poll so switching rows never shows one survey's answers under another's.
  let expandedId = null;
  let detail = null;
  let detailError = '';
  let results = null;
  let resultsError = '';
  let showResults = false;

  let newPollOpen = false;
  let newPoll = emptyPollForm();
  let editPollForm = null;

  let questionForm = null;
  let editingQuestionId = null;

  let roomPickerFor = null;

  // Deletes get their own store: they collect the reason in the shared modal (the edits carry it in
  // the form), so the two flows must be able to be staged independently without one modal's pending
  // write opening the other's dialog.
  const deleteOps = createWriteOps();

  // The edits carry their reason as a field of the form itself (the confirm step below only re-reads
  // it back), so `ask` is given the reason instead of collecting it -- but the posting, the reason
  // history and the per-form busy/error/result bookkeeping are the shared ones. Each form passes a
  // `key` so its outcome renders next to it rather than in one banner for the whole page.
  const ops = createWriteOps();

  $: canManage = hasDashboardCapability($identity, CAPABILITIES.opsPollsManage);
  $: choiceTypeSelected = questionForm && CHOICE_TYPES.includes(Number(questionForm.questionType));

  async function loadPolls() {
    loading = true;
    error = '';
    forbidden = false;

    try {
      const data = await apiGet(`/api/polls${enabledOnly ? '?enabled=true' : ''}`);
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
      const data = await apiGet('/api/polls/question-types');
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
      detail = await apiGet(`/api/polls/${pollId}`);
    } catch (err) {
      detailError = err.message;
      detail = null;
    }
  }

  async function loadResults(pollId) {
    resultsError = '';

    try {
      results = await apiGet(`/api/polls/${pollId}/results`);
    } catch (err) {
      resultsError = err.message;
      results = null;
    }
  }

  async function toggleResults(pollId) {
    showResults = !showResults;

    if (showResults && !results) {
      await loadResults(pollId);
    }
  }

  const stage = (id, title, endpoint, valid, body, summary, onSuccess) =>
    ops.ask(endpoint, body, title, summary, {
      key: id,
      valid,
      invalidMessage: translate('polls.fillFields'),
      reason: body.reason,
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
      reason: form.reason.trim(),
    };

    return pollId === null ? body : { pollId, ...body };
  }

  function pollFormValid(form) {
    return (
      Boolean(form.code.trim()) &&
      Boolean(form.headline.trim()) &&
      Boolean(form.summary.trim()) &&
      reasonOk(form.reason)
    );
  }

  function stageCreatePoll() {
    if (!canManage) return;

    stage(
      'createPoll',
      translate('polls.newPoll'),
      '/api/operations/polls',
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
      reason: '',
    };
  }

  function stageUpdatePoll(pollId) {
    if (!canManage || !editPollForm) return;

    stage(
      `updatePoll:${pollId}`,
      translate('polls.editPoll'),
      '/api/operations/polls/update',
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
      reason: form.reason.trim(),
    };

    return questionId === null ? body : { questionId, ...body };
  }

  function questionFormValid(form) {
    if (!form.questionText.trim() || !reasonOk(form.reason)) return false;

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
      reason: '',
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
      isEdit ? '/api/operations/polls/questions/update' : '/api/operations/polls/questions',
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
      isPoll ? '/api/operations/polls/delete' : '/api/operations/polls/questions/delete',
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
      newPoll = newPoll;
    } else if (roomPickerFor === 'edit' && editPollForm) {
      editPollForm.roomId = item.id;
      editPollForm.roomName = item.name;
      editPollForm = editPollForm;
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
        <input type="checkbox" bind:checked={enabledOnly} on:change={loadPolls} />
        {$t('polls.enabledOnly')}
      </label>
      <button type="button" class="ghost-button" on:click={loadPolls} disabled={loading}>
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
        <button type="button" class="ghost-button" on:click={() => (newPollOpen = !newPollOpen)}>
          <Plus size={14} strokeWidth={2} aria-hidden="true" />
          {newPollOpen ? $t('polls.cancel') : $t('polls.newPoll')}
        </button>
      {/if}
    </div>

    {#if newPollOpen}
      <div class="catalog-card-detail">
        <div class="op-field">
          <label for="new-poll-code">{$t('polls.codeRequired')}</label>
          <input id="new-poll-code" bind:value={newPoll.code} placeholder={$t('polls.codePlaceholder')} />
          <small class="muted">{$t('polls.codeHint')}</small>
        </div>
        <div class="op-field">
          <label for="new-poll-headline">{$t('polls.headlineRequired')}</label>
          <input id="new-poll-headline" bind:value={newPoll.headline} />
        </div>
        <div class="op-field">
          <label for="new-poll-summary">{$t('polls.summaryRequired')}</label>
          <input id="new-poll-summary" bind:value={newPoll.summary} />
          <small class="muted">{$t('polls.offerHint')}</small>
        </div>
        <div class="op-field">
          <label for="new-poll-start">{$t('polls.startMessage')}</label>
          <input id="new-poll-start" bind:value={newPoll.startMessage} />
        </div>
        <div class="op-field">
          <label for="new-poll-end">{$t('polls.endMessage')}</label>
          <input id="new-poll-end" bind:value={newPoll.endMessage} />
        </div>
        <div class="op-field">
          <label for="new-poll-type">{$t('polls.pollType')}</label>
          <input id="new-poll-type" bind:value={newPoll.pollType} placeholder={$t('polls.pollTypePlaceholder')} />
        </div>
        <div class="op-field">
          <label><input type="checkbox" bind:checked={newPoll.npsPoll} /> {$t('polls.npsLabel')}</label>
          <small class="muted">{$t('polls.npsHint')}</small>
        </div>
        <div class="op-field">
          <label><input type="checkbox" bind:checked={newPoll.offerOnRoomEntry} /> {$t('polls.offerOnRoomEntry')}</label>
        </div>
        <div class="op-field">
          <span class="field-label">{$t('polls.roomPin')}</span>
          <div class="picker-row">
            <button type="button" class="ghost-button" on:click={() => (roomPickerFor = 'new')}>
              <House size={14} strokeWidth={2} aria-hidden="true" />
              {newPoll.roomName || $t('polls.anyRoom')}
            </button>
            {#if newPoll.roomId}
              <button type="button" class="ghost-button" on:click={() => (newPoll = clearRoom(newPoll))}>
                {$t('polls.clearRoom')}
              </button>
            {/if}
          </div>
          <small class="muted">{$t('polls.roomPinHint')}</small>
        </div>
        <div class="op-field">
          <label for="new-poll-sort">{$t('polls.sortOrder')}</label>
          <input id="new-poll-sort" type="number" bind:value={newPoll.sortOrder} />
        </div>
        <div class="op-field">
          <label><input type="checkbox" bind:checked={newPoll.enabled} /> {$t('polls.enabledLabel')}</label>
        </div>
        <div class="op-field">
          <label for="new-poll-reason">{$t('common.reason')}</label>
          <input id="new-poll-reason" bind:value={newPoll.reason} />
        </div>
        <button type="button" on:click={stageCreatePoll} disabled={busy.createPoll || !canManage}>
          {$t('polls.create')}
        </button>
        <OpResult result={$ops.results.createPoll} error={$ops.errors.createPoll} />
      </div>
    {/if}

    {#if error}
      <p class="empty-state danger">{error}</p>
    {:else if loading}
      <p class="empty-state">{$t('common.loading')}</p>
    {/if}

    <div class="catalog-list">
      {#each polls as poll (poll.id)}
        <article class="catalog-card">
          <button type="button" class="poll-row" on:click={() => openPoll(poll)}>
            <span class="catalog-row-main">
              <strong>{poll.headline}</strong>
              <small>{poll.code} - {$t('polls.questionCount', { roots: poll.rootQuestionCount, followUps: poll.followUpCount })}</small>
            </span>
            <span class="chip-row">
              {#if !poll.enabled}<span class="chip off">{$t('polls.chipDisabled')}</span>{/if}
              {#if poll.npsPoll}<span class="chip"><Split size={12} strokeWidth={2} aria-hidden="true" /> {$t('polls.chipNps')}</span>{/if}
              {#if poll.roomId}
                <span class="chip" class:warn={poll.roomMissing}>
                  <House size={12} strokeWidth={2} aria-hidden="true" />
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
                <p class="empty-state danger">{detailError}</p>
              {:else if !detail}
                <p class="empty-state">{$t('common.loading')}</p>
              {:else}
                <div class="detail-actions">
                  {#if canManage}
                    <button type="button" class="ghost-button" on:click={() => (editPollForm ? (editPollForm = null) : startEditPoll(detail))}>
                      <Pencil size={13} strokeWidth={2} aria-hidden="true" />
                      {editPollForm ? $t('polls.cancel') : $t('polls.editPoll')}
                    </button>
                  {/if}
                  <button type="button" class="ghost-button" on:click={() => toggleResults(poll.id)}>
                    <ChartColumn size={13} strokeWidth={2} aria-hidden="true" />
                    {showResults ? $t('polls.hideResults') : $t('polls.showResults')}
                  </button>
                  {#if canManage}
                    <button
                      type="button"
                      class="ghost-button danger"
                      on:click={() => askDelete({ kind: 'poll', id: poll.id, pollId: poll.id, label: poll.code })}
                    >
                      <Trash2 size={13} strokeWidth={2} aria-hidden="true" /> {$t('polls.deletePoll')}
                    </button>
                  {/if}
                </div>

                {#if editPollForm}
                  <div class="op-field">
                    <label for="edit-poll-code">{$t('polls.codeRequired')}</label>
                    <input id="edit-poll-code" bind:value={editPollForm.code} />
                  </div>
                  <div class="op-field">
                    <label for="edit-poll-headline">{$t('polls.headlineRequired')}</label>
                    <input id="edit-poll-headline" bind:value={editPollForm.headline} />
                  </div>
                  <div class="op-field">
                    <label for="edit-poll-summary">{$t('polls.summaryRequired')}</label>
                    <input id="edit-poll-summary" bind:value={editPollForm.summary} />
                  </div>
                  <div class="op-field">
                    <label for="edit-poll-start">{$t('polls.startMessage')}</label>
                    <input id="edit-poll-start" bind:value={editPollForm.startMessage} />
                  </div>
                  <div class="op-field">
                    <label for="edit-poll-end">{$t('polls.endMessage')}</label>
                    <input id="edit-poll-end" bind:value={editPollForm.endMessage} />
                  </div>
                  <div class="op-field">
                    <label for="edit-poll-type">{$t('polls.pollType')}</label>
                    <input id="edit-poll-type" bind:value={editPollForm.pollType} />
                  </div>
                  <div class="op-field">
                    <label><input type="checkbox" bind:checked={editPollForm.npsPoll} /> {$t('polls.npsLabel')}</label>
                  </div>
                  <div class="op-field">
                    <label><input type="checkbox" bind:checked={editPollForm.offerOnRoomEntry} /> {$t('polls.offerOnRoomEntry')}</label>
                  </div>
                  <div class="op-field">
                    <span class="field-label">{$t('polls.roomPin')}</span>
                    <div class="picker-row">
                      <button type="button" class="ghost-button" on:click={() => (roomPickerFor = 'edit')}>
                        <House size={14} strokeWidth={2} aria-hidden="true" />
                        {editPollForm.roomName || $t('polls.anyRoom')}
                      </button>
                      {#if editPollForm.roomId}
                        <button type="button" class="ghost-button" on:click={() => (editPollForm = clearRoom(editPollForm))}>
                          {$t('polls.clearRoom')}
                        </button>
                      {/if}
                    </div>
                  </div>
                  <div class="op-field">
                    <label for="edit-poll-sort">{$t('polls.sortOrder')}</label>
                    <input id="edit-poll-sort" type="number" bind:value={editPollForm.sortOrder} />
                  </div>
                  <div class="op-field">
                    <label><input type="checkbox" bind:checked={editPollForm.enabled} /> {$t('polls.enabledLabel')}</label>
                  </div>
                  <div class="op-field">
                    <label for="edit-poll-reason">{$t('common.reason')}</label>
                    <input id="edit-poll-reason" bind:value={editPollForm.reason} />
                  </div>
                  <button type="button" on:click={() => stageUpdatePoll(poll.id)} disabled={$ops.busyKeys[`updatePoll:${poll.id}`]}>
                    {$t('polls.save')}
                  </button>
                  <OpResult result={$ops.results[`updatePoll:${poll.id}`]} error={$ops.errors[`updatePoll:${poll.id}`]} />
                {/if}

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
                            <button type="button" class="ghost-button" on:click={() => startEditQuestion(poll.id, question)}>
                              <Pencil size={12} strokeWidth={2} aria-hidden="true" />
                            </button>
                            {#if detail.npsPoll}
                              <button type="button" class="ghost-button" on:click={() => startNewQuestion(poll.id, question.id)}>
                                <Split size={12} strokeWidth={2} aria-hidden="true" /> {$t('polls.addFollowUp')}
                              </button>
                            {/if}
                            <button
                              type="button"
                              class="ghost-button danger"
                              on:click={() => askDelete({ kind: 'question', id: question.id, pollId: poll.id, label: question.questionText })}
                            >
                              <Trash2 size={12} strokeWidth={2} aria-hidden="true" />
                            </button>
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
                              <button type="button" class="ghost-button" on:click={() => startEditFollowUp(poll.id, question.id, child)}>
                                <Pencil size={12} strokeWidth={2} aria-hidden="true" />
                              </button>
                              <button
                                type="button"
                                class="ghost-button danger"
                                on:click={() => askDelete({ kind: 'question', id: child.id, pollId: poll.id, label: child.questionText })}
                              >
                                <Trash2 size={12} strokeWidth={2} aria-hidden="true" />
                              </button>
                            </span>
                          {/if}
                        </div>
                      {/each}
                    </div>
                  {/each}
                </div>

                {#if canManage && !questionForm}
                  <button type="button" class="ghost-button" on:click={() => startNewQuestion(poll.id)}>
                    <Plus size={13} strokeWidth={2} aria-hidden="true" /> {$t('polls.newQuestion')}
                  </button>
                {/if}

                {#if questionForm && questionForm.pollId === poll.id}
                  <div class="question-form">
                    <h4>
                      {editingQuestionId ? $t('polls.editQuestion') : $t('polls.newQuestion')}
                      {#if questionForm.parentQuestionId}<span class="chip small">{$t('polls.chipFollowUp')}</span>{/if}
                    </h4>
                    <div class="op-field">
                      <label for="question-text">{$t('polls.questionTextRequired')}</label>
                      <input id="question-text" bind:value={questionForm.questionText} />
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
                        <input id="question-category" type="number" min="0" bind:value={questionForm.questionCategory} />
                        <small class="muted">{$t('polls.questionCategoryHint')}</small>
                      </div>
                    {/if}
                    <div class="op-field">
                      <label for="question-sort">{$t('polls.sortOrder')}</label>
                      <input id="question-sort" type="number" bind:value={questionForm.sortOrder} />
                    </div>

                    {#if choiceTypeSelected}
                      <fieldset class="op-subgroup">
                        <legend>{$t('polls.choicesLegend')}</legend>
                        {#each questionForm.choices as choice, index}
                          <div class="choice-row">
                            <input placeholder={$t('polls.choiceValue')} bind:value={choice.value} />
                            <input placeholder={$t('polls.choiceText')} bind:value={choice.choiceText} />
                            <input
                              type="number"
                              min="0"
                              title={$t('polls.choiceTypeHint')}
                              bind:value={choice.choiceType}
                              disabled={!detail.npsPoll}
                            />
                            <button
                              type="button"
                              class="ghost-button danger"
                              on:click={() => {
                                questionForm.choices = questionForm.choices.filter((_, i) => i !== index);
                                if (questionForm.choices.length === 0) questionForm.choices = [emptyChoice()];
                              }}
                            >
                              <Trash2 size={12} strokeWidth={2} aria-hidden="true" />
                            </button>
                          </div>
                        {/each}
                        <button
                          type="button"
                          class="ghost-button"
                          on:click={() => (questionForm.choices = [...questionForm.choices, emptyChoice()])}
                        >
                          <Plus size={12} strokeWidth={2} aria-hidden="true" /> {$t('polls.addChoice')}
                        </button>
                        <small class="muted">{$t('polls.choicesHint')}</small>
                      </fieldset>
                    {/if}

                    <div class="op-field">
                      <label for="question-reason">{$t('common.reason')}</label>
                      <input id="question-reason" bind:value={questionForm.reason} />
                    </div>
                    <div class="picker-row">
                      <button type="button" on:click={stageSaveQuestion}>{$t('polls.save')}</button>
                      <button type="button" class="ghost-button" on:click={() => { questionForm = null; editingQuestionId = null; }}>
                        {$t('polls.cancel')}
                      </button>
                    </div>
                    <OpResult
                      result={$ops.results[editingQuestionId ? `updateQuestion:${editingQuestionId}` : 'createQuestion']}
                      error={$ops.errors[editingQuestionId ? `updateQuestion:${editingQuestionId}` : 'createQuestion']}
                    />
                  </div>
                {/if}

                {#if showResults}
                  <h3 class="section-title">
                    <ChartColumn size={14} strokeWidth={2} aria-hidden="true" /> {$t('polls.resultsHeading')}
                  </h3>

                  {#if resultsError}
                    <p class="empty-state danger">{resultsError}</p>
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

{#if $ops.pending}
  <div class="modal-layer">
    <button class="modal-backdrop" type="button" aria-label="Cancel" on:click={() => ops.cancel()}></button>
    <section class="modal-panel" role="dialog" aria-modal="true" style="width: min(460px, 100%)">
      <header class="modal-header">
        <div>
          <p class="eyebrow">{$t('polls.confirmEyebrow')}</p>
          <h2>{$ops.pending.title}</h2>
        </div>
      </header>
      <p>{$ops.pending.summary}</p>
      <p class="muted">{$t('polls.reasonLabel', { reason: $ops.pending.reason })}</p>
      <div class="op-actions">
        <button type="button" on:click={() => ops.confirm()}>{$t('common.confirm')}</button>
        <button class="ghost-button" type="button" on:click={() => ops.cancel()}>{$t('polls.cancel')}</button>
      </div>
    </section>
  </div>
{/if}

<ConfirmReasonModal
  open={Boolean($deleteOps.pending)}
  title={$deleteOps.pending?.title ?? ''}
  summary={$deleteOps.pending?.summary ?? ''}
  confirmLabel={$t('common.confirm')}
  busy={$deleteOps.busy}
  error={$deleteOps.error}
  danger={$deleteOps.pending?.danger ?? false}
  on:confirm={(e) => deleteOps.confirm(e.detail)}
  on:cancel={() => deleteOps.cancel()}
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

  .catalog-card-detail {
    border-top: 1px solid var(--line);
    padding: 12px;
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
    border-radius: 10px;
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
    border-radius: 10px;
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
    border-radius: 10px;
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
    border-radius: 10px;
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
    border-radius: 10px;
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

  .panel-head {
    flex-wrap: wrap;
    row-gap: 8px;
  }

  .panel-head h2 {
    display: flex;
    align-items: center;
    gap: 8px;
  }
</style>
