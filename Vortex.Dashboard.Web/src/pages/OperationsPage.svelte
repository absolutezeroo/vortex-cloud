<script>
  import OpResult from '../components/OpResult.svelte';
  import { Coins, Zap, Package, UserX } from '@lucide/svelte';
  import { isPermissionDeniedError, hasDashboardCapability } from '../lib/permissions.js';
  import { apiPost } from '../lib/api.js';
  import { OPERATION_CAPABILITIES } from '../lib/dashboardPermissions.js';
  import { reasonOk, positive, nonNegative } from '../lib/validation.js';
  import AccessDeniedNotice from '../components/AccessDeniedNotice.svelte';
  import PickerModal from '../components/PickerModal.svelte';
  import { identity, openPlayer, openItem } from '../lib/session.js';
  import { t, translate } from '../lib/i18n.js';

  // One state bag per action.
  let credits = { playerId: '', playerName: '', playerOnline: false, amount: '', reason: '' };
  let activity = {
    playerId: '',
    playerName: '',
    playerOnline: false,
    type: '0',
    amount: '',
    reason: '',
  };
  let item = {
    playerId: '',
    playerName: '',
    playerOnline: false,
    definitionId: '',
    defName: '',
    defSprite: '',
    defIcon: '',
    extraData: '',
    reason: '',
  };
  let kick = { playerId: '', playerName: '', playerOnline: false, reason: '' };

  // Per-action UI state, keyed by action id.
  let results = {};
  let errors = {};
  let busy = {};

  // Confirmation gate: nothing is sent until the operator confirms.
  let pending = null;

  // Active picker modal (user / furniture).
  let picker = null;

  const capabilityByAction = {
    credits: OPERATION_CAPABILITIES.credits,
    activity: OPERATION_CAPABILITIES.activity,
    item: OPERATION_CAPABILITIES.item,
    kick: OPERATION_CAPABILITIES.kick,
  };

  $: canCredits = hasDashboardCapability($identity, capabilityByAction.credits);
  $: canActivity = hasDashboardCapability($identity, capabilityByAction.activity);
  $: canItem = hasDashboardCapability($identity, capabilityByAction.item);
  $: canKick = hasDashboardCapability($identity, capabilityByAction.kick);

  function pickUser(apply) {
    picker = { kind: 'user', title: translate('operations.selectPlayerTitle'), onSelect: apply };
  }

  function pickFurniture(apply) {
    picker = { kind: 'furniture', title: translate('operations.selectFurnitureTitle'), onSelect: apply };
  }

  function stage(id, title, endpoint, valid, body, summary) {
    errors = { ...errors, [id]: '' };

    if (!valid) {
      errors = {
        ...errors,
        [id]: translate('operations.fillFields'),
      };
      return;
    }

    pending = { id, title, endpoint, body, summary, reason: body.reason };
  }

  function stageCredits() {
    if (!canCredits) {
      errors = {
        ...errors,
        credits: translate('operations.creditsAccessDenied'),
      };
      return;
    }

    stage(
      'credits',
      translate('operations.giveCredits'),
      '/api/v1/operations/currency/credits',
      positive(credits.playerId) && positive(credits.amount) && reasonOk(credits.reason),
      {
        playerId: Number(credits.playerId),
        amount: Number(credits.amount),
        reason: credits.reason.trim(),
      },
      translate('operations.creditsSummary', { amount: credits.amount, name: credits.playerName || translate('operations.player'), id: credits.playerId }),
    );
  }

  function stageActivity() {
    if (!canActivity) {
      errors = {
        ...errors,
        activity: translate('operations.activityAccessDenied'),
      };
      return;
    }

    stage(
      'activity',
      translate('operations.giveActivityPoints'),
      '/api/v1/operations/currency/activity-points',
      positive(activity.playerId) &&
        nonNegative(activity.type) &&
        positive(activity.amount) &&
        reasonOk(activity.reason),
      {
        playerId: Number(activity.playerId),
        type: Number(activity.type),
        amount: Number(activity.amount),
        reason: activity.reason.trim(),
      },
      translate('operations.activitySummary', { amount: activity.amount, type: activity.type, name: activity.playerName || translate('operations.player'), id: activity.playerId }),
    );
  }

  function stageItem() {
    if (!canItem) {
      errors = {
        ...errors,
        item: translate('operations.itemAccessDenied'),
      };
      return;
    }

    stage(
      'item',
      translate('operations.giveFurniture'),
      '/api/v1/operations/items/grant',
      positive(item.playerId) && positive(item.definitionId) && reasonOk(item.reason),
      {
        playerId: Number(item.playerId),
        definitionId: Number(item.definitionId),
        extraData: item.extraData.trim() ? item.extraData.trim() : null,
        reason: item.reason.trim(),
      },
      translate('operations.furnitureSummary', { name: item.defName || translate('operations.furniture'), id: item.definitionId, playerName: item.playerName || translate('operations.player'), playerId: item.playerId }),
    );
  }

  function stageKick() {
    if (!canKick) {
      errors = {
        ...errors,
        kick: translate('operations.kickAccessDenied'),
      };
      return;
    }

    stage(
      'kick',
      translate('operations.kickPlayer'),
      '/api/v1/operations/players/kick',
      positive(kick.playerId) && reasonOk(kick.reason),
      { playerId: Number(kick.playerId), reason: kick.reason.trim() },
      translate('operations.kickSummary', { name: kick.playerName || translate('operations.player'), id: kick.playerId }),
    );
  }

  function cancel() {
    pending = null;
  }

  async function confirm() {
    if (!pending) {
      return;
    }

    const { id, endpoint, body } = pending;
    pending = null;

    busy = { ...busy, [id]: true };
    errors = { ...errors, [id]: '' };
    results = { ...results, [id]: null };

    try {
      results = { ...results, [id]: await apiPost(endpoint, body) };
    } catch (err) {
      errors = {
        ...errors,
        [id]: isPermissionDeniedError(err) ? translate('common.insufficientRightsAction') : err.code || err.message,
      };
    } finally {
      busy = { ...busy, [id]: false };
    }
  }

  async function copy(value) {
    try {
      await navigator.clipboard.writeText(value || '');
    } catch {
      // Clipboard is best-effort; the id is also visible on screen.
    }
  }
</script>

<section class="panel">
  <div class="panel-head"><h2>{$t('operations.title')}</h2></div>
  <p class="muted">
    {$t('operations.description')}
  </p>
</section>

<div class="op-grid">
  <section class="panel op-panel" style="border-left-color: var(--ok);">
    <div class="panel-head"><h2><Coins size={17} strokeWidth={2} /> {$t('operations.giveCredits')}</h2></div>
    {#if !canCredits}
      <AccessDeniedNotice message={$t('operations.creditsAccessDenied')} />
    {:else}
      <div class="op-field">
        <span class="op-label">{$t('common.playerRequired')}</span>
        <div class="op-pick">
          <button
            class="ghost-button"
            type="button"
            on:click={() =>
              pickUser(
                (u) =>
                  (credits = {
                    ...credits,
                    playerId: u.id,
                    playerName: u.name,
                    playerOnline: u.online,
                  }),
              )}
          >
            {$t('common.selectUser')}
          </button>
          {#if credits.playerId}
            <span class="op-chip">
              <span class="op-dot" class:on={credits.playerOnline}></span>
              {credits.playerName} <small>#{credits.playerId}</small>
            </span>
          {:else}
            <span class="muted">{$t('common.noUserSelected')}</span>
          {/if}
        </div>
      </div>
      <div class="op-field">
        <label for="credits-amount">{$t('operations.amount')}</label>
        <input id="credits-amount" type="number" min="1" bind:value={credits.amount} placeholder="100" />
      </div>
      <div class="op-field">
        <label for="credits-reason">{$t('common.reasonRequired')}</label>
        <input id="credits-reason" bind:value={credits.reason} placeholder={$t('common.reasonPlaceholder')} list="reason-history" />
      </div>
      <div class="op-actions">
        <button type="button" on:click={stageCredits} disabled={busy.credits}>{$t('common.run')}</button>
      </div>
      {#if errors.credits}<p class="empty-state danger">{errors.credits}</p>{/if}
      {#if results.credits}
        <OpResult result={results.credits} onCopy={copy} copyLabel={$t('common.copy')} />
      {/if}
    {/if}
  </section>

  <section class="panel op-panel" style="border-left-color: var(--ok);">
    <div class="panel-head"><h2><Zap size={17} strokeWidth={2} /> {$t('operations.giveActivityPoints')}</h2></div>
    {#if !canActivity}
      <AccessDeniedNotice message={$t('operations.activityAccessDenied')} />
    {:else}
      <div class="op-field">
        <span class="op-label">{$t('common.playerRequired')}</span>
        <div class="op-pick">
          <button
            class="ghost-button"
            type="button"
            on:click={() =>
              pickUser(
                (u) =>
                  (activity = {
                    ...activity,
                    playerId: u.id,
                    playerName: u.name,
                    playerOnline: u.online,
                  }),
              )}
          >
            {$t('common.selectUser')}
          </button>
          {#if activity.playerId}
            <span class="op-chip">
              <span class="op-dot" class:on={activity.playerOnline}></span>
              {activity.playerName} <small>#{activity.playerId}</small>
            </span>
          {:else}
            <span class="muted">{$t('common.noUserSelected')}</span>
          {/if}
        </div>
      </div>
      <div class="op-field">
        <label for="activity-type">{$t('operations.activityPointType')}</label>
        <input id="activity-type" type="number" min="0" bind:value={activity.type} placeholder="0" />
      </div>
      <div class="op-field">
        <label for="activity-amount">{$t('operations.amount')}</label>
        <input id="activity-amount" type="number" min="1" bind:value={activity.amount} placeholder="50" />
      </div>
      <div class="op-field">
        <label for="activity-reason">{$t('common.reasonRequired')}</label>
        <input id="activity-reason" bind:value={activity.reason} placeholder={$t('common.reasonPlaceholder')} list="reason-history" />
      </div>
      <div class="op-actions">
        <button type="button" on:click={stageActivity} disabled={busy.activity}>{$t('common.run')}</button>
      </div>
      {#if errors.activity}<p class="empty-state danger">{errors.activity}</p>{/if}
      {#if results.activity}
        <OpResult result={results.activity} onCopy={copy} copyLabel={$t('common.copy')} />
      {/if}
    {/if}
  </section>

  <section class="panel op-panel" style="border-left-color: var(--ok);">
    <div class="panel-head"><h2><Package size={17} strokeWidth={2} /> {$t('operations.giveFurniture')}</h2></div>
    {#if !canItem}
      <AccessDeniedNotice message={$t('operations.itemAccessDenied')} />
    {:else}
      <div class="op-field">
        <span class="op-label">{$t('common.playerRequired')}</span>
        <div class="op-pick">
          <button
            class="ghost-button"
            type="button"
            on:click={() =>
              pickUser(
                (u) =>
                  (item = { ...item, playerId: u.id, playerName: u.name, playerOnline: u.online }),
              )}
          >
            {$t('common.selectUser')}
          </button>
          {#if item.playerId}
            <span class="op-chip">
              <span class="op-dot" class:on={item.playerOnline}></span>
              {item.playerName} <small>#{item.playerId}</small>
            </span>
          {:else}
            <span class="muted">{$t('common.noUserSelected')}</span>
          {/if}
        </div>
      </div>
      <div class="op-field">
        <span class="op-label">{$t('common.selectFurniture')} *</span>
        <div class="op-pick">
          <button
            class="ghost-button"
            type="button"
            on:click={() =>
              pickFurniture(
                (f) =>
                  (item = {
                    ...item,
                    definitionId: f.id,
                    defName: f.name,
                    defSprite: f.spriteId,
                    defIcon: f.iconUrl,
                  }),
              )}
          >
            {$t('common.selectFurniture')}
          </button>
          {#if item.definitionId}
            <span class="op-chip">
              {#if item.defIcon}
                <img class="op-sprite" src={item.defIcon} alt="" />
              {:else}
                <span class="op-sprite">{item.defSprite}</span>
              {/if}
              {item.defName} <small>#{item.definitionId}</small>
            </span>
          {:else}
            <span class="muted">{$t('common.noFurnitureSelected')}</span>
          {/if}
        </div>
      </div>
      <div class="op-field">
        <label for="item-extra">{$t('operations.extraDataOptional')}</label>
        <input id="item-extra" bind:value={item.extraData} placeholder={$t('operations.extraDataPlaceholder')} />
      </div>
      <div class="op-field">
        <label for="item-reason">{$t('common.reasonRequired')}</label>
        <input id="item-reason" bind:value={item.reason} placeholder={$t('common.reasonPlaceholder')} list="reason-history" />
      </div>
      <div class="op-actions">
        <button type="button" on:click={stageItem} disabled={busy.item}>{$t('common.run')}</button>
      </div>
      {#if errors.item}<p class="empty-state danger">{errors.item}</p>{/if}
      {#if results.item}
        <OpResult result={results.item} onCopy={copy} copyLabel={$t('common.copy')} />
      {/if}
    {/if}
  </section>

  <section class="panel op-panel" style="border-left-color: var(--danger);">
    <div class="panel-head"><h2><UserX size={17} strokeWidth={2} /> {$t('operations.kickPlayer')}</h2></div>
    {#if !canKick}
      <AccessDeniedNotice message={$t('operations.kickAccessDenied')} />
    {:else}
      <div class="op-field">
        <span class="op-label">{$t('common.playerRequired')}</span>
        <div class="op-pick">
          <button
            class="ghost-button"
            type="button"
            on:click={() =>
              pickUser(
                (u) =>
                  (kick = { ...kick, playerId: u.id, playerName: u.name, playerOnline: u.online }),
              )}
          >
            {$t('common.selectUser')}
          </button>
          {#if kick.playerId}
            <span class="op-chip">
              <span class="op-dot" class:on={kick.playerOnline}></span>
              {kick.playerName} <small>#{kick.playerId}</small>
            </span>
          {:else}
            <span class="muted">{$t('common.noUserSelected')}</span>
          {/if}
        </div>
      </div>
      <div class="op-field">
        <label for="kick-reason">{$t('common.reasonRequired')}</label>
        <input id="kick-reason" bind:value={kick.reason} placeholder={$t('common.reasonPlaceholder')} list="reason-history" />
      </div>
      <div class="op-actions">
        <button type="button" on:click={stageKick} disabled={busy.kick}>{$t('common.run')}</button>
      </div>
      {#if errors.kick}<p class="empty-state danger">{errors.kick}</p>{/if}
      {#if results.kick}
        <OpResult result={results.kick} onCopy={copy} copyLabel={$t('common.copy')} />
      {/if}
    {/if}
  </section>
</div>

{#if picker}
  <PickerModal
    kind={picker.kind}
    title={picker.title}
    onSelect={picker.onSelect}
    onClose={() => (picker = null)}
    canSelect={picker.kind === 'user' ? canCredits || canActivity || canItem || canKick : canItem}
  />
{/if}

{#if pending}
  <div class="modal-layer">
    <button class="modal-backdrop" type="button" aria-label="Cancel" on:click={cancel}></button>
    <section class="modal-panel" role="dialog" aria-modal="true" style="width: min(460px, 100%)">
      <header class="modal-header">
        <div>
          <p class="eyebrow">{$t('operations.confirmEyebrow')}</p>
          <h2>{pending.title}</h2>
        </div>
      </header>
      <p>{pending.summary}</p>
      <p class="muted">{$t('vouchers.reasonLabel', { reason: pending.reason })}</p>
      <div class="op-actions">
        <button type="button" on:click={confirm}>{$t('common.confirm')}</button>
        <button class="ghost-button" type="button" on:click={cancel}>{$t('common.cancel')}</button>
      </div>
    </section>
  </div>
{/if}
