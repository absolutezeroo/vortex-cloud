<script lang="ts">
  import ConfirmStagedModal from '../components/ConfirmStagedModal.svelte';
  import PageHeader from '../components/PageHeader.svelte';
  import OpResult from '../components/OpResult.svelte';
  import { isPermissionDeniedError, hasDashboardCapability } from '../lib/permissions';
  import { apiGet, describeApiError } from '../lib/api';
  import { createWriteOps } from '../lib/writeOps';
  import { compactCorrelation, formatDate } from '../lib/format';
  import { CAPABILITIES } from '../lib/dashboardPermissions';
  import { positive, nonNegative } from '../lib/validation';
  import AccessDeniedNotice from '../components/AccessDeniedNotice.svelte';
  import StatCard from '../components/StatCard.svelte';
  import { Activity, Coins, Hash, Timer } from '@lucide/svelte';
  import { identity } from '../lib/session';
  import { t, translate } from '../lib/i18n';
  import type { Translator } from '../lib/i18n';
  import type { VoucherSnapshot } from '../lib/apiTypes';

  /** Number fields bind to inputs that hand back a string while being typed. */
  type Num = number | string;

  /** The create form. expiresAt is a datetime-local value, sent as an ISO instant. */
  type CreateForm = {
    code: string;
    currencyType: Num;
    activityPointType: Num;
    amount: Num;
    maxRedemptions: Num;
    expiresAt: string;
  };

  const currencyTypes = [
    { value: 1, key: 'vouchers.currencyCredits' },
    { value: 2, key: 'vouchers.currencySilver' },
    { value: 3, key: 'vouchers.currencyEmeralds' },
    { value: 4, key: 'vouchers.currencyActivityPoints' },
  ];

  function currencyLabel(value: Num, translator: Translator) {
    const entry = currencyTypes.find((c) => c.value === Number(value));
    return entry ? translator(entry.key) : String(value);
  }

  let create = $state<CreateForm>({
    code: '',
    currencyType: 1,
    activityPointType: '',
    amount: '',
    maxRedemptions: '',
    expiresAt: '',
  });
  let deactivate = $state({ code: '' });
  let lookupCode = $state('');
  let lookupResult = $state<VoucherSnapshot | null>(null);
  let lookupError = $state('');
  let lookupLoading = $state(false);

  // Every write is staged here and confirmed in the dialog below before it is posted. createWriteOps
  // owns that cycle -- posting, remembering the audited reason, and tracking each form's busy state,
  // error and result under its own key -- so the page only describes what each button writes.
  const ops = createWriteOps();

  let canManage = $derived(hasDashboardCapability($identity, CAPABILITIES.opsManageVouchers));

  function reasonError(id: string, message: string) {
    ops.fail(id, message);
  }

  const stage = (
    id: string,
    title: string,
    endpoint: string,
    valid: boolean,
    body: Record<string, unknown>,
    summary: string,
  ) =>
    ops.ask(endpoint, body, title, summary, {
      key: id,
      valid,
      invalidMessage: translate('vouchers.fillFields'),
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
        (!needsActivityType || nonNegative(create.activityPointType)),
      {
        code: create.code.trim(),
        currencyType: Number(create.currencyType),
        activityPointType: needsActivityType ? Number(create.activityPointType) : null,
        amount: Number(create.amount),
        maxRedemptions: create.maxRedemptions ? Number(create.maxRedemptions) : null,
        expiresAt: create.expiresAt ? new Date(create.expiresAt).toISOString() : null,
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
      Boolean(deactivate.code.trim()),
      { code: deactivate.code.trim() },
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
      lookupResult = await apiGet<VoucherSnapshot>(
        `/api/v1/operations/vouchers/${encodeURIComponent(lookupCode.trim())}`,
      );
    } catch (err) {
      lookupError = isPermissionDeniedError(err)
        ? translate('common.insufficientRights')
        : describeApiError(err);
    } finally {
      lookupLoading = false;
    }
  }

  async function copy(value: string) {
    try {
      await navigator.clipboard.writeText(value || '');
    } catch {
      // Clipboard is best-effort.
    }
  }
</script>

<section class="panel">
  <PageHeader title={$t('vouchers.title')} description={$t('vouchers.description')} />
</section>

<div class="op-grid">
  <section class="panel">
    <div class="panel-head"><h2>{$t('vouchers.createTitle')}</h2></div>
    {#if !canManage}
      <AccessDeniedNotice message={$t('vouchers.accessDenied')} />
    {:else}
      <div class="op-field">
        <label for="voucher-code">{$t('vouchers.code')}</label>
        <input autocomplete="off" spellcheck="false" id="voucher-code" bind:value={create.code} placeholder="SUMMER2026" style="text-transform: uppercase;" />
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
          <input autocomplete="off" spellcheck="false" id="voucher-activity-type" type="number" min="0" bind:value={create.activityPointType} placeholder="0" />
        </div>
      {/if}
      <div class="op-field">
        <label for="voucher-amount">{$t('vouchers.amount')}</label>
        <input autocomplete="off" spellcheck="false" id="voucher-amount" type="number" min="1" bind:value={create.amount} placeholder="100" />
      </div>
      <div class="op-field">
        <label for="voucher-max-redemptions">{$t('vouchers.maxRedemptions')}</label>
        <input autocomplete="off" spellcheck="false" id="voucher-max-redemptions" type="number" min="1" bind:value={create.maxRedemptions} placeholder={$t('vouchers.unlimited')} />
      </div>
      <div class="op-field">
        <label for="voucher-expires">{$t('vouchers.expiresAt')}</label>
        <input autocomplete="off" spellcheck="false" id="voucher-expires" type="datetime-local" bind:value={create.expiresAt} />
      </div>
      <div class="op-actions">
        <button class="success" type="button" onclick={stageCreate} disabled={$ops.busyKeys.create}>{$t('common.run')}</button>
      </div>
      {#if $ops.errors.create}<p class="empty-state danger" role="alert">{$ops.errors.create}</p>{/if}
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
        <input autocomplete="off" spellcheck="false" id="deactivate-code" bind:value={deactivate.code} placeholder="SUMMER2026" style="text-transform: uppercase;" />
      </div>
      <div class="op-actions">
        <button type="button" onclick={stageDeactivate} disabled={$ops.busyKeys.deactivate}>{$t('common.run')}</button>
      </div>
      {#if $ops.errors.deactivate}<p class="empty-state danger" role="alert">{$ops.errors.deactivate}</p>{/if}
      {#if $ops.results.deactivate}
        <OpResult result={$ops.results.deactivate} onCopy={copy} copyLabel={$t('common.copy')} />
      {/if}
    {/if}
  </section>

  <section class="panel">
    <div class="panel-head"><h2>{$t('vouchers.lookupTitle')}</h2></div>
</section>

<section class="panel">
    <form class="toolbar" onsubmit={(event) => { event.preventDefault(); lookup(); }}>
      <input autocomplete="off" spellcheck="false" bind:value={lookupCode} placeholder={$t('vouchers.voucherCode')} style="text-transform: uppercase;" />
      <button type="submit" disabled={lookupLoading}>{$t('vouchers.inspect')}</button>
    </form>
    {#if lookupLoading}
      <p class="muted">{$t('pickerModal.loading')}</p>
    {:else if lookupError}
      <p class="empty-state danger" role="alert">{lookupError}</p>
    {:else if lookupResult}
      {@const voucher = lookupResult}
      {#if !voucher.exists}
        <p class="empty-state">{$t('vouchers.noVoucher')}</p>
      {:else}
        <div class="metric-grid compact">
          <StatCard label={$t('vouchers.status')} value={voucher.isActive ? $t('vouchers.active') : $t('vouchers.inactive')}>
            {#snippet icon()}
              <Activity size={15} strokeWidth={2} aria-hidden="true" />
            {/snippet}
          </StatCard>
          <StatCard label={$t('vouchers.currencyCol')} value={currencyLabel(voucher.currencyType, $t)} accent>
            {#snippet icon()}
              <Coins size={15} strokeWidth={2} aria-hidden="true" />
            {/snippet}
          </StatCard>
          <StatCard label={$t('vouchers.amountCol')} value={voucher.amount} accent>
            {#snippet icon()}
              <Coins size={15} strokeWidth={2} aria-hidden="true" />
            {/snippet}
          </StatCard>
          <StatCard label={$t('vouchers.redemptions')}>
            {#snippet icon()}
              <Hash size={15} strokeWidth={2} aria-hidden="true" />
            {/snippet}
            {#snippet value()}
              <span>{voucher.redemptionCount}{voucher.maxRedemptions ? ` / ${voucher.maxRedemptions}` : ''}</span>
            {/snippet}
          </StatCard>
          <StatCard label={$t('vouchers.expires')} value={voucher.expiresAt ? formatDate(voucher.expiresAt) : $t('vouchers.never')}>
            {#snippet icon()}
              <Timer size={15} strokeWidth={2} aria-hidden="true" />
            {/snippet}
          </StatCard>
        </div>
      {/if}
    {/if}
  </section>
</div>

<ConfirmStagedModal {ops} eyebrow={$t('vouchers.confirmEyebrow')} />
