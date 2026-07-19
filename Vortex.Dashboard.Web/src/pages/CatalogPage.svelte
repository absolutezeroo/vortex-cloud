<script>
  import { onMount } from 'svelte';
  import {
    ChevronRight,
    Coins,
    Eye,
    EyeOff,
    Folder,
    FolderOpen,
    Image,
    Package,
    Pencil,
    Plus,
    Tag,
    Trash2,
  } from '@lucide/svelte';
  import { apiGet, apiPost } from '../lib/api.js';
  import { isPermissionDeniedError, hasDashboardCapability } from '../lib/permissions.js';
  import { compactCorrelation } from '../lib/format.js';
  import { CAPABILITIES } from '../lib/dashboardPermissions.js';
  import { reasonOk, nonNegative } from '../lib/validation.js';
  import { rememberReason } from '../lib/reasonHistory.js';
  import { PRODUCT_TYPES } from '../lib/furnitureEnums.js';
  import AccessDeniedNotice from '../components/AccessDeniedNotice.svelte';
  import PickerModal from '../components/PickerModal.svelte';
  import CatalogIconPickerModal from '../components/CatalogIconPickerModal.svelte';
  import { identity } from '../lib/session.js';
  import { t, translate } from '../lib/i18n.js';

  const CATALOG_TYPES = [
    { value: 0, key: 'catalogAdmin.typeNormal' },
    { value: 1, key: 'catalogAdmin.typeBuildersClub' },
  ];

  const LAYOUT_OPTIONS = [
    'default_3x3', 'badge_display', 'builders_club_addons', 'builders_club_frontpage',
    'builders_club_loyalty', 'club_buy', 'club_gifts', 'frontpage4', 'frontpage_featured',
    'guild_custom_furni', 'guild_forum', 'guild_frontpage', 'info_duckets', 'info_loyalty',
    'info_rentables', 'loyalty_vip_buy', 'marketplace', 'marketplace_own_items', 'monkey',
    'petcustomization', 'pets', 'pets2', 'pets3', 'recycler', 'recycler_info', 'recycler_prizes',
    'roomads', 'single_bundle', 'soundmachine', 'spaces_new', 'trophies', 'vip_buy',
    'old_layout_marketplace', 'old_layout_marketplace_own_items',
  ];

  function formatLayoutLabel(wireValue) {
    return wireValue
      .split('_')
      .map((word) => word.charAt(0).toUpperCase() + word.slice(1))
      .join(' ');
  }

  function emptyPageForm() {
    return {
      localization: '', name: '', icon: 0, layout: 'default_3x3', sortOrder: 0, visible: true,
      imageDataText: '', textDataText: '', reason: '',
    };
  }

  function emptyOfferForm() {
    return {
      localizationId: '', costCredits: 0, costCurrency: 0, currencyTypeId: '',
      canGift: true, canBundle: true, clubLevel: 0, discountPercent: 0, visible: true, reason: '',
    };
  }

  function emptyProductForm() {
    return {
      productType: 0, furnitureDefinitionId: '', furnitureName: '', furnitureIcon: '', furnitureSprite: '',
      extraParam: '', quantity: 1, uniqueSize: 0, uniqueRemaining: 0, buildersClubEligible: false, reason: '',
    };
  }

  // catalog_pages.image_data/text_data are stored as JSON string lists. A one-item-per-line
  // textarea is the simplest editable form for that shape -- blank lines are dropped, and an
  // empty result becomes null (matches "not set" rather than an empty array).
  function linesToArray(text) {
    const lines = text
      .split('\n')
      .map((line) => line.trim())
      .filter((line) => line.length > 0);
    return lines.length > 0 ? lines : null;
  }

  function arrayToLines(value) {
    return Array.isArray(value) ? value.join('\n') : '';
  }

  let catalogType = 0;
  let parentChain = []; // [{ id, label }], ancestors of the current level (root = empty array).

  let pages = [];
  let pagesLoading = false;
  let pagesError = '';
  let pagesForbidden = false;

  let currentPage = null;
  let currentPageLoading = false;

  let currencyTypes = [];
  let iconTemplate = '';

  // 'new' | 'edit' | null -- which form's icon field the picker modal is currently targeting.
  let iconPickerTarget = null;

  function iconUrlFor(id) {
    return iconTemplate && Number(id) > 0 ? iconTemplate.replace('{id}', String(id)) : null;
  }

  let newPageOpen = false;
  let newPage = emptyPageForm();
  let editPageOpen = false;
  let editPageForm = null;

  let newOfferOpen = false;
  let newOffer = emptyOfferForm();
  let editOfferId = null;
  let editOfferForm = null;

  let selectedOfferId = null;
  let offerDetail = null;
  let offerDetailLoading = false;
  let offerDetailError = '';

  let newProductOpen = false;
  let newProduct = emptyProductForm();
  let editProductId = null;
  let editProductForm = null;

  let results = {};
  let errors = {};
  let busy = {};
  let pending = null;
  let picker = null;

  $: canManage = hasDashboardCapability($identity, CAPABILITIES.opsCatalogManage);

  // Plain function, not a `$:` derived value: navigation functions below mutate `parentChain` and
  // then immediately call loadPages()/loadCurrentPage() in the same synchronous block. A `$:`
  // reactive statement would not have recomputed yet at that point (Svelte batches reactive
  // updates after the current call stack), so those calls would read a stale parentId.
  function currentParentId() {
    return parentChain.length > 0 ? parentChain[parentChain.length - 1].id : null;
  }

  async function loadPages() {
    pagesLoading = true;
    pagesError = '';
    pagesForbidden = false;

    const parentId = currentParentId();
    const params = new URLSearchParams({ catalogType: String(catalogType) });
    if (parentId !== null) params.set('parentId', String(parentId));

    try {
      const data = await apiGet(`/api/v1/catalog/pages?${params}`);
      pages = data.items || [];
    } catch (err) {
      if (isPermissionDeniedError(err)) {
        pagesForbidden = true;
        pages = [];
        return;
      }

      pagesError = err.message;
      pages = [];
    } finally {
      pagesLoading = false;
    }
  }

  async function loadCurrentPage() {
    const parentId = currentParentId();

    if (parentId === null) {
      currentPage = null;
      return;
    }

    currentPageLoading = true;

    try {
      currentPage = await apiGet(`/api/v1/catalog/pages/${parentId}`);
    } catch (err) {
      currentPage = null;
      pagesError = isPermissionDeniedError(err) ? translate('common.insufficientRights') : err.code || err.message;
    } finally {
      currentPageLoading = false;
    }
  }

  async function loadOfferDetail(offerId) {
    selectedOfferId = offerId;
    offerDetail = null;
    offerDetailError = '';
    offerDetailLoading = true;
    newProductOpen = false;
    editProductId = null;

    try {
      offerDetail = await apiGet(`/api/v1/catalog/offers/${offerId}`);
    } catch (err) {
      offerDetailError = isPermissionDeniedError(err) ? translate('common.insufficientRights') : err.code || err.message;
    } finally {
      offerDetailLoading = false;
    }
  }

  // An "offer" (the priced catalog listing) and its "product(s)" (what actually gets delivered)
  // are two different DB rows, but almost every offer has exactly one product -- calling the
  // expand button "Products" on a row that already reads as "one item" is confusing. Label it by
  // what's actually there instead. `expandedOfferId` is passed in (rather than read from the
  // outer `selectedOfferId` inside the function body) so this stays part of the template
  // expression Svelte's reactivity tracks -- a value only read inside a called function's body
  // wouldn't be seen as a dependency of the {expression} it's called from.
  function offerActionLabel(offer, expandedOfferId, translator) {
    const expanded = expandedOfferId === offer.id;

    if (offer.productCount === 0) {
      return expanded ? translator('catalogAdmin.hide') : translator('catalogAdmin.addItem');
    }

    if (offer.productCount === 1) {
      return expanded ? translator('catalogAdmin.hideDetails') : translator('catalogAdmin.manage');
    }

    return expanded ? translator('catalogAdmin.hideItems') : translator('catalogAdmin.itemsCount', { count: offer.productCount });
  }

  // Toggles the inline products panel under the clicked offer's own card -- clicking the same
  // offer's action button again collapses it instead of re-fetching.
  async function toggleOfferDetail(offerId) {
    if (selectedOfferId === offerId) {
      selectedOfferId = null;
      offerDetail = null;
      return;
    }

    await loadOfferDetail(offerId);
  }

  async function refreshAll() {
    await loadPages();
    await loadCurrentPage();
    if (selectedOfferId) {
      await loadOfferDetail(selectedOfferId);
    }
  }

  function switchCatalogType(value) {
    if (catalogType === value) return;
    catalogType = value;
    parentChain = [];
    currentPage = null;
    selectedOfferId = null;
    offerDetail = null;
    newPageOpen = false;
    editPageOpen = false;
    void loadPages();
  }

  function drillInto(page) {
    parentChain = [...parentChain, { id: page.Id ?? page.id, label: page.Localization ?? page.localization }];
    selectedOfferId = null;
    offerDetail = null;
    newPageOpen = false;
    editPageOpen = false;
    void loadPages();
    void loadCurrentPage();
  }

  function drillToBreadcrumb(index) {
    // index -1 = root.
    parentChain = index < 0 ? [] : parentChain.slice(0, index + 1);
    selectedOfferId = null;
    offerDetail = null;
    newPageOpen = false;
    editPageOpen = false;
    void loadPages();
    void loadCurrentPage();
  }

  function goUp() {
    drillToBreadcrumb(parentChain.length - 2);
  }

  function stage(id, title, endpoint, valid, body, summary, onSuccess) {
    errors = { ...errors, [id]: '' };

    if (!valid) {
      errors = { ...errors, [id]: translate('catalogAdmin.fillFields') };
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
        [id]: isPermissionDeniedError(err) ? translate('common.insufficientRightsAction') : err.code || err.message,
      };
    } finally {
      busy = { ...busy, [id]: false };
    }
  }

  function stageCreatePage() {
    if (!canManage) {
      errors = { ...errors, createPage: translate('common.insufficientRights') };
      return;
    }

    const parentId = currentParentId();

    stage(
      'createPage',
      translate('catalogAdmin.newPage'),
      '/api/v1/operations/catalog/pages',
      Boolean(newPage.localization.trim()) && reasonOk(newPage.reason),
      {
        catalogType,
        parentId,
        localization: newPage.localization.trim(),
        name: newPage.name.trim() || null,
        icon: Number(newPage.icon) || 0,
        layout: newPage.layout,
        imageData: linesToArray(newPage.imageDataText),
        textData: linesToArray(newPage.textDataText),
        sortOrder: Number(newPage.sortOrder) || 0,
        visible: newPage.visible,
        reason: newPage.reason.trim(),
      },
      translate('catalogAdmin.createPageSummary', { name: newPage.localization.trim(), parent: parentId ? `#${parentId}` : translate('catalogAdmin.root').toLowerCase() }),
      async () => {
        newPageOpen = false;
        newPage = emptyPageForm();
        await loadPages();
      },
    );
  }

  function startEditPage() {
    if (!currentPage) return;
    editPageForm = {
      localization: currentPage.localization,
      name: currentPage.name || '',
      icon: currentPage.icon,
      layout: currentPage.layout,
      sortOrder: currentPage.sortOrder,
      visible: currentPage.visible,
      imageDataText: arrayToLines(currentPage.imageData),
      textDataText: arrayToLines(currentPage.textData),
      reason: '',
    };
    editPageOpen = true;
  }

  function stageUpdatePage() {
    if (!canManage || !currentPage || !editPageForm) return;

    stage(
      'updatePage',
      translate('catalogAdmin.edit'),
      '/api/v1/operations/catalog/pages/update',
      Boolean(editPageForm.localization.trim()) && reasonOk(editPageForm.reason),
      {
        pageId: currentPage.id,
        parentId: currentPage.parentEntityId,
        localization: editPageForm.localization.trim(),
        name: editPageForm.name.trim() || null,
        icon: Number(editPageForm.icon) || 0,
        layout: editPageForm.layout,
        imageData: linesToArray(editPageForm.imageDataText),
        textData: linesToArray(editPageForm.textDataText),
        sortOrder: Number(editPageForm.sortOrder) || 0,
        visible: editPageForm.visible,
        reason: editPageForm.reason.trim(),
      },
      translate('catalogAdmin.updatePageSummary', { id: currentPage.id }),
      async () => {
        editPageOpen = false;
        await loadCurrentPage();
        await loadPages();
        if (parentChain.length > 0) {
          parentChain = parentChain.map((p, i) =>
            i === parentChain.length - 1 ? { ...p, label: editPageForm.localization.trim() } : p,
          );
        }
      },
    );
  }

  function stageDeletePage() {
    if (!canManage || !currentPage) return;

    stage(
      'deletePage',
      translate('catalogAdmin.deletePage'),
      '/api/v1/operations/catalog/pages/delete',
      reasonOk(deletePageReason),
      { pageId: currentPage.id, reason: deletePageReason.trim() },
      translate('catalogAdmin.deletePageSummary', { id: currentPage.id, name: currentPage.localization }),
      async () => {
        deletePageReason = '';
        goUp();
      },
    );
  }

  let deletePageReason = '';

  // "Localization id" is a free-form internal slug (see tools/catalog_converter/convert.py --
  // it's copied verbatim from the source catalog's own name column, no required format). Asking
  // an operator to invent one from nothing is the friction point; pre-filling a reasonable unique
  // default they can just accept -- or edit -- removes the "what do I even type here" blocker.
  function openNewOffer() {
    if (newOfferOpen) {
      newOfferOpen = false;
      return;
    }

    if (!newOffer.localizationId.trim() && currentPage) {
      newOffer = {
        ...newOffer,
        localizationId: `${currentPage.localization}_offer_${currentPage.offers.length + 1}`,
      };
    }

    newOfferOpen = true;
  }

  function stageCreateOffer() {
    if (!canManage || !currentPage) return;

    const currencyTypeId = newOffer.currencyTypeId === '' ? null : Number(newOffer.currencyTypeId);

    stage(
      'createOffer',
      translate('catalogAdmin.newOffer'),
      '/api/v1/operations/catalog/offers',
      Boolean(newOffer.localizationId.trim()) && nonNegative(newOffer.costCredits) && reasonOk(newOffer.reason),
      {
        pageId: currentPage.id,
        localizationId: newOffer.localizationId.trim(),
        costCredits: Number(newOffer.costCredits) || 0,
        costCurrency: Number(newOffer.costCurrency) || 0,
        currencyTypeId,
        canGift: newOffer.canGift,
        canBundle: newOffer.canBundle,
        clubLevel: Number(newOffer.clubLevel) || 0,
        discountPercent: Number(newOffer.discountPercent) || 0,
        visible: newOffer.visible,
        reason: newOffer.reason.trim(),
      },
      translate('catalogAdmin.createOfferSummary', { name: newOffer.localizationId.trim(), id: currentPage.id }),
      async () => {
        newOfferOpen = false;
        newOffer = emptyOfferForm();
        await loadCurrentPage();
      },
    );
  }

  function startEditOffer(offer) {
    editOfferId = offer.id;
    editOfferForm = {
      localizationId: offer.localizationId,
      costCredits: offer.costCredits,
      costCurrency: offer.costCurrency,
      currencyTypeId: offer.currencyTypeId ?? '',
      canGift: offer.canGift,
      canBundle: offer.canBundle,
      clubLevel: offer.clubLevel,
      discountPercent: offer.discountPercent,
      visible: offer.visible,
      reason: '',
    };
  }

  function stageUpdateOffer() {
    if (!canManage || !editOfferForm || editOfferId === null) return;

    const currencyTypeId = editOfferForm.currencyTypeId === '' ? null : Number(editOfferForm.currencyTypeId);

    stage(
      'updateOffer',
      translate('catalogAdmin.edit'),
      '/api/v1/operations/catalog/offers/update',
      Boolean(editOfferForm.localizationId.trim()) && nonNegative(editOfferForm.costCredits) && reasonOk(editOfferForm.reason),
      {
        offerId: editOfferId,
        localizationId: editOfferForm.localizationId.trim(),
        costCredits: Number(editOfferForm.costCredits) || 0,
        costCurrency: Number(editOfferForm.costCurrency) || 0,
        currencyTypeId,
        canGift: editOfferForm.canGift,
        canBundle: editOfferForm.canBundle,
        clubLevel: Number(editOfferForm.clubLevel) || 0,
        discountPercent: Number(editOfferForm.discountPercent) || 0,
        visible: editOfferForm.visible,
        reason: editOfferForm.reason.trim(),
      },
      translate('catalogAdmin.updateOfferSummary', { id: editOfferId }),
      async () => {
        const id = editOfferId;
        editOfferId = null;
        await loadCurrentPage();
        if (selectedOfferId === id) {
          await loadOfferDetail(id);
        }
      },
    );
  }

  let deleteOfferReason = {};

  function stageDeleteOffer(offer) {
    if (!canManage) return;

    stage(
      'deleteOffer',
      translate('catalogAdmin.deleteOffer'),
      '/api/v1/operations/catalog/offers/delete',
      reasonOk(deleteOfferReason[offer.id]),
      { offerId: offer.id, reason: (deleteOfferReason[offer.id] || '').trim() },
      translate('catalogAdmin.deleteOfferSummary', { id: offer.id, name: offer.localizationId }),
      async () => {
        deleteOfferReason = { ...deleteOfferReason, [offer.id]: '' };
        if (selectedOfferId === offer.id) {
          selectedOfferId = null;
          offerDetail = null;
        }
        await loadCurrentPage();
      },
    );
  }

  function pickProductFurniture(apply) {
    picker = { kind: 'furniture', title: translate('common.selectFurniture'), onSelect: apply };
  }

  function stageCreateProduct() {
    if (!canManage || selectedOfferId === null) return;

    stage(
      'createProduct',
      translate('catalogAdmin.addItem'),
      '/api/v1/operations/catalog/products',
      reasonOk(newProduct.reason),
      {
        offerId: selectedOfferId,
        productType: Number(newProduct.productType),
        furnitureDefinitionId: newProduct.furnitureDefinitionId ? Number(newProduct.furnitureDefinitionId) : null,
        extraParam: newProduct.extraParam.trim() ? newProduct.extraParam.trim() : null,
        quantity: Number(newProduct.quantity) || 1,
        uniqueSize: Number(newProduct.uniqueSize) || 0,
        uniqueRemaining: Number(newProduct.uniqueRemaining) || 0,
        buildersClubEligible: newProduct.buildersClubEligible,
        reason: newProduct.reason.trim(),
      },
      translate('catalogAdmin.addProductSummary', { id: selectedOfferId }),
      async () => {
        newProductOpen = false;
        newProduct = emptyProductForm();
        await loadOfferDetail(selectedOfferId);
      },
    );
  }

  function startEditProduct(product) {
    editProductId = product.id;
    editProductForm = {
      productType: product.productType,
      furnitureDefinitionId: product.furnitureDefinitionEntityId ?? '',
      furnitureName: product.furnitureName || '',
      extraParam: product.extraParam || '',
      quantity: product.quantity,
      uniqueSize: product.uniqueSize,
      uniqueRemaining: product.uniqueRemaining,
      buildersClubEligible: product.buildersClubEligible,
      reason: '',
    };
  }

  function stageUpdateProduct() {
    if (!canManage || !editProductForm || editProductId === null) return;

    stage(
      'updateProduct',
      translate('catalogAdmin.edit'),
      '/api/v1/operations/catalog/products/update',
      reasonOk(editProductForm.reason),
      {
        productId: editProductId,
        productType: Number(editProductForm.productType),
        furnitureDefinitionId: editProductForm.furnitureDefinitionId ? Number(editProductForm.furnitureDefinitionId) : null,
        extraParam: editProductForm.extraParam.trim() ? editProductForm.extraParam.trim() : null,
        quantity: Number(editProductForm.quantity) || 1,
        uniqueSize: Number(editProductForm.uniqueSize) || 0,
        uniqueRemaining: Number(editProductForm.uniqueRemaining) || 0,
        buildersClubEligible: editProductForm.buildersClubEligible,
        reason: editProductForm.reason.trim(),
      },
      translate('catalogAdmin.updateProductSummary', { id: editProductId }),
      async () => {
        editProductId = null;
        await loadOfferDetail(selectedOfferId);
      },
    );
  }

  let deleteProductReason = {};

  function stageDeleteProduct(product) {
    if (!canManage) return;

    stage(
      'deleteProduct',
      translate('catalogAdmin.deleteProduct'),
      '/api/v1/operations/catalog/products/delete',
      reasonOk(deleteProductReason[product.id]),
      { productId: product.id, reason: (deleteProductReason[product.id] || '').trim() },
      translate('catalogAdmin.deleteProductSummary', { id: product.id }),
      async () => {
        deleteProductReason = { ...deleteProductReason, [product.id]: '' };
        await loadOfferDetail(selectedOfferId);
      },
    );
  }

  onMount(async () => {
    await loadPages();
    try {
      const data = await apiGet('/api/v1/catalog/currency-types');
      currencyTypes = data.items || [];
    } catch {
      // Non-fatal: the offer forms fall back to a plain numeric currency id.
    }
    try {
      const data = await apiGet('/api/v1/catalog/icon-template');
      iconTemplate = data.template || '';
    } catch {
      // Non-fatal: falls back to showing just the numeric icon id with no preview image.
    }
  });
</script>

<section class="panel">
  <div class="panel-head">
    <h2>{$t('catalogAdmin.title')}</h2>
    <button type="button" class="ghost-button" on:click={refreshAll} disabled={pagesLoading}>{$t('common.refresh')}</button>
  </div>
  <p class="muted">
    {$t('catalogAdmin.description')}
  </p>

  <div class="catalog-tabs">
    {#each CATALOG_TYPES as ct}
      <button type="button" class="catalog-tab" class:active={catalogType === ct.value} on:click={() => switchCatalogType(ct.value)}>
        <Tag size={14} strokeWidth={2} aria-hidden="true" />
        {$t(ct.key)}
      </button>
    {/each}
  </div>

  <nav class="breadcrumb" aria-label="Catalog page path">
    <button type="button" class="crumb-button" class:active={parentChain.length === 0} on:click={() => drillToBreadcrumb(-1)}>
      <Folder size={14} strokeWidth={2} aria-hidden="true" /> {$t('catalogAdmin.root')}
    </button>
    {#each parentChain as crumb, i}
      <ChevronRight size={14} strokeWidth={2} class="muted" aria-hidden="true" />
      <button type="button" class="crumb-button" class:active={i === parentChain.length - 1} on:click={() => drillToBreadcrumb(i)}>
        {crumb.label}
      </button>
    {/each}
  </nav>
</section>

{#if pagesForbidden}
  <AccessDeniedNotice message={$t('catalogAdmin.accessDenied')} />
{:else}
  {#if currentPage}
    <section class="panel">
      <div class="panel-head">
        <div class="page-heading">
          <span class="page-avatar">
            {#if currentPage.iconUrl}
              <img src={currentPage.iconUrl} alt="" />
            {:else}
              <FolderOpen size={20} strokeWidth={2} aria-hidden="true" />
            {/if}
          </span>
          <div>
            <h2>{currentPage.name || currentPage.localization}</h2>
            <small class="muted">{currentPage.localization} - #{currentPage.id}</small>
          </div>
        </div>
        {#if canManage}
          <button type="button" class="ghost-button" on:click={startEditPage}>
            <Pencil size={14} strokeWidth={2} aria-hidden="true" /> {$t('catalogAdmin.edit')}
          </button>
        {/if}
      </div>
      <div class="metric-grid compact">
        <article><span>{$t('catalogAdmin.layout')}</span><strong>{currentPage.layout}</strong></article>
        <article>
          <span>{$t('catalogAdmin.icon')}</span>
          <strong class="icon-preview">
            {#if currentPage.iconUrl}<img src={currentPage.iconUrl} alt="" />{/if}
            #{currentPage.icon}
          </strong>
        </article>
        <article><span>{$t('catalogAdmin.sortOrder')}</span><strong>{currentPage.sortOrder}</strong></article>
        <article>
          <span>{$t('catalogAdmin.visible')}</span>
          <strong>
            <span class="status-badge" class:status-badge--ok={currentPage.visible} class:status-badge--bad={!currentPage.visible}>
              {#if currentPage.visible}<Eye size={12} strokeWidth={2} aria-hidden="true" />{:else}<EyeOff size={12} strokeWidth={2} aria-hidden="true" />{/if}
              {currentPage.visible ? $t('catalogAdmin.visible') : $t('catalogAdmin.hidden')}
            </span>
          </strong>
        </article>
        <article><span>{$t('catalogAdmin.imageData')}</span><strong>{$t('catalogAdmin.lineCount', { count: (currentPage.imageData || []).length })}</strong></article>
        <article><span>{$t('catalogAdmin.textData')}</span><strong>{$t('catalogAdmin.lineCount', { count: (currentPage.textData || []).length })}</strong></article>
      </div>

      {#if editPageOpen && editPageForm}
        <div class="catalog-card-detail">
          <div class="op-field">
            <label for="edit-page-localization">{$t('catalogAdmin.localizationKeyRequired')}</label>
            <input id="edit-page-localization" bind:value={editPageForm.localization} />
          </div>
          <div class="op-field">
            <label for="edit-page-name">{$t('catalogAdmin.displayName')}</label>
            <input id="edit-page-name" bind:value={editPageForm.name} />
          </div>
          <div class="op-field">
            <span class="op-label">{$t('catalogAdmin.icon')}</span>
            <div class="op-pick">
              <button class="ghost-button" type="button" on:click={() => (iconPickerTarget = 'edit')}>
                <Image size={14} strokeWidth={2} aria-hidden="true" /> {$t('catalogAdmin.selectIcon')}
              </button>
              <span class="op-chip">
                {#if iconUrlFor(editPageForm.icon)}<img class="op-sprite" src={iconUrlFor(editPageForm.icon)} alt="" />{:else}<span class="op-sprite">{editPageForm.icon}</span>{/if}
                #{editPageForm.icon}
              </span>
            </div>
          </div>
          <div class="op-field">
            <label for="edit-page-layout">{$t('catalogAdmin.layout')}</label>
            <select id="edit-page-layout" bind:value={editPageForm.layout}>
              {#each LAYOUT_OPTIONS as l}<option value={l}>{formatLayoutLabel(l)}</option>{/each}
            </select>
          </div>
          <div class="op-field">
            <label for="edit-page-sort">{$t('catalogAdmin.sortOrder')}</label>
            <input id="edit-page-sort" type="number" bind:value={editPageForm.sortOrder} />
          </div>
          <div class="op-field">
            <label><input type="checkbox" bind:checked={editPageForm.visible} /> {$t('catalogAdmin.visible')}</label>
          </div>
          <div class="op-field">
            <label for="edit-page-image-data">{$t('catalogAdmin.imageDataOptional')}</label>
            <textarea id="edit-page-image-data" rows="3" bind:value={editPageForm.imageDataText} placeholder="promo_banner.png"></textarea>
          </div>
          <div class="op-field">
            <label for="edit-page-text-data">{$t('catalogAdmin.textDataOptional')}</label>
            <textarea id="edit-page-text-data" rows="3" bind:value={editPageForm.textDataText} placeholder="Welcome to our shop!"></textarea>
          </div>
          <div class="op-field">
            <label for="edit-page-reason">{$t('common.reasonRequired')}</label>
            <input id="edit-page-reason" bind:value={editPageForm.reason} placeholder={$t('common.reasonPlaceholderChange')} list="reason-history" />
          </div>
          <div class="op-actions">
            <button type="button" on:click={stageUpdatePage} disabled={busy.updatePage}>{$t('catalogAdmin.save')}</button>
            <button class="ghost-button" type="button" on:click={() => (editPageOpen = false)}>{$t('catalogAdmin.cancel')}</button>
          </div>
          {#if errors.updatePage}<p class="empty-state danger">{errors.updatePage}</p>{/if}
          {#if results.updatePage}
            <p class="op-result" class:danger={!results.updatePage.ok}>
              {results.updatePage.ok ? '✅' : '❌'} {results.updatePage.message} - cid
              <code>{compactCorrelation(results.updatePage.correlationId)}</code>
            </p>
          {/if}
        </div>
      {/if}

      {#if canManage}
        <div class="catalog-card-detail">
          <div class="op-field">
            <label for="delete-page-reason">{$t('catalogAdmin.deletePageReasonLabel')}</label>
            <div class="op-pick">
              <input id="delete-page-reason" bind:value={deletePageReason} placeholder={$t('catalogAdmin.deleteWhyPlaceholder')} style="flex: 1;" />
              <button type="button" class="ghost-button danger" on:click={stageDeletePage}>
                <Trash2 size={14} strokeWidth={2} aria-hidden="true" /> {$t('catalogAdmin.deletePage')}
              </button>
            </div>
            <p class="muted">{$t('catalogAdmin.deletePageBlockedNote')}</p>
          </div>
          {#if errors.deletePage}<p class="empty-state danger">{errors.deletePage}</p>{/if}
          {#if results.deletePage}
            <p class="op-result" class:danger={!results.deletePage.ok}>
              {results.deletePage.ok ? '✅' : '❌'} {results.deletePage.message} - cid
              <code>{compactCorrelation(results.deletePage.correlationId)}</code>
            </p>
          {/if}
        </div>
      {/if}
    </section>
  {/if}

  <section class="panel">
    <div class="panel-head">
      <h2><Folder size={17} strokeWidth={2} aria-hidden="true" /> {$t('catalogAdmin.subPages')}</h2>
      {#if canManage}
        <button type="button" class="ghost-button" on:click={() => (newPageOpen = !newPageOpen)}>
          <Plus size={14} strokeWidth={2} aria-hidden="true" /> {newPageOpen ? $t('catalogAdmin.cancel') : $t('catalogAdmin.newPage')}
        </button>
      {/if}
    </div>

    {#if newPageOpen}
      <div class="catalog-card-detail">
        <div class="op-field">
          <label for="new-page-localization">{$t('catalogAdmin.localizationKeyRequired')}</label>
          <input id="new-page-localization" bind:value={newPage.localization} placeholder={$t('catalogAdmin.localizationPlaceholder')} />
        </div>
        <div class="op-field">
          <label for="new-page-name">{$t('catalogAdmin.displayName')}</label>
          <input id="new-page-name" bind:value={newPage.name} />
        </div>
        <div class="op-field">
          <span class="op-label">{$t('catalogAdmin.icon')}</span>
          <div class="op-pick">
            <button class="ghost-button" type="button" on:click={() => (iconPickerTarget = 'new')}>
              <Image size={14} strokeWidth={2} aria-hidden="true" /> {$t('catalogAdmin.selectIcon')}
            </button>
            <span class="op-chip">
              {#if iconUrlFor(newPage.icon)}<img class="op-sprite" src={iconUrlFor(newPage.icon)} alt="" />{:else}<span class="op-sprite">{newPage.icon}</span>{/if}
              #{newPage.icon}
            </span>
          </div>
        </div>
        <div class="op-field">
          <label for="new-page-layout">{$t('catalogAdmin.layout')}</label>
          <select id="new-page-layout" bind:value={newPage.layout}>
            {#each LAYOUT_OPTIONS as l}<option value={l}>{formatLayoutLabel(l)}</option>{/each}
          </select>
        </div>
        <div class="op-field">
          <label for="new-page-sort">{$t('catalogAdmin.sortOrder')}</label>
          <input id="new-page-sort" type="number" bind:value={newPage.sortOrder} />
        </div>
        <div class="op-field">
          <label><input type="checkbox" bind:checked={newPage.visible} /> {$t('catalogAdmin.visible')}</label>
        </div>
        <div class="op-field">
          <label for="new-page-image-data">{$t('catalogAdmin.imageDataOptional')}</label>
          <textarea id="new-page-image-data" rows="3" bind:value={newPage.imageDataText} placeholder="promo_banner.png"></textarea>
        </div>
        <div class="op-field">
          <label for="new-page-text-data">{$t('catalogAdmin.textDataOptional')}</label>
          <textarea id="new-page-text-data" rows="3" bind:value={newPage.textDataText} placeholder="Welcome to our shop!"></textarea>
        </div>
        <div class="op-field">
          <label for="new-page-reason">{$t('common.reasonRequired')}</label>
          <input id="new-page-reason" bind:value={newPage.reason} placeholder={$t('catalogAdmin.reasonPagePlaceholder')} list="reason-history" />
        </div>
        <div class="op-actions">
          <button type="button" on:click={stageCreatePage} disabled={busy.createPage}>{$t('catalogAdmin.create')}</button>
        </div>
        {#if errors.createPage}<p class="empty-state danger">{errors.createPage}</p>{/if}
        {#if results.createPage}
          <p class="op-result" class:danger={!results.createPage.ok}>
            {results.createPage.ok ? '✅' : '❌'} {results.createPage.message} - cid
            <code>{compactCorrelation(results.createPage.correlationId)}</code>
          </p>
        {/if}
      </div>
    {/if}

    {#if pagesLoading}
      <p class="muted">{$t('catalogAdmin.loadingPages')}</p>
    {:else if pagesError}
      <p class="empty-state danger">{pagesError}</p>
    {:else if pages.length === 0}
      <p class="empty-state">{$t('catalogAdmin.noSubPages')}</p>
    {:else}
      <div class="catalog-list">
        {#each pages as page (page.id)}
          <button type="button" class="catalog-row" on:click={() => drillInto(page)}>
            <span class="catalog-row-icon">
              {#if page.iconUrl}
                <img src={page.iconUrl} alt="" />
              {:else}
                <Folder size={18} strokeWidth={2} aria-hidden="true" />
              {/if}
            </span>
            <span class="catalog-row-main">
              <strong>{page.name || page.localization}</strong>
              <small class="muted">{page.localization} - #{page.id}</small>
            </span>
            <span class="catalog-row-meta">
              <span class="op-chip" title={$t('catalogAdmin.subPagesTooltip')}><Folder size={12} strokeWidth={2} aria-hidden="true" /> {page.childCount}</span>
              <span class="op-chip" title={$t('catalogAdmin.offersTooltip')}><Tag size={12} strokeWidth={2} aria-hidden="true" /> {page.offerCount}</span>
              <span class="status-badge" class:status-badge--ok={page.visible} class:status-badge--bad={!page.visible}>
                {#if page.visible}<Eye size={12} strokeWidth={2} aria-hidden="true" />{:else}<EyeOff size={12} strokeWidth={2} aria-hidden="true" />{/if}
              </span>
            </span>
            <ChevronRight size={16} strokeWidth={2} class="muted" aria-hidden="true" />
          </button>
        {/each}
      </div>
    {/if}
  </section>

  {#if currentPage}
    <section class="panel">
      <div class="panel-head">
        <h2><Tag size={17} strokeWidth={2} aria-hidden="true" /> {$t('catalogAdmin.offersOnPage')}</h2>
        {#if canManage}
          <button type="button" class="ghost-button" on:click={openNewOffer}>
            <Plus size={14} strokeWidth={2} aria-hidden="true" /> {newOfferOpen ? $t('catalogAdmin.cancel') : $t('catalogAdmin.newOffer')}
          </button>
        {/if}
      </div>

      {#if newOfferOpen}
        <div class="catalog-card-detail">
          <div class="op-field">
            <label for="new-offer-localization">{$t('catalogAdmin.localizationIdRequired')}</label>
            <input id="new-offer-localization" bind:value={newOffer.localizationId} />
            <small class="muted">{$t('catalogAdmin.localizationIdHint')}</small>
          </div>
          <div class="op-field">
            <label for="new-offer-credits">{$t('catalogAdmin.costCredits')}</label>
            <input id="new-offer-credits" type="number" min="0" bind:value={newOffer.costCredits} />
          </div>
          <div class="op-field">
            <label for="new-offer-currency-amount">{$t('catalogAdmin.costSecondary')}</label>
            <input id="new-offer-currency-amount" type="number" min="0" bind:value={newOffer.costCurrency} />
          </div>
          <div class="op-field">
            <label for="new-offer-currency-type">{$t('catalogAdmin.currencyTypeOptional')}</label>
            <select id="new-offer-currency-type" bind:value={newOffer.currencyTypeId}>
              <option value="">{$t('catalogAdmin.none')}</option>
              {#each currencyTypes as c}<option value={c.id}>{c.name || c.type} (#{c.id})</option>{/each}
            </select>
          </div>
          <div class="op-field">
            <label for="new-offer-club-level">{$t('catalogAdmin.clubLevel')}</label>
            <input id="new-offer-club-level" type="number" min="0" bind:value={newOffer.clubLevel} />
          </div>
          <div class="op-field">
            <label for="new-offer-discount">{$t('catalogAdmin.discountPercent')}</label>
            <input id="new-offer-discount" type="number" min="0" max="100" bind:value={newOffer.discountPercent} />
          </div>
          <div class="op-field">
            <label><input type="checkbox" bind:checked={newOffer.canGift} /> {$t('catalogAdmin.canGift')}</label>
          </div>
          <div class="op-field">
            <label><input type="checkbox" bind:checked={newOffer.canBundle} /> {$t('catalogAdmin.canBundle')}</label>
          </div>
          <div class="op-field">
            <label><input type="checkbox" bind:checked={newOffer.visible} /> {$t('catalogAdmin.visible')}</label>
          </div>
          <div class="op-field">
            <label for="new-offer-reason">{$t('common.reasonRequired')}</label>
            <input id="new-offer-reason" bind:value={newOffer.reason} placeholder={$t('catalogAdmin.reasonOfferPlaceholder')} list="reason-history" />
          </div>
          <div class="op-actions">
            <button type="button" on:click={stageCreateOffer} disabled={busy.createOffer}>{$t('catalogAdmin.create')}</button>
          </div>
          {#if errors.createOffer}<p class="empty-state danger">{errors.createOffer}</p>{/if}
          {#if results.createOffer}
            <p class="op-result" class:danger={!results.createOffer.ok}>
              {results.createOffer.ok ? '✅' : '❌'} {results.createOffer.message} - cid
              <code>{compactCorrelation(results.createOffer.correlationId)}</code>
            </p>
          {/if}
        </div>
      {/if}

      {#if currentPage.offers.length === 0}
        <p class="empty-state">{$t('catalogAdmin.noOffers')}</p>
      {:else}
        <div class="catalog-list">
          {#each currentPage.offers as offer (offer.id)}
            <div class="catalog-card">
              <div class="catalog-row static">
                <span class="catalog-row-icon"><Tag size={18} strokeWidth={2} aria-hidden="true" /></span>
                <span class="catalog-row-main">
                  <strong>{offer.localizationId}</strong>
                  <small class="muted">#{offer.id}{offer.clubLevel > 0 ? ` - HC${offer.clubLevel}` : ''}{offer.discountPercent > 0 ? ` - -${offer.discountPercent}%` : ''}</small>
                </span>
                <span class="catalog-row-meta">
                  <span class="cost-chip"><Coins size={12} strokeWidth={2} aria-hidden="true" /> {offer.costCredits}c{offer.currencyTypeId ? ` + ${offer.costCurrency} ${offer.currencyName || `#${offer.currencyTypeId}`}` : ''}</span>
                  <span class="status-badge" class:status-badge--ok={offer.visible} class:status-badge--bad={!offer.visible}>
                    {#if offer.visible}<Eye size={12} strokeWidth={2} aria-hidden="true" />{:else}<EyeOff size={12} strokeWidth={2} aria-hidden="true" />{/if}
                  </span>
                </span>
                <div class="op-actions">
                  <button type="button" class="ghost-button" class:active={selectedOfferId === offer.id} on:click={() => toggleOfferDetail(offer.id)}>
                    <Package size={14} strokeWidth={2} aria-hidden="true" /> {offerActionLabel(offer, selectedOfferId, $t)}
                  </button>
                  {#if canManage}
                    <button type="button" class="ghost-button" on:click={() => startEditOffer(offer)}>
                      <Pencil size={14} strokeWidth={2} aria-hidden="true" /> {$t('catalogAdmin.edit')}
                    </button>
                  {/if}
                </div>
              </div>

              {#if offer.productCount === 1 && offer.singleProduct}
                <div class="catalog-row-sub">
                  <span class="catalog-row-icon small">
                    {#if offer.singleProduct.furnitureIconUrl}
                      <img src={offer.singleProduct.furnitureIconUrl} alt="" />
                    {:else}
                      <Image size={13} strokeWidth={2} aria-hidden="true" />
                    {/if}
                  </span>
                  <span class="muted">{offer.singleProduct.furnitureName || offer.singleProduct.productTypeLabel}</span>
                  <span class="op-chip" title="Quantity">x{offer.singleProduct.quantity}</span>
                  {#if offer.singleProduct.uniqueSize > 0}
                    <span class="op-chip" title="Unique remaining/size">{offer.singleProduct.uniqueRemaining}/{offer.singleProduct.uniqueSize}</span>
                  {/if}
                  {#if offer.singleProduct.buildersClubEligible}
                    <span class="status-badge status-badge--ok">BC</span>
                  {/if}
                </div>
              {/if}

              {#if editOfferId === offer.id && editOfferForm}
                <div class="catalog-card-detail">
                  <div class="op-field">
                    <label for={`edit-offer-localization-${offer.id}`}>{$t('catalogAdmin.localizationIdRequired')}</label>
                    <input id={`edit-offer-localization-${offer.id}`} bind:value={editOfferForm.localizationId} />
                  </div>
                  <div class="op-field">
                    <label for={`edit-offer-credits-${offer.id}`}>{$t('catalogAdmin.costCredits')}</label>
                    <input id={`edit-offer-credits-${offer.id}`} type="number" min="0" bind:value={editOfferForm.costCredits} />
                  </div>
                  <div class="op-field">
                    <label for={`edit-offer-currency-amount-${offer.id}`}>{$t('catalogAdmin.costSecondary')}</label>
                    <input id={`edit-offer-currency-amount-${offer.id}`} type="number" min="0" bind:value={editOfferForm.costCurrency} />
                  </div>
                  <div class="op-field">
                    <label for={`edit-offer-currency-type-${offer.id}`}>{$t('catalogAdmin.currencyTypeOptional')}</label>
                    <select id={`edit-offer-currency-type-${offer.id}`} bind:value={editOfferForm.currencyTypeId}>
                      <option value="">{$t('catalogAdmin.none')}</option>
                      {#each currencyTypes as c}<option value={c.id}>{c.name || c.type} (#{c.id})</option>{/each}
                    </select>
                  </div>
                  <div class="op-field">
                    <label for={`edit-offer-club-${offer.id}`}>{$t('catalogAdmin.clubLevel')}</label>
                    <input id={`edit-offer-club-${offer.id}`} type="number" min="0" bind:value={editOfferForm.clubLevel} />
                  </div>
                  <div class="op-field">
                    <label for={`edit-offer-discount-${offer.id}`}>{$t('catalogAdmin.discountPercent')}</label>
                    <input id={`edit-offer-discount-${offer.id}`} type="number" min="0" max="100" bind:value={editOfferForm.discountPercent} />
                  </div>
                  <div class="op-field"><label><input type="checkbox" bind:checked={editOfferForm.canGift} /> {$t('catalogAdmin.canGift')}</label></div>
                  <div class="op-field"><label><input type="checkbox" bind:checked={editOfferForm.canBundle} /> {$t('catalogAdmin.canBundle')}</label></div>
                  <div class="op-field"><label><input type="checkbox" bind:checked={editOfferForm.visible} /> {$t('catalogAdmin.visible')}</label></div>
                  <div class="op-field">
                    <label for={`edit-offer-reason-${offer.id}`}>{$t('common.reasonRequired')}</label>
                    <input id={`edit-offer-reason-${offer.id}`} bind:value={editOfferForm.reason} placeholder={$t('common.reasonPlaceholderChange')} list="reason-history" />
                  </div>
                  <div class="op-actions">
                    <button type="button" on:click={stageUpdateOffer} disabled={busy.updateOffer}>{$t('catalogAdmin.save')}</button>
                    <button class="ghost-button" type="button" on:click={() => (editOfferId = null)}>{$t('catalogAdmin.cancel')}</button>
                  </div>
                  {#if errors.updateOffer}<p class="empty-state danger">{errors.updateOffer}</p>{/if}
                  {#if results.updateOffer}
                    <p class="op-result" class:danger={!results.updateOffer.ok}>
                      {results.updateOffer.ok ? '✅' : '❌'} {results.updateOffer.message} - cid
                      <code>{compactCorrelation(results.updateOffer.correlationId)}</code>
                    </p>
                  {/if}
                </div>
              {/if}

              {#if canManage}
                <div class="catalog-card-detail op-pick">
                  <input bind:value={deleteOfferReason[offer.id]} placeholder={$t('catalogAdmin.deleteOfferReasonPlaceholder')} list="reason-history" style="flex: 1;" />
                  <button type="button" class="ghost-button danger" on:click={() => stageDeleteOffer(offer)}>
                    <Trash2 size={14} strokeWidth={2} aria-hidden="true" /> {$t('catalogAdmin.deleteOffer')}
                  </button>
                </div>
              {/if}

              {#if selectedOfferId === offer.id}
                <div class="catalog-card-detail products-panel">
                  <div class="panel-head">
                    <h3><Package size={15} strokeWidth={2} aria-hidden="true" /> {$t('catalogAdmin.itemsDelivered')}</h3>
                    {#if canManage}
                      <button type="button" class="ghost-button" on:click={() => (newProductOpen = !newProductOpen)}>
                        <Plus size={14} strokeWidth={2} aria-hidden="true" /> {newProductOpen ? $t('catalogAdmin.cancel') : $t('catalogAdmin.addItem')}
                      </button>
                    {/if}
                  </div>

                  {#if offerDetailLoading}
                    <p class="muted">{$t('catalogAdmin.loadingProducts')}</p>
                  {:else if offerDetailError}
                    <p class="empty-state danger">{offerDetailError}</p>
                  {:else if offerDetail}
                    {#if newProductOpen}
                      <div class="catalog-card-detail">
                        <div class="op-field">
                          <label for="new-product-type">{$t('catalogAdmin.productType')}</label>
                          <select id="new-product-type" bind:value={newProduct.productType}>
                            {#each PRODUCT_TYPES as pt}<option value={pt.value}>{pt.label}</option>{/each}
                          </select>
                        </div>
                        <div class="op-field">
                          <span class="op-label">{$t('catalogAdmin.furnitureOptional')}</span>
                          <div class="op-pick">
                            <button
                              class="ghost-button"
                              type="button"
                              on:click={() => pickProductFurniture((f) => (newProduct = { ...newProduct, furnitureDefinitionId: f.id, furnitureName: f.name, furnitureSprite: f.spriteId, furnitureIcon: f.iconUrl }))}
                            >
                              <Image size={14} strokeWidth={2} aria-hidden="true" /> {$t('common.selectFurniture')}
                            </button>
                            {#if newProduct.furnitureDefinitionId}
                              <span class="op-chip">
                                {#if newProduct.furnitureIcon}<img class="op-sprite" src={newProduct.furnitureIcon} alt="" />{:else}<span class="op-sprite">{newProduct.furnitureSprite}</span>{/if}
                                {newProduct.furnitureName} <small>#{newProduct.furnitureDefinitionId}</small>
                              </span>
                              <button class="ghost-button" type="button" on:click={() => (newProduct = { ...newProduct, furnitureDefinitionId: '', furnitureName: '', furnitureIcon: '', furnitureSprite: '' })}>{$t('catalogAdmin.clear')}</button>
                            {:else}
                              <span class="muted">{$t('common.noFurnitureSelected')}</span>
                            {/if}
                          </div>
                        </div>
                        <div class="op-field">
                          <label for="new-product-extra">{$t('catalogAdmin.extraData')}</label>
                          <input id="new-product-extra" bind:value={newProduct.extraParam} placeholder={$t('operations.extraDataPlaceholder')} />
                        </div>
                        <div class="op-field">
                          <label for="new-product-quantity">{$t('catalogAdmin.quantity')}</label>
                          <input id="new-product-quantity" type="number" min="1" bind:value={newProduct.quantity} />
                        </div>
                        <div class="op-field">
                          <label for="new-product-unique-size">{$t('catalogAdmin.uniqueSizeHint')}</label>
                          <input id="new-product-unique-size" type="number" min="0" bind:value={newProduct.uniqueSize} />
                        </div>
                        <div class="op-field">
                          <label for="new-product-unique-remaining">{$t('catalogAdmin.uniqueRemaining')}</label>
                          <input id="new-product-unique-remaining" type="number" min="0" bind:value={newProduct.uniqueRemaining} />
                        </div>
                        <div class="op-field">
                          <label><input type="checkbox" bind:checked={newProduct.buildersClubEligible} /> {$t('catalogAdmin.bcEligible')}</label>
                        </div>
                        <div class="op-field">
                          <label for="new-product-reason">{$t('common.reasonRequired')}</label>
                          <input id="new-product-reason" bind:value={newProduct.reason} placeholder={$t('catalogAdmin.reasonProductPlaceholder')} list="reason-history" />
                        </div>
                        <div class="op-actions">
                          <button type="button" on:click={stageCreateProduct} disabled={busy.createProduct}>{$t('catalogAdmin.create')}</button>
                        </div>
                        {#if errors.createProduct}<p class="empty-state danger">{errors.createProduct}</p>{/if}
                        {#if results.createProduct}
                          <p class="op-result" class:danger={!results.createProduct.ok}>
                            {results.createProduct.ok ? '✅' : '❌'} {results.createProduct.message} - cid
                            <code>{compactCorrelation(results.createProduct.correlationId)}</code>
                          </p>
                        {/if}
                      </div>
                    {/if}

                    {#if offerDetail.products.length === 0}
                      <p class="empty-state">{$t('catalogAdmin.noProducts')}</p>
                    {:else}
                      <div class="catalog-list">
                        {#each offerDetail.products as product (product.id)}
                          <div class="catalog-card">
                            <div class="catalog-row static">
                              <span class="catalog-row-icon">
                                {#if product.furnitureIconUrl}
                                  <img src={product.furnitureIconUrl} alt="" />
                                {:else}
                                  <Image size={18} strokeWidth={2} aria-hidden="true" />
                                {/if}
                              </span>
                              <span class="catalog-row-main">
                                <strong>{product.furnitureName || product.productTypeLabel}</strong>
                                <small class="muted">{product.productTypeLabel}{product.furnitureDefinitionEntityId ? ` - #${product.furnitureDefinitionEntityId}` : ''}</small>
                              </span>
                              <span class="catalog-row-meta">
                                <span class="op-chip" title="Quantity">x{product.quantity}</span>
                                {#if product.uniqueSize > 0}
                                  <span class="op-chip" title="Unique remaining/size">{product.uniqueRemaining}/{product.uniqueSize}</span>
                                {/if}
                                {#if product.buildersClubEligible}
                                  <span class="status-badge status-badge--ok">BC</span>
                                {/if}
                              </span>
                              {#if canManage}
                                <button type="button" class="ghost-button" on:click={() => startEditProduct(product)}>
                                  <Pencil size={14} strokeWidth={2} aria-hidden="true" /> {$t('catalogAdmin.edit')}
                                </button>
                              {/if}
                            </div>

                            {#if editProductId === product.id && editProductForm}
                              <div class="catalog-card-detail">
                                <div class="op-field">
                                  <label for={`edit-product-type-${product.id}`}>{$t('catalogAdmin.productType')}</label>
                                  <select id={`edit-product-type-${product.id}`} bind:value={editProductForm.productType}>
                                    {#each PRODUCT_TYPES as pt}<option value={pt.value}>{pt.label}</option>{/each}
                                  </select>
                                </div>
                                <div class="op-field">
                                  <label for={`edit-product-def-${product.id}`}>{$t('catalogAdmin.furnitureDefIdOptional')}</label>
                                  <input id={`edit-product-def-${product.id}`} type="number" min="0" bind:value={editProductForm.furnitureDefinitionId} />
                                </div>
                                <div class="op-field">
                                  <label for={`edit-product-extra-${product.id}`}>{$t('catalogAdmin.extraData')}</label>
                                  <input id={`edit-product-extra-${product.id}`} bind:value={editProductForm.extraParam} />
                                </div>
                                <div class="op-field">
                                  <label for={`edit-product-qty-${product.id}`}>{$t('catalogAdmin.quantity')}</label>
                                  <input id={`edit-product-qty-${product.id}`} type="number" min="1" bind:value={editProductForm.quantity} />
                                </div>
                                <div class="op-field">
                                  <label for={`edit-product-usize-${product.id}`}>{$t('catalogAdmin.uniqueSizeHint')}</label>
                                  <input id={`edit-product-usize-${product.id}`} type="number" min="0" bind:value={editProductForm.uniqueSize} />
                                </div>
                                <div class="op-field">
                                  <label for={`edit-product-urem-${product.id}`}>{$t('catalogAdmin.uniqueRemaining')}</label>
                                  <input id={`edit-product-urem-${product.id}`} type="number" min="0" bind:value={editProductForm.uniqueRemaining} />
                                </div>
                                <div class="op-field"><label><input type="checkbox" bind:checked={editProductForm.buildersClubEligible} /> {$t('catalogAdmin.bcEligible')}</label></div>
                                <div class="op-field">
                                  <label for={`edit-product-reason-${product.id}`}>{$t('common.reasonRequired')}</label>
                                  <input id={`edit-product-reason-${product.id}`} bind:value={editProductForm.reason} placeholder={$t('common.reasonPlaceholderChange')} list="reason-history" />
                                </div>
                                <div class="op-actions">
                                  <button type="button" on:click={stageUpdateProduct} disabled={busy.updateProduct}>{$t('catalogAdmin.save')}</button>
                                  <button class="ghost-button" type="button" on:click={() => (editProductId = null)}>{$t('catalogAdmin.cancel')}</button>
                                </div>
                                {#if errors.updateProduct}<p class="empty-state danger">{errors.updateProduct}</p>{/if}
                                {#if results.updateProduct}
                                  <p class="op-result" class:danger={!results.updateProduct.ok}>
                                    {results.updateProduct.ok ? '✅' : '❌'} {results.updateProduct.message} - cid
                                    <code>{compactCorrelation(results.updateProduct.correlationId)}</code>
                                  </p>
                                {/if}
                              </div>
                            {/if}

                            {#if canManage}
                              <div class="catalog-card-detail op-pick">
                                <input bind:value={deleteProductReason[product.id]} placeholder={$t('catalogAdmin.deleteProductReasonPlaceholder')} list="reason-history" style="flex: 1;" />
                                <button type="button" class="ghost-button danger" on:click={() => stageDeleteProduct(product)}>
                                  <Trash2 size={14} strokeWidth={2} aria-hidden="true" /> {$t('catalogAdmin.deleteProduct')}
                                </button>
                              </div>
                            {/if}
                          </div>
                        {/each}
                      </div>
                    {/if}
                    {#if errors.deleteProduct}<p class="empty-state danger">{errors.deleteProduct}</p>{/if}
                    {#if results.deleteProduct}
                      <p class="op-result" class:danger={!results.deleteProduct.ok}>
                        {results.deleteProduct.ok ? '✅' : '❌'} {results.deleteProduct.message} - cid
                        <code>{compactCorrelation(results.deleteProduct.correlationId)}</code>
                      </p>
                    {/if}
                  {/if}
                </div>
              {/if}
            </div>
          {/each}
        </div>
      {/if}
      {#if errors.deleteOffer}<p class="empty-state danger">{errors.deleteOffer}</p>{/if}
      {#if results.deleteOffer}
        <p class="op-result" class:danger={!results.deleteOffer.ok}>
          {results.deleteOffer.ok ? '✅' : '❌'} {results.deleteOffer.message} - cid
          <code>{compactCorrelation(results.deleteOffer.correlationId)}</code>
        </p>
      {/if}
    </section>
  {/if}
{/if}

{#if picker}
  <PickerModal
    kind={picker.kind}
    title={picker.title}
    onSelect={picker.onSelect}
    onClose={() => (picker = null)}
    canSelect={canManage}
  />
{/if}

{#if iconPickerTarget}
  <CatalogIconPickerModal
    title={$t('catalogAdmin.selectCatalogIcon')}
    onSelect={(id) => {
      if (iconPickerTarget === 'new') {
        newPage = { ...newPage, icon: id };
      } else if (iconPickerTarget === 'edit' && editPageForm) {
        editPageForm = { ...editPageForm, icon: id };
      }
    }}
    onClose={() => (iconPickerTarget = null)}
  />
{/if}

{#if pending}
  <div class="modal-layer">
    <button class="modal-backdrop" type="button" aria-label="Cancel" on:click={cancelPending}></button>
    <section class="modal-panel" role="dialog" aria-modal="true" style="width: min(460px, 100%)">
      <header class="modal-header">
        <div>
          <p class="eyebrow">{$t('catalogAdmin.confirmEyebrow')}</p>
          <h2>{pending.title}</h2>
        </div>
      </header>
      <p>{pending.summary}</p>
      <p class="muted">{$t('vouchers.reasonLabel', { reason: pending.reason })}</p>
      <div class="op-actions">
        <button type="button" on:click={confirmPending}>{$t('common.confirm')}</button>
        <button class="ghost-button" type="button" on:click={cancelPending}>{$t('catalogAdmin.cancel')}</button>
      </div>
    </section>
  </div>
{/if}

<style>
  .catalog-tabs {
    display: flex;
    gap: 6px;
    margin-top: 10px;
    overflow-x: auto;
    flex-wrap: nowrap;
    padding-bottom: 8px;
    scrollbar-width: thin;
    scrollbar-color: var(--line-strong) transparent;
  }

  .catalog-tabs::-webkit-scrollbar {
    height: 6px;
  }

  .catalog-tabs::-webkit-scrollbar-track {
    background: transparent;
  }

  .catalog-tabs::-webkit-scrollbar-thumb {
    background: var(--line-strong);
    border-radius: 999px;
  }

  .catalog-tabs::-webkit-scrollbar-thumb:hover {
    background: var(--muted);
  }

  .catalog-tab {
    flex: 0 0 auto;
    padding: 7px 14px;
    border-radius: 10px;
    border: 1px solid var(--line);
    background: transparent;
    color: var(--muted);
    cursor: pointer;
    font-size: 0.85rem;
    white-space: nowrap;
  }

  .catalog-tab.active {
    border-color: var(--accent);
    color: var(--ink);
    background: rgba(var(--accent-rgb), 0.12);
  }

  .breadcrumb {
    display: flex;
    align-items: center;
    gap: 6px;
    margin-top: 10px;
    flex-wrap: nowrap;
    overflow-x: auto;
    padding-bottom: 8px;
    scrollbar-width: thin;
    scrollbar-color: var(--line-strong) transparent;
  }

  .breadcrumb::-webkit-scrollbar {
    height: 6px;
  }

  .breadcrumb::-webkit-scrollbar-track {
    background: transparent;
  }

  .breadcrumb::-webkit-scrollbar-thumb {
    background: var(--line-strong);
    border-radius: 999px;
  }

  .breadcrumb::-webkit-scrollbar-thumb:hover {
    background: var(--muted);
  }

  .breadcrumb button {
    flex: 0 0 auto;
    white-space: nowrap;
  }

  .crumb-button {
    display: inline-flex;
    align-items: center;
    gap: 6px;
    padding: 6px 12px;
    border-radius: 999px;
    border: 1px solid var(--line);
    background: transparent;
    color: var(--muted);
    cursor: pointer;
    font-size: 0.82rem;
  }

  .crumb-button.active {
    border-color: var(--accent);
    color: var(--ink);
    background: rgba(var(--accent-rgb), 0.12);
  }

  .ghost-button.danger {
    color: var(--danger);
    border-color: rgba(var(--danger-rgb), 0.4);
  }

  .ghost-button.active {
    border-color: var(--accent);
    color: var(--ink);
    background: rgba(var(--accent-rgb), 0.12);
  }

  .ghost-button,
  .crumb-button,
  .catalog-tab,
  .op-actions button {
    display: inline-flex;
    align-items: center;
    gap: 6px;
  }

  .panel-head {
    flex-wrap: wrap;
    row-gap: 8px;
  }

  .panel-head h2 {
    display: flex;
    align-items: center;
    gap: 8px;
  }

  .panel-head h3 {
    display: flex;
    align-items: center;
    gap: 8px;
    margin: 0;
    font-size: 0.95rem;
  }

  .products-panel > .panel-head {
    margin-bottom: 10px;
  }

  .page-heading {
    display: flex;
    align-items: center;
    gap: 12px;
    min-width: 0;
    flex-wrap: wrap;
  }

  .page-heading h2 {
    margin: 0;
  }

  .page-avatar {
    width: 40px;
    height: 40px;
    flex: 0 0 auto;
    display: grid;
    place-items: center;
    border: 1px solid var(--line-strong);
    border-radius: 10px;
    background: var(--surface-raised);
    color: var(--accent);
    overflow: hidden;
  }

  .page-avatar img {
    width: 100%;
    height: 100%;
    object-fit: contain;
    image-rendering: pixelated;
    image-rendering: crisp-edges;
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

  .catalog-row {
    display: flex;
    align-items: center;
    flex-wrap: wrap;
    row-gap: 8px;
    gap: 12px;
    width: 100%;
    padding: 10px 12px;
    background: transparent;
    border: none;
    color: inherit;
    text-align: left;
    cursor: pointer;
    font: inherit;
  }

  button.catalog-row:hover {
    background: var(--surface-hover);
  }

  .catalog-row.static {
    cursor: default;
  }

  .catalog-row-icon {
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

  .catalog-row-icon img {
    width: 100%;
    height: 100%;
    object-fit: contain;
    image-rendering: pixelated;
    image-rendering: crisp-edges;
  }

  .catalog-row-icon.small {
    width: 22px;
    height: 22px;
    border-radius: 6px;
  }

  /* Compact secondary line under an offer's main row, showing the single product it delivers
     without requiring a click -- see offerActionLabel()'s doc comment for why this exists. */
  .catalog-row-sub {
    display: flex;
    align-items: center;
    flex-wrap: wrap;
    gap: 8px;
    padding: 0 12px 10px 46px;
    font-size: 0.82rem;
  }

  .catalog-row-sub .op-chip,
  .catalog-row-sub .status-badge {
    height: 22px;
    box-sizing: border-box;
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

  .catalog-row-meta {
    display: flex;
    align-items: center;
    gap: 6px;
    flex: 0 1 auto;
    flex-wrap: wrap;
  }

  /* .op-chip, .status-badge and .cost-chip are three differently-authored pill styles (two
     global, one local) with different padding/font-size, so side by side their heights don't
     match. Force them to the same box height here rather than touching the global classes,
     which are reused unrelated places elsewhere in the dashboard. */
  .catalog-row-meta > .op-chip,
  .catalog-row-meta > .status-badge,
  .catalog-row-meta > .cost-chip {
    height: 24px;
    box-sizing: border-box;
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

  .catalog-card-detail {
    border-top: 1px solid var(--line);
    padding: 12px;
  }

  .icon-preview {
    display: inline-flex;
    align-items: center;
    gap: 6px;
  }

  .icon-preview img {
    width: 20px;
    height: 20px;
    object-fit: contain;
    image-rendering: pixelated;
    image-rendering: crisp-edges;
  }
</style>
