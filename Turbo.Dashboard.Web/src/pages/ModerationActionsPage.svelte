<script>
  import { Ban, ShieldCheck, VolumeX, Lock, LockOpen } from '@lucide/svelte';
  import { isPermissionDeniedError, hasDashboardCapability } from '../lib/permissions.js';
  import { apiPost } from '../lib/api.js';
  import { compactCorrelation } from '../lib/format.js';
  import { MODERATION_OPERATION_CAPABILITIES } from '../lib/dashboardPermissions.js';
  import { reasonOk, positive } from '../lib/validation.js';
  import AccessDeniedNotice from '../components/AccessDeniedNotice.svelte';
  import PickerModal from '../components/PickerModal.svelte';
  import { identity } from '../lib/session.js';

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
    picker = { kind: 'user', title: 'Select a player', onSelect: apply };
  }

  function stage(id, title, endpoint, valid, body, summary) {
    errors = { ...errors, [id]: '' };

    if (!valid) {
      errors = {
        ...errors,
        [id]:
          'Select a target and fill the fields: durations must be positive (unless permanent) and the reason needs at least 3 characters.',
      };
      return;
    }

    pending = { id, title, endpoint, body, summary, reason: body.reason };
  }

  function stageBan() {
    if (!canBan) {
      errors = { ...errors, ban: 'Droits insuffisants pour bannir un compte.' };
      return;
    }

    const validDuration = ban.permanent || positive(ban.durationSeconds);

    stage(
      'ban',
      'Ban account',
      '/api/v1/operations/players/ban',
      positive(ban.playerId) && validDuration && reasonOk(ban.reason),
      {
        playerId: Number(ban.playerId),
        permanent: ban.permanent,
        durationSeconds: ban.permanent ? null : Number(ban.durationSeconds),
        reason: ban.reason.trim(),
      },
      `${ban.permanent ? 'Permanently ban' : `Ban for ${ban.durationSeconds}s`} ${ban.playerName || 'player'} (#${ban.playerId}).`,
    );
  }

  function stageUnban() {
    if (!canUnban) {
      errors = { ...errors, unban: 'Droits insuffisants pour lever un ban.' };
      return;
    }

    stage(
      'unban',
      'Lift account ban',
      '/api/v1/operations/players/unban',
      positive(unban.playerId) && reasonOk(unban.reason),
      { playerId: Number(unban.playerId), reason: unban.reason.trim() },
      `Lift the account ban on ${unban.playerName || 'player'} (#${unban.playerId}).`,
    );
  }

  function stageMute() {
    if (!canMute) {
      errors = { ...errors, mute: 'Droits insuffisants pour mute un joueur.' };
      return;
    }

    stage(
      'mute',
      'Mute player',
      '/api/v1/operations/players/mute',
      positive(mute.playerId) && positive(mute.durationSeconds) && reasonOk(mute.reason),
      {
        playerId: Number(mute.playerId),
        durationSeconds: Number(mute.durationSeconds),
        reason: mute.reason.trim(),
      },
      `Mute ${mute.playerName || 'player'} (#${mute.playerId}) for ${mute.durationSeconds}s. Only works while the player is currently in a room.`,
    );
  }

  function stageTradingLock() {
    if (!canTradingLock) {
      errors = { ...errors, tradingLock: "Droits insuffisants pour bloquer les échanges." };
      return;
    }

    const validDuration = tradingLock.permanent || positive(tradingLock.durationSeconds);

    stage(
      'tradingLock',
      'Lock trading',
      '/api/v1/operations/players/trading-lock',
      positive(tradingLock.playerId) && validDuration && reasonOk(tradingLock.reason),
      {
        playerId: Number(tradingLock.playerId),
        permanent: tradingLock.permanent,
        durationSeconds: tradingLock.permanent ? null : Number(tradingLock.durationSeconds),
        reason: tradingLock.reason.trim(),
      },
      `${tradingLock.permanent ? 'Permanently lock' : `Lock for ${tradingLock.durationSeconds}s`} trading for ${tradingLock.playerName || 'player'} (#${tradingLock.playerId}).`,
    );
  }

  function stageTradingUnlock() {
    if (!canTradingUnlock) {
      errors = { ...errors, tradingUnlock: "Droits insuffisants pour débloquer les échanges." };
      return;
    }

    stage(
      'tradingUnlock',
      'Lift trading lock',
      '/api/v1/operations/players/trading-unlock',
      positive(tradingUnlock.playerId) && reasonOk(tradingUnlock.reason),
      { playerId: Number(tradingUnlock.playerId), reason: tradingUnlock.reason.trim() },
      `Lift the trading lock on ${tradingUnlock.playerName || 'player'} (#${tradingUnlock.playerId}).`,
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
        [id]: isPermissionDeniedError(err) ? 'Droits insuffisants pour effectuer cette action.' : err.code || err.message,
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
  <div class="panel-head"><h2>Moderation actions</h2></div>
  <p class="muted">
    Controlled sanction actions. Pick a target, give a mandatory reason, and confirm; every run is
    audited with a correlation id and routed through the game grains. For trend/history data see
    Moderation stats.
  </p>
</section>

<div class="op-grid">
  <section class="panel op-panel" style="border-left-color: var(--danger);">
    <div class="panel-head"><h2><Ban size={17} strokeWidth={2} /> Ban account</h2></div>
    {#if !canBan}
      <AccessDeniedNotice title="Droits insuffisants" message="Vous n'avez pas la permission de bannir un compte." />
    {:else}
      <div class="op-field">
        <span class="op-label">Player *</span>
        <div class="op-pick">
          <button
            class="ghost-button"
            type="button"
            on:click={() =>
              pickUser(
                (u) => (ban = { ...ban, playerId: u.id, playerName: u.name, playerOnline: u.online }),
              )}
          >
            Select user
          </button>
          {#if ban.playerId}
            <span class="op-chip">
              <span class="op-dot" class:on={ban.playerOnline}></span>
              {ban.playerName} <small>#{ban.playerId}</small>
            </span>
          {:else}
            <span class="muted">no user selected</span>
          {/if}
        </div>
      </div>
      <div class="op-checkbox-field">
        <input id="ban-permanent" type="checkbox" bind:checked={ban.permanent} />
        <label for="ban-permanent">Permanent</label>
      </div>
      {#if !ban.permanent}
        <div class="op-field">
          <label for="ban-duration">Duration (seconds)</label>
          <input id="ban-duration" type="number" min="1" bind:value={ban.durationSeconds} placeholder="86400" />
        </div>
      {/if}
      <div class="op-field">
        <label for="ban-reason">Reason *</label>
        <input id="ban-reason" bind:value={ban.reason} placeholder="why this action?" />
      </div>
      <div class="op-actions">
        <button type="button" on:click={stageBan} disabled={busy.ban}>Run</button>
      </div>
      {#if errors.ban}<p class="empty-state danger">{errors.ban}</p>{/if}
      {#if results.ban}
        <p class="op-result" class:danger={!results.ban.ok}>
          {results.ban.ok ? '✅' : '❌'} {results.ban.message} - cid
          <code>{compactCorrelation(results.ban.correlationId)}</code>
          <button class="ghost-button" type="button" on:click={() => copy(results.ban.correlationId)}>copy</button>
        </p>
      {/if}
    {/if}
  </section>

  <section class="panel op-panel" style="border-left-color: var(--ok);">
    <div class="panel-head"><h2><ShieldCheck size={17} strokeWidth={2} /> Lift account ban</h2></div>
    {#if !canUnban}
      <AccessDeniedNotice title="Droits insuffisants" message="Vous n'avez pas la permission de lever un ban." />
    {:else}
      <div class="op-field">
        <span class="op-label">Player *</span>
        <div class="op-pick">
          <button
            class="ghost-button"
            type="button"
            on:click={() =>
              pickUser(
                (u) => (unban = { ...unban, playerId: u.id, playerName: u.name, playerOnline: u.online }),
              )}
          >
            Select user
          </button>
          {#if unban.playerId}
            <span class="op-chip">
              <span class="op-dot" class:on={unban.playerOnline}></span>
              {unban.playerName} <small>#{unban.playerId}</small>
            </span>
          {:else}
            <span class="muted">no user selected</span>
          {/if}
        </div>
      </div>
      <div class="op-field">
        <label for="unban-reason">Reason *</label>
        <input id="unban-reason" bind:value={unban.reason} placeholder="why this action?" />
      </div>
      <div class="op-actions">
        <button type="button" on:click={stageUnban} disabled={busy.unban}>Run</button>
      </div>
      {#if errors.unban}<p class="empty-state danger">{errors.unban}</p>{/if}
      {#if results.unban}
        <p class="op-result" class:danger={!results.unban.ok}>
          {results.unban.ok ? '✅' : '❌'} {results.unban.message} - cid
          <code>{compactCorrelation(results.unban.correlationId)}</code>
          <button class="ghost-button" type="button" on:click={() => copy(results.unban.correlationId)}>copy</button>
        </p>
      {/if}
    {/if}
  </section>

  <section class="panel op-panel" style="border-left-color: var(--warning);">
    <div class="panel-head"><h2><VolumeX size={17} strokeWidth={2} /> Mute player</h2></div>
    {#if !canMute}
      <AccessDeniedNotice title="Droits insuffisants" message="Vous n'avez pas la permission de mute un joueur." />
    {:else}
      <p class="muted">Room-scoped — only works while the player is currently in a room.</p>
      <div class="op-field">
        <span class="op-label">Player *</span>
        <div class="op-pick">
          <button
            class="ghost-button"
            type="button"
            on:click={() =>
              pickUser(
                (u) => (mute = { ...mute, playerId: u.id, playerName: u.name, playerOnline: u.online }),
              )}
          >
            Select user
          </button>
          {#if mute.playerId}
            <span class="op-chip">
              <span class="op-dot" class:on={mute.playerOnline}></span>
              {mute.playerName} <small>#{mute.playerId}</small>
            </span>
          {:else}
            <span class="muted">no user selected</span>
          {/if}
        </div>
      </div>
      <div class="op-field">
        <label for="mute-duration">Duration (seconds) *</label>
        <input id="mute-duration" type="number" min="1" bind:value={mute.durationSeconds} placeholder="600" />
      </div>
      <div class="op-field">
        <label for="mute-reason">Reason *</label>
        <input id="mute-reason" bind:value={mute.reason} placeholder="why this action?" />
      </div>
      <div class="op-actions">
        <button type="button" on:click={stageMute} disabled={busy.mute}>Run</button>
      </div>
      {#if errors.mute}<p class="empty-state danger">{errors.mute}</p>{/if}
      {#if results.mute}
        <p class="op-result" class:danger={!results.mute.ok}>
          {results.mute.ok ? '✅' : '❌'} {results.mute.message} - cid
          <code>{compactCorrelation(results.mute.correlationId)}</code>
          <button class="ghost-button" type="button" on:click={() => copy(results.mute.correlationId)}>copy</button>
        </p>
      {/if}
    {/if}
  </section>

  <section class="panel op-panel" style="border-left-color: var(--warning);">
    <div class="panel-head"><h2><Lock size={17} strokeWidth={2} /> Lock trading</h2></div>
    {#if !canTradingLock}
      <AccessDeniedNotice title="Droits insuffisants" message="Vous n'avez pas la permission de bloquer les échanges." />
    {:else}
      <div class="op-field">
        <span class="op-label">Player *</span>
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
            Select user
          </button>
          {#if tradingLock.playerId}
            <span class="op-chip">
              <span class="op-dot" class:on={tradingLock.playerOnline}></span>
              {tradingLock.playerName} <small>#{tradingLock.playerId}</small>
            </span>
          {:else}
            <span class="muted">no user selected</span>
          {/if}
        </div>
      </div>
      <div class="op-checkbox-field">
        <input id="tradinglock-permanent" type="checkbox" bind:checked={tradingLock.permanent} />
        <label for="tradinglock-permanent">Permanent</label>
      </div>
      {#if !tradingLock.permanent}
        <div class="op-field">
          <label for="tradinglock-duration">Duration (seconds)</label>
          <input id="tradinglock-duration" type="number" min="1" bind:value={tradingLock.durationSeconds} placeholder="86400" />
        </div>
      {/if}
      <div class="op-field">
        <label for="tradinglock-reason">Reason *</label>
        <input id="tradinglock-reason" bind:value={tradingLock.reason} placeholder="why this action?" />
      </div>
      <div class="op-actions">
        <button type="button" on:click={stageTradingLock} disabled={busy.tradingLock}>Run</button>
      </div>
      {#if errors.tradingLock}<p class="empty-state danger">{errors.tradingLock}</p>{/if}
      {#if results.tradingLock}
        <p class="op-result" class:danger={!results.tradingLock.ok}>
          {results.tradingLock.ok ? '✅' : '❌'} {results.tradingLock.message} - cid
          <code>{compactCorrelation(results.tradingLock.correlationId)}</code>
          <button class="ghost-button" type="button" on:click={() => copy(results.tradingLock.correlationId)}>copy</button>
        </p>
      {/if}
    {/if}
  </section>

  <section class="panel op-panel" style="border-left-color: var(--ok);">
    <div class="panel-head"><h2><LockOpen size={17} strokeWidth={2} /> Lift trading lock</h2></div>
    {#if !canTradingUnlock}
      <AccessDeniedNotice title="Droits insuffisants" message="Vous n'avez pas la permission de débloquer les échanges." />
    {:else}
      <div class="op-field">
        <span class="op-label">Player *</span>
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
            Select user
          </button>
          {#if tradingUnlock.playerId}
            <span class="op-chip">
              <span class="op-dot" class:on={tradingUnlock.playerOnline}></span>
              {tradingUnlock.playerName} <small>#{tradingUnlock.playerId}</small>
            </span>
          {:else}
            <span class="muted">no user selected</span>
          {/if}
        </div>
      </div>
      <div class="op-field">
        <label for="tradingunlock-reason">Reason *</label>
        <input id="tradingunlock-reason" bind:value={tradingUnlock.reason} placeholder="why this action?" />
      </div>
      <div class="op-actions">
        <button type="button" on:click={stageTradingUnlock} disabled={busy.tradingUnlock}>Run</button>
      </div>
      {#if errors.tradingUnlock}<p class="empty-state danger">{errors.tradingUnlock}</p>{/if}
      {#if results.tradingUnlock}
        <p class="op-result" class:danger={!results.tradingUnlock.ok}>
          {results.tradingUnlock.ok ? '✅' : '❌'} {results.tradingUnlock.message} - cid
          <code>{compactCorrelation(results.tradingUnlock.correlationId)}</code>
          <button class="ghost-button" type="button" on:click={() => copy(results.tradingUnlock.correlationId)}>copy</button>
        </p>
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
          <p class="eyebrow">Confirm sanction</p>
          <h2>{pending.title}</h2>
        </div>
      </header>
      <p>{pending.summary}</p>
      <p class="muted">Reason: {pending.reason}</p>
      <div class="op-actions">
        <button type="button" on:click={confirm}>Confirm</button>
        <button class="ghost-button" type="button" on:click={cancel}>Cancel</button>
      </div>
    </section>
  </div>
{/if}
