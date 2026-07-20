<script>
  import OpResult from '../components/OpResult.svelte';
  import { Ban, ShieldCheck, VolumeX, Lock, LockOpen } from '@lucide/svelte';
  import { isPermissionDeniedError, hasDashboardCapability } from '../lib/permissions.js';
  import { apiPost } from '../lib/api.js';
  import { MODERATION_OPERATION_CAPABILITIES } from '../lib/dashboardPermissions.js';
  import { reasonOk, positive } from '../lib/validation.js';
  import AccessDeniedNotice from '../components/AccessDeniedNotice.svelte';
  import PickerModal from '../components/PickerModal.svelte';
  import { identity } from '../lib/session.js';
  import { t, translate } from '../lib/i18n.js';

  // One state bag per action, mirroring OperationsPage's pattern.
  let ban = {
    playerId: '',
    playerName: '',
    playerOnline: false,
    permanent: false,
    durationSeconds: '',
    reason: '',
  };
  let unban = { playerId: '', playerName: '', playerOnline: false, reason: '' };
  let mute = { playerId: '', playerName: '', playerOnline: false, durationSeconds: '', reason: '' };
  let tradingLock = {
    playerId: '',
    playerName: '',
    playerOnline: false,
    permanent: false,
    durationSeconds: '',
    reason: '',
  };
  let tradingUnlock = { playerId: '', playerName: '', playerOnline: false, reason: '' };

  let results = {};
  let errors = {};
  let busy = {};
  let pending = null;
  let picker = null;

  const capabilityByAction = {
    ban: MODERATION_OPERATION_CAPABILITIES.ban,
    unban: MODERATION_OPERATION_CAPABILITIES.unban,
    mute: MODERATION_OPERATION_CAPABILITIES.mute,
    tradingLock: MODERATION_OPERATION_CAPABILITIES.tradingLock,
    tradingUnlock: MODERATION_OPERATION_CAPABILITIES.tradingUnlock,
  };

  $: canBan = hasDashboardCapability($identity, capabilityByAction.ban);
  $: canUnban = hasDashboardCapability($identity, capabilityByAction.unban);
  $: canMute = hasDashboardCapability($identity, capabilityByAction.mute);
  $: canTradingLock = hasDashboardCapability($identity, capabilityByAction.tradingLock);
  $: canTradingUnlock = hasDashboardCapability($identity, capabilityByAction.tradingUnlock);

  function pickUser(apply) {
    picker = { kind: 'user', title: translate('operations.selectPlayerTitle'), onSelect: apply };
  }

  function stage(id, title, endpoint, valid, body, summary) {
    errors = { ...errors, [id]: '' };

    if (!valid) {
      errors = {
        ...errors,
        [id]: translate('moderationActions.fillFields'),
      };
      return;
    }

    pending = { id, title, endpoint, body, summary, reason: body.reason };
  }

  function stageBan() {
    if (!canBan) {
      errors = { ...errors, ban: translate('moderationActions.banAccessDenied') };
      return;
    }

    const validDuration = ban.permanent || positive(ban.durationSeconds);

    stage(
      'ban',
      translate('moderationActions.banAccount'),
      '/api/v1/operations/players/ban',
      positive(ban.playerId) && validDuration && reasonOk(ban.reason),
      {
        playerId: Number(ban.playerId),
        permanent: ban.permanent,
        durationSeconds: ban.permanent ? null : Number(ban.durationSeconds),
        reason: ban.reason.trim(),
      },
      translate('moderationActions.banSummary', {
        action: ban.permanent ? translate('moderationActions.permanentlyBan') : translate('moderationActions.banFor', { seconds: ban.durationSeconds }),
        name: ban.playerName || translate('moderationActions.player'),
        id: ban.playerId,
      }),
    );
  }

  function stageUnban() {
    if (!canUnban) {
      errors = { ...errors, unban: translate('moderationActions.unbanAccessDenied') };
      return;
    }

    stage(
      'unban',
      translate('moderationActions.liftAccountBan'),
      '/api/v1/operations/players/unban',
      positive(unban.playerId) && reasonOk(unban.reason),
      { playerId: Number(unban.playerId), reason: unban.reason.trim() },
      translate('moderationActions.liftBanSummary', { name: unban.playerName || translate('moderationActions.player'), id: unban.playerId }),
    );
  }

  function stageMute() {
    if (!canMute) {
      errors = { ...errors, mute: translate('moderationActions.muteAccessDenied') };
      return;
    }

    stage(
      'mute',
      translate('moderationActions.mutePlayer'),
      '/api/v1/operations/players/mute',
      positive(mute.playerId) && positive(mute.durationSeconds) && reasonOk(mute.reason),
      {
        playerId: Number(mute.playerId),
        durationSeconds: Number(mute.durationSeconds),
        reason: mute.reason.trim(),
      },
      translate('moderationActions.muteSummary', { name: mute.playerName || translate('moderationActions.player'), id: mute.playerId, seconds: mute.durationSeconds }),
    );
  }

  function stageTradingLock() {
    if (!canTradingLock) {
      errors = { ...errors, tradingLock: translate('moderationActions.lockAccessDenied') };
      return;
    }

    const validDuration = tradingLock.permanent || positive(tradingLock.durationSeconds);

    stage(
      'tradingLock',
      translate('moderationActions.lockTrading'),
      '/api/v1/operations/players/trading-lock',
      positive(tradingLock.playerId) && validDuration && reasonOk(tradingLock.reason),
      {
        playerId: Number(tradingLock.playerId),
        permanent: tradingLock.permanent,
        durationSeconds: tradingLock.permanent ? null : Number(tradingLock.durationSeconds),
        reason: tradingLock.reason.trim(),
      },
      translate('moderationActions.lockTradingSummary', {
        action: tradingLock.permanent ? translate('moderationActions.permanentlyLock') : translate('moderationActions.lockFor', { seconds: tradingLock.durationSeconds }),
        name: tradingLock.playerName || translate('moderationActions.player'),
        id: tradingLock.playerId,
      }),
    );
  }

  function stageTradingUnlock() {
    if (!canTradingUnlock) {
      errors = { ...errors, tradingUnlock: translate('moderationActions.unlockAccessDenied') };
      return;
    }

    stage(
      'tradingUnlock',
      translate('moderationActions.liftTradingLock'),
      '/api/v1/operations/players/trading-unlock',
      positive(tradingUnlock.playerId) && reasonOk(tradingUnlock.reason),
      { playerId: Number(tradingUnlock.playerId), reason: tradingUnlock.reason.trim() },
      translate('moderationActions.liftLockSummary', { name: tradingUnlock.playerName || translate('moderationActions.player'), id: tradingUnlock.playerId }),
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
  <div class="panel-head"><h2>{$t('moderationActions.title')}</h2></div>
  <p class="muted">
    {$t('moderationActions.description')}
  </p>
</section>

<div class="op-grid">
  <section class="panel op-panel" style="border-left-color: var(--danger);">
    <div class="panel-head"><h2><Ban size={17} strokeWidth={2} /> {$t('moderationActions.banAccount')}</h2></div>
    {#if !canBan}
      <AccessDeniedNotice message={$t('moderationActions.banAccessDenied')} />
    {:else}
      <div class="op-field">
        <span class="op-label">{$t('common.playerRequired')}</span>
        <div class="op-pick">
          <button
            class="ghost-button"
            type="button"
            on:click={() =>
              pickUser(
                (u) => (ban = { ...ban, playerId: u.id, playerName: u.name, playerOnline: u.online }),
              )}
          >
            {$t('common.selectUser')}
          </button>
          {#if ban.playerId}
            <span class="op-chip">
              <span class="op-dot" class:on={ban.playerOnline}></span>
              {ban.playerName} <small>#{ban.playerId}</small>
            </span>
          {:else}
            <span class="muted">{$t('common.noUserSelected')}</span>
          {/if}
        </div>
      </div>
      <div class="op-checkbox-field">
        <input id="ban-permanent" type="checkbox" bind:checked={ban.permanent} />
        <label for="ban-permanent">{$t('common.permanent')}</label>
      </div>
      {#if !ban.permanent}
        <div class="op-field">
          <label for="ban-duration">{$t('moderationActions.durationSeconds')}</label>
          <input id="ban-duration" type="number" min="1" bind:value={ban.durationSeconds} placeholder="86400" />
        </div>
      {/if}
      <div class="op-field">
        <label for="ban-reason">{$t('common.reasonRequired')}</label>
        <input id="ban-reason" bind:value={ban.reason} placeholder={$t('common.reasonPlaceholder')} list="reason-history" />
      </div>
      <div class="op-actions">
        <button type="button" on:click={stageBan} disabled={busy.ban}>{$t('common.run')}</button>
      </div>
      {#if errors.ban}<p class="empty-state danger">{errors.ban}</p>{/if}
      {#if results.ban}
        <OpResult result={results.ban} onCopy={copy} copyLabel={$t('common.copy')} />
      {/if}
    {/if}
  </section>

  <section class="panel op-panel" style="border-left-color: var(--ok);">
    <div class="panel-head"><h2><ShieldCheck size={17} strokeWidth={2} /> {$t('moderationActions.liftAccountBan')}</h2></div>
    {#if !canUnban}
      <AccessDeniedNotice message={$t('moderationActions.unbanAccessDenied')} />
    {:else}
      <div class="op-field">
        <span class="op-label">{$t('common.playerRequired')}</span>
        <div class="op-pick">
          <button
            class="ghost-button"
            type="button"
            on:click={() =>
              pickUser(
                (u) => (unban = { ...unban, playerId: u.id, playerName: u.name, playerOnline: u.online }),
              )}
          >
            {$t('common.selectUser')}
          </button>
          {#if unban.playerId}
            <span class="op-chip">
              <span class="op-dot" class:on={unban.playerOnline}></span>
              {unban.playerName} <small>#{unban.playerId}</small>
            </span>
          {:else}
            <span class="muted">{$t('common.noUserSelected')}</span>
          {/if}
        </div>
      </div>
      <div class="op-field">
        <label for="unban-reason">{$t('common.reasonRequired')}</label>
        <input id="unban-reason" bind:value={unban.reason} placeholder={$t('common.reasonPlaceholder')} list="reason-history" />
      </div>
      <div class="op-actions">
        <button type="button" on:click={stageUnban} disabled={busy.unban}>{$t('common.run')}</button>
      </div>
      {#if errors.unban}<p class="empty-state danger">{errors.unban}</p>{/if}
      {#if results.unban}
        <OpResult result={results.unban} onCopy={copy} copyLabel={$t('common.copy')} />
      {/if}
    {/if}
  </section>

  <section class="panel op-panel" style="border-left-color: var(--warning);">
    <div class="panel-head"><h2><VolumeX size={17} strokeWidth={2} /> {$t('moderationActions.mutePlayer')}</h2></div>
    {#if !canMute}
      <AccessDeniedNotice message={$t('moderationActions.muteAccessDenied')} />
    {:else}
      <p class="muted">{$t('moderationActions.roomScopedNote')}</p>
      <div class="op-field">
        <span class="op-label">{$t('common.playerRequired')}</span>
        <div class="op-pick">
          <button
            class="ghost-button"
            type="button"
            on:click={() =>
              pickUser(
                (u) => (mute = { ...mute, playerId: u.id, playerName: u.name, playerOnline: u.online }),
              )}
          >
            {$t('common.selectUser')}
          </button>
          {#if mute.playerId}
            <span class="op-chip">
              <span class="op-dot" class:on={mute.playerOnline}></span>
              {mute.playerName} <small>#{mute.playerId}</small>
            </span>
          {:else}
            <span class="muted">{$t('common.noUserSelected')}</span>
          {/if}
        </div>
      </div>
      <div class="op-field">
        <label for="mute-duration">{$t('moderationActions.durationSecondsRequired')}</label>
        <input id="mute-duration" type="number" min="1" bind:value={mute.durationSeconds} placeholder="600" />
      </div>
      <div class="op-field">
        <label for="mute-reason">{$t('common.reasonRequired')}</label>
        <input id="mute-reason" bind:value={mute.reason} placeholder={$t('common.reasonPlaceholder')} list="reason-history" />
      </div>
      <div class="op-actions">
        <button type="button" on:click={stageMute} disabled={busy.mute}>{$t('common.run')}</button>
      </div>
      {#if errors.mute}<p class="empty-state danger">{errors.mute}</p>{/if}
      {#if results.mute}
        <OpResult result={results.mute} onCopy={copy} copyLabel={$t('common.copy')} />
      {/if}
    {/if}
  </section>

  <section class="panel op-panel" style="border-left-color: var(--warning);">
    <div class="panel-head"><h2><Lock size={17} strokeWidth={2} /> {$t('moderationActions.lockTrading')}</h2></div>
    {#if !canTradingLock}
      <AccessDeniedNotice message={$t('moderationActions.lockAccessDenied')} />
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
                  (tradingLock = {
                    ...tradingLock,
                    playerId: u.id,
                    playerName: u.name,
                    playerOnline: u.online,
                  }),
              )}
          >
            {$t('common.selectUser')}
          </button>
          {#if tradingLock.playerId}
            <span class="op-chip">
              <span class="op-dot" class:on={tradingLock.playerOnline}></span>
              {tradingLock.playerName} <small>#{tradingLock.playerId}</small>
            </span>
          {:else}
            <span class="muted">{$t('common.noUserSelected')}</span>
          {/if}
        </div>
      </div>
      <div class="op-checkbox-field">
        <input id="tradinglock-permanent" type="checkbox" bind:checked={tradingLock.permanent} />
        <label for="tradinglock-permanent">{$t('common.permanent')}</label>
      </div>
      {#if !tradingLock.permanent}
        <div class="op-field">
          <label for="tradinglock-duration">{$t('moderationActions.durationSeconds')}</label>
          <input id="tradinglock-duration" type="number" min="1" bind:value={tradingLock.durationSeconds} placeholder="86400" />
        </div>
      {/if}
      <div class="op-field">
        <label for="tradinglock-reason">{$t('common.reasonRequired')}</label>
        <input id="tradinglock-reason" bind:value={tradingLock.reason} placeholder={$t('common.reasonPlaceholder')} list="reason-history" />
      </div>
      <div class="op-actions">
        <button type="button" on:click={stageTradingLock} disabled={busy.tradingLock}>{$t('common.run')}</button>
      </div>
      {#if errors.tradingLock}<p class="empty-state danger">{errors.tradingLock}</p>{/if}
      {#if results.tradingLock}
        <OpResult result={results.tradingLock} onCopy={copy} copyLabel={$t('common.copy')} />
      {/if}
    {/if}
  </section>

  <section class="panel op-panel" style="border-left-color: var(--ok);">
    <div class="panel-head"><h2><LockOpen size={17} strokeWidth={2} /> {$t('moderationActions.liftTradingLock')}</h2></div>
    {#if !canTradingUnlock}
      <AccessDeniedNotice message={$t('moderationActions.unlockAccessDenied')} />
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
                  (tradingUnlock = {
                    ...tradingUnlock,
                    playerId: u.id,
                    playerName: u.name,
                    playerOnline: u.online,
                  }),
              )}
          >
            {$t('common.selectUser')}
          </button>
          {#if tradingUnlock.playerId}
            <span class="op-chip">
              <span class="op-dot" class:on={tradingUnlock.playerOnline}></span>
              {tradingUnlock.playerName} <small>#{tradingUnlock.playerId}</small>
            </span>
          {:else}
            <span class="muted">{$t('common.noUserSelected')}</span>
          {/if}
        </div>
      </div>
      <div class="op-field">
        <label for="tradingunlock-reason">{$t('common.reasonRequired')}</label>
        <input id="tradingunlock-reason" bind:value={tradingUnlock.reason} placeholder={$t('common.reasonPlaceholder')} list="reason-history" />
      </div>
      <div class="op-actions">
        <button type="button" on:click={stageTradingUnlock} disabled={busy.tradingUnlock}>{$t('common.run')}</button>
      </div>
      {#if errors.tradingUnlock}<p class="empty-state danger">{errors.tradingUnlock}</p>{/if}
      {#if results.tradingUnlock}
        <OpResult result={results.tradingUnlock} onCopy={copy} copyLabel={$t('common.copy')} />
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
    canSelect={canBan || canUnban || canMute || canTradingLock || canTradingUnlock}
  />
{/if}

{#if pending}
  <div class="modal-layer">
    <button class="modal-backdrop" type="button" aria-label="Cancel" on:click={cancel}></button>
    <section class="modal-panel" role="dialog" aria-modal="true" style="width: min(460px, 100%)">
      <header class="modal-header">
        <div>
          <p class="eyebrow">{$t('moderationActions.confirmSanction')}</p>
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
