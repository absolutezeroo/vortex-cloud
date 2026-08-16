<script>
  import { onMount } from 'svelte';
  import { Dices, Link2, Plus, RefreshCw, Trash2 } from '@lucide/svelte';
  import OpResult from '../components/OpResult.svelte';
  import EmptyState from '../components/EmptyState.svelte';
  import AssetImage from '../components/AssetImage.svelte';
  import PickerModal from '../components/PickerModal.svelte';
  import AccessDeniedNotice from '../components/AccessDeniedNotice.svelte';
  import ConfirmReasonModal from '../components/ConfirmReasonModal.svelte';
  import { apiGet } from '../lib/api.js';
  import { createWriteOps } from '../lib/writeOps.js';
  import Tabs from '../components/Tabs.svelte';
  import Pagination from '../components/Pagination.svelte';
  import { formatNumber } from '../lib/format.js';
  import { isPermissionDeniedError, hasDashboardCapability } from '../lib/permissions.js';
  import { CAPABILITIES } from '../lib/dashboardPermissions.js';
  import { identity } from '../lib/session.js';
  import { t } from '../lib/i18n.js';

  // Only floor/wall entries name a furniture definition; effect and club prizes carry their target
  // in extraParam instead, so the form swaps which field it asks for.
  const FURNITURE_TYPES = ['Floor', 'Wall'];

  let pools = [];
  let entries = [];
  let totals = [];
  let bindings = [];

  let tab = 'pools';

  // The bindings table is the one list here that grows with the hotel rather than with the operator:
  // every crackable furni is a row, which is a four-figure scroll on a real database. Filter first --
  // an operator arrives knowing which furni they are looking for -- and page what is left. Both are
  // client-side because the whole set already arrived with the page.
  const BINDINGS_PAGE_SIZE = 25;
  let bindingQuery = '';
  let bindingPool = '';
  let bindingPage = 1;

  $: filteredBindings = bindings.filter((b) => {
    if (bindingPool && b.pool !== bindingPool) return false;
    if (!bindingQuery.trim()) return true;

    const needle = bindingQuery.trim().toLowerCase();

    return (
      (b.furnitureName ?? '').toLowerCase().includes(needle) ||
      (b.furnitureLogic ?? '').toLowerCase().includes(needle) ||
      String(b.furnitureDefinitionId).includes(needle)
    );
  });

  $: bindingPageCount = Math.max(1, Math.ceil(filteredBindings.length / BINDINGS_PAGE_SIZE));
  // Narrowing the filter can strand the operator past the last page; walk them back to it.
  $: if (bindingPage > bindingPageCount) bindingPage = bindingPageCount;
  $: pagedBindings = filteredBindings.slice(
    (bindingPage - 1) * BINDINGS_PAGE_SIZE,
    bindingPage * BINDINGS_PAGE_SIZE
  );
  $: bindingPools = [...new Set(bindings.map((b) => b.pool))].sort();

  let productTypes = [];
  let stats = null;
  let statsDays = 7;

  let loading = false;
  let denied = false;
  let error = '';

  let selectedPoolId = null;
  let newPool = emptyPool();
  let newEntry = emptyEntry();
  let newBinding = emptyBinding();

  // Typing a definition id by hand means looking it up somewhere else first; the furniture
  // picker is what every other page uses for this.
  let picking = null;

  // Every write is staged through the shared reason modal; createWriteOps posts it, remembers the
  // reason (which the hand-rolled version here used to drop) and refreshes both reads.
  const ops = createWriteOps(async () => {
    newPool = emptyPool();
    newEntry = emptyEntry();
    newBinding = emptyBinding();
    await load();
    await loadStats();
  });

  function emptyPool() {
    return { code: '', name: '', variants: '', notes: '', enabled: true };
  }

  function emptyBinding() {
    return { furnitureDefinitionId: '', poolCode: '', hitsRequired: 1, enabled: true };
  }

  function emptyEntry() {
    return {
      variant: '',
      productType: 'Floor',
      furnitureDefinitionId: '',
      extraParam: '',
      weight: 10,
      enabled: true,
    };
  }

  $: canManage = hasDashboardCapability($identity, CAPABILITIES.opsPrizePoolsManage);
  $: selectedPool = pools.find((p) => p.id === selectedPoolId) ?? pools[0] ?? null;
  $: poolEntries = selectedPool ? entries.filter((e) => e.poolId === selectedPool.id) : [];
  $: poolStats = stats?.pools?.find((p) => p.pool === selectedPool?.code) ?? null;

  // The share a draw really sees: an entry competes with the pool entries that can be drawn beside
  // it, which for a variantless one is every variant of the pool. The server groups them the same
  // way the picker does, so this only reads the number back.
  function expectedShare(entry) {
    if (!entry.enabled) return null;
    const group = totals.find((g) => g.poolId === entry.poolId && g.variant === entry.variant);
    if (!group || group.totalWeight <= 0) return null;
    return Math.round((entry.weight / group.totalWeight) * 1000) / 10;
  }

  function drawsOf(entry) {
    return poolStats?.entries.find((e) => e.entryId === entry.id)?.draws ?? 0;
  }

  function actualShare(entry) {
    if (!poolStats || poolStats.draws <= 0) return null;
    return Math.round((drawsOf(entry) / poolStats.draws) * 1000) / 10;
  }

  function entryTarget(entry) {
    if (FURNITURE_TYPES.includes(entry.productType)) {
      return entry.furnitureName ?? `#${entry.furnitureDefinitionId}`;
    }
    return entry.extraParam || '—';
  }

  function percent(value) {
    return value === null ? '—' : `${value}%`;
  }

  async function load() {
    loading = true;
    error = '';
    try {
      const data = await apiGet('/api/prize-pools');
      pools = data.pools?.items ?? [];
      entries = data.entries?.items ?? [];
      totals = data.totals ?? [];
      bindings = data.bindings?.items ?? [];
      productTypes = data.productTypes ?? [];
      if (selectedPoolId === null && pools.length > 0) selectedPoolId = pools[0].id;
      denied = false;
    } catch (e) {
      if (isPermissionDeniedError(e)) denied = true;
      else error = e?.message ?? String(e);
    } finally {
      loading = false;
    }
  }

  async function loadStats() {
    try {
      stats = await apiGet(`/api/prize-pools/stats?days=${statsDays}`);
    } catch {
      stats = null;
    }
  }

  const stage = (title, summary, endpoint, body, danger = false) =>
    ops.ask(endpoint, body, title, summary, { danger });

  onMount(async () => {
    await load();
    await loadStats();
  });
</script>

<section class="panel">
  <div class="panel-head">
    <h2><Dices size={17} strokeWidth={2} aria-hidden="true" /> {$t('prizePools.title')}</h2>
    <div class="toolbar">
      <button type="button" class="ghost-button" on:click={load} disabled={loading}>
        {$t('common.refresh')}
      </button>
      {#if canManage}
        <button
          type="button"
          class="ghost-button"
          on:click={() =>
            stage(
              $t('prizePools.reload'),
              $t('prizePools.reloaded'),
              '/api/operations/prize-pools/reload',
              {}
            )}
        >
          <RefreshCw size={14} strokeWidth={2} aria-hidden="true" />
          {$t('prizePools.reload')}
        </button>
      {/if}
    </div>
  </div>
  <p class="muted">{$t('prizePools.description')}</p>
</section>

{#if denied}
  <AccessDeniedNotice message={$t('prizePools.accessDenied')} />
{:else}
  {#if error}
    <EmptyState kind="error" message={error} />
  {/if}
  <OpResult result={$ops.result} />

  <Tabs
    bind:active={tab}
    storageKey="prizePools"
    tabs={[
      { id: 'pools', label: $t('prizePools.tabPools'), icon: Dices, count: pools.length },
      { id: 'bindings', label: $t('prizePools.tabBindings'), icon: Link2, count: bindings.length },
    ]}
  />

  {#if tab === 'pools'}
  <section class="panel">
    <div class="panel-head">
      <h2>{$t('prizePools.poolsHeading')}</h2>
    </div>
    <p class="muted">{$t('prizePools.poolsHint')} {$t('prizePools.variantsHint')}</p>

    {#if loading}
      <EmptyState kind="loading" />
    {:else if pools.length === 0}
      <EmptyState message={$t('prizePools.noPools')} />
    {:else}
      <div class="inline-list">
        {#each pools as pool (pool.id)}
          <button
            type="button"
            class="btn btn-ghost btn-sm"
            aria-pressed={selectedPool?.id === pool.id}
            on:click={() => (selectedPoolId = pool.id)}
          >
            {pool.name}
            {#if pool.isBuiltIn}
              <span class="status-badge status-badge--ok">{$t('prizePools.builtIn')}</span>
            {/if}
            {#if !pool.enabled}
              <span class="status-badge status-badge--warn">{$t('prizePools.enabled')}</span>
            {/if}
          </button>
        {/each}
      </div>
    {/if}

    {#if canManage}
      <div class="op-grid">
        <div class="op-field">
          <label for="pool-code">{$t('prizePools.code')}</label>
          <input id="pool-code" bind:value={newPool.code} />
        </div>
        <div class="op-field">
          <label for="pool-name">{$t('prizePools.name')}</label>
          <input id="pool-name" bind:value={newPool.name} />
        </div>
        <div class="op-field">
          <label for="pool-variants">{$t('prizePools.variants')}</label>
          <input id="pool-variants" bind:value={newPool.variants} />
        </div>
        <div class="op-field">
          <label for="pool-notes">{$t('prizePools.notes')}</label>
          <input id="pool-notes" bind:value={newPool.notes} />
        </div>
      </div>
      <div class="op-actions">
        <button
          type="button"
          class="btn btn-primary btn-sm"
          disabled={!newPool.code.trim() || !newPool.name.trim()}
          on:click={() =>
            stage($t('prizePools.newPool'), newPool.code, '/api/operations/prize-pools', {
              ...newPool,
            })}
        >
          <Plus size={14} strokeWidth={2} aria-hidden="true" />
          {$t('prizePools.newPool')}
        </button>
      </div>
    {/if}
  </section>

  {#if selectedPool}
    <section class="panel">
      <div class="panel-head">
        <h2>{$t('prizePools.entriesHeading')} — {selectedPool.name}</h2>
        <div class="toolbar">
          <select bind:value={statsDays} on:change={loadStats}>
            {#each [1, 7, 30, 90] as days}
              <option value={days}>{days}</option>
            {/each}
          </select>
        </div>
      </div>
      <p class="muted">{$t('prizePools.entriesHint')}</p>

      <div class="stats">
        <div class="stat">
          <span class="stat-label">{$t('prizePools.entriesHeading')}</span>
          <strong>{formatNumber(poolEntries.length)}</strong>
        </div>
        <div class="stat">
          <span class="stat-label">{$t('prizePools.draws')}</span>
          <strong>{formatNumber(poolStats?.draws ?? 0)}</strong>
        </div>
      </div>

      {#if poolEntries.length === 0}
        <EmptyState message={$t('prizePools.noEntries')} />
      {:else}
        <div class="table-wrap">
          <table>
            <thead>
              <tr>
                <th>{$t('prizePools.productType')}</th>
                <th>{$t('prizePools.variant')}</th>
                <th>{$t('prizePools.weight')}</th>
                <th>{$t('prizePools.expected')}</th>
                <th>{$t('prizePools.actual')}</th>
                <th>{$t('prizePools.draws')}</th>
                {#if canManage}<th></th>{/if}
              </tr>
            </thead>
            <tbody>
              {#each poolEntries as entry (entry.id)}
                <tr>
                  <td>
                    <span class="cell-row">
                      <AssetImage
                        src={entry.furnitureIconUrl}
                        alt={entry.furnitureName ?? ''}
                        size={32}
                      />
                      <span class="cell-text">
                        <strong class="truncate">{entryTarget(entry)}</strong>
                        <span class="muted">{entry.productType}</span>
                      </span>
                    </span>
                    {#if !entry.enabled}
                      <span class="status-badge status-badge--warn"
                        >{$t('prizePools.enabled')}</span
                      >
                    {/if}
                  </td>
                  <td>{entry.variant || '—'}</td>
                  <td>{formatNumber(entry.weight)}</td>
                  <td>{percent(expectedShare(entry))}</td>
                  <td>{percent(actualShare(entry))}</td>
                  <td>{formatNumber(drawsOf(entry))}</td>
                  {#if canManage}
                    <td>
                      <button
                        type="button"
                        class="icon-btn"
                        title={$t('prizePools.remove')}
                        on:click={() =>
                          stage(
                            $t('prizePools.remove'),
                            entryTarget(entry),
                            '/api/operations/prize-pools/entries/delete',
                            { entryId: entry.id },
                            true
                          )}
                      >
                        <Trash2 size={14} strokeWidth={2} aria-hidden="true" />
                      </button>
                    </td>
                  {/if}
                </tr>
              {/each}
            </tbody>
          </table>
        </div>
      {/if}

      {#if canManage}
        <div class="op-grid">
          <div class="op-field">
            <label for="entry-type">{$t('prizePools.productType')}</label>
            <select id="entry-type" bind:value={newEntry.productType}>
              {#each productTypes as type}
                <option value={type}>{type}</option>
              {/each}
            </select>
          </div>
          {#if FURNITURE_TYPES.includes(newEntry.productType)}
            <div class="op-field">
              <label for="entry-definition">{$t('prizePools.furnitureDefinitionId')}</label>
              <div class="op-pick">
                <input
                  id="entry-definition"
                  type="number"
                  bind:value={newEntry.furnitureDefinitionId}
                />
                <button
                  type="button"
                  class="ghost-button"
                  on:click={() => (picking = 'entry')}>{$t('prizePools.pick')}</button
                >
              </div>
            </div>
          {:else}
            <div class="op-field">
              <label for="entry-extra">{$t('prizePools.extraParam')}</label>
              <input id="entry-extra" bind:value={newEntry.extraParam} />
            </div>
          {/if}
          <div class="op-field">
            <label for="entry-variant">{$t('prizePools.variant')}</label>
            <input id="entry-variant" bind:value={newEntry.variant} />
          </div>
          <div class="op-field">
            <label for="entry-weight">{$t('prizePools.weight')}</label>
            <input id="entry-weight" type="number" min="1" bind:value={newEntry.weight} />
          </div>
        </div>
        <div class="op-actions">
          <button
            type="button"
            class="btn btn-primary btn-sm"
            on:click={() =>
              stage(
                $t('prizePools.newEntry'),
                selectedPool.code,
                '/api/operations/prize-pools/entries',
                {
                  ...newEntry,
                  poolCode: selectedPool.code,
                  furnitureDefinitionId: Number(newEntry.furnitureDefinitionId) || 0,
                  weight: Number(newEntry.weight) || 1,
                }
              )}
          >
            <Plus size={14} strokeWidth={2} aria-hidden="true" />
            {$t('prizePools.newEntry')}
          </button>
        </div>
      {/if}
    </section>
  {/if}
  {/if}

  {#if tab === 'bindings'}
  <section class="panel">
    <div class="panel-head">
      <h2>
        <Link2 size={17} strokeWidth={2} aria-hidden="true" />
        {$t('prizePools.bindingsHeading')}
      </h2>
    </div>
    <p class="muted">{$t('prizePools.bindingsHint')}</p>

    {#if bindings.length === 0}
      <EmptyState message={$t('prizePools.noBindings')} />
    {:else}
      <div class="toolbar-grid">
        <label>
          {$t('prizePools.searchBindings')}
          <input
            type="search"
            bind:value={bindingQuery}
            on:input={() => (bindingPage = 1)}
            placeholder={$t('prizePools.searchBindingsPlaceholder')}
          />
        </label>
        <label>
          {$t('prizePools.pool')}
          <select bind:value={bindingPool} on:change={() => (bindingPage = 1)}>
            <option value="">{$t('prizePools.allPools')}</option>
            {#each bindingPools as pool}
              <option value={pool}>{pool}</option>
            {/each}
          </select>
        </label>
      </div>

      <div class="table-wrap">
        <table>
          <thead>
            <tr>
              <th>{$t('prizePools.furnitureDefinitionId')}</th>
              <th>{$t('prizePools.pool')}</th>
              <th>{$t('prizePools.hitsRequired')}</th>
              {#if canManage}<th></th>{/if}
            </tr>
          </thead>
          <tbody>
            {#each pagedBindings as binding (binding.id)}
              <tr>
                <td>
                  <span class="cell-row">
                    <AssetImage
                      src={binding.furnitureIconUrl}
                      alt={binding.furnitureName ?? ''}
                      size={32}
                    />
                    <span class="cell-text">
                      <strong class="truncate"
                        >{binding.furnitureName ?? `#${binding.furnitureDefinitionId}`}</strong
                      >
                      <span class="muted">{binding.furnitureLogic ?? '—'}</span>
                    </span>
                  </span>
                </td>
                <td><span class="status-badge">{binding.pool}</span></td>
                <td>{formatNumber(binding.hitsRequired)}</td>
                {#if canManage}
                  <td>
                    <button
                      type="button"
                      class="icon-btn"
                      title={$t('prizePools.remove')}
                      on:click={() =>
                        stage(
                          $t('prizePools.remove'),
                          binding.furnitureName ?? `#${binding.furnitureDefinitionId}`,
                          '/api/operations/prize-pools/bindings/delete',
                          { bindingId: binding.id },
                          true
                        )}
                    >
                      <Trash2 size={14} strokeWidth={2} aria-hidden="true" />
                    </button>
                  </td>
                {/if}
              </tr>
            {/each}
          </tbody>
        </table>
      </div>

      {#if filteredBindings.length === 0}
        <EmptyState message={$t('prizePools.noMatchingBindings')} />
      {:else if bindingPageCount > 1}
        <Pagination
          page={bindingPage}
          pageCount={bindingPageCount}
          total={filteredBindings.length}
          pageSize={BINDINGS_PAGE_SIZE}
          label={$t('prizePools.bindingsLabel')}
          prevLabel={$t('common.prev')}
          nextLabel={$t('common.next')}
          pageWord={$t('common.page')}
          on:change={(e) => (bindingPage = e.detail)}
        />
      {/if}
    {/if}

    {#if canManage}
      <div class="op-grid">
        <div class="op-field">
          <label for="binding-definition">{$t('prizePools.furnitureDefinitionId')}</label>
          <div class="op-pick">
            <input
              id="binding-definition"
              type="number"
              bind:value={newBinding.furnitureDefinitionId}
            />
            <button type="button" class="ghost-button" on:click={() => (picking = 'binding')}
              >{$t('prizePools.pick')}</button
            >
          </div>
        </div>
        <div class="op-field">
          <label for="binding-pool">{$t('prizePools.pool')}</label>
          <select id="binding-pool" bind:value={newBinding.poolCode}>
            <option value="">—</option>
            {#each pools as pool (pool.id)}
              <option value={pool.code}>{pool.name}</option>
            {/each}
          </select>
        </div>
        <div class="op-field">
          <label for="binding-hits">{$t('prizePools.hitsRequired')}</label>
          <input id="binding-hits" type="number" min="1" bind:value={newBinding.hitsRequired} />
        </div>
      </div>
      <div class="op-actions">
        <button
          type="button"
          class="btn btn-primary btn-sm"
          disabled={!newBinding.furnitureDefinitionId || !newBinding.poolCode}
          on:click={() =>
            stage(
              $t('prizePools.newBinding'),
              newBinding.poolCode,
              '/api/operations/prize-pools/bindings',
              {
                ...newBinding,
                furnitureDefinitionId: Number(newBinding.furnitureDefinitionId) || 0,
                hitsRequired: Number(newBinding.hitsRequired) || 1,
              }
            )}
        >
          <Plus size={14} strokeWidth={2} aria-hidden="true" />
          {$t('prizePools.newBinding')}
        </button>
      </div>
    {/if}
  </section>
  {/if}
{/if}

{#if picking}
  <PickerModal
    kind="furniture"
    title={$t('prizePools.pickFurniture')}
    onSelect={(item) => {
      if (picking === 'entry') newEntry.furnitureDefinitionId = item.id;
      else newBinding.furnitureDefinitionId = item.id;
      picking = null;
    }}
    onClose={() => (picking = null)}
  />
{/if}

<ConfirmReasonModal
  open={Boolean($ops.pending)}
  title={$ops.pending?.title ?? ''}
  changes={$ops.pending?.changes ?? []}
  noteOnly={$ops.pending?.noteOnly ?? false}
  summary={$ops.pending?.summary ?? ''}
  confirmLabel={$ops.pending?.title ?? $t('prizePools.save')}
  busy={$ops.busy}
  error={$ops.error}
  danger={$ops.pending?.danger ?? false}
  on:confirm={(e) => ops.confirm(e.detail)}
  on:cancel={() => ops.cancel()}
/>

<style>
  /* The global tokens style each block but not the gaps between them inside one panel, so the
     stats ran into the table and the table into the action row. */
  .panel > .stats,
  .panel > .table-wrap,
  .panel > .op-grid,
  .panel > .inline-list {
    margin-bottom: 18px;
  }


  /* Icon and label sit on one line. AssetImage and the label are both block-level here, so without
     this they stack and the row grows to two lines for no reason. */
  .cell-row {
    display: flex;
    align-items: center;
    gap: 8px;
    min-width: 0;
  }

  .cell-row .cell-text {
    display: flex;
    flex-direction: column;
    min-width: 0;
  }

  .panel > .op-actions {
    margin-top: 4px;
  }

  /* Selected pool: aria-pressed is the state, so it drives the styling rather than a second class. */
  .btn[aria-pressed='true'] {
    border-color: var(--accent);
    color: var(--accent);
  }
</style>
