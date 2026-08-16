<script>

  import ConfirmStagedModal from '../components/ConfirmStagedModal.svelte';
  import OpResult from '../components/OpResult.svelte';
  import { Eye, EyeOff, Image, Package, Pencil, Plus, Trash2 } from '@lucide/svelte';
  import { apiGet } from '../lib/api.js';
  import { createResource } from '../lib/resource.js';
  import { createWriteOps } from '../lib/writeOps.js';
  import { hasDashboardCapability } from '../lib/permissions.js';
  import { CAPABILITIES } from '../lib/dashboardPermissions.js';
  import { diffFields } from '../lib/changes.js';
  import {
    PRODUCT_TYPES,
    FURNITURE_CATEGORIES,
    USAGE_POLICIES,
    STUFF_DATA_TYPES,
    LOGIC_GROUPS,
  } from '../lib/furnitureEnums.js';
  import AccessDeniedNotice from '../components/AccessDeniedNotice.svelte';
  import Drawer from '../components/Drawer.svelte';
  import PageHeader from '../components/PageHeader.svelte';
  import Pagination from '../components/Pagination.svelte';
  import { identity } from '../lib/session.js';
  import { t, translate } from '../lib/i18n.js';

  function emptyForm() {
    return {
      spriteId: 0,
      name: '',
      productType: 0,
      furniCategory: 1,
      // "none" is not a real registered logic key (see LOGIC_GROUPS' doc comment) -- it silently
      // falls back to default_floor at runtime with a console warning, so default to the real key
      // instead of the DB column's misleading legacy default value.
      logic: 'default_floor',
      totalStates: 0,
      width: 1,
      length: 1,
      stackHeight: 0,
      canStack: true,
      canWalk: false,
      canSit: false,
      canLay: false,
      canRecycle: false,
      canTrade: true,
      canGroup: true,
      canSell: true,
      usagePolicy: 1,
      extraData: '',
      stuffDataType: 0,
    };
  }

  let page = $state(1);
  let limit = 40;
  let query = $state('');

  // One drawer for both jobs: { mode: 'create' | 'edit', id, form }. Null means closed, which is
  // also what makes "is anything being edited" a single question rather than two flags that can
  // disagree.
  let drawer = $state(null);

  function openCreate() {
    drawer = { mode: 'create', id: null, form: emptyForm() };
  }

  function closeDrawer() {
    drawer = null;
  }

  // Every write is staged here and confirmed in the dialog below before it is posted. createWriteOps
  // owns that cycle -- posting, remembering the audited reason, and tracking each form's busy state,
  // error and result under its own key -- so the page only describes what each button writes.
  const ops = createWriteOps();

  // Page and search term are the cache key, so paging is "move the variable" and nothing else --
  // the read follows, and stepping back to a page already seen is served from cache.
  const definitions = createResource(
    () => ['furniture-definitions', page, limit, query.trim()],
    () => {
      const params = new URLSearchParams({ page: String(page), limit: String(limit) });
      if (query.trim()) params.set('q', query.trim());

      return apiGet(`/api/v1/furniture/definitions?${params}`);
    }
  );

  let canManage = $derived(hasDashboardCapability($identity, CAPABILITIES.opsFurnitureManage));
  let items = $derived(definitions.data?.items ?? []);
  let total = $derived(definitions.data?.total ?? 0);
  let totalPages = $derived(Math.max(1, Math.ceil(total / limit)));

  function search() {
    page = 1;
  }

  function goToPage(next) {
    page = Math.min(totalPages, Math.max(1, next));
  }

  function specFrom(form) {
    return {
      spriteId: Number(form.spriteId) || 0,
      name: form.name.trim(),
      productType: Number(form.productType),
      furniCategory: Number(form.furniCategory),
      logic: form.logic.trim() || 'default_floor',
      totalStates: Number(form.totalStates) || 0,
      width: Number(form.width) || 0,
      length: Number(form.length) || 0,
      stackHeight: Number(form.stackHeight) || 0,
      canStack: form.canStack,
      canWalk: form.canWalk,
      canSit: form.canSit,
      canLay: form.canLay,
      canRecycle: form.canRecycle,
      canTrade: form.canTrade,
      canGroup: form.canGroup,
      canSell: form.canSell,
      usagePolicy: Number(form.usagePolicy),
      extraData: form.extraData.trim() ? form.extraData.trim() : null,
      stuffDataType: Number(form.stuffDataType),
    };
  }

  // No `reason` is passed: createWriteOps builds the audited sentence from this summary plus the
  // fields that actually changed, and the confirm dialog asks for a note only as an optional extra.
  const stage = (id, title, endpoint, valid, body, summary, onSuccess, changes = []) =>
    ops.ask(endpoint, body, title, summary, {
      key: id,
      valid,
      invalidMessage: translate('furnitureAdmin.fillFields'),
      changes,
      onSuccess,
    });

  function stageCreate() {
    if (!canManage || drawer?.mode !== 'create') {
      ops.fail('create', translate('furnitureAdmin.createAccessDenied'));
      return;
    }

    const newForm = drawer.form;

    stage(
      'create',
      translate('furnitureAdmin.createTitle'),
      '/api/v1/operations/furniture/definitions',
      Number(newForm.spriteId) > 0 && Boolean(newForm.name.trim()),
      specFrom(newForm),
      translate('furnitureAdmin.createSummary', { name: newForm.name.trim(), sprite: newForm.spriteId }),
      async () => {
        closeDrawer();
        await definitions.refresh();
      },
    );
  }

  // What the fields the audit compares against are called. Only these keys are diffed, so the
  // form can hold scratch state without it reaching the audit line.
  const diffFieldSpecs = () => [
    { key: 'spriteId', label: 'Sprite id' },
    { key: 'name', label: translate('furnitureAdmin.nameRequired').replace(' *', '') },
    { key: 'productType', label: translate('furnitureAdmin.productType') },
    { key: 'furniCategory', label: translate('furnitureAdmin.category') },
    { key: 'logic', label: translate('furnitureAdmin.logic') },
    { key: 'totalStates', label: translate('furnitureAdmin.totalStates') },
    { key: 'width', label: translate('furnitureAdmin.width') },
    { key: 'length', label: translate('furnitureAdmin.length') },
    { key: 'stackHeight', label: translate('furnitureAdmin.stackHeight') },
    { key: 'usagePolicy', label: translate('furnitureAdmin.usagePolicy') },
    { key: 'stuffDataType', label: translate('furnitureAdmin.stuffDataType') },
    { key: 'extraData', label: translate('furnitureAdmin.extraDataOptional') },
    { key: 'canStack', label: translate('furnitureAdmin.canStack') },
    { key: 'canWalk', label: translate('furnitureAdmin.canWalk') },
    { key: 'canSit', label: translate('furnitureAdmin.canSit') },
    { key: 'canLay', label: translate('furnitureAdmin.canLay') },
    { key: 'canRecycle', label: translate('furnitureAdmin.canRecycle') },
    { key: 'canTrade', label: translate('furnitureAdmin.canTrade') },
    { key: 'canGroup', label: translate('furnitureAdmin.canGroup') },
    { key: 'canSell', label: translate('furnitureAdmin.canSell') },
  ];

  function startEdit(item) {
    drawer = {
      mode: 'edit',
      id: item.id,
      // Kept so the audit can say what the value WAS, which is the half only the screen knew.
      before: specFrom({
        ...item,
        name: item.name || '',
        logic: item.logic || '',
        extraData: item.extraData || '',
      }),
      form: {
        spriteId: item.spriteId,
        name: item.name,
        productType: item.productType,
        furniCategory: item.furniCategory,
        logic: item.logic,
        totalStates: item.totalStates,
        width: item.width,
        length: item.length,
        stackHeight: item.stackHeight,
        canStack: item.canStack,
        canWalk: item.canWalk,
        canSit: item.canSit,
        canLay: item.canLay,
        canRecycle: item.canRecycle,
        canTrade: item.canTrade,
        canGroup: item.canGroup,
        canSell: item.canSell,
        usagePolicy: item.usagePolicy,
        extraData: item.extraData || '',
        stuffDataType: item.stuffDataType,
      },
    };
  }

  function stageUpdate() {
    if (!canManage || drawer?.mode !== 'edit') return;

    const editForm = drawer.form;
    const editingId = drawer.id;

    stage(
      'update',
      translate('furnitureAdmin.updateTitle'),
      '/api/v1/operations/furniture/definitions/update',
      Number(editForm.spriteId) > 0 && Boolean(editForm.name.trim()),
      { definitionId: editingId, ...specFrom(editForm) },
      translate('furnitureAdmin.updateSummary', { id: editingId }),
      async () => {
        closeDrawer();
        await definitions.refresh();
      },
      diffFields(drawer.before, specFrom(editForm), diffFieldSpecs()),
    );
  }

  function stageDelete(item) {
    if (!canManage) return;

    stage(
      'delete',
      translate('furnitureAdmin.deleteTitle'),
      '/api/v1/operations/furniture/definitions/delete',
      true,
      { definitionId: item.id },
      translate('furnitureAdmin.deleteSummary', { name: item.name, id: item.id }),
      definitions.refresh,
    );
  }
</script>

<!-- The create and edit forms were two copies of the same twelve fields, one spliced above the list
     and one inside whichever card was open. They are one snippet now, rendered once, in the drawer. -->
{#snippet definitionFields(form, prefix)}
  <div class="form-grid">
    <div class="op-field">
      <label for={`${prefix}-sprite`}>{$t('furnitureAdmin.spriteIdRequired')}</label>
      <input id={`${prefix}-sprite`} type="number" min="1" bind:value={form.spriteId} />
    </div>
    <div class="op-field">
      <label for={`${prefix}-name`}>{$t('furnitureAdmin.nameRequired')}</label>
      <input id={`${prefix}-name`} bind:value={form.name} placeholder={$t('furnitureAdmin.namePlaceholder')} />
    </div>
    <div class="op-field">
      <label for={`${prefix}-type`}>{$t('furnitureAdmin.productType')}</label>
      <select id={`${prefix}-type`} bind:value={form.productType}>
        {#each PRODUCT_TYPES as t}<option value={t.value}>{t.label}</option>{/each}
      </select>
    </div>
    <div class="op-field">
      <label for={`${prefix}-category`}>{$t('furnitureAdmin.category')}</label>
      <select id={`${prefix}-category`} bind:value={form.furniCategory}>
        {#each FURNITURE_CATEGORIES as c}<option value={c.value}>{c.label}</option>{/each}
      </select>
    </div>
    <div class="op-field">
      <label for={`${prefix}-logic`}>{$t('furnitureAdmin.logic')}</label>
      <select id={`${prefix}-logic`} bind:value={form.logic}>
        {#each LOGIC_GROUPS as group}
          <optgroup label={group.label}>
            {#each group.options as o}<option value={o.value}>{o.label}</option>{/each}
          </optgroup>
        {/each}
      </select>
    </div>
    <div class="op-field">
      <label for={`${prefix}-states`}>{$t('furnitureAdmin.totalStates')}</label>
      <input id={`${prefix}-states`} type="number" min="0" bind:value={form.totalStates} />
    </div>
    <div class="op-field">
      <label for={`${prefix}-width`}>{$t('furnitureAdmin.width')}</label>
      <input id={`${prefix}-width`} type="number" min="1" bind:value={form.width} />
    </div>
    <div class="op-field">
      <label for={`${prefix}-length`}>{$t('furnitureAdmin.length')}</label>
      <input id={`${prefix}-length`} type="number" min="1" bind:value={form.length} />
    </div>
    <div class="op-field">
      <label for={`${prefix}-height`}>{$t('furnitureAdmin.stackHeight')}</label>
      <input id={`${prefix}-height`} type="number" step="0.1" min="0" bind:value={form.stackHeight} />
    </div>
    <div class="op-field">
      <label for={`${prefix}-usage`}>{$t('furnitureAdmin.usagePolicy')}</label>
      <select id={`${prefix}-usage`} bind:value={form.usagePolicy}>
        {#each USAGE_POLICIES as u}<option value={u.value}>{u.label}</option>{/each}
      </select>
    </div>
    <div class="op-field">
      <label for={`${prefix}-stuffdata`}>{$t('furnitureAdmin.stuffDataType')}</label>
      <select id={`${prefix}-stuffdata`} bind:value={form.stuffDataType}>
        {#each STUFF_DATA_TYPES as s}<option value={s.value}>{s.label}</option>{/each}
      </select>
    </div>
    <div class="op-field">
      <label for={`${prefix}-extra`}>{$t('furnitureAdmin.extraDataOptional')}</label>
      <input id={`${prefix}-extra`} bind:value={form.extraData} />
    </div>
  </div>

  <div class="checkbox-grid">
    <label><input type="checkbox" bind:checked={form.canStack} /> {$t('furnitureAdmin.canStack')}</label>
    <label><input type="checkbox" bind:checked={form.canWalk} /> {$t('furnitureAdmin.canWalk')}</label>
    <label><input type="checkbox" bind:checked={form.canSit} /> {$t('furnitureAdmin.canSit')}</label>
    <label><input type="checkbox" bind:checked={form.canLay} /> {$t('furnitureAdmin.canLay')}</label>
    <label><input type="checkbox" bind:checked={form.canRecycle} /> {$t('furnitureAdmin.canRecycle')}</label>
    <label><input type="checkbox" bind:checked={form.canTrade} /> {$t('furnitureAdmin.canTrade')}</label>
    <label><input type="checkbox" bind:checked={form.canGroup} /> {$t('furnitureAdmin.canGroup')}</label>
    <label><input type="checkbox" bind:checked={form.canSell} /> {$t('furnitureAdmin.canSell')}</label>
  </div>

{/snippet}

<section class="panel">
  <PageHeader title={$t('furnitureAdmin.title')} description={$t('furnitureAdmin.description')}>
    {#snippet icon()}
      <Package size={18} strokeWidth={2} aria-hidden="true" />
    {/snippet}
    {#snippet actions()}
      {#if canManage}
        <button type="button" class="ghost-button" onclick={openCreate}>
          <Plus size={14} strokeWidth={2} aria-hidden="true" /> {$t('furnitureAdmin.newDefinition')}
        </button>
      {/if}
    {/snippet}
  </PageHeader>

  <form class="toolbar" onsubmit={(event) => { event.preventDefault(); search(); }}>
    <input bind:value={query} placeholder={$t('furnitureAdmin.searchPlaceholder')} />
    <button type="submit" disabled={definitions.loading}>{$t('furnitureAdmin.search')}</button>
  </form>

  {#if definitions.forbidden}
    <AccessDeniedNotice message={$t('furnitureAdmin.accessDenied')} />
  {:else if definitions.loading}
    <p class="muted">{$t('furnitureAdmin.loading')}</p>
  {:else if definitions.error}
    <p class="empty-state danger">{definitions.error}</p>
  {:else if items.length === 0}
    <p class="empty-state">{$t('furnitureAdmin.noMatch')}</p>
  {:else}
    <div class="furni-list">
      {#each items as item (item.id)}
        <!-- Every card is the same height now. It used to carry a "reason to delete" text input,
             which both made the cards ragged and asked for the reason before anyone had confirmed
             they wanted to delete anything. The confirm dialog collects it instead. -->
        <div class="furni-card">
          <div class="furni-row">
            <span class="furni-row-icon">
              {#if item.iconUrl}
                <img src={item.iconUrl} alt="" />
              {:else}
                <Image size={18} strokeWidth={2} aria-hidden="true" />
              {/if}
            </span>
            <span class="furni-row-main">
              <strong>{item.name}</strong>
              <small class="muted">#{item.id} - sprite {item.spriteId} - {item.productTypeLabel} - {item.furniCategoryLabel}</small>
            </span>
            <span class="furni-row-meta">
              <span class="op-chip" title="Size">{item.width}x{item.length}</span>
              <span class="status-badge" class:status-badge--ok={item.canTrade} class:status-badge--bad={!item.canTrade}>
                {#if item.canTrade}<Eye size={12} strokeWidth={2} aria-hidden="true" />{:else}<EyeOff size={12} strokeWidth={2} aria-hidden="true" />{/if}
                {$t('furnitureAdmin.trade')}
              </span>
            </span>
            {#if canManage}
              <span class="furni-row-actions">
                <button type="button" class="ghost-button" onclick={() => startEdit(item)}>
                  <Pencil size={14} strokeWidth={2} aria-hidden="true" /> {$t('furnitureAdmin.edit')}
                </button>
                <button type="button" class="ghost-button danger" onclick={() => stageDelete(item)} aria-label={$t('furnitureAdmin.delete')}>
                  <Trash2 size={14} strokeWidth={2} aria-hidden="true" />
                </button>
              </span>
            {/if}
          </div>
        </div>
      {/each}
    </div>

    {#if $ops.errors.delete}<p class="empty-state danger">{$ops.errors.delete}</p>{/if}
    {#if $ops.results.delete}
      <OpResult result={$ops.results.delete} />
    {/if}

    <Pagination
      page={page}
      pageCount={totalPages}
      pageWord={$t('common.page')}
      prevLabel={$t('furnitureAdmin.previous')}
      nextLabel={$t('common.next')}
      onchange={goToPage}
    />
  {/if}
</section>

{#if drawer}
  <Drawer
    title={drawer.mode === 'create' ? $t('furnitureAdmin.createTitle') : $t('furnitureAdmin.updateTitle')}
    eyebrow={$t('furnitureAdmin.title')}
    onclose={closeDrawer}
  >
    {@render definitionFields(drawer.form, drawer.mode === 'create' ? 'new-furni' : `edit-furni-${drawer.id}`)}

    {#if $ops.errors[drawer.mode === 'create' ? 'create' : 'update']}
      <p class="empty-state danger">{$ops.errors[drawer.mode === 'create' ? 'create' : 'update']}</p>
    {/if}
    {#if $ops.results[drawer.mode === 'create' ? 'create' : 'update']}
      <OpResult result={$ops.results[drawer.mode === 'create' ? 'create' : 'update']} />
    {/if}

    {#snippet actions()}
      {#if drawer.mode === 'create'}
        <button type="button" onclick={stageCreate} disabled={$ops.busyKeys.create}>{$t('furnitureAdmin.create')}</button>
      {:else}
        <button type="button" onclick={stageUpdate} disabled={$ops.busyKeys.update}>{$t('furnitureAdmin.save')}</button>
      {/if}
      <button type="button" class="ghost-button" onclick={closeDrawer}>{$t('furnitureAdmin.cancel')}</button>
    {/snippet}
  </Drawer>
{/if}

<ConfirmStagedModal {ops} eyebrow={$t('furnitureAdmin.confirmEyebrow')} />


<style>
  .ghost-button {
    display: inline-flex;
    align-items: center;
    gap: 6px;
  }

  .form-grid {
    display: grid;
    grid-template-columns: repeat(auto-fill, minmax(160px, 1fr));
    gap: 10px 16px;
    /* Grid items default to min-width: auto, which refuses to shrink below the intrinsic content
       width of whatever's inside (a <select> with long option text, in this form) -- that silently
       breaks the minmax()/auto-fit wrapping and lets the row overflow its container instead of
       adding rows. min-width: 0 here (and width: 100% + min-width: 0 on the fields themselves)
       is what actually makes the grid responsive. */
    min-width: 0;
  }

  .form-grid > .op-field {
    min-width: 0;
  }

  .form-grid > .op-field input,
  .form-grid > .op-field select {
    width: 100%;
    min-width: 0;
    box-sizing: border-box;
  }

  .checkbox-grid {
    display: grid;
    grid-template-columns: repeat(auto-fit, minmax(160px, 1fr));
    gap: 6px 16px;
    margin: 10px 0;
    font-size: 0.85rem;
    min-width: 0;
  }

  .checkbox-grid label {
    min-width: 0;
  }

  .checkbox-grid label {
    display: flex;
    align-items: center;
    gap: 6px;
  }

  .furni-list {
    display: grid;
    grid-template-columns: repeat(4, minmax(0, 1fr));
    gap: 8px;
    margin-top: 10px;
  }

  @media (max-width: 1700px) {
    .furni-list {
      grid-template-columns: repeat(3, minmax(0, 1fr));
    }
  }

  @media (max-width: 1300px) {
    .furni-list {
      grid-template-columns: repeat(2, minmax(0, 1fr));
    }
  }

  @media (max-width: 900px) {
    .furni-list {
      grid-template-columns: minmax(0, 1fr);
    }
  }

  .furni-card {
    border: 1px solid var(--line);
    border-radius: 12px;
    overflow: hidden;
    background: var(--surface-strong);
  }

  .furni-row {
    display: flex;
    align-items: center;
    flex-wrap: wrap;
    row-gap: 8px;
    gap: 12px;
    padding: 10px 12px;
  }

  .furni-row-icon {
    width: 38px;
    height: 38px;
    flex: 0 0 auto;
    display: grid;
    place-items: center;
    border: 1px solid var(--line-strong);
    border-radius: 9px;
    background: var(--input-bg);
    color: var(--accent);
    overflow: hidden;
  }

  .furni-row-icon img {
    width: 100%;
    height: 100%;
    object-fit: contain;
    image-rendering: pixelated;
    image-rendering: crisp-edges;
  }

  .furni-row-main {
    display: grid;
    gap: 2px;
    min-width: 120px;
    flex: 1 1 200px;
  }

  .furni-row-main strong {
    overflow: hidden;
    text-overflow: ellipsis;
    white-space: nowrap;
  }

  .furni-row-meta {
    display: flex;
    align-items: center;
    gap: 6px;
    flex-wrap: wrap;
  }

  .furni-row-meta > .op-chip,
  .furni-row-meta > .status-badge {
    height: 24px;
    box-sizing: border-box;
  }

  /* Edit and delete sit together at the end of the row, so every card is the same height and the
     destructive one is never the first thing under the cursor. */
  .furni-row-actions {
    display: inline-flex;
    align-items: center;
    gap: 6px;
    flex: none;
  }
</style>
