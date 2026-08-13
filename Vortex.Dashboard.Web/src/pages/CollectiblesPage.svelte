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

  // Which furniture picker is open, if any: the collection's two prizes and the collection item all
  // pick from the same catalogue, so they share one modal rather than three.
  let picking = null;

  // The icons of the two prizes, remembered from the pick. They are not in `data` like the item
  // icons are -- a prize is a classname on the collection, not a row in its item list -- so without
  // this the admin picks a chair and sees only the word.
  let rewardIconUrl = null;
  let bonusIconUrl = null;

  // Only the statuses mean something, and only on this side: the client parses the field and never
  // reads it, so Draft is a collection the server withholds rather than one the client hides.
  const STATUS_OPTIONS = [
    { value: 0, key: 'collectibles.statusDraft' },
    { value: 1, key: 'collectibles.statusVisible' },
    { value: 2, key: 'collectibles.statusArchived' },
  ];

  // The client's own list, from its rarity colour table. Anything outside it still renders, but in
  // the default grey, so typing a rarity of one's own invention is how an item ends up looking
  // broken for no visible reason.
  const RARITY_OPTIONS = ['common', 'uncommon', 'rare', 'epic', 'legendary', 'legendary+'];

  const statusLabel = (value) =>
    $t(STATUS_OPTIONS.find((o) => o.value === Number(value))?.key ?? 'collectibles.statusUnknown');

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
                <td>
                  <span
                    class="status-badge"
                    class:status-badge--ok={Number(collection.status) === 1}
                  >
                    {statusLabel(collection.status)}
                  </span>
                </td>
                <td>{collection.releasedAt ? formatDate(collection.releasedAt) : '—'}</td>
                {#if canManage}
                  <td class="row-actions">
                    <button
                      type="button"
                      class="ghost-button"
                      on:click|stopPropagation={() => {
                        collectionForm = { ...collection };
                        // The prize icons are not in the listing, so they stay blank until the
                        // admin picks again rather than showing the previous collection's.
                        rewardIconUrl = null;
                        bonusIconUrl = null;
                      }}
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
        class="inline-form editor-form"
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
          <select bind:value={collectionForm.status}>
            {#each STATUS_OPTIONS as option}
              <option value={option.value}>{$t(option.key)}</option>
            {/each}
          </select>
          <small class="muted">{$t('collectibles.statusHelp')}</small>
        </label>
        <label>
          {$t('collectibles.rewardProduct')}
          <span class="cell">
            <AssetImage src={rewardIconUrl} alt={collectionForm.rewardProductCode} size={32} />
            <input bind:value={collectionForm.rewardProductCode} placeholder="classname" readonly />
            <button type="button" class="ghost-button" on:click={() => (picking = 'reward')}>
              {$t('collectibles.pickFurniture')}
            </button>
            {#if collectionForm.rewardProductCode}
              <button
                type="button"
                class="ghost-button"
                on:click={() => {
                  collectionForm.rewardProductCode = '';
                  rewardIconUrl = null;
                }}
              >
                {$t('collectibles.clearPrize')}
              </button>
            {/if}
          </span>
          <small class="muted">{$t('collectibles.rewardProductHelp')}</small>
        </label>
        <label>
          {$t('collectibles.bonusProduct')}
          <span class="cell">
            <AssetImage src={bonusIconUrl} alt={collectionForm.bonusProductCode} size={32} />
            <input bind:value={collectionForm.bonusProductCode} placeholder="classname" readonly />
            <button type="button" class="ghost-button" on:click={() => (picking = 'bonus')}>
              {$t('collectibles.pickFurniture')}
            </button>
            {#if collectionForm.bonusProductCode}
              <button
                type="button"
                class="ghost-button"
                on:click={() => {
                  collectionForm.bonusProductCode = '';
                  bonusIconUrl = null;
                }}
              >
                {$t('collectibles.clearPrize')}
              </button>
            {/if}
          </span>
          <small class="muted">{$t('collectibles.bonusProductHelp')}</small>
        </label>
        <div class="form-actions">
          <button type="submit" disabled={!collectionForm.collectionCode.trim() || !collectionForm.name.trim()}>
            {collectionForm.id ? $t('collectibles.updateCollection') : $t('collectibles.addCollection')}
          </button>
          {#if collectionForm.id}
            <button
              type="button"
              class="ghost-button"
              on:click={() => {
                collectionForm = emptyCollection();
                rewardIconUrl = null;
                bonusIconUrl = null;
              }}
            >
              {$t('collectibles.newCollection')}
            </button>
          {/if}
        </div>
      </form>

      {#if expanded}
        <h3 class="subhead">{$t('collectibles.itemEditorTitle')}</h3>
        <form
          class="inline-form editor-form"
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
              <input bind:value={itemForm.productCode} placeholder="classname" readonly />
              <button type="button" class="ghost-button" on:click={() => (picking = 'item')}>
                {$t('collectibles.pickFurniture')}
              </button>
            </span>
            <small class="muted">{$t('collectibles.itemProductHelp')}</small>
          </label>
          <label>
            {$t('collectibles.colRarity')}
            <select bind:value={itemForm.rarity}>
              <option value="">{$t('collectibles.rarityNone')}</option>
              {#each RARITY_OPTIONS as rarity}
                <option value={rarity}>{rarity}</option>
              {/each}
            </select>
            <small class="muted">{$t('collectibles.rarityHelp')}</small>
          </label>
          <label>
            {$t('collectibles.colItemScore')}
            <input type="number" bind:value={itemForm.score} min="0" />
          </label>
          <label>
            {$t('collectibles.sortOrder')}
            <input type="number" bind:value={itemForm.sortOrder} />
          </label>
          <div class="form-actions">
            <button type="submit" disabled={!itemForm.productCode.trim()}>{$t('collectibles.saveItem')}</button>
            <button type="button" class="ghost-button" on:click={() => (itemForm = emptyItem())}>
              {$t('collectibles.newItem')}
            </button>
          </div>
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

{#if picking}
  <PickerModal
    kind="furniture"
    title={$t('collectibles.pickFurniture')}
    onSelect={(picked) => {
      if (picking === 'reward') {
        collectionForm.rewardProductCode = picked.name;
        rewardIconUrl = picked.iconUrl ?? null;
      } else if (picking === 'bonus') {
        collectionForm.bonusProductCode = picked.name;
        bonusIconUrl = picked.iconUrl ?? null;
      } else {
        itemForm.productCode = picked.name;
      }

      picking = null;
    }}
    onClose={() => (picking = null)}
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

  /* These two editors carry a line of help under most fields, and the shared .inline-form aligns
     its children on their bottom edge -- so hints of different lengths pushed each field to its own
     height and the row read as broken. A grid gives every field its own cell and starts them all at
     the top, which leaves the hints free to be as long as they need to be. */
  .editor-form {
    display: grid;
    grid-template-columns: repeat(auto-fit, minmax(230px, 1fr));
    align-items: start;
    gap: 14px 12px;
  }

  .editor-form label {
    gap: 6px;
  }

  .editor-form small {
    line-height: 1.35;
  }

  /* The buttons are not a field: they take the whole width below the last row rather than sitting
     in a cell of their own and stretching to a column's width. */
  .editor-form .form-actions {
    grid-column: 1 / -1;
    display: flex;
    flex-wrap: wrap;
    gap: 10px;
  }

  /* A picked value and its two buttons can outgrow a narrow column; let them wrap inside it. */
  .editor-form .cell {
    display: flex;
    flex-wrap: wrap;
  }

  .editor-form .cell input {
    min-width: 0;
    flex: 1 1 110px;
  }
</style>
