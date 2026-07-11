<script>
  import { isPermissionDeniedError, hasDashboardCapability } from '../lib/permissions.js';
  import { apiPost } from '../lib/api.js';
  import { compactCorrelation } from '../lib/format.js';
  import { OPERATION_CAPABILITIES } from '../lib/dashboardPermissions.js';
  import { reasonOk, positive, nonNegative } from '../lib/validation.js';
  import AccessDeniedNotice from '../components/AccessDeniedNotice.svelte';
  import PickerModal from '../components/PickerModal.svelte';
  import { identity, openPlayer, openItem } from '../lib/session.js';

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
    picker = { kind: 'user', title: 'Select a player', onSelect: apply };
  }

  function pickFurniture(apply) {
    picker = { kind: 'furniture', title: 'Select furniture', onSelect: apply };
  }

  function stage(id, title, endpoint, valid, body, summary) {
    errors = { ...errors, [id]: '' };

    if (!valid) {
      errors = {
        ...errors,
        [id]:
          'Select a target and fill the fields: amounts must be positive and the reason needs at least 3 characters.',
      };
      return;
    }

    pending = { id, title, endpoint, body, summary, reason: body.reason };
  }

  function stageCredits() {
    if (!canCredits) {
      errors = {
        ...errors,
        credits: 'Droits insuffisants pour accorder des crédits.',
      };
      return;
    }

    stage(
      'credits',
      'Give credits',
      '/api/v1/operations/currency/credits',
      positive(credits.playerId) && positive(credits.amount) && reasonOk(credits.reason),
      {
        playerId: Number(credits.playerId),
        amount: Number(credits.amount),
        reason: credits.reason.trim(),
      },
      `Give ${credits.amount} credits to ${credits.playerName || 'player'} (#${credits.playerId}).`,
    );
  }

  function stageActivity() {
    if (!canActivity) {
      errors = {
        ...errors,
        activity: 'Droits insuffisants pour accorder des activity points.',
      };
      return;
    }

    stage(
      'activity',
      'Give activity points',
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
      `Give ${activity.amount} activity points (type ${activity.type}) to ${activity.playerName || 'player'} (#${activity.playerId}).`,
    );
  }

  function stageItem() {
    if (!canItem) {
      errors = {
        ...errors,
        item: 'Droits insuffisants pour donner un objet.',
      };
      return;
    }

    stage(
      'item',
      'Give furniture',
      '/api/v1/operations/items/grant',
      positive(item.playerId) && positive(item.definitionId) && reasonOk(item.reason),
      {
        playerId: Number(item.playerId),
        definitionId: Number(item.definitionId),
        extraData: item.extraData.trim() ? item.extraData.trim() : null,
        reason: item.reason.trim(),
      },
      `Give ${item.defName || 'furniture'} (#${item.definitionId}) to ${item.playerName || 'player'} (#${item.playerId}).`,
    );
  }

  function stageKick() {
    if (!canKick) {
      errors = {
        ...errors,
        kick: 'Droits insuffisants pour expulser un joueur.',
      };
      return;
    }

    stage(
      'kick',
      'Kick player',
      '/api/v1/operations/players/kick',
      positive(kick.playerId) && reasonOk(kick.reason),
      { playerId: Number(kick.playerId), reason: kick.reason.trim() },
      `Force-disconnect ${kick.playerName || 'player'} (#${kick.playerId}).`,
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
  <div class="panel-head"><h2>Operations</h2></div>
  <p class="muted">
    Controlled admin actions. Pick a target, give a mandatory reason, and confirm; every run requires
    the admin token, is audited with a correlation id, and is routed through the game grains.
  </p>
</section>

<div class="op-grid">
  <section class="panel">
    <div class="panel-head"><h2>Give credits</h2></div>
    {#if !canCredits}
      <AccessDeniedNotice title="Droits insuffisants" message="Vous n'avez pas la permission de créditer des Habbo." />
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
                  (credits = {
                    ...credits,
                    playerId: u.id,
                    playerName: u.name,
                    playerOnline: u.online,
                  }),
              )}
          >
            Select user
          </button>
          {#if credits.playerId}
            <span class="op-chip">
              <span class="op-dot" class:on={credits.playerOnline}></span>
              {credits.playerName} <small>#{credits.playerId}</small>
            </span>
          {:else}
            <span class="muted">no user selected</span>
          {/if}
        </div>
      </div>
      <div class="op-field">
        <label for="credits-amount">Amount</label>
        <input id="credits-amount" type="number" min="1" bind:value={credits.amount} placeholder="100" />
      </div>
      <div class="op-field">
        <label for="credits-reason">Reason *</label>
        <input id="credits-reason" bind:value={credits.reason} placeholder="why this action?" />
      </div>
      <div class="op-actions">
        <button type="button" on:click={stageCredits} disabled={busy.credits}>Run</button>
      </div>
      {#if errors.credits}<p class="empty-state danger">{errors.credits}</p>{/if}
      {#if results.credits}
        <p class="op-result" class:danger={!results.credits.ok}>
          {results.credits.ok ? '✅' : '❌'} {results.credits.message} - cid
          <code>{compactCorrelation(results.credits.correlationId)}</code>
          <button class="ghost-button" type="button" on:click={() => copy(results.credits.correlationId)}>copy</button>
        </p>
      {/if}
    {/if}
  </section>

  <section class="panel">
    <div class="panel-head"><h2>Give activity points</h2></div>
    {#if !canActivity}
      <AccessDeniedNotice title="Droits insuffisants" message="Vous n'avez pas la permission d'accorder des activity points." />
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
                  (activity = {
                    ...activity,
                    playerId: u.id,
                    playerName: u.name,
                    playerOnline: u.online,
                  }),
              )}
          >
            Select user
          </button>
          {#if activity.playerId}
            <span class="op-chip">
              <span class="op-dot" class:on={activity.playerOnline}></span>
              {activity.playerName} <small>#{activity.playerId}</small>
            </span>
          {:else}
            <span class="muted">no user selected</span>
          {/if}
        </div>
      </div>
      <div class="op-field">
        <label for="activity-type">Activity point type</label>
        <input id="activity-type" type="number" min="0" bind:value={activity.type} placeholder="0" />
      </div>
      <div class="op-field">
        <label for="activity-amount">Amount</label>
        <input id="activity-amount" type="number" min="1" bind:value={activity.amount} placeholder="50" />
      </div>
      <div class="op-field">
        <label for="activity-reason">Reason *</label>
        <input id="activity-reason" bind:value={activity.reason} placeholder="why this action?" />
      </div>
      <div class="op-actions">
        <button type="button" on:click={stageActivity} disabled={busy.activity}>Run</button>
      </div>
      {#if errors.activity}<p class="empty-state danger">{errors.activity}</p>{/if}
      {#if results.activity}
        <p class="op-result" class:danger={!results.activity.ok}>
          {results.activity.ok ? '✅' : '❌'} {results.activity.message} - cid
          <code>{compactCorrelation(results.activity.correlationId)}</code>
          <button class="ghost-button" type="button" on:click={() => copy(results.activity.correlationId)}>copy</button>
        </p>
      {/if}
    {/if}
  </section>

  <section class="panel">
    <div class="panel-head"><h2>Give furniture</h2></div>
    {#if !canItem}
      <AccessDeniedNotice title="Droits insuffisants" message="Vous n'avez pas la permission de donner des objets." />
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
                  (item = { ...item, playerId: u.id, playerName: u.name, playerOnline: u.online }),
              )}
          >
            Select user
          </button>
          {#if item.playerId}
            <span class="op-chip">
              <span class="op-dot" class:on={item.playerOnline}></span>
              {item.playerName} <small>#{item.playerId}</small>
            </span>
          {:else}
            <span class="muted">no furniture selected</span>
          {/if}
        </div>
      </div>
      <div class="op-field">
        <span class="op-label">Furniture *</span>
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
            Select furniture
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
            <span class="muted">no furniture selected</span>
          {/if}
        </div>
      </div>
      <div class="op-field">
        <label for="item-extra">Extra data (optional)</label>
        <input id="item-extra" bind:value={item.extraData} placeholder="state / wired data" />
      </div>
      <div class="op-field">
        <label for="item-reason">Reason *</label>
        <input id="item-reason" bind:value={item.reason} placeholder="why this action?" />
      </div>
      <div class="op-actions">
        <button type="button" on:click={stageItem} disabled={busy.item}>Run</button>
      </div>
      {#if errors.item}<p class="empty-state danger">{errors.item}</p>{/if}
      {#if results.item}
        <p class="op-result" class:danger={!results.item.ok}>
          {results.item.ok ? '✅' : '❌'} {results.item.message} - cid
          <code>{compactCorrelation(results.item.correlationId)}</code>
          <button class="ghost-button" type="button" on:click={() => copy(results.item.correlationId)}>copy</button>
        </p>
      {/if}
    {/if}
  </section>

  <section class="panel">
    <div class="panel-head"><h2>Kick player</h2></div>
    {#if !canKick}
      <AccessDeniedNotice title="Droits insuffisants" message="Vous n'avez pas la permission d'expulser un joueur." />
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
                  (kick = { ...kick, playerId: u.id, playerName: u.name, playerOnline: u.online }),
              )}
          >
            Select user
          </button>
          {#if kick.playerId}
            <span class="op-chip">
              <span class="op-dot" class:on={kick.playerOnline}></span>
              {kick.playerName} <small>#{kick.playerId}</small>
            </span>
          {:else}
            <span class="muted">no user selected</span>
          {/if}
        </div>
      </div>
      <div class="op-field">
        <label for="kick-reason">Reason *</label>
        <input id="kick-reason" bind:value={kick.reason} placeholder="why this action?" />
      </div>
      <div class="op-actions">
        <button type="button" on:click={stageKick} disabled={busy.kick}>Run</button>
      </div>
      {#if errors.kick}<p class="empty-state danger">{errors.kick}</p>{/if}
      {#if results.kick}
        <p class="op-result" class:danger={!results.kick.ok}>
          {results.kick.ok ? '✅' : '❌'} {results.kick.message} - cid
          <code>{compactCorrelation(results.kick.correlationId)}</code>
          <button class="ghost-button" type="button" on:click={() => copy(results.kick.correlationId)}>copy</button>
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
    canSelect={picker.kind === 'user' ? canCredits || canActivity || canItem || canKick : canItem}
  />
{/if}

{#if pending}
  <div class="modal-layer">
    <button class="modal-backdrop" type="button" aria-label="Cancel" on:click={cancel}></button>
    <section class="modal-panel" role="dialog" aria-modal="true" style="width: min(460px, 100%)">
      <header class="modal-header">
        <div>
          <p class="eyebrow">Confirm admin action</p>
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
