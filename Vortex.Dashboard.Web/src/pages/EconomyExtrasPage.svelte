<script>
  // The economy's smaller tables, each of which had no surface at all: LTD series with their raffle
  // outcomes, rentable spaces with who is actually renting them, the currency catalogue with how
  // much of each is held, and the builders-club ladder.
  import { onMount } from 'svelte';
  import { apiGet } from '../lib/api.js';
  import { createWriteOps } from '../lib/writeOps.js';
  import { hasDashboardCapability } from '../lib/permissions.js';
  import { CAPABILITIES } from '../lib/dashboardPermissions.js';
  import { identity } from '../lib/session.js';
  import ConfirmReasonModal from '../components/ConfirmReasonModal.svelte';
  import OpResult from '../components/OpResult.svelte';
  import { formatNumber, formatDate, formatDuration } from '../lib/format.js';
  import { isPermissionDeniedError } from '../lib/permissions.js';
  import { openPlayer } from '../lib/session.js';
  import AccessDeniedNotice from '../components/AccessDeniedNotice.svelte';
  import Drawer from '../components/Drawer.svelte';
  import PageHeader from '../components/PageHeader.svelte';
  import AssetImage from '../components/AssetImage.svelte';
  import EntityLink from '../components/EntityLink.svelte';
  import StatCard from '../components/StatCard.svelte';
  import Tabs from '../components/Tabs.svelte';
  import { Ticket, Store, Coins, Hammer } from '@lucide/svelte';
  import TableFilter from '../components/TableFilter.svelte';
  import { filterRows } from '../lib/tableView.js';
  import { t } from '../lib/i18n.js';

  let loading = $state(false);
  let forbidden = $state(false);
  let error = $state('');
  let data = $state(null);

  // Both grow without bound -- one row per LTD series ever run, one per rented space.
  let ltdQuery = $state('');
  let rentQuery = $state('');
  let ltdRows = $derived(data?.ltdSeries || []);
  let rentRows = $derived(data?.rentableSpaces || []);
  let ltdView = $derived(filterRows(ltdRows, ltdQuery));
  let rentView = $derived(filterRows(rentRows, rentQuery));

  const ops = createWriteOps(refresh);

  // These sections are independent jobs that were stacked vertically, so reaching the last one
  // meant scrolling past every other. Nothing here is read against anything else -- which is
  // both what makes tabs right and what would have made them wrong.
  let tab = $state('ltd');

  let canManage = $derived(hasDashboardCapability($identity, CAPABILITIES.opsContentManage));

  const emptyCurrency = () => ({
    id: 0,
    name: '',
    currencyType: 0,
    activityPointType: null,
    enabled: true,
    startingAmount: 0,
  });
  const emptyTier = () => ({ level: 1, furniLimit: 0 });
  const emptyTerms = () => ({
    furnitureId: 0,
    price: 0,
    currencyTypeId: 0,
    rentDurationSeconds: 3600,
    requiresHc: false,
  });

  let currencyForm = $state(null);
  let tierForm = $state(null);
  let termsForm = $state(null);

  // Placed-furniture ids have no picker of their own (the furniture picker searches definitions, not
  // placed items), so the id at least previews the space it points at.
  let termsPreviewUrl =
    $derived((data?.rentableSpaces || []).find((r) => r.furnitureId === Number(termsForm?.furnitureId))
      ?.iconUrl ?? null);

  async function refresh() {
    loading = true;
    error = '';
    forbidden = false;

    try {
      data = await apiGet('/api/v1/economy/extras');
    } catch (err) {
      if (isPermissionDeniedError(err)) {
        forbidden = true;
        data = null;
        return;
      }

      error = err.message;
      data = null;
    } finally {
      loading = false;
    }
  }

  function resultSummary(row) {
    if (!row.entriesByResult || row.entriesByResult.length === 0) return '—';
    return row.entriesByResult.map((e) => `${e.result}: ${formatNumber(e.count)}`).join(', ');
  }

  onMount(() => {
    void refresh();
  });
</script>

<section class="panel">
  <PageHeader title={$t('economyExtras.title')} description={$t('economyExtras.description')}>
    {#snippet actions()}
      <button type="button" onclick={refresh} disabled={loading} class="warning">{$t('common.refresh')}</button>
    {/snippet}
  </PageHeader>

  {#if loading}
    <p class="muted">{$t('common.loading')}</p>
  {:else if forbidden}
    <AccessDeniedNotice message={$t('economyExtras.accessDenied')} />
  {:else if error}
    <p class="empty-state danger" role="alert">{error}</p>
  {/if}
</section>

{#if data}
  <div class="metric-grid" style="margin-top: 12px;">
    <StatCard
      label={$t('economyExtras.ltdSeries')}
      value={formatNumber(data.totals.ltdSeries)}
      sub={$t('economyExtras.running', { count: formatNumber(data.totals.runningSeries) })}
    >
      {#snippet icon()}
        <Ticket size={15} strokeWidth={2} aria-hidden="true" />
      {/snippet}
    </StatCard>
    <StatCard
      label={$t('economyExtras.rentableSpaces')}
      value={formatNumber(data.totals.rentableSpaces)}
      sub={$t('economyExtras.rentedNow', { count: formatNumber(data.totals.rentedNow) })}
    >
      {#snippet icon()}
        <Store size={15} strokeWidth={2} aria-hidden="true" />
      {/snippet}
    </StatCard>
    <StatCard label={$t('economyExtras.currencies')} value={formatNumber(data.totals.currencies)}>
      {#snippet icon()}
        <Coins size={15} strokeWidth={2} aria-hidden="true" />
      {/snippet}
    </StatCard>
    <StatCard label={$t('economyExtras.buildersClubTiers')} value={formatNumber(data.totals.buildersClubTiers)}>
      {#snippet icon()}
        <Hammer size={15} strokeWidth={2} aria-hidden="true" />
      {/snippet}
    </StatCard>
  </div>

  <Tabs
    bind:active={tab}
    storageKey="economyExtras"
    tabs={[
      { id: 'ltd', label: $t('economyExtras.tabLtd'), icon: Ticket },
      { id: 'rentables', label: $t('economyExtras.tabRentables'), icon: Store },
      { id: 'currencies', label: $t('economyExtras.tabCurrencies'), icon: Coins },
      { id: 'builders', label: $t('economyExtras.tabBuilders'), icon: Hammer },
    ]}
  />

  {#if tab === 'ltd'}
  <section class="panel" style="margin-top: 12px;">
    <div class="panel-head"><h2>{$t('economyExtras.ltdTitle')}</h2></div>
    <TableFilter bind:query={ltdQuery} shown={ltdView.length} total={ltdRows.length} />
    <div class="table-wrap">
      <table>
        <thead>
          <tr>
            <th>{$t('economyExtras.colItem')}</th>
            <th>{$t('economyExtras.colSold')}</th>
            <th>{$t('economyExtras.colCost')}</th>
            <th>{$t('economyExtras.colWindow')}</th>
            <th>{$t('economyExtras.colState')}</th>
            <th>{$t('economyExtras.colEntries')}</th>
            <th>{$t('economyExtras.colEnds')}</th>
          </tr>
        </thead>
        <tbody>
          {#each ltdView as row}
            <tr>
              <td>
                <span class="cell">
                  <AssetImage src={row.iconUrl} alt={row.productName || ''} size={32} />
                  <span>{row.productName || `#${row.productId}`}</span>
                </span>
              </td>
              <td>{formatNumber(row.sold)} / {formatNumber(row.totalQuantity)}</td>
              <td>{formatNumber(row.costCredits)}</td>
              <td>{formatDuration(row.raffleWindowSeconds)}</td>
              <td>
                {#if row.running}
                  <span class="status-badge status-badge--ok">{$t('economyExtras.stateRunning')}</span>
                {:else if row.hasRaffleFinished}
                  <span class="status-badge status-badge--unknown">{$t('economyExtras.stateFinished')}</span>
                {:else}
                  <span class="status-badge status-badge--warn">{$t('economyExtras.stateIdle')}</span>
                {/if}
                {#if row.pendingEntries > 0}
                  <span class="status-badge status-badge--warn">
                    {$t('economyExtras.pending', { count: row.pendingEntries })}
                  </span>
                {/if}
              </td>
              <td>{resultSummary(row)}</td>
              <td>{row.endsAt ? formatDate(row.endsAt) : '—'}</td>
            </tr>
          {:else}
            <tr><td colspan="7" class="muted">{$t('economyExtras.noSeries')}</td></tr>
          {/each}
        </tbody>
      </table>
    </div>
  </section>
  {/if}

  {#if tab === 'rentables'}
  <section class="panel" style="margin-top: 12px;">
    <div class="panel-head">
      <h2>{$t('economyExtras.rentableTitle')}</h2>
      {#if canManage}
        <button type="button" class="ghost-button" onclick={() => (termsForm = emptyTerms())}>{$t('economyExtras.termsEditorTitle')}</button>
      {/if}
    </div>
    <p class="muted">{$t('economyExtras.rentableDescription')}</p>
    <TableFilter bind:query={rentQuery} shown={rentView.length} total={rentRows.length} />
    <div class="table-wrap">
      <table>
        <thead>
          <tr>
            <th>{$t('economyExtras.colFurniture')}</th>
            <th>{$t('economyExtras.colRenter')}</th>
            <th>{$t('economyExtras.colUntil')}</th>
            <th>{$t('economyExtras.colTerms')}</th>
          </tr>
        </thead>
        <tbody>
          {#each rentView as row}
            <tr>
              <td>
                <span class="cell">
                  <AssetImage src={row.iconUrl} alt={row.furnitureName || ''} size={32} />
                  <span>{row.furnitureName || `#${row.furnitureId}`}</span>
                </span>
              </td>
              <td>
                {#if row.renterId}
                  <EntityLink type="player" id={row.renterId} label={row.renterName} {openPlayer} />
                {:else}
                  <span class="muted">{$t('economyExtras.vacant')}</span>
                {/if}
              </td>
              <td>{row.rentedUntil ? formatDate(row.rentedUntil) : '—'}</td>
              <td>
                {#if row.hasTerms}
                  <span class="status-badge status-badge--ok">{$t('economyExtras.termsSet')}</span>
                {:else}
                  <span class="status-badge status-badge--warn">{$t('economyExtras.noTerms')}</span>
                {/if}
              </td>
            </tr>
          {:else}
            <tr><td colspan="4" class="muted">{$t('economyExtras.noRentals')}</td></tr>
          {/each}
        </tbody>
      </table>
    </div>
  </section>
  {/if}

  {#if tab === 'currencies'}
  <section class="panel" style="margin-top: 12px;">
    <div class="panel-head">
      <h2>{$t('economyExtras.currenciesTitle')}</h2>
      {#if canManage}
        <button type="button" class="success" onclick={() => (currencyForm = emptyCurrency())}>{$t('economyExtras.newCurrency')}</button>
      {/if}
    </div>
    <div class="table-wrap">
      <table>
        <thead>
          <tr>
            <th>{$t('economyExtras.colCurrency')}</th>
            <th>{$t('economyExtras.colType')}</th>
            <th>{$t('economyExtras.colPointType')}</th>
            <th>{$t('economyExtras.colEnabled')}</th>
            <th>{$t('economyExtras.colStarting')}</th>
            <th>{$t('economyExtras.colWallets')}</th>
            <th>{$t('economyExtras.colHeld')}</th>
            {#if canManage}<th></th>{/if}
          </tr>
        </thead>
        <tbody>
          {#each data.currencies || [] as row}
            <tr>
              <td>{row.name || `#${row.id}`}</td>
              <td>{row.currencyType}</td>
              <td>{row.activityPointType ?? '—'}</td>
              <td>{row.enabled ? $t('common.yes') : $t('common.no')}</td>
              <td>{formatNumber(row.startingAmount)}</td>
              <td>{formatNumber(row.walletRows)}</td>
              <td>{formatNumber(row.totalHeld)}</td>
              {#if canManage}
                <td class="row-actions">
                  <button type="button" class="ghost-button" onclick={() => (currencyForm = { ...row })}>
                    {$t('economyExtras.edit')}
                  </button>
                </td>
              {/if}
            </tr>
          {:else}
            <tr><td colspan="7" class="muted">{$t('economyExtras.noCurrencies')}</td></tr>
          {/each}
        </tbody>
      </table>
    </div>
  </section>

{#if canManage && currencyForm}
  <Drawer title={currencyForm.id ? $t('economyExtras.updateCurrency') : $t('economyExtras.addCurrency')} eyebrow={$t('economyExtras.currenciesTitle')} onclose={() => (currencyForm = null)}>
        <form id="currency-form"
          class="inline-form"
          onsubmit={(event) => {
            event.preventDefault();
            ops.ask(
              '/api/v1/operations/content/currencies',
              {
                currencyId: Number(currencyForm.id) || 0,
                name: currencyForm.name,
                currencyType: Number(currencyForm.currencyType) || 0,
                activityPointType:
                  currencyForm.activityPointType === null || currencyForm.activityPointType === ''
                    ? null
                    : Number(currencyForm.activityPointType),
                enabled: Boolean(currencyForm.enabled),
                startingAmount: Number(currencyForm.startingAmount) || 0,
              },
              currencyForm.id ? $t('economyExtras.updateCurrency') : $t('economyExtras.addCurrency'),
              $t('economyExtras.saveCurrencySummary', { name: currencyForm.name })
            );
          }}
        >
          <label>
            {$t('economyExtras.colCurrency')}
            <input autocomplete="off" spellcheck="false" bind:value={currencyForm.name} />
          </label>
          <label>
            {$t('economyExtras.colType')}
            <input autocomplete="off" spellcheck="false" type="number" bind:value={currencyForm.currencyType} min="0" />
          </label>
          <label>
            {$t('economyExtras.colPointType')}
            <input autocomplete="off" spellcheck="false" type="number" bind:value={currencyForm.activityPointType} />
          </label>
          <label>
            {$t('economyExtras.colStarting')}
            <input autocomplete="off" spellcheck="false" type="number" bind:value={currencyForm.startingAmount} min="0" />
          </label>
          <label class="check">
            <input autocomplete="off" spellcheck="false" type="checkbox" bind:checked={currencyForm.enabled} />
            {$t('economyExtras.colEnabled')}
          </label>
          <button class="success" type="submit" disabled={!currencyForm.name.trim()}>
            {currencyForm.id ? $t('economyExtras.updateCurrency') : $t('economyExtras.addCurrency')}
          </button>
        </form>

    {#if $ops.error}<p class="empty-state danger" role="alert">{$ops.error}</p>{/if}
    {#if $ops.result}<OpResult result={$ops.result} />{/if}

    {#snippet actions()}
      <button class="success" type="submit" form="currency-form" disabled={!currencyForm.name.trim()}>{currencyForm.id ? $t('economyExtras.updateCurrency') : $t('economyExtras.addCurrency')}</button>
      <button type="button" class="ghost-button" onclick={() => (currencyForm = null)}>{$t('common.cancel')}</button>
    {/snippet}
  </Drawer>
{/if}

{#if canManage && tierForm}
  <Drawer title={$t('economyExtras.buildersClubTitle')} eyebrow={$t('economyExtras.editorTitle')} onclose={() => (tierForm = null)}>
        <form id="tier-form"
          class="inline-form"
          onsubmit={(event) => {
            event.preventDefault();
            ops.ask(
              '/api/v1/operations/content/builders-club',
              { level: Number(tierForm.level), furniLimit: Number(tierForm.furniLimit) },
              $t('economyExtras.saveTier'),
              $t('economyExtras.saveTierSummary', { level: tierForm.level, limit: tierForm.furniLimit })
            );
          }}
        >
          <label>
            {$t('economyExtras.colLevel')}
            <input autocomplete="off" spellcheck="false" type="number" bind:value={tierForm.level} min="1" />
          </label>
          <label>
            {$t('economyExtras.colFurniLimit')}
            <input autocomplete="off" spellcheck="false" type="number" bind:value={tierForm.furniLimit} min="0" />
          </label>
        </form>

    {#if $ops.error}<p class="empty-state danger" role="alert">{$ops.error}</p>{/if}
    {#if $ops.result}<OpResult result={$ops.result} />{/if}

    {#snippet actions()}
      <button type="submit" form="tier-form">{$t('economyExtras.saveTier')}</button>
      <button type="button" class="ghost-button" onclick={() => (tierForm = null)}>{$t('common.cancel')}</button>
    {/snippet}
  </Drawer>
{/if}

{#if canManage && termsForm}
  <Drawer title={$t('economyExtras.termsEditorTitle')} eyebrow={$t('economyExtras.editorTitle')} onclose={() => (termsForm = null)}>
        <form id="terms-form"
          class="inline-form"
          onsubmit={(event) => {
            event.preventDefault();
            ops.ask(
              '/api/v1/operations/content/rentable-terms',
              {
                furnitureId: Number(termsForm.furnitureId),
                price: Number(termsForm.price) || 0,
                currencyTypeId: Number(termsForm.currencyTypeId),
                rentDurationSeconds: Number(termsForm.rentDurationSeconds),
                requiresHc: Boolean(termsForm.requiresHc),
              },
              $t('economyExtras.saveTerms'),
              $t('economyExtras.saveTermsSummary', { id: termsForm.furnitureId })
            );
          }}
        >
          <label>
            {$t('economyExtras.colFurniture')}
            <span class="cell">
              <input autocomplete="off" spellcheck="false" type="number" bind:value={termsForm.furnitureId} min="1" />
              <AssetImage src={termsPreviewUrl} alt="" size={32} />
            </span>
          </label>
          <label>
            {$t('economyExtras.price')}
            <input autocomplete="off" spellcheck="false" type="number" bind:value={termsForm.price} min="0" />
          </label>
          <label>
            {$t('economyExtras.colCurrency')}
            <select bind:value={termsForm.currencyTypeId}>
              <option value={0}>{$t('economyExtras.pickCurrency')}</option>
              {#each data.currencies || [] as currency}
                <option value={currency.id}>{currency.name || `#${currency.id}`}</option>
              {/each}
            </select>
          </label>
          <label>
            {$t('economyExtras.durationSeconds')}
            <input autocomplete="off" spellcheck="false" type="number" bind:value={termsForm.rentDurationSeconds} min="1" />
          </label>
          <label class="check">
            <input autocomplete="off" spellcheck="false" type="checkbox" bind:checked={termsForm.requiresHc} />
            {$t('economyExtras.requiresHc')}
          </label>
        </form>

    {#if $ops.error}<p class="empty-state danger" role="alert">{$ops.error}</p>{/if}
    {#if $ops.result}<OpResult result={$ops.result} />{/if}

    {#snippet actions()}
      <button type="submit" form="terms-form" disabled={!termsForm.furnitureId || !termsForm.currencyTypeId || !termsForm.rentDurationSeconds}>{$t('economyExtras.saveTerms')}</button>
      <button type="button" class="ghost-button" onclick={() => (termsForm = null)}>{$t('common.cancel')}</button>
    {/snippet}
  </Drawer>
{/if}

  {/if}

  {#if tab === 'builders'}
  <section class="panel" style="margin-top: 12px;">
    <div class="panel-head">
      <h2>{$t('economyExtras.buildersClubTitle')}</h2>
      {#if canManage}
        <button type="button" class="ghost-button" onclick={() => (tierForm = emptyTier())}>{$t('economyExtras.saveTier')}</button>
      {/if}
    </div>
    <div class="table-wrap">
      <table>
        <thead>
          <tr>
            <th>{$t('economyExtras.colLevel')}</th>
            <th>{$t('economyExtras.colFurniLimit')}</th>
            {#if canManage}<th></th>{/if}
          </tr>
        </thead>
        <tbody>
          {#each data.buildersClub || [] as row}
            <tr>
              <td>{row.level}</td>
              <td>{formatNumber(row.furniLimit)}</td>
              {#if canManage}
                <td class="row-actions">
                  <button type="button" class="ghost-button" onclick={() => (tierForm = { ...row })}>
                    {$t('economyExtras.edit')}
                  </button>
                  <button
                    type="button"
                    class="ghost-button danger"
                    onclick={() =>
                      ops.ask(
                        '/api/v1/operations/content/builders-club/delete',
                        { tierId: row.id },
                        $t('economyExtras.deleteTier'),
                        $t('economyExtras.deleteTierSummary', { level: row.level })
                      )}
                  >
                    {$t('economyExtras.delete')}
                  </button>
                </td>
              {/if}
            </tr>
          {:else}
            <tr><td colspan="2" class="muted">{$t('economyExtras.noTiers')}</td></tr>
          {/each}
        </tbody>
      </table>
    </div>
  </section>
  {/if}
{/if}

<ConfirmReasonModal
  open={Boolean($ops.pending)}
  title={$ops.pending?.title ?? ''}
  changes={$ops.pending?.changes ?? []}
  noteOnly={$ops.pending?.noteOnly ?? false}
  summary={$ops.pending?.summary ?? ''}
  confirmLabel={$ops.pending?.title ?? $t('common.confirm')}
  busy={$ops.busy}
  error={$ops.error}
  onconfirm={ops.confirm}
  oncancel={() => ops.cancel()}
/>

<style>
  .inline-form label.check {
    flex-direction: row;
    align-items: center;
    gap: 6px;
  }

  .cell {
    display: inline-flex;
    align-items: center;
    gap: 8px;
  }
</style>
