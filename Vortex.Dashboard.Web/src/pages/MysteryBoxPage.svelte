<script>
  import { onMount } from 'svelte';
  import {
    Gift,
    Key,
    Package,
    Pencil,
    Plus,
    RefreshCw,
    Sparkles,
    Trash2,
    TriangleAlert,
  } from '@lucide/svelte';
  import OpResult from '../components/OpResult.svelte';
  import AccessDeniedNotice from '../components/AccessDeniedNotice.svelte';
  import ConfirmReasonModal from '../components/ConfirmReasonModal.svelte';
  import PickerModal from '../components/PickerModal.svelte';
  import AssetImage from '../components/AssetImage.svelte';
  import { apiGet, apiPost } from '../lib/api.js';
  import { formatNumber } from '../lib/format.js';
  import { isPermissionDeniedError, hasDashboardCapability } from '../lib/permissions.js';
  import { CAPABILITIES } from '../lib/dashboardPermissions.js';
  import { rememberReason } from '../lib/reasonHistory.js';
  import { identity } from '../lib/session.js';
  import { t, translate } from '../lib/i18n.js';

  // Only floor/wall prizes name a furniture definition; effect and club prizes carry their target in
  // extraParam instead, so the form swaps which field it asks for.
  const FURNITURE_TYPES = ['Floor', 'Wall'];

  // Prizes live in the shared prize pools every reward furniture draws from, so a pool travels as
  // its code rather than as a mystery-box-only enum name. These two are seeded and drawn by code.
  const POOL_BOX = 'mystery-box';
  const POOL_TROPHY = 'mystery-trophy';

  function emptyPrizeForm() {
    return {
      pool: POOL_BOX,
      color: '',
      productType: 'Floor',
      furnitureDefinitionId: '',
      extraParam: '',
      weight: 10,
      enabled: true,
    };
  }

  let definitions = [];
  let prizes = [];
  let pools = [];
  let colors = [];
  let productTypes = [];
  let stats = null;
  let statsDays = 7;

  let loading = false;
  let error = '';
  let forbidden = false;

  let newPrizeOpen = false;
  let newPrize = emptyPrizeForm();
  let editPrizeId = null;
  let editPrize = null;

  // Furniture is picked the same way players are: an operator knows the box, not its id.
  let furniPicker = null;

  // Both grants target a picked player, not a typed name: the shared PickerModal searches the live
  // directory, so the operation carries an unambiguous id and a rename or a typo cannot misfire it.
  let keyForm = { playerId: 0, playerName: '', playerOnline: false, color: 'purple' };
  let boxForm = {
    playerId: 0,
    playerName: '',
    playerOnline: false,
    furnitureDefinitionId: '',
    color: 'purple',
  };
  let picker = null;

  function pickPlayer(apply) {
    picker = { title: translate('mysteryBox.selectPlayerTitle'), onSelect: apply };
  }

  let results = {};
  let errors = {};
  // The action awaiting its reason. Every write goes through the shared reason modal rather than a
  // reason field per form, so the audit trail can never be skipped by a stray Enter key.
  let pending = null;
  let pendingBusy = false;
  let pendingError = '';

  $: canManage = hasDashboardCapability($identity, CAPABILITIES.opsMysteryBoxManage);
  $: boxPrizes = prizes.filter((p) => p.pool === POOL_BOX);
  $: trophyPrizes = prizes.filter((p) => p.pool === POOL_TROPHY);

  // Share of a pool, computed against the entries a draw actually competes with: same pool, and a
  // colourless prize competes with the colour-specific ones for that colour only.
  function shareOf(prize) {
    if (!prize.enabled) return null;
    const pool = pools.find((p) => p.pool === prize.pool && p.color === prize.color);
    if (!pool || pool.totalWeight <= 0) return null;
    return Math.round((prize.weight / pool.totalWeight) * 1000) / 10;
  }

  function prizeTarget(prize) {
    if (FURNITURE_TYPES.includes(prize.productType)) {
      return prize.furnitureName || `#${prize.furnitureDefinitionId}`;
    }
    return prize.extraParam || '—';
  }

  async function load() {
    loading = true;
    error = '';
    forbidden = false;

    try {
      const data = await apiGet('/api/mystery-box');
      definitions = data.definitions?.items || [];
      prizes = data.prizes?.items || [];
      pools = data.pools || [];
      colors = data.colors || [];
      productTypes = data.productTypes || [];

      if (colors.length > 0 && !colors.includes(keyForm.color)) {
        keyForm = { ...keyForm, color: colors[0] };
        boxForm = { ...boxForm, color: colors[0] };
      }

      if (definitions.length === 1 && !boxForm.furnitureDefinitionId) {
        boxForm = { ...boxForm, furnitureDefinitionId: definitions[0].id };
      }
    } catch (err) {
      if (isPermissionDeniedError(err)) {
        forbidden = true;
        definitions = [];
        prizes = [];
        return;
      }

      error = err.message;
    } finally {
      loading = false;
    }
  }

  // Best-effort: the page is usable without the activity panel, so a failure here must not blank the
  // admin surface.
  async function loadStats() {
    try {
      stats = await apiGet(`/api/mystery-box/stats?days=${statsDays}`);
    } catch {
      stats = null;
    }
  }

  function stage(id, title, endpoint, valid, body, summary, onSuccess) {
    errors = { ...errors, [id]: '' };

    if (!valid) {
      errors = { ...errors, [id]: translate('mysteryBox.fillFields') };
      return;
    }

    pendingError = '';
    pending = { id, title, endpoint, body, summary, onSuccess };
  }

  async function confirmPending(reason) {
    if (!pending) return;

    const { id, endpoint, body, onSuccess } = pending;

    pendingBusy = true;
    pendingError = '';
    errors = { ...errors, [id]: '' };

    try {
      const result = await apiPost(endpoint, { ...body, reason });
      results = { ...results, [id]: result };

      if (result.ok) {
        rememberReason(reason);
        pending = null;
        await onSuccess?.();
      } else {
        // Keep the modal open on a named server refusal (invalid_color,
        // definition_already_registered, …) so the operator can fix it in place.
        pendingError = result.message || result.code || translate('mysteryBox.fillFields');
      }
    } catch (err) {
      pendingError = isPermissionDeniedError(err)
        ? translate('common.insufficientRightsAction')
        : err.code || err.message;
    } finally {
      pendingBusy = false;
    }
  }

  function cancelPending() {
    pending = null;
    pendingError = '';
  }

  function prizeBody(form) {
    const usesFurniture = FURNITURE_TYPES.includes(form.productType);

    return {
      pool: form.pool,
      color: form.color,
      productType: form.productType,
      furnitureDefinitionId: usesFurniture ? Number(form.furnitureDefinitionId) || 0 : 0,
      extraParam: usesFurniture ? '' : form.extraParam.trim(),
      weight: Number(form.weight) || 0,
      enabled: form.enabled,
    };
  }

  function prizeFormValid(form) {
    const body = prizeBody(form);

    if (body.weight <= 0) return false;

    return FURNITURE_TYPES.includes(form.productType)
      ? body.furnitureDefinitionId > 0
      : body.extraParam.length > 0;
  }

  function stageCreatePrize() {
    if (!canManage) return;

    stage(
      'createPrize',
      translate('mysteryBox.newPrize'),
      '/api/operations/mystery-box/prizes',
      prizeFormValid(newPrize),
      prizeBody(newPrize),
      translate('mysteryBox.createPrizeSummary', { pool: newPrize.pool }),
      async () => {
        newPrizeOpen = false;
        newPrize = emptyPrizeForm();
        await load();
      },
    );
  }

  function startEditPrize(prize) {
    editPrizeId = prize.id;
    editPrize = {
      pool: prize.pool,
      color: prize.color,
      productType: prize.productType,
      furnitureDefinitionId: prize.furnitureDefinitionId || '',
      extraParam: prize.extraParam || '',
      weight: prize.weight,
      enabled: prize.enabled,
    };
  }

  function stageUpdatePrize() {
    if (!canManage || !editPrize) return;

    stage(
      'updatePrize',
      translate('mysteryBox.edit'),
      '/api/operations/mystery-box/prizes/update',
      prizeFormValid(editPrize),
      { prizeId: editPrizeId, ...prizeBody(editPrize) },
      translate('mysteryBox.updatePrizeSummary', { id: editPrizeId }),
      async () => {
        editPrizeId = null;
        editPrize = null;
        await load();
      },
    );
  }

  function stageDeletePrize(prize) {
    if (!canManage) return;

    stage(
      'deletePrize',
      translate('mysteryBox.prizesHeading'),
      '/api/operations/mystery-box/prizes/delete',
      true,
      { prizeId: prize.id },
      translate('mysteryBox.deletePrizeSummary', { id: prize.id }),
      load,
    );
  }

  function stageGrantKey() {
    if (!canManage) return;

    stage(
      'grantKey',
      translate('mysteryBox.grantKey'),
      '/api/operations/mystery-box/keys',
      keyForm.playerId > 0 && Boolean(keyForm.color),
      { playerId: keyForm.playerId, color: keyForm.color },
      translate('mysteryBox.grantKeySummary', {
        name: keyForm.playerName,
        color: keyForm.color,
      }),
      loadStats,
    );
  }

  function stageGrantBox() {
    if (!canManage) return;

    const furnitureDefinitionId = Number(boxForm.furnitureDefinitionId) || 0;

    stage(
      'grantBox',
      translate('mysteryBox.grantBox'),
      '/api/operations/mystery-box/boxes',
      boxForm.playerId > 0 && furnitureDefinitionId > 0 && Boolean(boxForm.color),
      { playerId: boxForm.playerId, furnitureDefinitionId, color: boxForm.color },
      translate('mysteryBox.grantBoxSummary', {
        name: boxForm.playerName,
        color: boxForm.color,
      }),
      loadStats,
    );
  }

  function stageReload() {
    if (!canManage) return;

    stage(
      'reload',
      translate('mysteryBox.reload'),
      '/api/operations/mystery-box/reload',
      true,
      {},
      translate('mysteryBox.reloadSummary'),
      load,
    );
  }

  onMount(() => {
    load();
    loadStats();
  });
</script>

<section class="panel">
  <div class="panel-head">
    <h2>{$t('mysteryBox.title')}</h2>
    <div class="head-actions">
      <button type="button" class="ghost-button" on:click={load} disabled={loading}>
        {$t('common.refresh')}
      </button>
      {#if canManage}
        <button type="button" class="ghost-button" on:click={stageReload}>
          <RefreshCw size={14} strokeWidth={2} aria-hidden="true" /> {$t('mysteryBox.reload')}
        </button>
      {/if}
    </div>
  </div>
  <p class="muted">{$t('mysteryBox.description')}</p>
  {#if errors.reload}<p class="empty-state danger">{errors.reload}</p>{/if}
  {#if results.reload}<OpResult result={results.reload} />{/if}
</section>

{#if forbidden}
  <AccessDeniedNotice message={$t('mysteryBox.accessDenied')} />
{:else}
  {#if stats}
    <section class="panel">
      <div class="panel-head">
        <h2><Sparkles size={17} strokeWidth={2} aria-hidden="true" /> {$t('mysteryBox.statsHeading')}</h2>
        <label class="filter-field">
          {$t('mysteryBox.windowDays')}
          <select bind:value={statsDays} on:change={loadStats}>
            {#each [7, 30, 90] as days}
              <option value={days}>{$t('mysteryBox.days', { count: days })}</option>
            {/each}
          </select>
        </label>
      </div>
      <div class="stat-grid">
        <div class="stat-tile">
          <span class="stat-label">{$t('mysteryBox.boxesOpened')}</span>
          <strong>{formatNumber(stats.boxesOpened)}</strong>
        </div>
        <div class="stat-tile">
          <span class="stat-label">{$t('mysteryBox.trophiesOpened')}</span>
          <strong>{formatNumber(stats.trophiesOpened)}</strong>
        </div>
        <div class="stat-tile">
          <span class="stat-label">{$t('mysteryBox.prizesAwarded')}</span>
          <strong>{formatNumber(stats.prizesAwarded)}</strong>
        </div>
        <div class="stat-tile">
          <span class="stat-label">{$t('mysteryBox.keysGranted')}</span>
          <strong>{formatNumber(stats.keysGranted)}</strong>
        </div>
        <div class="stat-tile">
          <span class="stat-label">{$t('mysteryBox.keysConsumed')}</span>
          <strong>{formatNumber(stats.keysConsumed)}</strong>
        </div>
        <div class="stat-tile" title={$t('mysteryBox.keysOutstandingHint')}>
          <span class="stat-label">{$t('mysteryBox.keysOutstanding')}</span>
          <strong>{formatNumber(stats.keysOutstanding)}</strong>
        </div>
        <div class="stat-tile">
          <span class="stat-label">{$t('mysteryBox.boxesInCirculation')}</span>
          <strong>{formatNumber(stats.boxesInCirculation)}</strong>
        </div>
      </div>
      {#if stats.keysByColor?.length > 0}
        <h3 class="subheading">{$t('mysteryBox.keysByColor')}</h3>
        <div class="chip-row">
          {#each stats.keysByColor as row (row.color)}
            <span class="op-chip">
              <Key size={12} strokeWidth={2} aria-hidden="true" />
              {row.color} — {$t('mysteryBox.held')} {formatNumber(row.held)} / {$t('mysteryBox.spent')}
              {formatNumber(row.spent)}
            </span>
          {/each}
        </div>
      {/if}
    </section>
  {/if}

  <section class="panel">
    <div class="panel-head">
      <h2><Package size={17} strokeWidth={2} aria-hidden="true" /> {$t('mysteryBox.definitionsHeading')}</h2>
    </div>
    <p class="muted">{$t('mysteryBox.definitionsHint')}</p>

    {#if loading}
      <p class="muted">{$t('common.loading')}</p>
    {:else if error}
      <p class="empty-state danger">{error}</p>
    {:else if definitions.length === 0}
      <p class="empty-state">{$t('mysteryBox.noDefinitions')}</p>
    {:else}
      <div class="catalog-list">
        {#each definitions as definition (definition.id)}
          <div class="catalog-card">
            <div class="offer-head">
              <span class="catalog-row-main">
                <AssetImage
                  src={definition.furnitureIconUrl}
                  alt={definition.name}
                  size={32}
                />
                <strong>{definition.name}</strong>
                <small class="muted">#{definition.id} — sprite {definition.spriteId}</small>
              </span>
              {#if definition.totalStates < 25}
                <span class="status-badge status-badge--warn" title={$t('mysteryBox.statesWarning')}>
                  <TriangleAlert size={12} strokeWidth={2} aria-hidden="true" /> {definition.totalStates}/25
                </span>
              {/if}
            </div>
            {#if definition.totalStates < 25}
              <p class="detail-warning">{$t('mysteryBox.statesWarning')}</p>
            {/if}
          </div>
        {/each}
      </div>
    {/if}
  </section>

  <section class="panel">
    <div class="panel-head">
      <h2><Gift size={17} strokeWidth={2} aria-hidden="true" /> {$t('mysteryBox.prizesHeading')}</h2>
      {#if canManage}
        <button type="button" class="ghost-button" on:click={() => (newPrizeOpen = !newPrizeOpen)}>
          <Plus size={14} strokeWidth={2} aria-hidden="true" />
          {newPrizeOpen ? $t('mysteryBox.cancel') : $t('mysteryBox.newPrize')}
        </button>
      {/if}
    </div>
    <p class="muted">{$t('mysteryBox.prizesHint')}</p>

    {#if newPrizeOpen}
      <div class="catalog-card-detail">
        <div class="op-field">
          <label for="new-prize-pool">{$t('mysteryBox.pool')}</label>
          <select id="new-prize-pool" bind:value={newPrize.pool}>
            <option value={POOL_BOX}>{$t('mysteryBox.poolBox')}</option>
            <option value={POOL_TROPHY}>{$t('mysteryBox.poolTrophy')}</option>
          </select>
        </div>
        {#if newPrize.pool === POOL_BOX}
          <div class="op-field">
            <label for="new-prize-color">{$t('mysteryBox.color')}</label>
            <select id="new-prize-color" bind:value={newPrize.color}>
              <option value="">{$t('mysteryBox.colorAny')}</option>
              {#each colors as color}
                <option value={color}>{color}</option>
              {/each}
            </select>
          </div>
        {/if}
        <div class="op-field">
          <label for="new-prize-type">{$t('mysteryBox.productType')}</label>
          <select id="new-prize-type" bind:value={newPrize.productType}>
            {#each productTypes as productType}
              <option value={productType}>{productType}</option>
            {/each}
          </select>
        </div>
        {#if FURNITURE_TYPES.includes(newPrize.productType)}
          <div class="op-field">
            <label for="new-prize-furni">{$t('mysteryBox.furnitureDefinitionId')}</label>
            <div class="op-pick">
              <input
                id="new-prize-furni"
                type="number"
                min="1"
                bind:value={newPrize.furnitureDefinitionId}
              />
              <button
                type="button"
                class="ghost-button"
                on:click={() =>
                  (furniPicker = (item) => (newPrize.furnitureDefinitionId = item.id))}
                >{$t('mysteryBox.pick')}</button
              >
            </div>
          </div>
        {:else}
          <div class="op-field">
            <label for="new-prize-extra">{$t('mysteryBox.extraParam')}</label>
            <input id="new-prize-extra" bind:value={newPrize.extraParam} />
            <small class="muted">{$t('mysteryBox.extraParamHint')}</small>
          </div>
        {/if}
        <div class="op-field">
          <label for="new-prize-weight">{$t('mysteryBox.weight')}</label>
          <input id="new-prize-weight" type="number" min="1" bind:value={newPrize.weight} />
          <small class="muted">{$t('mysteryBox.weightHint')}</small>
        </div>
        <div class="op-field">
          <label><input type="checkbox" bind:checked={newPrize.enabled} /> {$t('mysteryBox.enabled')}</label>
        </div>
        <div class="op-actions">
          <button type="button" on:click={stageCreatePrize}>{$t('mysteryBox.create')}</button>
        </div>
        {#if errors.createPrize}<p class="empty-state danger">{errors.createPrize}</p>{/if}
        {#if results.createPrize}<OpResult result={results.createPrize} />{/if}
      </div>
    {/if}

    {#if pools.length > 0}
      <div class="chip-row">
        {#each pools as pool (`${pool.pool}:${pool.color}`)}
          <span class="op-chip">
            {pool.pool === POOL_BOX ? $t('mysteryBox.poolBox') : $t('mysteryBox.poolTrophy')}
            {pool.color ? ` · ${pool.color}` : ''} — {$t('mysteryBox.poolSummary', {
              entries: pool.entries,
              weight: pool.totalWeight,
            })}
          </span>
        {/each}
      </div>
    {/if}

    {#if prizes.length === 0}
      <p class="empty-state">{$t('mysteryBox.noPrizes')}</p>
    {:else}
      {#each [{ label: $t('mysteryBox.poolBox'), rows: boxPrizes }, { label: $t('mysteryBox.poolTrophy'), rows: trophyPrizes }] as group}
        {#if group.rows.length > 0}
          <h3 class="subheading">{group.label}</h3>
          <div class="catalog-list">
            {#each group.rows as prize (prize.id)}
              <div class="catalog-card">
                <div class="offer-head">
                  <span class="catalog-row-main">
                    <AssetImage
                      src={prize.furnitureIconUrl}
                      alt={prize.furnitureName ?? ''}
                      size={32}
                    />
                    <strong>{prizeTarget(prize)}</strong>
                    <small class="muted">#{prize.id} — {prize.productType}{prize.color ? ` · ${prize.color}` : ''}</small>
                  </span>
                  <span class="cost-chip">{$t('mysteryBox.weight')} {prize.weight}</span>
                  {#if shareOf(prize) !== null}
                    <span class="op-chip">{$t('mysteryBox.share')} {shareOf(prize)}%</span>
                  {/if}
                  {#if !prize.enabled}
                    <span class="status-badge status-badge--bad">{$t('mysteryBox.enabled')}: {$t('common.no')}</span>
                  {/if}
                  {#if canManage}
                    <div class="op-actions offer-actions">
                      <button type="button" class="ghost-button" on:click={() => startEditPrize(prize)}>
                        <Pencil size={14} strokeWidth={2} aria-hidden="true" /> {$t('mysteryBox.edit')}
                      </button>
                      <button type="button" class="ghost-button danger" on:click={() => stageDeletePrize(prize)}>
                        <Trash2 size={14} strokeWidth={2} aria-hidden="true" />
                      </button>
                    </div>
                  {/if}
                </div>
                {#if editPrizeId === prize.id && editPrize}
                  <div class="catalog-card-detail">
                    <div class="op-field">
                      <label for={`edit-prize-pool-${prize.id}`}>{$t('mysteryBox.pool')}</label>
                      <select id={`edit-prize-pool-${prize.id}`} bind:value={editPrize.pool}>
                        <option value={POOL_BOX}>{$t('mysteryBox.poolBox')}</option>
                        <option value={POOL_TROPHY}>{$t('mysteryBox.poolTrophy')}</option>
                      </select>
                    </div>
                    {#if editPrize.pool === POOL_BOX}
                      <div class="op-field">
                        <label for={`edit-prize-color-${prize.id}`}>{$t('mysteryBox.color')}</label>
                        <select id={`edit-prize-color-${prize.id}`} bind:value={editPrize.color}>
                          <option value="">{$t('mysteryBox.colorAny')}</option>
                          {#each colors as color}
                            <option value={color}>{color}</option>
                          {/each}
                        </select>
                      </div>
                    {/if}
                    <div class="op-field">
                      <label for={`edit-prize-type-${prize.id}`}>{$t('mysteryBox.productType')}</label>
                      <select id={`edit-prize-type-${prize.id}`} bind:value={editPrize.productType}>
                        {#each productTypes as productType}
                          <option value={productType}>{productType}</option>
                        {/each}
                      </select>
                    </div>
                    {#if FURNITURE_TYPES.includes(editPrize.productType)}
                      <div class="op-field">
                        <label for={`edit-prize-furni-${prize.id}`}>{$t('mysteryBox.furnitureDefinitionId')}</label>
                        <div class="op-pick">
                          <input
                            id={`edit-prize-furni-${prize.id}`}
                            type="number"
                            min="1"
                            bind:value={editPrize.furnitureDefinitionId}
                          />
                          <button
                            type="button"
                            class="ghost-button"
                            on:click={() =>
                              (furniPicker = (item) =>
                                (editPrize.furnitureDefinitionId = item.id))}
                            >{$t('mysteryBox.pick')}</button
                          >
                        </div>
                      </div>
                    {:else}
                      <div class="op-field">
                        <label for={`edit-prize-extra-${prize.id}`}>{$t('mysteryBox.extraParam')}</label>
                        <input id={`edit-prize-extra-${prize.id}`} bind:value={editPrize.extraParam} />
                        <small class="muted">{$t('mysteryBox.extraParamHint')}</small>
                      </div>
                    {/if}
                    <div class="op-field">
                      <label for={`edit-prize-weight-${prize.id}`}>{$t('mysteryBox.weight')}</label>
                      <input id={`edit-prize-weight-${prize.id}`} type="number" min="1" bind:value={editPrize.weight} />
                    </div>
                    <div class="op-field">
                      <label><input type="checkbox" bind:checked={editPrize.enabled} /> {$t('mysteryBox.enabled')}</label>
                    </div>
                    <div class="op-actions">
                      <button type="button" on:click={stageUpdatePrize}>{$t('mysteryBox.save')}</button>
                      <button type="button" class="ghost-button" on:click={() => { editPrizeId = null; editPrize = null; }}>
                        {$t('mysteryBox.cancel')}
                      </button>
                    </div>
                    {#if errors.updatePrize}<p class="empty-state danger">{errors.updatePrize}</p>{/if}
                  </div>
                {/if}
              </div>
            {/each}
          </div>
        {/if}
      {/each}
    {/if}
    {#if results.updatePrize}<OpResult result={results.updatePrize} />{/if}
    {#if results.deletePrize}<OpResult result={results.deletePrize} />{/if}
    {#if errors.deletePrize}<p class="empty-state danger">{errors.deletePrize}</p>{/if}
  </section>

  {#if canManage}
    <section class="panel">
      <div class="panel-head">
        <h2><Package size={17} strokeWidth={2} aria-hidden="true" /> {$t('mysteryBox.boxesHeading')}</h2>
      </div>
      <p class="muted">{$t('mysteryBox.boxesHint')}</p>
      {#if definitions.length === 0}
        <p class="empty-state">{$t('mysteryBox.noDefinitions')}</p>
      {:else}
        <div class="catalog-card-detail">
          <div class="op-field">
            <span class="field-label">{$t('common.playerRequired')}</span>
            <div class="picker-row">
              <button
                type="button"
                class="ghost-button"
                on:click={() =>
                  pickPlayer(
                    (u) =>
                      (boxForm = {
                        ...boxForm,
                        playerId: u.id,
                        playerName: u.name,
                        playerOnline: u.online,
                      }),
                  )}
              >
                {$t('common.selectUser')}
              </button>
              {#if boxForm.playerId}
                <span class="op-chip">
                  <span class="op-dot" class:on={boxForm.playerOnline}></span>
                  {boxForm.playerName} <small>#{boxForm.playerId}</small>
                </span>
              {:else}
                <span class="muted">{$t('common.noUserSelected')}</span>
              {/if}
            </div>
          </div>
          <div class="op-field">
            <label for="grant-box-definition">{$t('mysteryBox.boxFurniture')}</label>
            <select id="grant-box-definition" bind:value={boxForm.furnitureDefinitionId}>
              <option value="">—</option>
              {#each definitions as definition (definition.id)}
                <option value={definition.id}>{definition.name}</option>
              {/each}
            </select>
          </div>
          <div class="op-field">
            <label for="grant-box-color">{$t('mysteryBox.color')}</label>
            <select id="grant-box-color" bind:value={boxForm.color}>
              {#each colors as color}
                <option value={color}>{color}</option>
              {/each}
            </select>
            <small class="muted">{$t('mysteryBox.colorHint')}</small>
          </div>
          <div class="op-actions">
            <button type="button" on:click={stageGrantBox}>{$t('mysteryBox.grantBox')}</button>
          </div>
          {#if errors.grantBox}<p class="empty-state danger">{errors.grantBox}</p>{/if}
          {#if results.grantBox}<OpResult result={results.grantBox} />{/if}
        </div>
      {/if}
    </section>

    <section class="panel">
      <div class="panel-head">
        <h2><Key size={17} strokeWidth={2} aria-hidden="true" /> {$t('mysteryBox.keysHeading')}</h2>
      </div>
      <p class="muted">{$t('mysteryBox.keysHint')}</p>
      <div class="catalog-card-detail">
        <div class="op-field">
          <span class="field-label">{$t('common.playerRequired')}</span>
          <div class="picker-row">
            <button
              type="button"
              class="ghost-button"
              on:click={() =>
                pickPlayer(
                  (u) =>
                    (keyForm = {
                      ...keyForm,
                      playerId: u.id,
                      playerName: u.name,
                      playerOnline: u.online,
                    }),
                )}
            >
              {$t('common.selectUser')}
            </button>
            {#if keyForm.playerId}
              <span class="op-chip">
                <span class="op-dot" class:on={keyForm.playerOnline}></span>
                {keyForm.playerName} <small>#{keyForm.playerId}</small>
              </span>
            {:else}
              <span class="muted">{$t('common.noUserSelected')}</span>
            {/if}
          </div>
        </div>
        <div class="op-field">
          <label for="grant-key-color">{$t('mysteryBox.color')}</label>
          <select id="grant-key-color" bind:value={keyForm.color}>
            {#each colors as color}
              <option value={color}>{color}</option>
            {/each}
          </select>
        </div>
        <div class="op-actions">
          <button type="button" on:click={stageGrantKey}>{$t('mysteryBox.grantKey')}</button>
        </div>
        {#if errors.grantKey}<p class="empty-state danger">{errors.grantKey}</p>{/if}
        {#if results.grantKey}<OpResult result={results.grantKey} />{/if}
      </div>
    </section>
  {/if}
{/if}

{#if furniPicker}
  <PickerModal
    kind="furniture"
    title={$t('mysteryBox.pickFurniture')}
    onSelect={(item) => {
      furniPicker(item);
      furniPicker = null;
    }}
    onClose={() => (furniPicker = null)}
    canSelect={canManage}
  />
{/if}

{#if picker}
  <PickerModal
    kind="user"
    title={picker.title}
    onSelect={picker.onSelect}
    onClose={() => (picker = null)}
    canSelect={canManage}
  />
{/if}

<ConfirmReasonModal
  open={Boolean(pending)}
  title={pending?.title ?? ''}
  summary={pending?.summary ?? ''}
  confirmLabel={pending?.title ?? $t('common.confirm')}
  busy={pendingBusy}
  error={pendingError}
  danger={false}
  on:confirm={(e) => confirmPending(e.detail)}
  on:cancel={cancelPending}
/>

<style>
  .head-actions {
    display: flex;
    align-items: center;
    gap: 8px;
    flex-wrap: wrap;
  }

  .filter-field {
    display: inline-flex;
    align-items: center;
    gap: 6px;
    font-size: 0.85rem;
    color: var(--muted);
    white-space: nowrap;
  }

  .ghost-button.danger {
    color: var(--danger);
    border-color: rgba(var(--danger-rgb), 0.4);
  }

  .ghost-button,
  .op-actions button {
    display: inline-flex;
    align-items: center;
    gap: 6px;
  }

  .field-label {
    font-size: 0.85rem;
    color: var(--muted);
  }

  .picker-row {
    display: flex;
    align-items: center;
    gap: 8px;
    flex-wrap: wrap;
  }

  .op-dot {
    width: 8px;
    height: 8px;
    border-radius: 50%;
    background: var(--muted);
  }

  .op-dot.on {
    background: var(--ok, var(--accent));
  }

  .subheading {
    margin: 14px 0 4px;
    font-size: 0.9rem;
    color: var(--muted);
  }

  .chip-row {
    display: flex;
    flex-wrap: wrap;
    gap: 6px;
    margin-top: 8px;
  }

  .stat-grid {
    display: grid;
    grid-template-columns: repeat(auto-fit, minmax(140px, 1fr));
    gap: 8px;
    margin-top: 10px;
  }

  .stat-tile {
    border: 1px solid var(--line);
    border-radius: 10px;
    background: var(--surface-strong);
    padding: 10px 12px;
    display: grid;
    gap: 2px;
  }

  .stat-label {
    font-size: 0.78rem;
    color: var(--muted);
  }

  .stat-tile strong {
    font-size: 1.25rem;
  }

  .catalog-list {
    display: grid;
    gap: 8px;
    margin-top: 10px;
  }

  .catalog-card {
    border: 1px solid var(--line);
    border-radius: 12px;
    overflow: hidden;
    background: var(--surface-strong);
  }

  .catalog-row-main {
    display: grid;
    gap: 2px;
    min-width: 120px;
    flex: 1 1 160px;
  }

  .catalog-row-main strong {
    overflow: hidden;
    text-overflow: ellipsis;
    white-space: nowrap;
  }

  .cost-chip {
    display: inline-flex;
    align-items: center;
    gap: 5px;
    border: 1px solid var(--warning-border);
    background: var(--warning-bg);
    color: var(--warning);
    border-radius: 999px;
    padding: 0 9px;
    font-size: 0.78rem;
    font-weight: 700;
    white-space: nowrap;
  }

  .offer-head {
    display: flex;
    align-items: center;
    gap: 12px;
    padding: 10px 12px;
    flex-wrap: wrap;
  }

  .offer-actions {
    margin-left: auto;
  }

  .detail-warning {
    margin: 0;
    padding: 0 12px 10px;
    font-size: 0.8rem;
    color: var(--warning);
  }

  .catalog-card-detail {
    border-top: 1px solid var(--line);
    padding: 12px;
  }
</style>
