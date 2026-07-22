<script>
  import { onMount } from 'svelte';
  import OpResult from '../components/OpResult.svelte';
  import AccessDeniedNotice from '../components/AccessDeniedNotice.svelte';
  import { isPermissionDeniedError, hasDashboardCapability } from '../lib/permissions.js';
  import { apiGet, apiPost } from '../lib/api.js';
  import { CAPABILITIES } from '../lib/dashboardPermissions.js';
  import { reasonOk } from '../lib/validation.js';
  import { identity } from '../lib/session.js';
  import { t, translate } from '../lib/i18n.js';

  let items = [];
  let editValues = {};
  let loading = true;
  let loadError = '';

  let results = {};
  let busy = {};
  let pending = null;
  let reason = '';
  let reasonError = '';

  $: canManage = hasDashboardCapability($identity, CAPABILITIES.opsManageConfig);

  // Preserve the catalog order coming from the API while bucketing into the declared groups.
  $: groups = (() => {
    const order = [];
    const byGroup = new Map();
    for (const item of items) {
      if (!byGroup.has(item.group)) {
        byGroup.set(item.group, []);
        order.push(item.group);
      }
      byGroup.get(item.group).push(item);
    }
    return order.map((name) => ({ name, items: byGroup.get(name) }));
  })();

  function effectiveValue(item) {
    return item.currentValue ?? item.defaultValue;
  }

  function isDirty(item) {
    return (editValues[item.key] ?? '') !== effectiveValue(item);
  }

  async function load() {
    loading = true;
    loadError = '';
    try {
      const data = await apiGet('/api/v1/config');
      items = data?.items ?? [];
      const next = {};
      for (const item of items) {
        next[item.key] = effectiveValue(item);
      }
      editValues = next;
    } catch (err) {
      loadError = isPermissionDeniedError(err)
        ? translate('common.insufficientRights')
        : err.code || err.message;
    } finally {
      loading = false;
    }
  }

  onMount(load);

  function startSave(item) {
    if (!canManage || !isDirty(item)) {
      return;
    }
    reason = '';
    reasonError = '';
    pending = { key: item.key, value: editValues[item.key] ?? '', description: item.description };
  }

  function cancel() {
    pending = null;
  }

  async function confirm() {
    if (!pending) {
      return;
    }
    if (!reasonOk(reason)) {
      reasonError = translate('config.reasonMissing');
      return;
    }

    const { key, value } = pending;
    pending = null;
    busy = { ...busy, [key]: true };
    results = { ...results, [key]: null };

    try {
      const result = await apiPost('/api/v1/operations/config', {
        key,
        value,
        reason: reason.trim(),
      });
      results = { ...results, [key]: result };

      // Reflect the new value locally on success so "overridden"/dirty state is accurate.
      if (result?.ok) {
        items = items.map((it) =>
          it.key === key ? { ...it, currentValue: value, isOverridden: true } : it,
        );
      }
    } catch (err) {
      results = {
        ...results,
        [key]: {
          ok: false,
          correlationId: '',
          message: isPermissionDeniedError(err)
            ? translate('common.insufficientRightsAction')
            : err.code || err.message,
        },
      };
    } finally {
      busy = { ...busy, [key]: false };
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
  <div class="panel-head"><h2>{$t('config.title')}</h2></div>
  <p class="muted">{$t('config.description')}</p>
</section>

{#if loading}
  <section class="panel"><p class="muted">{$t('pickerModal.loading')}</p></section>
{:else if loadError}
  <section class="panel"><p class="empty-state danger">{loadError}</p></section>
{:else}
  {#if !canManage}
    <AccessDeniedNotice message={$t('config.readOnlyNotice')} />
  {/if}

  {#each groups as group}
    <section class="panel">
      <div class="panel-head"><h2>{group.name}</h2></div>
      <div class="config-list">
        {#each group.items as item (item.key)}
          <div class="config-row">
            <div class="config-meta">
              <div class="config-key">
                <code>{item.key}</code>
                {#if item.isOverridden}<span class="pill">{$t('config.overridden')}</span>{/if}
              </div>
              <p class="muted small">{item.description}</p>
              <p class="muted small">
                {$t('config.default')}: <code>{item.defaultValue}</code> · {item.kind}
              </p>
            </div>
            <div class="config-edit">
              {#if item.kind === 'Bool'}
                <select bind:value={editValues[item.key]} disabled={!canManage}>
                  <option value="true">true</option>
                  <option value="false">false</option>
                </select>
              {:else if item.kind === 'Json'}
                <textarea rows="2" bind:value={editValues[item.key]} disabled={!canManage}></textarea>
              {:else if item.kind === 'Int'}
                <input type="number" bind:value={editValues[item.key]} disabled={!canManage} />
              {:else}
                <input type="text" bind:value={editValues[item.key]} disabled={!canManage} />
              {/if}
              <button
                type="button"
                on:click={() => startSave(item)}
                disabled={!canManage || busy[item.key] || !isDirty(item)}
              >
                {$t('config.save')}
              </button>
            </div>
            {#if results[item.key]}
              <div class="config-result">
                <OpResult result={results[item.key]} onCopy={copy} copyLabel={$t('common.copy')} />
              </div>
            {/if}
          </div>
        {/each}
      </div>
    </section>
  {/each}
{/if}

{#if pending}
  <div class="modal-layer">
    <button class="modal-backdrop" type="button" aria-label="Cancel" on:click={cancel}></button>
    <section class="modal-panel" role="dialog" aria-modal="true" style="width: min(460px, 100%)">
      <header class="modal-header">
        <div>
          <p class="eyebrow">{$t('config.confirmEyebrow')}</p>
          <h2>{pending.key}</h2>
        </div>
      </header>
      <p class="muted small">{pending.description}</p>
      <p>{$t('config.newValue')}: <code>{pending.value}</code></p>
      <div class="op-field">
        <label for="config-reason">{$t('common.reasonRequired')}</label>
        <input
          id="config-reason"
          bind:value={reason}
          placeholder={$t('config.reasonPlaceholder')}
          list="reason-history"
        />
      </div>
      {#if reasonError}<p class="empty-state danger">{reasonError}</p>{/if}
      <div class="op-actions">
        <button type="button" on:click={confirm}>{$t('common.confirm')}</button>
        <button class="ghost-button" type="button" on:click={cancel}>{$t('common.cancel')}</button>
      </div>
    </section>
  </div>
{/if}

<style>
  .config-list {
    display: flex;
    flex-direction: column;
    gap: 0.75rem;
  }
  .config-row {
    display: grid;
    grid-template-columns: minmax(0, 1fr) minmax(0, 22rem);
    gap: 0.75rem 1.25rem;
    align-items: start;
    padding-bottom: 0.75rem;
    border-bottom: 1px solid var(--border, rgba(255, 255, 255, 0.08));
  }
  .config-row:last-child {
    border-bottom: none;
    padding-bottom: 0;
  }
  .config-key {
    display: flex;
    align-items: center;
    gap: 0.5rem;
    flex-wrap: wrap;
  }
  .config-edit {
    display: flex;
    gap: 0.5rem;
    align-items: start;
  }
  .config-edit input,
  .config-edit select,
  .config-edit textarea {
    flex: 1 1 auto;
    min-width: 0;
  }
  .config-result {
    grid-column: 1 / -1;
  }
  .small {
    font-size: 0.82em;
    margin: 0.15rem 0 0;
  }
  .pill {
    font-size: 0.72em;
    text-transform: uppercase;
    letter-spacing: 0.04em;
    padding: 0.1rem 0.45rem;
    border-radius: 999px;
    background: var(--accent-soft, rgba(212, 175, 55, 0.16));
    color: var(--accent, #d4af37);
  }
  @media (max-width: 720px) {
    .config-row {
      grid-template-columns: minmax(0, 1fr);
    }
  }
</style>
