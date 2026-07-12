<script>
  import { onMount } from 'svelte';
  import { Eye, EyeOff, Image, Package, Pencil, Plus, Trash2 } from '@lucide/svelte';
  import { apiGet, apiPost } from '../lib/api.js';
  import { isPermissionDeniedError, hasDashboardCapability } from '../lib/permissions.js';
  import { compactCorrelation } from '../lib/format.js';
  import { CAPABILITIES } from '../lib/dashboardPermissions.js';
  import { reasonOk } from '../lib/validation.js';
  import { rememberReason } from '../lib/reasonHistory.js';
  import {
    PRODUCT_TYPES,
    FURNITURE_CATEGORIES,
    USAGE_POLICIES,
    STUFF_DATA_TYPES,
    LOGIC_GROUPS,
  } from '../lib/furnitureEnums.js';
  import AccessDeniedNotice from '../components/AccessDeniedNotice.svelte';
  import { identity } from '../lib/session.js';

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
      reason: '',
    };
  }

  let items = [];
  let total = 0;
  let page = 1;
  let limit = 40;
  let query = '';
  let loading = false;
  let error = '';
  let forbidden = false;

  let newOpen = false;
  let newForm = emptyForm();
  let editingId = null;
  let editForm = null;

  let deleteReason = {};

  let results = {};
  let errors = {};
  let busy = {};
  let pending = null;

  $: canManage = hasDashboardCapability($identity, CAPABILITIES.opsFurnitureManage);
  $: totalPages = Math.max(1, Math.ceil(total / limit));

  async function refresh() {
    loading = true;
    error = '';
    forbidden = false;

    const params = new URLSearchParams({ page: String(page), limit: String(limit) });
    if (query.trim()) params.set('q', query.trim());

    try {
      const data = await apiGet(`/api/v1/furniture/definitions?${params}`);
      items = data.items || [];
      total = data.total || 0;
    } catch (err) {
      if (isPermissionDeniedError(err)) {
        forbidden = true;
        items = [];
        return;
      }

      error = err.message;
      items = [];
    } finally {
      loading = false;
    }
  }

  function search() {
    page = 1;
    void refresh();
  }

  function goToPage(next) {
    page = Math.min(totalPages, Math.max(1, next));
    void refresh();
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

  function stage(id, title, endpoint, valid, body, summary, onSuccess) {
    errors = { ...errors, [id]: '' };

    if (!valid) {
      errors = { ...errors, [id]: 'Fill the required fields (sprite id, name, reason >= 3 chars).' };
      return;
    }

    pending = { id, title, endpoint, body, summary, reason: body.reason, onSuccess };
  }

  function cancelPending() {
    pending = null;
  }

  async function confirmPending() {
    if (!pending) return;

    const { id, endpoint, body, reason, onSuccess } = pending;
    pending = null;

    busy = { ...busy, [id]: true };
    errors = { ...errors, [id]: '' };
    results = { ...results, [id]: null };

    try {
      const result = await apiPost(endpoint, body);
      results = { ...results, [id]: result };

      if (result.ok) {
        rememberReason(reason);
        await onSuccess?.();
      }
    } catch (err) {
      errors = {
        ...errors,
        [id]: isPermissionDeniedError(err) ? 'Droits insuffisants pour effectuer cette action.' : err.code || err.message,
      };
    } finally {
      busy = { ...busy, [id]: false };
    }
  }

  function stageCreate() {
    if (!canManage) {
      errors = { ...errors, create: 'Droits insuffisants.' };
      return;
    }

    stage(
      'create',
      'Create furniture definition',
      '/api/v1/operations/furniture/definitions',
      Number(newForm.spriteId) > 0 && Boolean(newForm.name.trim()) && reasonOk(newForm.reason),
      { ...specFrom(newForm), reason: newForm.reason.trim() },
      `Create furniture definition "${newForm.name.trim()}" (sprite #${newForm.spriteId}).`,
      async () => {
        newOpen = false;
        newForm = emptyForm();
        await refresh();
      },
    );
  }

  function startEdit(item) {
    editingId = item.id;
    editForm = {
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
      reason: '',
    };
  }

  function stageUpdate() {
    if (!canManage || editingId === null || !editForm) return;

    stage(
      'update',
      'Update furniture definition',
      '/api/v1/operations/furniture/definitions/update',
      Number(editForm.spriteId) > 0 && Boolean(editForm.name.trim()) && reasonOk(editForm.reason),
      { definitionId: editingId, ...specFrom(editForm), reason: editForm.reason.trim() },
      `Update furniture definition #${editingId}.`,
      async () => {
        editingId = null;
        await refresh();
      },
    );
  }

  function stageDelete(item) {
    if (!canManage) return;

    stage(
      'delete',
      'Delete furniture definition',
      '/api/v1/operations/furniture/definitions/delete',
      reasonOk(deleteReason[item.id]),
      { definitionId: item.id, reason: (deleteReason[item.id] || '').trim() },
      `Delete "${item.name}" (#${item.id}). Blocked if it's still placed/owned or used by a catalog product.`,
      async () => {
        deleteReason = { ...deleteReason, [item.id]: '' };
        await refresh();
      },
    );
  }

  onMount(() => {
    void refresh();
  });
</script>

<section class="panel">
  <div class="panel-head">
    <h2><Package size={18} strokeWidth={2} aria-hidden="true" /> Furniture definitions</h2>
    {#if canManage}
      <button type="button" class="ghost-button" on:click={() => (newOpen = !newOpen)}>
        <Plus size={14} strokeWidth={2} aria-hidden="true" /> {newOpen ? 'Cancel' : 'New definition'}
      </button>
    {/if}
  </div>
  <p class="muted">
    The catalog and inventory both reference these by id. Changes go live immediately for connected
    clients -- no restart needed.
  </p>

  <form class="toolbar" on:submit|preventDefault={search}>
    <input bind:value={query} placeholder="search by name, id or sprite id" />
    <button type="submit" disabled={loading}>Search</button>
  </form>

  {#if newOpen}
    <div class="furni-card-detail">
      <div class="form-grid">
        <div class="op-field">
          <label for="new-furni-sprite">Sprite id *</label>
          <input id="new-furni-sprite" type="number" min="1" bind:value={newForm.spriteId} />
        </div>
        <div class="op-field">
          <label for="new-furni-name">Name *</label>
          <input id="new-furni-name" bind:value={newForm.name} placeholder="throne" />
        </div>
        <div class="op-field">
          <label for="new-furni-type">Product type</label>
          <select id="new-furni-type" bind:value={newForm.productType}>
            {#each PRODUCT_TYPES as t}<option value={t.value}>{t.label}</option>{/each}
          </select>
        </div>
        <div class="op-field">
          <label for="new-furni-category">Category</label>
          <select id="new-furni-category" bind:value={newForm.furniCategory}>
            {#each FURNITURE_CATEGORIES as c}<option value={c.value}>{c.label}</option>{/each}
          </select>
        </div>
        <div class="op-field">
          <label for="new-furni-logic">Logic</label>
          <select id="new-furni-logic" bind:value={newForm.logic}>
            {#each LOGIC_GROUPS as group}
              <optgroup label={group.label}>
                {#each group.options as o}<option value={o.value}>{o.label}</option>{/each}
              </optgroup>
            {/each}
          </select>
        </div>
        <div class="op-field">
          <label for="new-furni-states">Total states</label>
          <input id="new-furni-states" type="number" min="0" bind:value={newForm.totalStates} />
        </div>
        <div class="op-field">
          <label for="new-furni-width">Width</label>
          <input id="new-furni-width" type="number" min="1" bind:value={newForm.width} />
        </div>
        <div class="op-field">
          <label for="new-furni-length">Length</label>
          <input id="new-furni-length" type="number" min="1" bind:value={newForm.length} />
        </div>
        <div class="op-field">
          <label for="new-furni-height">Stack height</label>
          <input id="new-furni-height" type="number" step="0.1" min="0" bind:value={newForm.stackHeight} />
        </div>
        <div class="op-field">
          <label for="new-furni-usage">Usage policy</label>
          <select id="new-furni-usage" bind:value={newForm.usagePolicy}>
            {#each USAGE_POLICIES as u}<option value={u.value}>{u.label}</option>{/each}
          </select>
        </div>
        <div class="op-field">
          <label for="new-furni-stuffdata">Stuff data type</label>
          <select id="new-furni-stuffdata" bind:value={newForm.stuffDataType}>
            {#each STUFF_DATA_TYPES as s}<option value={s.value}>{s.label}</option>{/each}
          </select>
        </div>
        <div class="op-field">
          <label for="new-furni-extra">Extra data (optional)</label>
          <input id="new-furni-extra" bind:value={newForm.extraData} />
        </div>
      </div>
      <div class="checkbox-grid">
        <label><input type="checkbox" bind:checked={newForm.canStack} /> Can stack</label>
        <label><input type="checkbox" bind:checked={newForm.canWalk} /> Can walk</label>
        <label><input type="checkbox" bind:checked={newForm.canSit} /> Can sit</label>
        <label><input type="checkbox" bind:checked={newForm.canLay} /> Can lay</label>
        <label><input type="checkbox" bind:checked={newForm.canRecycle} /> Can recycle</label>
        <label><input type="checkbox" bind:checked={newForm.canTrade} /> Can trade</label>
        <label><input type="checkbox" bind:checked={newForm.canGroup} /> Can group</label>
        <label><input type="checkbox" bind:checked={newForm.canSell} /> Can sell (marketplace)</label>
      </div>
      <div class="op-field">
        <label for="new-furni-reason">Reason *</label>
        <input id="new-furni-reason" bind:value={newForm.reason} placeholder="why this definition?" list="reason-history" />
      </div>
      <div class="op-actions">
        <button type="button" on:click={stageCreate} disabled={busy.create}>Create</button>
      </div>
      {#if errors.create}<p class="empty-state danger">{errors.create}</p>{/if}
      {#if results.create}
        <p class="op-result" class:danger={!results.create.ok}>
          {results.create.ok ? '✅' : '❌'} {results.create.message} - cid
          <code>{compactCorrelation(results.create.correlationId)}</code>
        </p>
      {/if}
    </div>
  {/if}

  {#if forbidden}
    <AccessDeniedNotice message="Vous n'avez pas la permission de consulter les définitions de mobilier." />
  {:else if loading}
    <p class="muted">Loading...</p>
  {:else if error}
    <p class="empty-state danger">{error}</p>
  {:else if items.length === 0}
    <p class="empty-state">No furniture definitions match.</p>
  {:else}
    <div class="furni-list">
      {#each items as item (item.id)}
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
                Trade
              </span>
            </span>
            {#if canManage}
              <button type="button" class="ghost-button" on:click={() => startEdit(item)}>
                <Pencil size={14} strokeWidth={2} aria-hidden="true" /> Edit
              </button>
            {/if}
          </div>

          {#if editingId === item.id && editForm}
            <div class="furni-card-detail">
              <div class="form-grid">
                <div class="op-field">
                  <label for={`edit-furni-sprite-${item.id}`}>Sprite id *</label>
                  <input id={`edit-furni-sprite-${item.id}`} type="number" min="1" bind:value={editForm.spriteId} />
                </div>
                <div class="op-field">
                  <label for={`edit-furni-name-${item.id}`}>Name *</label>
                  <input id={`edit-furni-name-${item.id}`} bind:value={editForm.name} />
                </div>
                <div class="op-field">
                  <label for={`edit-furni-type-${item.id}`}>Product type</label>
                  <select id={`edit-furni-type-${item.id}`} bind:value={editForm.productType}>
                    {#each PRODUCT_TYPES as t}<option value={t.value}>{t.label}</option>{/each}
                  </select>
                </div>
                <div class="op-field">
                  <label for={`edit-furni-category-${item.id}`}>Category</label>
                  <select id={`edit-furni-category-${item.id}`} bind:value={editForm.furniCategory}>
                    {#each FURNITURE_CATEGORIES as c}<option value={c.value}>{c.label}</option>{/each}
                  </select>
                </div>
                <div class="op-field">
                  <label for={`edit-furni-logic-${item.id}`}>Logic</label>
                  <select id={`edit-furni-logic-${item.id}`} bind:value={editForm.logic}>
                    {#each LOGIC_GROUPS as group}
                      <optgroup label={group.label}>
                        {#each group.options as o}<option value={o.value}>{o.label}</option>{/each}
                      </optgroup>
                    {/each}
                  </select>
                </div>
                <div class="op-field">
                  <label for={`edit-furni-states-${item.id}`}>Total states</label>
                  <input id={`edit-furni-states-${item.id}`} type="number" min="0" bind:value={editForm.totalStates} />
                </div>
                <div class="op-field">
                  <label for={`edit-furni-width-${item.id}`}>Width</label>
                  <input id={`edit-furni-width-${item.id}`} type="number" min="1" bind:value={editForm.width} />
                </div>
                <div class="op-field">
                  <label for={`edit-furni-length-${item.id}`}>Length</label>
                  <input id={`edit-furni-length-${item.id}`} type="number" min="1" bind:value={editForm.length} />
                </div>
                <div class="op-field">
                  <label for={`edit-furni-height-${item.id}`}>Stack height</label>
                  <input id={`edit-furni-height-${item.id}`} type="number" step="0.1" min="0" bind:value={editForm.stackHeight} />
                </div>
                <div class="op-field">
                  <label for={`edit-furni-usage-${item.id}`}>Usage policy</label>
                  <select id={`edit-furni-usage-${item.id}`} bind:value={editForm.usagePolicy}>
                    {#each USAGE_POLICIES as u}<option value={u.value}>{u.label}</option>{/each}
                  </select>
                </div>
                <div class="op-field">
                  <label for={`edit-furni-stuffdata-${item.id}`}>Stuff data type</label>
                  <select id={`edit-furni-stuffdata-${item.id}`} bind:value={editForm.stuffDataType}>
                    {#each STUFF_DATA_TYPES as s}<option value={s.value}>{s.label}</option>{/each}
                  </select>
                </div>
                <div class="op-field">
                  <label for={`edit-furni-extra-${item.id}`}>Extra data (optional)</label>
                  <input id={`edit-furni-extra-${item.id}`} bind:value={editForm.extraData} />
                </div>
              </div>
              <div class="checkbox-grid">
                <label><input type="checkbox" bind:checked={editForm.canStack} /> Can stack</label>
                <label><input type="checkbox" bind:checked={editForm.canWalk} /> Can walk</label>
                <label><input type="checkbox" bind:checked={editForm.canSit} /> Can sit</label>
                <label><input type="checkbox" bind:checked={editForm.canLay} /> Can lay</label>
                <label><input type="checkbox" bind:checked={editForm.canRecycle} /> Can recycle</label>
                <label><input type="checkbox" bind:checked={editForm.canTrade} /> Can trade</label>
                <label><input type="checkbox" bind:checked={editForm.canGroup} /> Can group</label>
                <label><input type="checkbox" bind:checked={editForm.canSell} /> Can sell (marketplace)</label>
              </div>
              <div class="op-field">
                <label for={`edit-furni-reason-${item.id}`}>Reason *</label>
                <input id={`edit-furni-reason-${item.id}`} bind:value={editForm.reason} placeholder="why this change?" list="reason-history" />
              </div>
              <div class="op-actions">
                <button type="button" on:click={stageUpdate} disabled={busy.update}>Save</button>
                <button class="ghost-button" type="button" on:click={() => (editingId = null)}>Cancel</button>
              </div>
              {#if errors.update}<p class="empty-state danger">{errors.update}</p>{/if}
              {#if results.update}
                <p class="op-result" class:danger={!results.update.ok}>
                  {results.update.ok ? '✅' : '❌'} {results.update.message} - cid
                  <code>{compactCorrelation(results.update.correlationId)}</code>
                </p>
              {/if}
            </div>
          {/if}

          {#if canManage}
            <div class="furni-card-detail op-pick">
              <input bind:value={deleteReason[item.id]} placeholder="reason to delete this definition" list="reason-history" style="flex: 1;" />
              <button type="button" class="ghost-button danger" on:click={() => stageDelete(item)}>
                <Trash2 size={14} strokeWidth={2} aria-hidden="true" /> Delete
              </button>
            </div>
          {/if}
        </div>
      {/each}
    </div>
    {#if errors.delete}<p class="empty-state danger">{errors.delete}</p>{/if}
    {#if results.delete}
      <p class="op-result" class:danger={!results.delete.ok}>
        {results.delete.ok ? '✅' : '❌'} {results.delete.message} - cid
        <code>{compactCorrelation(results.delete.correlationId)}</code>
      </p>
    {/if}

    <div class="op-actions" style="margin-top: 10px;">
      <button type="button" class="ghost-button" disabled={page <= 1} on:click={() => goToPage(page - 1)}>Previous</button>
      <span class="muted">Page {page} / {totalPages} ({total} total)</span>
      <button type="button" class="ghost-button" disabled={page >= totalPages} on:click={() => goToPage(page + 1)}>Next</button>
    </div>
  {/if}
</section>

{#if pending}
  <div class="modal-layer">
    <button class="modal-backdrop" type="button" aria-label="Cancel" on:click={cancelPending}></button>
    <section class="modal-panel" role="dialog" aria-modal="true" style="width: min(460px, 100%)">
      <header class="modal-header">
        <div>
          <p class="eyebrow">Confirm furniture action</p>
          <h2>{pending.title}</h2>
        </div>
      </header>
      <p>{pending.summary}</p>
      <p class="muted">Reason: {pending.reason}</p>
      <div class="op-actions">
        <button type="button" on:click={confirmPending}>Confirm</button>
        <button class="ghost-button" type="button" on:click={cancelPending}>Cancel</button>
      </div>
    </section>
  </div>
{/if}

<style>
  .panel-head h2 {
    display: flex;
    align-items: center;
    gap: 8px;
  }

  .ghost-button {
    display: inline-flex;
    align-items: center;
    gap: 6px;
  }

  .ghost-button.danger {
    color: var(--danger);
    border-color: rgba(var(--danger-rgb), 0.4);
  }

  .form-grid {
    display: grid;
    grid-template-columns: repeat(auto-fit, minmax(160px, 1fr));
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
    gap: 8px;
    margin-top: 10px;
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

  .furni-card-detail {
    border-top: 1px solid var(--line);
    padding: 12px;
  }
</style>
