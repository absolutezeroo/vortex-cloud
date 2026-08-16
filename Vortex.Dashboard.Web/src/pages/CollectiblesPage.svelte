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
  import { CURRENCY_KIND, currencyChipClass } from '../lib/currency.js';
  import CurrencyIcon from '../components/CurrencyIcon.svelte';
  import { isPermissionDeniedError } from '../lib/permissions.js';
  import { openPlayer } from '../lib/session.js';
  import AccessDeniedNotice from '../components/AccessDeniedNotice.svelte';
  import AssetImage from '../components/AssetImage.svelte';
  import EmptyState from '../components/EmptyState.svelte';
  import EntityLink from '../components/EntityLink.svelte';
  import PickerModal from '../components/PickerModal.svelte';
  import StatCard from '../components/StatCard.svelte';
  import Tabs from '../components/Tabs.svelte';
  import { Gem, Boxes, TriangleAlert, Trophy, Store, Gift, Hammer, Ticket, Sparkles } from '@lucide/svelte';
  import { t } from '../lib/i18n.js';

  // Three jobs on one page -- the collections, the shop and the collector standings -- and stacking
  // them meant scrolling past two to reach the third.
  let tab = 'collections';

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

  const emptyOffer = () => ({
    id: 0,
    productCode: '',
    emeraldPrice: 0,
    isFeatured: false,
    isLimited: false,
    mintLimit: 0,
    itemTypeId: '',
    productTypeId: 0,
    score: 0,
    rarity: '',
    enabled: true,
    sortOrder: 0,
  });

  let offerForm = emptyOffer();

  const emptyClaim = () => ({
    playerId: '',
    playerName: '',
    productCode: '',
    setId: '',
    defaultCollectionName: '',
    collection: '',
    claimLimit: 1,
    validTo: '',
  });

  let claimForm = emptyClaim();
  let claimIconUrl = null;

  // A window is not optional: the client greys the convert button out once the end date has passed,
  // and says nothing about why. A new type therefore opens now and runs for a year rather than
  // starting empty and being saved unusable.
  const isoLocal = (date) => new Date(date.getTime() - date.getTimezoneOffset() * 60000)
    .toISOString()
    .slice(0, 16);

  const emptyMintable = () => {
    const now = new Date();
    const inAYear = new Date(now.getTime() + 365 * 24 * 60 * 60 * 1000);

    return {
      id: 0,
      productCode: '',
      stampPrice: 1,
      startsAt: isoLocal(now),
      endsAt: isoLocal(inAYear),
      regionLocked: false,
      limitedEdition: false,
      editionSize: 0,
      enabled: true,
      sortOrder: 0,
    };
  };

  let mintableForm = emptyMintable();

  const emptyTokenOffer = () => ({
    id: 0,
    productCode: '',
    silverPrice: 10,
    amountTokens: 1,
    enabled: true,
    sortOrder: 0,
  });

  let tokenOfferForm = emptyTokenOffer();

  $: mintableIconUrl =
    (data?.mintableTypes || []).find((t) => t.productCode === mintableForm.productCode)?.iconUrl ??
    null;

  // The editors bind to datetime-local inputs, which speak local time with no zone; the API takes
  // instants. A row loaded for editing therefore has to come back the other way round.
  const editMintable = (row) => {
    mintableForm = {
      ...row,
      startsAt: isoLocal(new Date(row.startsAt)),
      endsAt: isoLocal(new Date(row.endsAt)),
    };
  };

  // The shop's own icon, looked up in the listing: unlike the two prizes, an offer is a row, so its
  // image is already on the page once it has been saved.
  $: offerIconUrl =
    (data?.storeOffers || []).find((o) => o.productCode === offerForm.productCode)?.iconUrl ?? null;

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
    <StatCard label={$t('collectibles.mintableOpen')} value={formatNumber(data.totals.mintableTypesOpen)}>
      <Hammer slot="icon" size={15} strokeWidth={2} aria-hidden="true" />
    </StatCard>
    <StatCard label={$t('collectibles.mintedRelics')} value={formatNumber(data.totals.mintedRelics)}>
      <Sparkles slot="icon" size={15} strokeWidth={2} aria-hidden="true" />
    </StatCard>
    <StatCard label={$t('collectibles.stampsHeld')} value={formatNumber(data.totals.stampsHeld)}>
      <Ticket slot="icon" size={15} strokeWidth={2} aria-hidden="true" />
    </StatCard>
  </div>

  <Tabs
    bind:active={tab}
    storageKey="collectibles"
    tabs={[
      { id: 'collections', label: $t('collectibles.tabCollections'), icon: Gem, count: (data.collections || []).length },
      { id: 'shop', label: $t('collectibles.tabShop'), icon: Store, count: (data.storeOffers || []).length },
      { id: 'minting', label: $t('collectibles.tabMinting'), icon: Hammer, count: (data.mintableTypes || []).length },
      { id: 'stamps', label: $t('collectibles.tabStamps'), icon: Ticket, count: (data.tokenOffers || []).length },
      { id: 'relics', label: $t('collectibles.tabRelics'), icon: Sparkles, count: (data.assets || []).length },
      { id: 'claims', label: $t('collectibles.tabClaims'), icon: Gift, count: (data.claims || []).length },
      { id: 'collectors', label: $t('collectibles.tabCollectors'), icon: Trophy, count: (data.topCollectors || []).length },
    ]}
  />

  {#if tab === 'collections'}
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
  {/if}

  {#if tab === 'shop'}
  <section class="panel" style="margin-top: 12px;">
    <div class="panel-head"><h2>{$t('collectibles.shopTitle')}</h2></div>
    <p class="muted">{$t('collectibles.shopDescription')}</p>
    {#if (data.storeOffers || []).length === 0}
      <EmptyState message={$t('collectibles.noOffers')} />
    {:else}
      <div class="table-wrap">
        <table>
          <thead>
            <tr>
              <th>{$t('collectibles.colItem')}</th>
              <th>{$t('collectibles.colPrice')}</th>
              <th>{$t('collectibles.colItemScore')}</th>
              <th>{$t('collectibles.colRarity')}</th>
              <th>{$t('collectibles.colStock')}</th>
              <th>{$t('collectibles.colOnSale')}</th>
              {#if canManage}<th></th>{/if}
            </tr>
          </thead>
          <tbody>
            {#each data.storeOffers as offer}
              <tr>
                <td>
                  <span class="cell">
                    <AssetImage src={offer.iconUrl} alt={offer.productCode} size={32} />
                    <code>{offer.productCode}</code>
                    {#if !offer.isNft}
                      <span class="status-badge status-badge--bad" title={$t('collectibles.notNftHelp')}>
                        {$t('collectibles.notNft')}
                      </span>
                    {/if}
                    {#if offer.isFeatured}
                      <span class="status-badge">{$t('collectibles.featured')}</span>
                    {/if}
                  </span>
                </td>
                <td>
                  <span class={currencyChipClass(CURRENCY_KIND.emeralds)}>
                    <CurrencyIcon kind={CURRENCY_KIND.emeralds} /> {formatNumber(offer.emeraldPrice)}
                  </span>
                </td>
                <td>{formatNumber(offer.score)}</td>
                <td>{offer.rarity || '—'}</td>
                <td>
                  {#if offer.mintLimit > 0}
                    {formatNumber(offer.soldCount)} / {formatNumber(offer.mintLimit)}
                  {:else}
                    {formatNumber(offer.soldCount)} / ∞
                  {/if}
                </td>
                <td>
                  {#if !offer.resolved}
                    <span class="status-badge status-badge--bad">{$t('collectibles.missingFurni')}</span>
                  {:else if !offer.enabled}
                    <span class="status-badge">{$t('collectibles.offerDisabled')}</span>
                  {:else if offer.soldOut}
                    <span class="status-badge status-badge--bad">{$t('collectibles.soldOut')}</span>
                  {:else}
                    <span class="status-badge status-badge--ok">{$t('collectibles.onSale')}</span>
                  {/if}
                </td>
                {#if canManage}
                  <td class="row-actions">
                    <button type="button" class="ghost-button" on:click={() => (offerForm = { ...offer })}>
                      {$t('collectibles.edit')}
                    </button>
                    <button
                      type="button"
                      class="ghost-button danger"
                      on:click={() =>
                        ops.ask(
                          '/api/v1/operations/content/store-offers/delete',
                          { offerId: offer.id },
                          $t('collectibles.deleteOffer'),
                          $t('collectibles.deleteOfferSummary', { code: offer.productCode })
                        )}
                    >
                      {$t('collectibles.delete')}
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
      <h3 class="subhead">{$t('collectibles.offerEditorTitle')}</h3>
      <form
        class="inline-form editor-form"
        on:submit|preventDefault={() =>
          ops.ask(
            '/api/v1/operations/content/store-offers',
            {
              offerId: Number(offerForm.id) || 0,
              productCode: offerForm.productCode,
              emeraldPrice: Number(offerForm.emeraldPrice) || 0,
              isFeatured: Boolean(offerForm.isFeatured),
              isLimited: Boolean(offerForm.isLimited),
              mintLimit: Number(offerForm.mintLimit) || 0,
              itemTypeId: offerForm.itemTypeId || '',
              productTypeId: Number(offerForm.productTypeId) || 0,
              score: Number(offerForm.score) || 0,
              rarity: offerForm.rarity || '',
              enabled: Boolean(offerForm.enabled),
              sortOrder: Number(offerForm.sortOrder) || 0,
            },
            offerForm.id ? $t('collectibles.updateOffer') : $t('collectibles.addOffer'),
            $t('collectibles.saveOfferSummary', { code: offerForm.productCode })
          )}
      >
        <label>
          {$t('collectibles.colItem')}
          <span class="cell">
            <AssetImage src={offerIconUrl} alt={offerForm.productCode} size={32} />
            <input bind:value={offerForm.productCode} placeholder="classname" readonly />
            <button type="button" class="ghost-button" on:click={() => (picking = 'offer')}>
              {$t('collectibles.pickFurniture')}
            </button>
          </span>
          <small class="muted">{$t('collectibles.offerProductHelpLong')}</small>
        </label>
        <label>
          {$t('collectibles.colPrice')}
          <input type="number" min="0" bind:value={offerForm.emeraldPrice} />
          <small class="muted">{$t('collectibles.priceHelp')}</small>
        </label>
        <label>
          {$t('collectibles.colItemScore')}
          <input type="number" min="0" bind:value={offerForm.score} />
          <small class="muted">{$t('collectibles.offerScoreHelp')}</small>
        </label>
        <label>
          {$t('collectibles.colRarity')}
          <select bind:value={offerForm.rarity}>
            <option value="">{$t('collectibles.rarityNone')}</option>
            {#each RARITY_OPTIONS as rarity}
              <option value={rarity}>{rarity}</option>
            {/each}
          </select>
        </label>
        <label>
          {$t('collectibles.mintLimit')}
          <input type="number" min="0" bind:value={offerForm.mintLimit} />
          <small class="muted">{$t('collectibles.mintLimitHelp')}</small>
        </label>
        <label>
          {$t('collectibles.sortOrder')}
          <input type="number" bind:value={offerForm.sortOrder} />
        </label>
        <label class="check">
          <input type="checkbox" bind:checked={offerForm.isFeatured} />
          {$t('collectibles.featured')}
        </label>
        <label class="check">
          <input type="checkbox" bind:checked={offerForm.isLimited} />
          {$t('collectibles.limitedEdition')}
        </label>
        <label class="check">
          <input type="checkbox" bind:checked={offerForm.enabled} />
          {$t('collectibles.offerEnabled')}
        </label>
        <div class="form-actions">
          <button type="submit" disabled={!offerForm.productCode.trim()}>
            {offerForm.id ? $t('collectibles.updateOffer') : $t('collectibles.addOffer')}
          </button>
          <button type="button" class="ghost-button" on:click={() => (offerForm = emptyOffer())}>
            {$t('collectibles.newOffer')}
          </button>
        </div>
      </form>
    {/if}
  </section>
  {/if}

  {#if tab === 'minting'}
  <section class="panel" style="margin-top: 12px;">
    <div class="panel-head"><h2>{$t('collectibles.mintingTitle')}</h2></div>
    <p class="muted">{$t('collectibles.mintingDescription')}</p>
    {#if (data.mintableTypes || []).length === 0}
      <EmptyState message={$t('collectibles.noMintableTypes')} />
    {:else}
      <div class="table-wrap">
        <table>
          <thead>
            <tr>
              <th>{$t('collectibles.colItem')}</th>
              <th>{$t('collectibles.colStampPrice')}</th>
              <th>{$t('collectibles.colEdition')}</th>
              <th>{$t('collectibles.colWindow')}</th>
              <th>{$t('collectibles.colOnSale')}</th>
              {#if canManage}<th></th>{/if}
            </tr>
          </thead>
          <tbody>
            {#each data.mintableTypes as type}
              <tr>
                <td>
                  <span class="cell">
                    <AssetImage src={type.iconUrl} alt={type.productCode} size={32} />
                    <code>{type.productCode}</code>
                    {#if !type.isNft}
                      <span class="status-badge status-badge--bad" title={$t('collectibles.notNftHelp')}>
                        {$t('collectibles.notNft')}
                      </span>
                    {/if}
                    {#if type.limitedEdition}
                      <span class="status-badge">{$t('collectibles.limitedEdition')}</span>
                    {/if}
                  </span>
                </td>
                <td>
                  {#if type.editionSize > 0}
                    {formatNumber(type.mintedCount)} / {formatNumber(type.editionSize)}
                  {:else}
                    {formatNumber(type.mintedCount)} / ∞
                  {/if}
                </td>
                <td>{formatDate(type.startsAt)} → {formatDate(type.endsAt)}</td>
                <td>
                  {#if !type.resolved}
                    <span class="status-badge status-badge--bad">{$t('collectibles.missingFurni')}</span>
                  {:else if !type.enabled}
                    <span class="status-badge">{$t('collectibles.offerDisabled')}</span>
                  {:else if type.exhausted}
                    <span class="status-badge status-badge--bad">{$t('collectibles.editionGone')}</span>
                  {:else if type.expired}
                    <span class="status-badge status-badge--bad">{$t('collectibles.windowClosed')}</span>
                  {:else if !type.open}
                    <span class="status-badge">{$t('collectibles.windowNotOpen')}</span>
                  {:else}
                    <span class="status-badge status-badge--ok">{$t('collectibles.mintable')}</span>
                  {/if}
                </td>
                {#if canManage}
                  <td class="row-actions">
                    <button type="button" class="ghost-button" on:click={() => editMintable(type)}>
                      {$t('collectibles.edit')}
                    </button>
                    <button
                      type="button"
                      class="ghost-button danger"
                      on:click={() =>
                        ops.ask(
                          '/api/v1/operations/content/mintable-types/delete',
                          { typeId: type.id },
                          $t('collectibles.deleteMintable'),
                          $t('collectibles.deleteMintableSummary', { code: type.productCode })
                        )}
                    >
                      {$t('collectibles.delete')}
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
      <h3 class="subhead">{$t('collectibles.mintableEditorTitle')}</h3>
      <form
        class="inline-form editor-form"
        on:submit|preventDefault={() =>
          ops.ask(
            '/api/v1/operations/content/mintable-types',
            {
              typeId: Number(mintableForm.id) || 0,
              productCode: mintableForm.productCode,
              stampPrice: Number(mintableForm.stampPrice) || 0,
              startsAt: new Date(mintableForm.startsAt).toISOString(),
              endsAt: new Date(mintableForm.endsAt).toISOString(),
              regionLocked: Boolean(mintableForm.regionLocked),
              limitedEdition: Boolean(mintableForm.limitedEdition),
              editionSize: Number(mintableForm.editionSize) || 0,
              enabled: Boolean(mintableForm.enabled),
              sortOrder: Number(mintableForm.sortOrder) || 0,
            },
            mintableForm.id ? $t('collectibles.updateMintable') : $t('collectibles.addMintable'),
            $t('collectibles.saveMintableSummary', { code: mintableForm.productCode })
          )}
      >
        <label>
          {$t('collectibles.colItem')}
          <span class="cell">
            <AssetImage src={mintableIconUrl} alt={mintableForm.productCode} size={32} />
            <input bind:value={mintableForm.productCode} placeholder="classname" readonly />
            <button type="button" class="ghost-button" on:click={() => (picking = 'mintable')}>
              {$t('collectibles.pickFurniture')}
            </button>
          </span>
          <small class="muted">{$t('collectibles.mintableProductHelp')}</small>
        </label>
        <label>
          {$t('collectibles.colStampPrice')}
          <input type="number" min="0" bind:value={mintableForm.stampPrice} />
          <small class="muted">{$t('collectibles.stampPriceHelp')}</small>
        </label>
        <label>
          {$t('collectibles.opensAt')}
          <input type="datetime-local" bind:value={mintableForm.startsAt} />
        </label>
        <label>
          {$t('collectibles.closesAt')}
          <input type="datetime-local" bind:value={mintableForm.endsAt} />
          <small class="muted">{$t('collectibles.windowHelp')}</small>
        </label>
        <label>
          {$t('collectibles.editionSize')}
          <input type="number" min="0" bind:value={mintableForm.editionSize} />
          <small class="muted">{$t('collectibles.editionSizeHelp')}</small>
        </label>
        <label>
          {$t('collectibles.sortOrder')}
          <input type="number" bind:value={mintableForm.sortOrder} />
        </label>
        <label class="check">
          <input type="checkbox" bind:checked={mintableForm.limitedEdition} />
          {$t('collectibles.limitedEdition')}
        </label>
        <label class="check">
          <input type="checkbox" bind:checked={mintableForm.regionLocked} />
          {$t('collectibles.regionLocked')}
        </label>
        <label class="check">
          <input type="checkbox" bind:checked={mintableForm.enabled} />
          {$t('collectibles.offerEnabled')}
        </label>
        <div class="form-actions">
          <button type="submit" disabled={!mintableForm.productCode.trim()}>
            {mintableForm.id ? $t('collectibles.updateMintable') : $t('collectibles.addMintable')}
          </button>
          <button type="button" class="ghost-button" on:click={() => (mintableForm = emptyMintable())}>
            {$t('collectibles.newMintable')}
          </button>
        </div>
      </form>
    {/if}
  </section>
  {/if}

  {#if tab === 'stamps'}
  <section class="panel" style="margin-top: 12px;">
    <div class="panel-head"><h2>{$t('collectibles.stampsTitle')}</h2></div>
    <p class="muted">{$t('collectibles.stampsDescription')}</p>
    {#if (data.tokenOffers || []).length === 0}
      <EmptyState message={$t('collectibles.noTokenOffers')} />
    {:else}
      <div class="table-wrap">
        <table>
          <thead>
            <tr>
              <th>{$t('collectibles.colBundle')}</th>
              <th>{$t('collectibles.colStamps')}</th>
              <th>{$t('collectibles.colSilverPrice')}</th>
              <th>{$t('collectibles.colOnSale')}</th>
              {#if canManage}<th></th>{/if}
            </tr>
          </thead>
          <tbody>
            {#each data.tokenOffers as offer}
              <tr>
                <td><code>{offer.productCode}</code></td>
                <td>{formatNumber(offer.amountTokens)}</td>
                <td>
                  <span class={currencyChipClass(CURRENCY_KIND.silver)}>
                    <CurrencyIcon kind={CURRENCY_KIND.silver} /> {formatNumber(offer.silverPrice)}
                  </span>
                </td>
                <td>
                  {#if offer.enabled}
                    <span class="status-badge status-badge--ok">{$t('collectibles.onSale')}</span>
                  {:else}
                    <span class="status-badge">{$t('collectibles.offerDisabled')}</span>
                  {/if}
                </td>
                {#if canManage}
                  <td class="row-actions">
                    <button type="button" class="ghost-button" on:click={() => (tokenOfferForm = { ...offer })}>
                      {$t('collectibles.edit')}
                    </button>
                    <button
                      type="button"
                      class="ghost-button danger"
                      on:click={() =>
                        ops.ask(
                          '/api/v1/operations/content/mint-token-offers/delete',
                          { offerId: offer.id },
                          $t('collectibles.deleteTokenOffer'),
                          $t('collectibles.deleteTokenOfferSummary', { code: offer.productCode })
                        )}
                    >
                      {$t('collectibles.delete')}
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
      <h3 class="subhead">{$t('collectibles.tokenOfferEditorTitle')}</h3>
      <form
        class="inline-form editor-form"
        on:submit|preventDefault={() =>
          ops.ask(
            '/api/v1/operations/content/mint-token-offers',
            {
              offerId: Number(tokenOfferForm.id) || 0,
              productCode: tokenOfferForm.productCode,
              silverPrice: Number(tokenOfferForm.silverPrice) || 0,
              amountTokens: Number(tokenOfferForm.amountTokens) || 0,
              enabled: Boolean(tokenOfferForm.enabled),
              sortOrder: Number(tokenOfferForm.sortOrder) || 0,
            },
            tokenOfferForm.id ? $t('collectibles.updateTokenOffer') : $t('collectibles.addTokenOffer'),
            $t('collectibles.saveTokenOfferSummary', { code: tokenOfferForm.productCode })
          )}
      >
        <label>
          {$t('collectibles.colBundle')}
          <input bind:value={tokenOfferForm.productCode} placeholder="stamps_10" />
          <small class="muted">{$t('collectibles.tokenProductHelp')}</small>
        </label>
        <label>
          {$t('collectibles.colStamps')}
          <input type="number" min="1" bind:value={tokenOfferForm.amountTokens} />
          <small class="muted">{$t('collectibles.amountTokensHelp')}</small>
        </label>
        <label>
          {$t('collectibles.colSilverPrice')}
          <input type="number" min="0" bind:value={tokenOfferForm.silverPrice} />
          <small class="muted">{$t('collectibles.silverPriceHelp')}</small>
        </label>
        <label>
          {$t('collectibles.sortOrder')}
          <input type="number" bind:value={tokenOfferForm.sortOrder} />
        </label>
        <label class="check">
          <input type="checkbox" bind:checked={tokenOfferForm.enabled} />
          {$t('collectibles.offerEnabled')}
        </label>
        <div class="form-actions">
          <button type="submit" disabled={!tokenOfferForm.productCode.trim()}>
            {tokenOfferForm.id ? $t('collectibles.updateTokenOffer') : $t('collectibles.addTokenOffer')}
          </button>
          <button type="button" class="ghost-button" on:click={() => (tokenOfferForm = emptyTokenOffer())}>
            {$t('collectibles.newTokenOffer')}
          </button>
        </div>
      </form>
    {/if}
  </section>
  {/if}

  {#if tab === 'relics'}
  <section class="panel" style="margin-top: 12px;">
    <div class="panel-head"><h2>{$t('collectibles.relicsTitle')}</h2></div>
    <p class="muted">{$t('collectibles.relicsDescription')}</p>
    {#if (data.assets || []).length === 0}
      <EmptyState message={$t('collectibles.noRelics')} />
    {:else}
      <div class="table-wrap">
        <table>
          <thead>
            <tr>
              <th>{$t('collectibles.colPlayer')}</th>
              <th>{$t('collectibles.colItem')}</th>
              <th>{$t('collectibles.colSerial')}</th>
              <th>{$t('collectibles.colStampPrice')}</th>
              <th>{$t('collectibles.colMintedAt')}</th>
              <th>{$t('collectibles.colHistory')}</th>
            </tr>
          </thead>
          <tbody>
            {#each data.assets as asset}
              <tr>
                <td>
                  <EntityLink label={asset.playerName} on:click={() => openPlayer(asset.playerId)} />
                </td>
                <td>
                  <span class="cell">
                    <AssetImage src={asset.iconUrl} alt={asset.productCode} size={32} />
                    <code>{asset.productCode}</code>
                  </span>
                </td>
                <td>
                  {#if asset.editionSize > 0}
                    #{formatNumber(asset.serialNumber)} / {formatNumber(asset.editionSize)}
                  {:else}
                    #{formatNumber(asset.serialNumber)}
                  {/if}
                </td>
                <td>{formatNumber(asset.stampCost)}</td>
                <td>{formatDate(asset.mintedAt)}</td>
                <td class="history">
                  {#each asset.history as move}
                    <div>
                      <span class="status-badge">{move.reason}</span>
                      {move.fromPlayer ?? '—'} → {move.toPlayer}
                      <span class="muted">{formatDate(move.at)}</span>
                    </div>
                  {/each}
                </td>
              </tr>
            {/each}
          </tbody>
        </table>
      </div>
    {/if}
  </section>
  {/if}

  {#if tab === 'claims'}
  <section class="panel" style="margin-top: 12px;">
    <div class="panel-head"><h2>{$t('collectibles.claimsTitle')}</h2></div>
    <p class="muted">{$t('collectibles.claimsDescription')}</p>
    {#if (data.claims || []).length === 0}
      <EmptyState message={$t('collectibles.noClaims')} />
    {:else}
      <div class="table-wrap">
        <table>
          <thead>
            <tr>
              <th>{$t('collectibles.colPlayer')}</th>
              <th>{$t('collectibles.colItem')}</th>
              <th>{$t('collectibles.colRemaining')}</th>
              <th>{$t('collectibles.colSet')}</th>
              <th>{$t('collectibles.colExpires')}</th>
              {#if canManage}<th></th>{/if}
            </tr>
          </thead>
          <tbody>
            {#each data.claims as claim}
              <tr>
                <td><EntityLink type="player" id={claim.playerId} label={claim.playerName} {openPlayer} /></td>
                <td>
                  <span class="cell">
                    <AssetImage src={claim.iconUrl} alt={claim.productCode} size={32} />
                    <code>{claim.productCode}</code>
                    {#if !claim.isNft}
                      <span class="status-badge status-badge--bad" title={$t('collectibles.notNftHelp')}>
                        {$t('collectibles.notNft')}
                      </span>
                    {/if}
                  </span>
                </td>
                <td>{formatNumber(claim.remaining)} / {formatNumber(claim.claimLimit)}</td>
                <td>{claim.setId || '—'}</td>
                <td>{claim.validTo ? formatDate(claim.validTo) : '—'}</td>
                {#if canManage}
                  <td class="row-actions">
                    <button
                      type="button"
                      class="ghost-button danger"
                      on:click={() =>
                        ops.ask(
                          '/api/v1/operations/content/claims/delete',
                          { claimId: claim.id },
                          $t('collectibles.deleteClaim'),
                          $t('collectibles.deleteClaimSummary', { code: claim.productCode, name: claim.playerName })
                        )}
                    >
                      {$t('collectibles.delete')}
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
      <h3 class="subhead">{$t('collectibles.claimEditorTitle')}</h3>
      <form
        class="inline-form editor-form"
        on:submit|preventDefault={() =>
          ops.ask(
            '/api/v1/operations/content/claims',
            {
              playerId: Number(claimForm.playerId) || 0,
              productCode: claimForm.productCode,
              setId: claimForm.setId || '',
              defaultCollectionName: claimForm.defaultCollectionName || '',
              collection: claimForm.collection || '',
              claimLimit: Number(claimForm.claimLimit) || 1,
              validFrom: null,
              validTo: claimForm.validTo ? new Date(claimForm.validTo).toISOString() : null,
            },
            $t('collectibles.addClaim'),
            $t('collectibles.saveClaimSummary', { code: claimForm.productCode, name: claimForm.playerName || claimForm.playerId })
          )}
      >
        <label>
          {$t('common.playerRequired')}
          <span class="cell">
            <button class="ghost-button" type="button" on:click={() => (picking = 'claimPlayer')}>
              {$t('common.selectUser')}
            </button>
            {#if claimForm.playerId}
              <span class="op-chip">{claimForm.playerName} <small>#{claimForm.playerId}</small></span>
            {:else}
              <span class="muted">{$t('common.noUserSelected')}</span>
            {/if}
          </span>
        </label>
        <label>
          {$t('collectibles.colItem')}
          <span class="cell">
            <AssetImage src={claimIconUrl} alt={claimForm.productCode} size={32} />
            <input bind:value={claimForm.productCode} placeholder="classname" readonly />
            <button type="button" class="ghost-button" on:click={() => (picking = 'claim')}>
              {$t('collectibles.pickFurniture')}
            </button>
          </span>
          <small class="muted">{$t('collectibles.claimProductHelp')}</small>
        </label>
        <label>
          {$t('collectibles.colSet')}
          <input bind:value={claimForm.setId} placeholder="2025_icy_christmas" />
          <small class="muted">{$t('collectibles.setIdHelp')}</small>
        </label>
        <label>
          {$t('collectibles.claimLimit')}
          <input type="number" min="1" bind:value={claimForm.claimLimit} />
          <small class="muted">{$t('collectibles.claimLimitHelp')}</small>
        </label>
        <label>
          {$t('collectibles.colExpires')}
          <input type="date" bind:value={claimForm.validTo} />
          <small class="muted">{$t('collectibles.expiresHelp')}</small>
        </label>
        <div class="form-actions">
          <button type="submit" disabled={!claimForm.productCode.trim() || !claimForm.playerId}>
            {$t('collectibles.addClaim')}
          </button>
          <button
            type="button"
            class="ghost-button"
            on:click={() => {
              claimForm = emptyClaim();
              claimIconUrl = null;
            }}
          >
            {$t('collectibles.newClaim')}
          </button>
        </div>
      </form>
    {/if}
  </section>
  {/if}

  {#if tab === 'collectors'}
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
{/if}

{#if picking === 'claimPlayer'}
  <PickerModal
    kind="user"
    title={$t('operations.selectPlayerTitle')}
    onSelect={(picked) => {
      claimForm.playerId = picked.id;
      claimForm.playerName = picked.name;
      picking = null;
    }}
    onClose={() => (picking = null)}
  />
{:else if picking}
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
      } else if (picking === 'offer') {
        offerForm.productCode = picked.name;
      } else if (picking === 'mintable') {
        mintableForm.productCode = picked.name;
      } else if (picking === 'claim') {
        claimForm.productCode = picked.name;
        claimIconUrl = picked.iconUrl ?? null;
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
  changes={$ops.pending?.changes ?? []}
  noteOnly={$ops.pending?.noteOnly ?? false}
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

  /* A Relic's whole history in one cell: short lines, and the oldest at the bottom, so the most
     recent hand it passed through is the one read first. */
  .history {
    font-size: 0.85em;
    line-height: 1.5;
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

  /* A checkbox reads as a switch beside its wording, not as a field stacked under a caption. */
  .editor-form label.check {
    flex-direction: row;
    align-items: center;
    gap: 8px;
  }

  .editor-form label.check input {
    width: auto;
  }
</style>
