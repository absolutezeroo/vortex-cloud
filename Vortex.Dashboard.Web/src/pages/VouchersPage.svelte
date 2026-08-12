<script>
  import ConfirmStagedModal from '../components/ConfirmStagedModal.svelte';
  import OpResult from '../components/OpResult.svelte';
  import { isPermissionDeniedError, hasDashboardCapability } from '../lib/permissions.js';
  import { apiGet } from '../lib/api.js';
  import { createWriteOps } from '../lib/writeOps.js';
  import { compactCorrelation, formatDate } from '../lib/format.js';
  import { CAPABILITIES } from '../lib/dashboardPermissions.js';
  import { reasonOk, positive, nonNegative } from '../lib/validation.js';
  import AccessDeniedNotice from '../components/AccessDeniedNotice.svelte';
  import StatCard from '../components/StatCard.svelte';
  import { Activity, Coins, Hash, Timer } from '@lucide/svelte';
  import { identity } from '../lib/session.js';
  import { t, translate } from '../lib/i18n.js';

  const currencyTypes = [
    { value: 1, key: 'vouchers.currencyCredits' },
    { value: 2, key: 'vouchers.currencySilver' },
    { value: 3, key: 'vouchers.currencyEmeralds' },
    { value: 4, key: 'vouchers.currencyActivityPoints' },
  ];

  function currencyLabel(value, translator) {
    const entry = currencyTypes.find((c) => c.value === Number(value));
    return entry ? translator(entry.key) : String(value);
  }

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

  // Every write is staged here and confirmed in the dialog below before it is posted. createWriteOps
  // owns that cycle -- posting, remembering the audited reason, and tracking each form's busy state,
  // error and result under its own key -- so the page only describes what each button writes.
  const ops = createWriteOps();

  $: canManage = hasDashboardCapability($identity, CAPABILITIES.opsManageVouchers);

  function reasonError(id, message) {
    ops.fail(id, message);
  }

  const stage = (id, title, endpoint, valid, body, summary) =>
    ops.ask(endpoint, body, title, summary, {
      key: id,
      valid,
      invalidMessage: translate('vouchers.fillFields'),
      reason: body.reason,
    });

  function stageCreate() {
    if (!canManage) {
      reasonError('create', translate('vouchers.createAccessDenied'));
      return;
    }

    const needsActivityType = Number(create.currencyType) === 4;

    stage(
      'create',
      translate('vouchers.createTitle'),
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
      translate('vouchers.createSummary', {
        code: create.code.trim(),
        amount: create.amount,
        currency: currencyLabel(create.currencyType, translate),
      }),
    );
  }

  function stageDeactivate() {
    if (!canManage) {
      reasonError('deactivate', translate('vouchers.deactivateAccessDenied'));
      return;
    }

    stage(
      'deactivate',
      translate('vouchers.deactivateTitle'),
      '/api/v1/operations/vouchers/deactivate',
      Boolean(deactivate.code.trim()) && reasonOk(deactivate.reason),
      { code: deactivate.code.trim(), reason: deactivate.reason.trim() },
      translate('vouchers.deactivateSummary', { code: deactivate.code.trim() }),
    );
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
      lookupError = isPermissionDeniedError(err) ? translate('common.insufficientRights') : err.code || err.message;
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
  <div class="panel-head"><h2>{$t('vouchers.title')}</h2></div>
  <p class="muted">{$t('vouchers.description')}</p>
</section>

<div class="op-grid">
  <section class="panel">
    <div class="panel-head"><h2>{$t('vouchers.createTitle')}</h2></div>
    {#if !canManage}
      <AccessDeniedNotice message={$t('vouchers.accessDenied')} />
    {:else}
      <div class="op-field">
        <label for="voucher-code">{$t('vouchers.code')}</label>
        <input id="voucher-code" bind:value={create.code} placeholder="SUMMER2026" style="text-transform: uppercase;" />
      </div>
      <div class="op-field">
        <label for="voucher-currency">{$t('vouchers.currency')}</label>
        <select id="voucher-currency" bind:value={create.currencyType}>
          {#each currencyTypes as ct}
            <option value={ct.value}>{$t(ct.key)}</option>
          {/each}
        </select>
      </div>
      {#if Number(create.currencyType) === 4}
        <div class="op-field">
          <label for="voucher-activity-type">{$t('vouchers.activityPointType')}</label>
          <input id="voucher-activity-type" type="number" min="0" bind:value={create.activityPointType} placeholder="0" />
        </div>
      {/if}
      <div class="op-field">
        <label for="voucher-amount">{$t('vouchers.amount')}</label>
        <input id="voucher-amount" type="number" min="1" bind:value={create.amount} placeholder="100" />
      </div>
      <div class="op-field">
        <label for="voucher-max-redemptions">{$t('vouchers.maxRedemptions')}</label>
        <input id="voucher-max-redemptions" type="number" min="1" bind:value={create.maxRedemptions} placeholder={$t('vouchers.unlimited')} />
      </div>
      <div class="op-field">
        <label for="voucher-expires">{$t('vouchers.expiresAt')}</label>
        <input id="voucher-expires" type="datetime-local" bind:value={create.expiresAt} />
      </div>
      <div class="op-field">
        <label for="voucher-reason">{$t('common.reasonRequired')}</label>
        <input id="voucher-reason" bind:value={create.reason} placeholder={$t('vouchers.reasonVoucher')} list="reason-history" />
      </div>
      <div class="op-actions">
        <button type="button" on:click={stageCreate} disabled={$ops.busyKeys.create}>{$t('common.run')}</button>
      </div>
      {#if $ops.errors.create}<p class="empty-state danger">{$ops.errors.create}</p>{/if}
      {#if $ops.results.create}
        <OpResult result={$ops.results.create} onCopy={copy} copyLabel={$t('common.copy')} />
      {/if}
    {/if}
  </section>

  <section class="panel">
    <div class="panel-head"><h2>{$t('vouchers.deactivateTitle')}</h2></div>
    {#if !canManage}
      <AccessDeniedNotice message={$t('vouchers.accessDenied')} />
    {:else}
      <div class="op-field">
        <label for="deactivate-code">{$t('vouchers.code')}</label>
        <input id="deactivate-code" bind:value={deactivate.code} placeholder="SUMMER2026" style="text-transform: uppercase;" />
      </div>
      <div class="op-field">
        <label for="deactivate-reason">{$t('common.reasonRequired')}</label>
        <input id="deactivate-reason" bind:value={deactivate.reason} placeholder={$t('vouchers.reasonDeactivate')} list="reason-history" />
      </div>
      <div class="op-actions">
        <button type="button" on:click={stageDeactivate} disabled={$ops.busyKeys.deactivate}>{$t('common.run')}</button>
      </div>
      {#if $ops.errors.deactivate}<p class="empty-state danger">{$ops.errors.deactivate}</p>{/if}
      {#if $ops.results.deactivate}
        <OpResult result={$ops.results.deactivate} onCopy={copy} copyLabel={$t('common.copy')} />
      {/if}
    {/if}
  </section>

  <section class="panel">
    <div class="panel-head"><h2>{$t('vouchers.lookupTitle')}</h2></div>
    <form class="toolbar" on:submit|preventDefault={lookup}>
      <input bind:value={lookupCode} placeholder={$t('vouchers.voucherCode')} style="text-transform: uppercase;" />
      <button type="submit" disabled={lookupLoading}>{$t('vouchers.inspect')}</button>
    </form>
    {#if lookupLoading}
      <p class="muted">{$t('pickerModal.loading')}</p>
    {:else if lookupError}
      <p class="empty-state danger">{lookupError}</p>
    {:else if lookupResult}
      {#if !lookupResult.exists}
        <p class="empty-state">{$t('vouchers.noVoucher')}</p>
      {:else}
        <div class="metric-grid compact">
          <StatCard label={$t('vouchers.status')} value={lookupResult.isActive ? $t('vouchers.active') : $t('vouchers.inactive')}>
            <Activity slot="icon" size={15} strokeWidth={2} aria-hidden="true" />
          </StatCard>
          <StatCard label={$t('vouchers.currencyCol')} value={currencyLabel(lookupResult.currencyType, $t)} accent>
            <Coins slot="icon" size={15} strokeWidth={2} aria-hidden="true" />
          </StatCard>
          <StatCard label={$t('vouchers.amountCol')} value={lookupResult.amount} accent>
            <Coins slot="icon" size={15} strokeWidth={2} aria-hidden="true" />
          </StatCard>
          <StatCard label={$t('vouchers.redemptions')}>
            <Hash slot="icon" size={15} strokeWidth={2} aria-hidden="true" />
            <span slot="value">{lookupResult.redemptionCount}{lookupResult.maxRedemptions ? ` / ${lookupResult.maxRedemptions}` : ''}</span>
          </StatCard>
          <StatCard label={$t('vouchers.expires')} value={lookupResult.expiresAt ? formatDate(lookupResult.expiresAt) : $t('vouchers.never')}>
            <Timer slot="icon" size={15} strokeWidth={2} aria-hidden="true" />
          </StatCard>
        </div>
      {/if}
    {/if}
  </section>
</div>

<ConfirmStagedModal {ops} eyebrow={$t('vouchers.confirmEyebrow')} />
