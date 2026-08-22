<script>
  // The five player operations, bound to a player the page already knows. They used to live on
  // /operations, where each of the five carried its own "Select user" button: giving someone credits
  // and then kicking them meant picking the same player twice, from two cards, with nothing on
  // screen tying the two actions to one person. Here the player is the context, not a field.
  //
  //   <PlayerOperationsPanel playerId={player.id} playerName={player.name} online={player.online} />
  import OpResult from './OpResult.svelte';
  import AccessDeniedNotice from './AccessDeniedNotice.svelte';
  import ConfirmStagedModal from './ConfirmStagedModal.svelte';
  import PickerModal from './PickerModal.svelte';
  import { Coins, Zap, Gem, Package, UserX } from '@lucide/svelte';
  import { hasDashboardCapability } from '../lib/permissions.js';
  import { createWriteOps } from '../lib/writeOps.js';
  import { OPERATION_CAPABILITIES } from '../lib/dashboardPermissions.js';
  import { reasonOk, positive, nonNegative } from '../lib/validation.js';
  import { identity } from '../lib/session.js';
  import { t, translate } from '../lib/i18n.js';

  /**
   * @typedef {Object} Props
   * @property {number|string} playerId
   * @property {string} [playerName]
   * @property {boolean} [online]
   * @property {() => void} [onDone] - called after a write lands, so the page can refresh its timeline
   */

  /** @type {Props} */
  let { playerId, playerName = '', online = false, onDone } = $props();

  const ops = createWriteOps(() => onDone?.());

  // One bag per action; the player is not in them -- it comes from the page.
  let credits = $state({ amount: '', reason: '' });
  let activity = $state({ type: '0', amount: '', reason: '' });
  // Silver and emeralds share one form: same grant, different currency.
  let collectibles = $state({ currency: 'silver', amount: '', reason: '' });
  let item = $state({ definitionId: '', defName: '', defSprite: '', defIcon: '', extraData: '', reason: '' });
  let kick = $state({ reason: '' });

  let picker = $state(null);

  let canCredits = $derived(hasDashboardCapability($identity, OPERATION_CAPABILITIES.credits));
  let canActivity = $derived(hasDashboardCapability($identity, OPERATION_CAPABILITIES.activity));
  let canCollectibles = $derived(hasDashboardCapability($identity, OPERATION_CAPABILITIES.collectibles));
  let canItem = $derived(hasDashboardCapability($identity, OPERATION_CAPABILITIES.item));
  let canKick = $derived(hasDashboardCapability($identity, OPERATION_CAPABILITIES.kick));

  let who = $derived(playerName || translate('operations.player'));

  const stage = (id, title, endpoint, valid, body, summary) =>
    ops.ask(endpoint, { playerId: Number(playerId), ...body }, title, summary, {
      key: id,
      valid: valid && positive(playerId),
      invalidMessage: translate('operations.fillFields'),
      reason: body.reason,
    });

  function stageCredits() {
    if (!canCredits) return void ops.fail('credits', translate('operations.creditsAccessDenied'));

    stage(
      'credits',
      translate('operations.giveCredits'),
      '/api/v1/operations/currency/credits',
      positive(credits.amount) && reasonOk(credits.reason),
      { amount: Number(credits.amount), reason: credits.reason.trim() },
      translate('operations.creditsSummary', { amount: credits.amount, name: who, id: playerId }),
    );
  }

  function stageActivity() {
    if (!canActivity) return void ops.fail('activity', translate('operations.activityAccessDenied'));

    stage(
      'activity',
      translate('operations.giveActivityPoints'),
      '/api/v1/operations/currency/activity-points',
      nonNegative(activity.type) && positive(activity.amount) && reasonOk(activity.reason),
      { type: Number(activity.type), amount: Number(activity.amount), reason: activity.reason.trim() },
      translate('operations.activitySummary', {
        amount: activity.amount,
        type: activity.type,
        name: who,
        id: playerId,
      }),
    );
  }

  function stageCollectibles() {
    if (!canCollectibles) {
      return void ops.fail('collectibles', translate('operations.collectiblesAccessDenied'));
    }

    stage(
      'collectibles',
      translate('operations.giveCollectiblesCurrency'),
      '/api/v1/operations/currency/collectibles',
      positive(collectibles.amount) && reasonOk(collectibles.reason),
      {
        currency: collectibles.currency,
        amount: Number(collectibles.amount),
        reason: collectibles.reason.trim(),
      },
      translate('operations.collectiblesSummary', {
        amount: collectibles.amount,
        currency: translate(
          collectibles.currency === 'emeralds'
            ? 'operations.currencyEmeralds'
            : 'operations.currencySilver',
        ),
        name: who,
        id: playerId,
      }),
    );
  }

  function stageItem() {
    if (!canItem) return void ops.fail('item', translate('operations.itemAccessDenied'));

    stage(
      'item',
      translate('operations.giveFurniture'),
      '/api/v1/operations/items/grant',
      positive(item.definitionId) && reasonOk(item.reason),
      {
        definitionId: Number(item.definitionId),
        extraData: item.extraData.trim() ? item.extraData.trim() : null,
        reason: item.reason.trim(),
      },
      translate('operations.furnitureSummary', {
        name: item.defName || translate('operations.furniture'),
        id: item.definitionId,
        playerName: who,
        playerId,
      }),
    );
  }

  function stageKick() {
    if (!canKick) return void ops.fail('kick', translate('operations.kickAccessDenied'));

    stage(
      'kick',
      translate('operations.kickPlayer'),
      '/api/v1/operations/players/kick',
      reasonOk(kick.reason),
      { reason: kick.reason.trim() },
      translate('operations.kickSummary', { name: who, id: playerId }),
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

<div class="op-grid">
  <section class="panel op-panel" style="border-left-color: var(--ok);">
    <div class="panel-head">
      <h3><Coins size={17} strokeWidth={2} aria-hidden="true" /> {$t('operations.giveCredits')}</h3>
    </div>
    {#if !canCredits}
      <AccessDeniedNotice message={$t('operations.creditsAccessDenied')} />
    {:else}
      <div class="op-field">
        <label for="credits-amount">{$t('operations.amount')}</label>
        <input
          id="credits-amount"
          type="number"
          min="1"
          autocomplete="off"
          spellcheck="false"
          bind:value={credits.amount}
          placeholder="100"
        />
      </div>
      <div class="op-field">
        <label for="credits-reason">{$t('common.reasonRequired')}</label>
        <input
          id="credits-reason"
          autocomplete="off"
          spellcheck="false"
          bind:value={credits.reason}
          placeholder={$t('common.reasonPlaceholder')}
          list="reason-history"
        />
      </div>
      <div class="op-actions">
        <button type="button" onclick={stageCredits} disabled={$ops.busyKeys.credits}>
          {$t('common.run')}
        </button>
      </div>
      {#if $ops.errors.credits}
        <p class="empty-state danger" role="alert">{$ops.errors.credits}</p>
      {/if}
      {#if $ops.results.credits}
        <OpResult result={$ops.results.credits} onCopy={copy} copyLabel={$t('common.copy')} />
      {/if}
    {/if}
  </section>

  <section class="panel op-panel" style="border-left-color: var(--ok);">
    <div class="panel-head">
      <h3><Zap size={17} strokeWidth={2} aria-hidden="true" /> {$t('operations.giveActivityPoints')}</h3>
    </div>
    {#if !canActivity}
      <AccessDeniedNotice message={$t('operations.activityAccessDenied')} />
    {:else}
      <div class="op-field">
        <label for="activity-type">{$t('operations.activityPointType')}</label>
        <input
          id="activity-type"
          type="number"
          min="0"
          autocomplete="off"
          spellcheck="false"
          bind:value={activity.type}
          placeholder="0"
        />
      </div>
      <div class="op-field">
        <label for="activity-amount">{$t('operations.amount')}</label>
        <input
          id="activity-amount"
          type="number"
          min="1"
          autocomplete="off"
          spellcheck="false"
          bind:value={activity.amount}
          placeholder="50"
        />
      </div>
      <div class="op-field">
        <label for="activity-reason">{$t('common.reasonRequired')}</label>
        <input
          id="activity-reason"
          autocomplete="off"
          spellcheck="false"
          bind:value={activity.reason}
          placeholder={$t('common.reasonPlaceholder')}
          list="reason-history"
        />
      </div>
      <div class="op-actions">
        <button type="button" onclick={stageActivity} disabled={$ops.busyKeys.activity}>
          {$t('common.run')}
        </button>
      </div>
      {#if $ops.errors.activity}
        <p class="empty-state danger" role="alert">{$ops.errors.activity}</p>
      {/if}
      {#if $ops.results.activity}
        <OpResult result={$ops.results.activity} onCopy={copy} copyLabel={$t('common.copy')} />
      {/if}
    {/if}
  </section>

  <section class="panel op-panel" style="border-left-color: var(--ok);">
    <div class="panel-head">
      <h3>
        <Gem size={17} strokeWidth={2} aria-hidden="true" />
        {$t('operations.giveCollectiblesCurrency')}
      </h3>
    </div>
    {#if !canCollectibles}
      <AccessDeniedNotice message={$t('operations.collectiblesAccessDenied')} />
    {:else}
      <div class="op-field">
        <label for="collectibles-currency">{$t('operations.collectiblesCurrency')}</label>
        <select id="collectibles-currency" bind:value={collectibles.currency}>
          <option value="silver">{$t('operations.currencySilver')}</option>
          <option value="emeralds">{$t('operations.currencyEmeralds')}</option>
        </select>
      </div>
      <div class="op-field">
        <label for="collectibles-amount">{$t('operations.amount')}</label>
        <input
          id="collectibles-amount"
          type="number"
          min="1"
          autocomplete="off"
          spellcheck="false"
          bind:value={collectibles.amount}
          placeholder="100"
        />
      </div>
      <div class="op-field">
        <label for="collectibles-reason">{$t('common.reasonRequired')}</label>
        <input
          id="collectibles-reason"
          autocomplete="off"
          spellcheck="false"
          bind:value={collectibles.reason}
          placeholder={$t('common.reasonPlaceholder')}
          list="reason-history"
        />
      </div>
      <div class="op-actions">
        <button type="button" onclick={stageCollectibles} disabled={$ops.busyKeys.collectibles}>
          {$t('common.run')}
        </button>
      </div>
      {#if $ops.errors.collectibles}
        <p class="empty-state danger" role="alert">{$ops.errors.collectibles}</p>
      {/if}
      {#if $ops.results.collectibles}
        <OpResult result={$ops.results.collectibles} onCopy={copy} copyLabel={$t('common.copy')} />
      {/if}
    {/if}
  </section>

  <section class="panel op-panel" style="border-left-color: var(--ok);">
    <div class="panel-head">
      <h3><Package size={17} strokeWidth={2} aria-hidden="true" /> {$t('operations.giveFurniture')}</h3>
    </div>
    {#if !canItem}
      <AccessDeniedNotice message={$t('operations.itemAccessDenied')} />
    {:else}
      <div class="op-field">
        <span class="op-label">{$t('common.selectFurniture')} *</span>
        <div class="op-pick">
          <button
            class="ghost-button"
            type="button"
            onclick={() =>
              (picker = {
                kind: 'furniture',
                title: translate('operations.selectFurnitureTitle'),
                onSelect: (f) =>
                  (item = {
                    ...item,
                    definitionId: f.id,
                    defName: f.name,
                    defSprite: f.spriteId,
                    defIcon: f.iconUrl,
                  }),
              })}
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
        <input
          id="item-extra"
          autocomplete="off"
          spellcheck="false"
          bind:value={item.extraData}
          placeholder={$t('operations.extraDataPlaceholder')}
        />
      </div>
      <div class="op-field">
        <label for="item-reason">{$t('common.reasonRequired')}</label>
        <input
          id="item-reason"
          autocomplete="off"
          spellcheck="false"
          bind:value={item.reason}
          placeholder={$t('common.reasonPlaceholder')}
          list="reason-history"
        />
      </div>
      <div class="op-actions">
        <button type="button" onclick={stageItem} disabled={$ops.busyKeys.item}>
          {$t('common.run')}
        </button>
      </div>
      {#if $ops.errors.item}
        <p class="empty-state danger" role="alert">{$ops.errors.item}</p>
      {/if}
      {#if $ops.results.item}
        <OpResult result={$ops.results.item} onCopy={copy} copyLabel={$t('common.copy')} />
      {/if}
    {/if}
  </section>

  <section class="panel op-panel" style="border-left-color: var(--danger);">
    <div class="panel-head">
      <h3><UserX size={17} strokeWidth={2} aria-hidden="true" /> {$t('operations.kickPlayer')}</h3>
      {#if !online}<small class="muted">{$t('playerOps.offlineKickHint')}</small>{/if}
    </div>
    {#if !canKick}
      <AccessDeniedNotice message={$t('operations.kickAccessDenied')} />
    {:else}
      <div class="op-field">
        <label for="kick-reason">{$t('common.reasonRequired')}</label>
        <input
          id="kick-reason"
          autocomplete="off"
          spellcheck="false"
          bind:value={kick.reason}
          placeholder={$t('common.reasonPlaceholder')}
          list="reason-history"
        />
      </div>
      <div class="op-actions">
        <button type="button" onclick={stageKick} disabled={$ops.busyKeys.kick || !online}>
          {$t('common.run')}
        </button>
      </div>
      {#if $ops.errors.kick}
        <p class="empty-state danger" role="alert">{$ops.errors.kick}</p>
      {/if}
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
    canSelect={canItem}
  />
{/if}

<ConfirmStagedModal {ops} eyebrow={$t('operations.confirmEyebrow')} />
