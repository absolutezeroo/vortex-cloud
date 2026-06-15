<script>
  import { apiPost } from '../lib/api.js';
  import { compactCorrelation } from '../lib/format.js';
  import PickerModal from '../components/PickerModal.svelte';

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

  function pickUser(apply) {
    picker = { kind: 'user', title: 'Select a player', onSelect: apply };
  }

  function pickFurniture(apply) {
    picker = { kind: 'furniture', title: 'Select furniture', onSelect: apply };
  }

  function reasonOk(reason) {
    return typeof reason === 'string' && reason.trim().length >= 3;
  }

  function positive(value) {
    const numeric = Number(value);
    return Number.isFinite(numeric) && numeric > 0;
  }

  function nonNegative(value) {
    const numeric = Number(value);
    return Number.isFinite(numeric) && numeric >= 0;
  }

  function stage(id, title, endpoint, valid, body, summary) {
    errors = { ...errors, [id]: '' };

    if (!valid) {
      errors = {
        ...errors,
        [id]: 'Select a target and fill the fields: amounts must be positive and the reason needs at least 3 characters.',
      };
      return;
    }

    pending = { id, title, endpoint, body, summary, reason: body.reason };
  }

  function stageCredits() {
    stage(
      'credits',
      'Give credits',
      '/api/ops/currency/credits',
      positive(credits.playerId) && positive(credits.amount) && reasonOk(credits.reason),
      {
        playerId: Number(credits.playerId),
        amount: Number(credits.amount),
        reason: credits.reason.trim(),
      },
      `Grant ${credits.amount} credits to ${credits.playerName || 'player'} (#${credits.playerId}).`,
    );
  }

  function stageActivity() {
    stage(
      'activity',
      'Give activity points',
      '/api/ops/currency/activity-points',
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
      `Grant ${activity.amount} activity points (type ${activity.type}) to ${activity.playerName || 'player'} (#${activity.playerId}).`,
    );
  }

  function stageItem() {
    stage(
      'item',
      'Give furniture',
      '/api/ops/item/grant',
      positive(item.playerId) && positive(item.definitionId) && reasonOk(item.reason),
      {
        playerId: Number(item.playerId),
        definitionId: Number(item.definitionId),
        extraData: item.extraData.trim() ? item.extraData.trim() : null,
        reason: item.reason.trim(),
      },
      `Grant ${item.defName || 'furniture'} (#${item.definitionId}) to ${item.playerName || 'player'} (#${item.playerId}).`,
    );
  }

  function stageKick() {
    stage(
      'kick',
      'Kick player',
      '/api/ops/player/kick',
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
      errors = { ...errors, [id]: err.code || err.message };
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
        {results.credits.ok ? '✓' : '✕'} {results.credits.message} - cid
        <code>{compactCorrelation(results.credits.correlationId)}</code>
        <button class="ghost-button" type="button" on:click={() => copy(results.credits.correlationId)}>copy</button>
      </p>
    {/if}
  </section>

  <section class="panel">
    <div class="panel-head"><h2>Give activity points</h2></div>
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
        {results.activity.ok ? '✓' : '✕'} {results.activity.message} - cid
        <code>{compactCorrelation(results.activity.correlationId)}</code>
        <button class="ghost-button" type="button" on:click={() => copy(results.activity.correlationId)}>copy</button>
      </p>
    {/if}
  </section>

  <section class="panel">
    <div class="panel-head"><h2>Give furniture</h2></div>
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
          <span class="muted">no user selected</span>
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
        {results.item.ok ? '✓' : '✕'} {results.item.message} - cid
        <code>{compactCorrelation(results.item.correlationId)}</code>
        <button class="ghost-button" type="button" on:click={() => copy(results.item.correlationId)}>copy</button>
      </p>
    {/if}
  </section>

  <section class="panel">
    <div class="panel-head"><h2>Kick player</h2></div>
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
        {results.kick.ok ? '✓' : '✕'} {results.kick.message} - cid
        <code>{compactCorrelation(results.kick.correlationId)}</code>
        <button class="ghost-button" type="button" on:click={() => copy(results.kick.correlationId)}>copy</button>
      </p>
    {/if}
  </section>
</div>

{#if picker}
  <PickerModal
    kind={picker.kind}
    title={picker.title}
    onSelect={picker.onSelect}
    onClose={() => (picker = null)}
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

<style>
  .op-grid {
    display: grid;
    grid-template-columns: repeat(auto-fit, minmax(300px, 1fr));
    gap: 16px;
  }

  .op-field {
    display: grid;
    gap: 5px;
    margin-bottom: 10px;
  }

  .op-field label,
  .op-field .op-label {
    font-size: 0.74rem;
    text-transform: uppercase;
    font-weight: 700;
    color: var(--muted);
  }

  .op-field input {
    border: 1px solid var(--line-strong);
    border-radius: 9px;
    background: #0f1724;
    color: var(--ink);
    padding: 9px 10px;
    outline: none;
  }

  .op-field input:focus {
    border-color: rgba(90, 167, 200, 0.58);
    box-shadow: 0 0 0 3px rgba(90, 167, 200, 0.12);
  }

  .op-pick {
    display: flex;
    gap: 10px;
    align-items: center;
    flex-wrap: wrap;
  }

  .op-chip {
    display: inline-flex;
    align-items: center;
    gap: 7px;
    border: 1px solid var(--line-strong);
    border-radius: 999px;
    background: var(--surface-strong);
    padding: 5px 11px;
    min-width: 0;
  }

  .op-chip small {
    color: var(--muted);
  }

  .op-dot {
    width: 9px;
    height: 9px;
    flex: 0 0 auto;
    border-radius: 999px;
    background: var(--muted);
  }

  .op-dot.on {
    background: var(--ok);
    box-shadow: 0 0 0 3px rgba(86, 185, 145, 0.18);
  }

  .op-sprite {
    display: grid;
    place-items: center;
    min-width: 26px;
    height: 22px;
    padding: 0 6px;
    border: 1px solid var(--line-strong);
    border-radius: 6px;
    background: #0f1724;
    color: var(--accent);
    font-size: 0.68rem;
    font-weight: 700;
    object-fit: contain;
  }

  .op-actions {
    display: flex;
    gap: 8px;
    align-items: center;
    margin-top: 4px;
  }

  .op-actions button {
    border: 1px solid rgba(90, 167, 200, 0.34);
    border-radius: 9px;
    background: #22445a;
    color: #f0f7fb;
    padding: 8px 12px;
    font-weight: 700;
  }

  .op-actions button.ghost-button {
    background: var(--surface-strong);
    color: var(--ink);
  }

  .op-actions button:disabled {
    opacity: 0.55;
    cursor: default;
  }

  .op-result {
    margin-top: 10px;
    display: flex;
    gap: 8px;
    align-items: center;
    flex-wrap: wrap;
    color: var(--ok);
  }

  .op-result.danger {
    color: var(--danger);
  }
</style>
