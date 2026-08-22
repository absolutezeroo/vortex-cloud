<script>
  import ConfirmStagedModal from '../components/ConfirmStagedModal.svelte';
  import PageHeader from '../components/PageHeader.svelte';
  import OpResult from '../components/OpResult.svelte';
  import { Coins, Zap, Gem, Package, UserX } from '@lucide/svelte';
  import { hasDashboardCapability } from '../lib/permissions.js';
  import { createWriteOps } from '../lib/writeOps.js';
  import { OPERATION_CAPABILITIES } from '../lib/dashboardPermissions.js';
  import { reasonOk, positive, nonNegative } from '../lib/validation.js';
  import AccessDeniedNotice from '../components/AccessDeniedNotice.svelte';
  import PickerModal from '../components/PickerModal.svelte';
  import { identity, openPlayer, openItem } from '../lib/session.js';
  import { t, translate } from '../lib/i18n.js';

  // One state bag per action.
  let credits = $state({ playerId: '', playerName: '', playerOnline: false, amount: '', reason: '' });
  let activity = $state({
    playerId: '',
    playerName: '',
    playerOnline: false,
    type: '0',
    amount: '',
    reason: '',
  });
  // Silver and emeralds share one form: they are the same grant with a different currency, and two
  // near-identical cards would only make the operator read both to find the right one.
  let collectibles = $state({
    playerId: '',
    playerName: '',
    playerOnline: false,
    currency: 'silver',
    amount: '',
    reason: '',
  });
  let item = $state({
    playerId: '',
    playerName: '',
    playerOnline: false,
    definitionId: '',
    defName: '',
    defSprite: '',
    defIcon: '',
    extraData: '',
    reason: '',
  });
  let kick = $state({ playerId: '', playerName: '', playerOnline: false, reason: '' });

  // Per-action UI state, keyed by action id.
  // Every write is staged here and confirmed in the dialog below before it is posted. createWriteOps
  // owns that cycle -- posting, remembering the audited reason, and tracking each form's busy state,
  // error and result under its own key -- so the page only describes what each button writes.
  const ops = createWriteOps();

  // Active picker modal (user / furniture).
  let picker = $state(null);

  const capabilityByAction = {
    credits: OPERATION_CAPABILITIES.credits,
    activity: OPERATION_CAPABILITIES.activity,
    collectibles: OPERATION_CAPABILITIES.collectibles,
    item: OPERATION_CAPABILITIES.item,
    kick: OPERATION_CAPABILITIES.kick,
  };

  let canCredits = $derived(hasDashboardCapability($identity, capabilityByAction.credits));
  let canActivity = $derived(hasDashboardCapability($identity, capabilityByAction.activity));
  let canCollectibles = $derived(hasDashboardCapability($identity, capabilityByAction.collectibles));
  let canItem = $derived(hasDashboardCapability($identity, capabilityByAction.item));
  let canKick = $derived(hasDashboardCapability($identity, capabilityByAction.kick));

  function pickUser(apply) {
    picker = { kind: 'user', title: translate('operations.selectPlayerTitle'), onSelect: apply };
  }

  function pickFurniture(apply) {
    picker = { kind: 'furniture', title: translate('operations.selectFurnitureTitle'), onSelect: apply };
  }

  const stage = (id, title, endpoint, valid, body, summary) =>
    ops.ask(endpoint, body, title, summary, {
      key: id,
      valid,
      invalidMessage: translate('operations.fillFields'),
      reason: body.reason,
    });

  function stageCredits() {
    if (!canCredits) {
      ops.fail('credits', translate('operations.creditsAccessDenied'));
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
      ops.fail('activity', translate('operations.activityAccessDenied'));
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

  function stageCollectibles() {
    if (!canCollectibles) {
      ops.fail('collectibles', translate('operations.collectiblesAccessDenied'));
      return;
    }

    const currencyLabel = translate(
      collectibles.currency === 'emeralds' ? 'operations.currencyEmeralds' : 'operations.currencySilver',
    );

    stage(
      'collectibles',
      translate('operations.giveCollectiblesCurrency'),
      '/api/v1/operations/currency/collectibles',
      positive(collectibles.playerId) && positive(collectibles.amount) && reasonOk(collectibles.reason),
      {
        playerId: Number(collectibles.playerId),
        currency: collectibles.currency,
        amount: Number(collectibles.amount),
        reason: collectibles.reason.trim(),
      },
      translate('operations.collectiblesSummary', {
        amount: collectibles.amount,
        currency: currencyLabel,
        name: collectibles.playerName || translate('operations.player'),
        id: collectibles.playerId,
      }),
    );
  }

  function stageItem() {
    if (!canItem) {
      ops.fail('item', translate('operations.itemAccessDenied'));
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
      ops.fail('kick', translate('operations.kickAccessDenied'));
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

  async function copy(value) {
    try {
      await navigator.clipboard.writeText(value || '');
    } catch {
      // Clipboard is best-effort; the id is also visible on screen.
    }
  }
</script>

<section class="panel">
  <PageHeader title={$t('operations.title')} description={$t('operations.description')} />
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
            onclick={() =>
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
        <input autocomplete="off" spellcheck="false" id="credits-amount" type="number" min="1" bind:value={credits.amount} placeholder="100" />
      </div>
      <div class="op-field">
        <label for="credits-reason">{$t('common.reasonRequired')}</label>
        <input autocomplete="off" spellcheck="false" id="credits-reason" bind:value={credits.reason} placeholder={$t('common.reasonPlaceholder')} list="reason-history" />
      </div>
      <div class="op-actions">
        <button type="button" onclick={stageCredits} disabled={$ops.busyKeys.credits}>{$t('common.run')}</button>
      </div>
      {#if $ops.errors.credits}<p class="empty-state danger" role="alert">{$ops.errors.credits}</p>{/if}
      {#if $ops.results.credits}
        <OpResult result={$ops.results.credits} onCopy={copy} copyLabel={$t('common.copy')} />
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
            onclick={() =>
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
        <input autocomplete="off" spellcheck="false" id="activity-type" type="number" min="0" bind:value={activity.type} placeholder="0" />
      </div>
      <div class="op-field">
        <label for="activity-amount">{$t('operations.amount')}</label>
        <input autocomplete="off" spellcheck="false" id="activity-amount" type="number" min="1" bind:value={activity.amount} placeholder="50" />
      </div>
      <div class="op-field">
        <label for="activity-reason">{$t('common.reasonRequired')}</label>
        <input autocomplete="off" spellcheck="false" id="activity-reason" bind:value={activity.reason} placeholder={$t('common.reasonPlaceholder')} list="reason-history" />
      </div>
      <div class="op-actions">
        <button type="button" onclick={stageActivity} disabled={$ops.busyKeys.activity}>{$t('common.run')}</button>
      </div>
      {#if $ops.errors.activity}<p class="empty-state danger" role="alert">{$ops.errors.activity}</p>{/if}
      {#if $ops.results.activity}
        <OpResult result={$ops.results.activity} onCopy={copy} copyLabel={$t('common.copy')} />
      {/if}
    {/if}
  </section>

  <section class="panel op-panel" style="border-left-color: var(--ok);">
    <div class="panel-head"><h2><Gem size={17} strokeWidth={2} /> {$t('operations.giveCollectiblesCurrency')}</h2></div>
    {#if !canCollectibles}
      <AccessDeniedNotice message={$t('operations.collectiblesAccessDenied')} />
    {:else}
      <div class="op-field">
        <span class="op-label">{$t('common.playerRequired')}</span>
        <div class="op-pick">
          <button
            class="ghost-button"
            type="button"
            onclick={() =>
              pickUser(
                (u) =>
                  (collectibles = {
                    ...collectibles,
                    playerId: u.id,
                    playerName: u.name,
                    playerOnline: u.online,
                  }),
              )}
          >
            {$t('common.selectUser')}
          </button>
          {#if collectibles.playerId}
            <span class="op-chip">
              <span class="op-dot" class:on={collectibles.playerOnline}></span>
              {collectibles.playerName} <small>#{collectibles.playerId}</small>
            </span>
          {:else}
            <span class="muted">{$t('common.noUserSelected')}</span>
          {/if}
        </div>
      </div>
      <div class="op-field">
        <label for="collectibles-currency">{$t('operations.collectiblesCurrency')}</label>
        <select id="collectibles-currency" bind:value={collectibles.currency}>
          <option value="silver">{$t('operations.currencySilver')}</option>
          <option value="emeralds">{$t('operations.currencyEmeralds')}</option>
        </select>
      </div>
      <div class="op-field">
        <label for="collectibles-amount">{$t('operations.amount')}</label>
        <input autocomplete="off" spellcheck="false" id="collectibles-amount" type="number" min="1" bind:value={collectibles.amount} placeholder="100" />
      </div>
      <div class="op-field">
        <label for="collectibles-reason">{$t('common.reasonRequired')}</label>
        <input autocomplete="off" spellcheck="false" id="collectibles-reason" bind:value={collectibles.reason} placeholder={$t('common.reasonPlaceholder')} list="reason-history" />
      </div>
      <div class="op-actions">
        <button type="button" onclick={stageCollectibles} disabled={$ops.busyKeys.collectibles}>{$t('common.run')}</button>
      </div>
      {#if $ops.errors.collectibles}<p class="empty-state danger" role="alert">{$ops.errors.collectibles}</p>{/if}
      {#if $ops.results.collectibles}
        <OpResult result={$ops.results.collectibles} onCopy={copy} copyLabel={$t('common.copy')} />
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
            onclick={() =>
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
            onclick={() =>
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
                <img class="op-sprite" src={item.defIcon} alt="" loading="lazy" />
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
        <input autocomplete="off" spellcheck="false" id="item-extra" bind:value={item.extraData} placeholder={$t('operations.extraDataPlaceholder')} />
      </div>
      <div class="op-field">
        <label for="item-reason">{$t('common.reasonRequired')}</label>
        <input autocomplete="off" spellcheck="false" id="item-reason" bind:value={item.reason} placeholder={$t('common.reasonPlaceholder')} list="reason-history" />
      </div>
      <div class="op-actions">
        <button type="button" onclick={stageItem} disabled={$ops.busyKeys.item}>{$t('common.run')}</button>
      </div>
      {#if $ops.errors.item}<p class="empty-state danger" role="alert">{$ops.errors.item}</p>{/if}
      {#if $ops.results.item}
        <OpResult result={$ops.results.item} onCopy={copy} copyLabel={$t('common.copy')} />
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
            onclick={() =>
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
        <input autocomplete="off" spellcheck="false" id="kick-reason" bind:value={kick.reason} placeholder={$t('common.reasonPlaceholder')} list="reason-history" />
      </div>
      <div class="op-actions">
        <button type="button" onclick={stageKick} disabled={$ops.busyKeys.kick}>{$t('common.run')}</button>
      </div>
      {#if $ops.errors.kick}<p class="empty-state danger" role="alert">{$ops.errors.kick}</p>{/if}
      {#if $ops.results.kick}
        <OpResult result={$ops.results.kick} onCopy={copy} copyLabel={$t('common.copy')} />
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

<ConfirmStagedModal {ops} eyebrow={$t('operations.confirmEyebrow')} />
