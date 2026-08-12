<script>
  // NFT collections, their items and the collector scores. The one thing worth flagging: an item
  // whose product code matches no furniture definition makes its collection impossible to complete,
  // and nothing else in the hotel would ever say so.
  import { onMount } from 'svelte';
  import { apiGet } from '../lib/api.js';
  import { createWriteOps } from '../lib/writeOps.js';
  import { hasDashboardCapability } from '../lib/permissions.js';
  import { CAPABILITIES } from '../lib/dashboardPermissions.js';
  import { identity } from '../lib/session.js';
  import ConfirmReasonModal from '../components/ConfirmReasonModal.svelte';
  import OpResult from '../components/OpResult.svelte';

  import { formatNumber, formatDate } from '../lib/format.js';
  import { isPermissionDeniedError } from '../lib/permissions.js';
  import { openPlayer } from '../lib/session.js';
  import AccessDeniedNotice from '../components/AccessDeniedNotice.svelte';
  import AssetImage from '../components/AssetImage.svelte';
  import EmptyState from '../components/EmptyState.svelte';
  import EntityLink from '../components/EntityLink.svelte';
  import PickerModal from '../components/PickerModal.svelte';
  import StatCard from '../components/StatCard.svelte';
  import { Gem, Boxes, TriangleAlert, Trophy } from '@lucide/svelte';
  import { t } from '../lib/i18n.js';

  let loading = false;
  let forbidden = false;
  let error = '';
  let data = null;
  let expanded = null;

  const ops = createWriteOps(refresh);

  $: canManage = hasDashboardCapability($identity, CAPABILITIES.opsContentManage);

  const emptyCollection = () => ({
    id: 0,
    collectionCode: '',
    name: '',
    boostScore: 0,
    status: 0,
    rewardProductCode: '',
    bonusProductCode: '',
  });
  const emptyItem = () => ({
    id: 0,
    productCode: '',
    itemTypeId: '',
    productTypeId: 0,
    score: 1,
    rarity: '',
    sortOrder: 0,
  });

  let collectionForm = emptyCollection();
  let itemForm = emptyItem();
  let pickingFurniture = false;

  // The product code is a furniture classname, so it is picked from the real catalogue rather than
  // typed: a code that matches nothing is exactly what makes a collection uncompletable.
  $: itemPreviewUrl =
    (data?.collections || [])
      .flatMap((c) => c.items || [])
      .find((i) => i.productCode === itemForm.productCode)?.iconUrl ?? null;

  async function refresh() {
    loading = true;
    error = '';
    forbidden = false;

    try {
      data = await apiGet('/api/v1/collectibles');
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

  onMount(() => {
    void refresh();
  });
</script>

<section class="panel">
  <div class="panel-head"><h2>{$t('collectibles.title')}</h2></div>
  <p class="muted">{$t('collectibles.description')}</p>
  <div class="toolbar">
    <button type="button" on:click={refresh} disabled={loading}>{$t('common.refresh')}</button>
  </div>

  {#if loading}
    <p class="muted">{$t('common.loading')}</p>
  {:else if forbidden}
    <AccessDeniedNotice message={$t('collectibles.accessDenied')} />
  {:else if error}
    <p class="empty-state danger">{error}</p>
  {/if}
</section>

{#if data}
  <div class="metric-grid" style="margin-top: 12px;">
    <StatCard label={$t('collectibles.collections')} value={formatNumber(data.totals.collections)}>
      <Gem slot="icon" size={15} strokeWidth={2} aria-hidden="true" />
    </StatCard>
    <StatCard label={$t('collectibles.items')} value={formatNumber(data.totals.items)}>
      <Boxes slot="icon" size={15} strokeWidth={2} aria-hidden="true" />
    </StatCard>
    <StatCard label={$t('collectibles.completable')} value={formatNumber(data.totals.completableCollections)}>
      <Gem slot="icon" size={15} strokeWidth={2} aria-hidden="true" />
    </StatCard>
    <StatCard label={$t('collectibles.unresolved')} value={formatNumber(data.totals.unresolvedItems)}>
      <TriangleAlert slot="icon" size={15} strokeWidth={2} aria-hidden="true" />
    </StatCard>
    <StatCard label={$t('collectibles.trackedPlayers')} value={formatNumber(data.totals.trackedPlayers)}>
      <Trophy slot="icon" size={15} strokeWidth={2} aria-hidden="true" />
    </StatCard>
  </div>

  <section class="panel" style="margin-top: 12px;">
    <div class="panel-head"><h2>{$t('collectibles.collectionsTitle')}</h2></div>
    {#if (data.collections || []).length === 0}
      <EmptyState message={$t('collectibles.noCollections')} />
    {:else}
      <div class="table-wrap">
        <table>
          <thead>
            <tr>
              <th>{$t('collectibles.colCollection')}</th>
              <th>{$t('collectibles.colCode')}</th>
              <th>{$t('collectibles.colItems')}</th>
              <th>{$t('collectibles.colScore')}</th>
              <th>{$t('collectibles.colBoost')}</th>
              <th>{$t('collectibles.colStatus')}</th>
              <th>{$t('collectibles.colReleased')}</th>
              {#if canManage}<th></th>{/if}
            </tr>
          </thead>
          <tbody>
            {#each data.collections as collection}
              <tr
                on:click={() => (expanded = expanded === collection.id ? null : collection.id)}
                style="cursor: pointer;"
                class:selected={expanded === collection.id}
              >
                <td>{collection.name}</td>
                <td><code>{collection.collectionCode}</code></td>
                <td>
                  {formatNumber(collection.itemCount)}
                  {#if collection.unresolvedItems > 0}
                    <span class="status-badge status-badge--bad">
                      {$t('collectibles.unresolvedCount', { count: collection.unresolvedItems })}
                    </span>
                  {/if}
                </td>
                <td>{formatNumber(collection.totalScore)}</td>
                <td>{formatNumber(collection.boostScore)}</td>
                <td>{collection.status}</td>
                <td>{collection.releasedAt ? formatDate(collection.releasedAt) : '—'}</td>
                {#if canManage}
                  <td class="row-actions">
                    <button
                      type="button"
                      class="ghost-button"
                      on:click|stopPropagation={() => (collectionForm = { ...collection })}
                    >
                      {$t('collectibles.edit')}
                    </button>
                    <button
                      type="button"
                      class="ghost-button danger"
                      on:click|stopPropagation={() =>
                        ops.ask(
                          '/api/v1/operations/content/collections/delete',
                          { collectionId: collection.id },
                          $t('collectibles.deleteCollection'),
                          $t('collectibles.deleteCollectionSummary', { name: collection.name })
                        )}
                    >
                      {$t('collectibles.delete')}
                    </button>
                  </td>
                {/if}
              </tr>
              {#if expanded === collection.id}
                <tr>
                  <td colspan={canManage ? 8 : 7}>
                    <div class="table-wrap">
                      <table>
                        <thead>
                          <tr>
                            <th>{$t('collectibles.colItem')}</th>
                            <th>{$t('collectibles.colRarity')}</th>
                            <th>{$t('collectibles.colItemScore')}</th>
                            <th>{$t('collectibles.colResolved')}</th>
                            {#if canManage}<th></th>{/if}
                          </tr>
                        </thead>
                        <tbody>
                          {#each collection.items || [] as item}
                            <tr>
                              <td>
                                <span class="cell">
                                  <AssetImage src={item.iconUrl} alt={item.productCode} size={32} />
                                  <code>{item.productCode}</code>
                                </span>
                              </td>
                              <td>{item.rarity || '—'}</td>
                              <td>{formatNumber(item.score)}</td>
                              <td>
                                {#if item.resolved}
                                  <span class="status-badge status-badge--ok">{$t('collectibles.resolved')}</span>
                                {:else}
                                  <span class="status-badge status-badge--bad">{$t('collectibles.missingFurni')}</span>
                                {/if}
                              </td>
                              {#if canManage}
                                <td class="row-actions">
                                  <button type="button" class="ghost-button" on:click={() => (itemForm = { ...item })}>
                                    {$t('collectibles.edit')}
                                  </button>
                                  <button
                                    type="button"
                                    class="ghost-button danger"
                                    on:click={() =>
                                      ops.ask(
                                        '/api/v1/operations/content/collections/items/delete',
                                        { itemId: item.id },
                                        $t('collectibles.deleteItem'),
                                        $t('collectibles.deleteItemSummary', { code: item.productCode })
                                      )}
                                  >
                                    {$t('collectibles.delete')}
                                  </button>
                                </td>
                              {/if}
                            </tr>
                          {:else}
                            <tr><td colspan="4" class="muted">{$t('collectibles.noItems')}</td></tr>
                          {/each}
                        </tbody>
                      </table>
                    </div>
                  </td>
                </tr>
              {/if}
            {/each}
          </tbody>
        </table>
      </div>
    {/if}
  </section>

  {#if canManage}
    <section class="panel" style="margin-top: 12px;">
      <div class="panel-head"><h2>{$t('collectibles.editorTitle')}</h2></div>
      <form
        class="inline-form"
        on:submit|preventDefault={() =>
          ops.ask(
            '/api/v1/operations/content/collections',
            {
              collectionId: Number(collectionForm.id) || 0,
              collectionCode: collectionForm.collectionCode,
              name: collectionForm.name,
              boostScore: Number(collectionForm.boostScore) || 0,
              status: Number(collectionForm.status) || 0,
              rewardProductCode: collectionForm.rewardProductCode || null,
              bonusProductCode: collectionForm.bonusProductCode || null,
            },
            collectionForm.id ? $t('collectibles.updateCollection') : $t('collectibles.addCollection'),
            $t('collectibles.saveCollectionSummary', { name: collectionForm.name })
          )}
      >
        <label>
          {$t('collectibles.colCode')}
          <input bind:value={collectionForm.collectionCode} />
        </label>
        <label>
          {$t('collectibles.colCollection')}
          <input bind:value={collectionForm.name} />
        </label>
        <label>
          {$t('collectibles.colBoost')}
          <input type="number" bind:value={collectionForm.boostScore} />
        </label>
        <label>
          {$t('collectibles.colStatus')}
          <input type="number" bind:value={collectionForm.status} />
        </label>
        <label>
          {$t('collectibles.rewardProduct')}
          <input bind:value={collectionForm.rewardProductCode} />
        </label>
        <label>
          {$t('collectibles.bonusProduct')}
          <input bind:value={collectionForm.bonusProductCode} />
        </label>
        <button type="submit" disabled={!collectionForm.collectionCode.trim() || !collectionForm.name.trim()}>
          {collectionForm.id ? $t('collectibles.updateCollection') : $t('collectibles.addCollection')}
        </button>
        {#if collectionForm.id}
          <button type="button" class="ghost-button" on:click={() => (collectionForm = emptyCollection())}>
            {$t('collectibles.newCollection')}
          </button>
        {/if}
      </form>

      {#if expanded}
        <h3 class="subhead">{$t('collectibles.itemEditorTitle')}</h3>
        <form
          class="inline-form"
          on:submit|preventDefault={() =>
            ops.ask(
              '/api/v1/operations/content/collections/items',
              {
                itemId: Number(itemForm.id) || 0,
                collectionId: expanded,
                productCode: itemForm.productCode,
                itemTypeId: itemForm.itemTypeId || '',
                productTypeId: Number(itemForm.productTypeId) || 0,
                score: Number(itemForm.score) || 0,
                rarity: itemForm.rarity || '',
                sortOrder: Number(itemForm.sortOrder) || 0,
              },
              $t('collectibles.saveItem'),
              $t('collectibles.saveItemSummary', { code: itemForm.productCode })
            )}
        >
          <label>
            {$t('collectibles.colItem')}
            <span class="cell">
              <AssetImage src={itemPreviewUrl} alt={itemForm.productCode} size={32} />
              <input bind:value={itemForm.productCode} placeholder="classname" />
              <button type="button" class="ghost-button" on:click={() => (pickingFurniture = true)}>
                {$t('collectibles.pickFurniture')}
              </button>
            </span>
          </label>
          <label>
            {$t('collectibles.colRarity')}
            <input bind:value={itemForm.rarity} />
          </label>
          <label>
            {$t('collectibles.colItemScore')}
            <input type="number" bind:value={itemForm.score} min="0" />
          </label>
          <label>
            {$t('collectibles.sortOrder')}
            <input type="number" bind:value={itemForm.sortOrder} />
          </label>
          <button type="submit" disabled={!itemForm.productCode.trim()}>{$t('collectibles.saveItem')}</button>
          <button type="button" class="ghost-button" on:click={() => (itemForm = emptyItem())}>
            {$t('collectibles.newItem')}
          </button>
        </form>
      {:else}
        <p class="muted">{$t('collectibles.pickToEditItems')}</p>
      {/if}

      {#if $ops.result}
        <OpResult result={$ops.result} />
      {/if}
    </section>
  {/if}

  <section class="panel" style="margin-top: 12px;">
    <div class="panel-head"><h2>{$t('collectibles.leaderboardTitle')}</h2></div>
    <div class="table-wrap">
      <table>
        <thead>
          <tr>
            <th>{$t('collectibles.colPlayer')}</th>
            <th>{$t('collectibles.colCollectorScore')}</th>
          </tr>
        </thead>
        <tbody>
          {#each data.topCollectors || [] as row}
            <tr>
              <td><EntityLink type="player" id={row.playerId} label={row.playerName} {openPlayer} /></td>
              <td>{formatNumber(row.score)}</td>
            </tr>
          {:else}
            <tr><td colspan="2" class="muted">{$t('collectibles.noCollectors')}</td></tr>
          {/each}
        </tbody>
      </table>
    </div>
  </section>
{/if}

{#if pickingFurniture}
  <PickerModal
    kind="furniture"
    title={$t('collectibles.pickFurniture')}
    onSelect={(picked) => {
      itemForm.productCode = picked.name;
      pickingFurniture = false;
    }}
    onClose={() => (pickingFurniture = false)}
  />
{/if}

<ConfirmReasonModal
  open={Boolean($ops.pending)}
  title={$ops.pending?.title ?? ''}
  summary={$ops.pending?.summary ?? ''}
  confirmLabel={$ops.pending?.title ?? $t('common.confirm')}
  busy={$ops.busy}
  error={$ops.error}
  on:confirm={(e) => ops.confirm(e.detail)}
  on:cancel={() => ops.cancel()}
/>

<style>

  tr.selected {
    background: var(--surface-raised, rgba(255, 255, 255, 0.04));
  }

  .cell {
    display: inline-flex;
    align-items: center;
    gap: 8px;
  }
</style>
