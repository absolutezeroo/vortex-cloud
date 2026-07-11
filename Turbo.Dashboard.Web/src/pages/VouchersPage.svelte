<script>
  import { isPermissionDeniedError, hasDashboardCapability } from '../lib/permissions.js';
  import { apiGet, apiPost } from '../lib/api.js';
  import { compactCorrelation, formatDate } from '../lib/format.js';
  import { CAPABILITIES } from '../lib/dashboardPermissions.js';
  import { reasonOk, positive, nonNegative } from '../lib/validation.js';
  import AccessDeniedNotice from '../components/AccessDeniedNotice.svelte';
  import { identity } from '../lib/session.js';

  const currencyTypes = [
    { value: 1, label: 'Credits' },
    { value: 2, label: 'Silver' },
    { value: 3, label: 'Emeralds' },
    { value: 4, label: 'Activity points' },
  ];

  let create = {
    code: '',
    currencyType: 1,
    activityPointType: '',
    amount: '',
    maxRedemptions: '',
    expiresAt: '',
    reason: '',
  };
  let deactivate = { code: '', reason: '' };
  let lookupCode = '';
  let lookupResult = null;
  let lookupError = '';
  let lookupLoading = false;

  let results = {};
  let errors = {};
  let busy = {};
  let pending = null;

  $: canManage = hasDashboardCapability($identity, CAPABILITIES.opsManageVouchers);

  function reasonError(id, message) {
    errors = { ...errors, [id]: message };
  }

  function stage(id, title, endpoint, valid, body, summary) {
    errors = { ...errors, [id]: '' };

    if (!valid) {
      errors = { ...errors, [id]: 'Fill the required fields (reason needs at least 3 characters).' };
      return;
    }

    pending = { id, title, endpoint, body, summary, reason: body.reason };
  }

  function stageCreate() {
    if (!canManage) {
      reasonError('create', 'Droits insuffisants pour créer un voucher.');
      return;
    }

    const needsActivityType = Number(create.currencyType) === 4;

    stage(
      'create',
      'Create voucher',
      '/api/v1/operations/vouchers',
      Boolean(create.code.trim()) &&
        positive(create.amount) &&
        (!needsActivityType || nonNegative(create.activityPointType)) &&
        reasonOk(create.reason),
      {
        code: create.code.trim(),
        currencyType: Number(create.currencyType),
        activityPointType: needsActivityType ? Number(create.activityPointType) : null,
        amount: Number(create.amount),
        maxRedemptions: create.maxRedemptions ? Number(create.maxRedemptions) : null,
        expiresAt: create.expiresAt ? new Date(create.expiresAt).toISOString() : null,
        reason: create.reason.trim(),
      },
      `Create voucher "${create.code.trim()}" worth ${create.amount} (${currencyTypes.find((c) => c.value === Number(create.currencyType))?.label}).`,
    );
  }

  function stageDeactivate() {
    if (!canManage) {
      reasonError('deactivate', 'Droits insuffisants pour désactiver un voucher.');
      return;
    }

    stage(
      'deactivate',
      'Deactivate voucher',
      '/api/v1/operations/vouchers/deactivate',
      Boolean(deactivate.code.trim()) && reasonOk(deactivate.reason),
      { code: deactivate.code.trim(), reason: deactivate.reason.trim() },
      `Deactivate voucher "${deactivate.code.trim()}" — it can no longer be redeemed.`,
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

  async function lookup() {
    if (!lookupCode.trim()) {
      return;
    }

    lookupLoading = true;
    lookupError = '';
    lookupResult = null;

    try {
      lookupResult = await apiGet(`/api/v1/operations/vouchers/${encodeURIComponent(lookupCode.trim())}`);
    } catch (err) {
      lookupError = isPermissionDeniedError(err) ? 'Droits insuffisants.' : err.code || err.message;
    } finally {
      lookupLoading = false;
    }
  }

  async function copy(value) {
    try {
      await navigator.clipboard.writeText(value || '');
    } catch {
      // Clipboard is best-effort.
    }
  }
</script>

<section class="panel">
  <div class="panel-head"><h2>Vouchers</h2></div>
  <p class="muted">Create and manage redeemable currency codes. Each player may redeem a given code only once.</p>
</section>

<div class="op-grid">
  <section class="panel">
    <div class="panel-head"><h2>Create voucher</h2></div>
    {#if !canManage}
      <AccessDeniedNotice title="Droits insuffisants" message="Vous n'avez pas la permission de gérer les vouchers." />
    {:else}
      <div class="op-field">
        <label for="voucher-code">Code *</label>
        <input id="voucher-code" bind:value={create.code} placeholder="SUMMER2026" style="text-transform: uppercase;" />
      </div>
      <div class="op-field">
        <label for="voucher-currency">Currency *</label>
        <select id="voucher-currency" bind:value={create.currencyType}>
          {#each currencyTypes as ct}
            <option value={ct.value}>{ct.label}</option>
          {/each}
        </select>
      </div>
      {#if Number(create.currencyType) === 4}
        <div class="op-field">
          <label for="voucher-activity-type">Activity point type *</label>
          <input id="voucher-activity-type" type="number" min="0" bind:value={create.activityPointType} placeholder="0" />
        </div>
      {/if}
      <div class="op-field">
        <label for="voucher-amount">Amount *</label>
        <input id="voucher-amount" type="number" min="1" bind:value={create.amount} placeholder="100" />
      </div>
      <div class="op-field">
        <label for="voucher-max-redemptions">Max redemptions (optional)</label>
        <input id="voucher-max-redemptions" type="number" min="1" bind:value={create.maxRedemptions} placeholder="unlimited" />
      </div>
      <div class="op-field">
        <label for="voucher-expires">Expires at (optional)</label>
        <input id="voucher-expires" type="datetime-local" bind:value={create.expiresAt} />
      </div>
      <div class="op-field">
        <label for="voucher-reason">Reason *</label>
        <input id="voucher-reason" bind:value={create.reason} placeholder="why this voucher?" />
      </div>
      <div class="op-actions">
        <button type="button" on:click={stageCreate} disabled={busy.create}>Run</button>
      </div>
      {#if errors.create}<p class="empty-state danger">{errors.create}</p>{/if}
      {#if results.create}
        <p class="op-result" class:danger={!results.create.ok}>
          {results.create.ok ? '✅' : '❌'} {results.create.message} - cid
          <code>{compactCorrelation(results.create.correlationId)}</code>
          <button class="ghost-button" type="button" on:click={() => copy(results.create.correlationId)}>copy</button>
        </p>
      {/if}
    {/if}
  </section>

  <section class="panel">
    <div class="panel-head"><h2>Deactivate voucher</h2></div>
    {#if !canManage}
      <AccessDeniedNotice title="Droits insuffisants" message="Vous n'avez pas la permission de gérer les vouchers." />
    {:else}
      <div class="op-field">
        <label for="deactivate-code">Code *</label>
        <input id="deactivate-code" bind:value={deactivate.code} placeholder="SUMMER2026" style="text-transform: uppercase;" />
      </div>
      <div class="op-field">
        <label for="deactivate-reason">Reason *</label>
        <input id="deactivate-reason" bind:value={deactivate.reason} placeholder="why deactivate?" />
      </div>
      <div class="op-actions">
        <button type="button" on:click={stageDeactivate} disabled={busy.deactivate}>Run</button>
      </div>
      {#if errors.deactivate}<p class="empty-state danger">{errors.deactivate}</p>{/if}
      {#if results.deactivate}
        <p class="op-result" class:danger={!results.deactivate.ok}>
          {results.deactivate.ok ? '✅' : '❌'} {results.deactivate.message} - cid
          <code>{compactCorrelation(results.deactivate.correlationId)}</code>
          <button class="ghost-button" type="button" on:click={() => copy(results.deactivate.correlationId)}>copy</button>
        </p>
      {/if}
    {/if}
  </section>

  <section class="panel">
    <div class="panel-head"><h2>Look up voucher</h2></div>
    <form class="toolbar" on:submit|preventDefault={lookup}>
      <input bind:value={lookupCode} placeholder="voucher code" style="text-transform: uppercase;" />
      <button type="submit" disabled={lookupLoading}>Inspect</button>
    </form>
    {#if lookupLoading}
      <p class="muted">Loading...</p>
    {:else if lookupError}
      <p class="empty-state danger">{lookupError}</p>
    {:else if lookupResult}
      {#if !lookupResult.exists}
        <p class="empty-state">No voucher with that code.</p>
      {:else}
        <div class="metric-grid compact">
          <article><span>Status</span><strong>{lookupResult.isActive ? 'Active' : 'Inactive'}</strong></article>
          <article><span>Currency</span><strong>{currencyTypes.find((c) => c.value === lookupResult.currencyType)?.label || lookupResult.currencyType}</strong></article>
          <article><span>Amount</span><strong>{lookupResult.amount}</strong></article>
          <article><span>Redemptions</span><strong>{lookupResult.redemptionCount}{lookupResult.maxRedemptions ? ` / ${lookupResult.maxRedemptions}` : ''}</strong></article>
          <article><span>Expires</span><strong>{lookupResult.expiresAt ? formatDate(lookupResult.expiresAt) : 'never'}</strong></article>
        </div>
      {/if}
    {/if}
  </section>
</div>

{#if pending}
  <div class="modal-layer">
    <button class="modal-backdrop" type="button" aria-label="Cancel" on:click={cancel}></button>
    <section class="modal-panel" role="dialog" aria-modal="true" style="width: min(460px, 100%)">
      <header class="modal-header">
        <div>
          <p class="eyebrow">Confirm voucher action</p>
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
