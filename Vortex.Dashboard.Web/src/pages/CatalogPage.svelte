<script>
  import ConfirmReasonModal from '../components/ConfirmReasonModal.svelte';
  import OpResult from '../components/OpResult.svelte';
  import { onMount } from 'svelte';
  import {
    Activity,
    ChevronRight,
    Coins,
    Eye,
    EyeOff,
    Folder,
    FolderOpen,
    Hash,
    Image,
    Package,
    Pencil,
    Plus,
    Tag,
    Trash2,
  } from '@lucide/svelte';
  import { apiGet } from '../lib/api.js';
  import { createWriteOps } from '../lib/writeOps.js';
  import { isPermissionDeniedError, hasDashboardCapability } from '../lib/permissions.js';
  import { CAPABILITIES } from '../lib/dashboardPermissions.js';
  import { nonNegative } from '../lib/validation.js';
  import { diffFields } from '../lib/changes.js';
  import { PRODUCT_TYPES } from '../lib/furnitureEnums.js';
  import AccessDeniedNotice from '../components/AccessDeniedNotice.svelte';
  import Drawer from '../components/Drawer.svelte';
  import PickerModal from '../components/PickerModal.svelte';
  import CatalogIconPickerModal from '../components/CatalogIconPickerModal.svelte';
  import StatCard from '../components/StatCard.svelte';
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
      imageDataText: '', textDataText: '',
    };
  }

  function emptyOfferForm() {
    return {
      localizationId: '', costCredits: 0, costCurrency: 0, currencyTypeId: '',
      canGift: true, canBundle: true, clubLevel: 0, discountPercent: 0, visible: true,
    };
  }

  function emptyProductForm() {
    return {
      productType: 0, furnitureDefinitionId: '', furnitureName: '', furnitureIcon: '', furnitureSprite: '',
      extraParam: '', quantity: 1, uniqueSize: 0, uniqueRemaining: 0, buildersClubEligible: false,
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

  let catalogType = $state(0);
  let parentChain = $state([]); // [{ id, label }], ancestors of the current level (root = empty array).

  let pages = $state([]);
  let pagesLoading = $state(false);
  let pagesError = $state('');
  let pagesForbidden = $state(false);

  let currentPage = $state(null);
  let currentPageLoading = false;

  let currencyTypes = $state([]);
  let iconTemplate = '';

  // 'new' | 'edit' | null -- which form's icon field the picker modal is currently targeting.
  let iconPickerTarget = $state(null);

  function iconUrlFor(id) {
    return iconTemplate && Number(id) > 0 ? iconTemplate.replace('{id}', String(id)) : null;
  }

  let newPageOpen = $state(false);
  let newPage = $state(emptyPageForm());
  let editPageOpen = $state(false);
  let editPageForm = $state(null);

  let newOfferOpen = $state(false);
  let newOffer = $state(emptyOfferForm());
  let editOfferId = $state(null);
  let editOfferForm = $state(null);

  let selectedOfferId = $state(null);
  let offerDetail = $state(null);
  let offerDetailLoading = $state(false);
  let offerDetailError = $state('');

  let newProductOpen = $state(false);
  let newProduct = $state(emptyProductForm());
  let editProductId = $state(null);
  let editProductForm = $state(null);

  // Every write is staged here and confirmed in the dialog below before it is posted. createWriteOps
  // owns that cycle -- posting, remembering the audited reason, and tracking each form's busy state,
  // error and result under its own key -- so the page only describes what each button writes.
  const ops = createWriteOps();
  let picker = $state(null);

  let canManage = $derived(hasDashboardCapability($identity, CAPABILITIES.opsCatalogManage));

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

  // `changes` is the before/after list for an edit (empty for a create or a delete). It is shown in
  // the confirm dialog and becomes the audited reason, which is why none of these actions asks the
  // operator to type one any more: the page already knows what it is about to do.
  const stage = (id, title, endpoint, valid, body, summary, onSuccess, changes = []) =>
    ops.ask(endpoint, body, title, summary, {
      key: id,
      valid,
      invalidMessage: translate('catalogAdmin.fillFields'),
      changes,
      onSuccess,
    });

  // The fields worth naming in an audit line, in the order an operator reads them. Short labels on
  // purpose -- the form's own "Cost (credits) *" reads badly inside a sentence.
  const OFFER_FIELDS = () => [
    { key: 'localizationId', label: translate('catalogAdmin.fieldLocalizationId') },
    { key: 'costCredits', label: translate('catalogAdmin.fieldCostCredits') },
    { key: 'costCurrency', label: translate('catalogAdmin.fieldCostCurrency') },
    { key: 'currencyTypeId', label: translate('catalogAdmin.fieldCurrencyType') },
    { key: 'canGift', label: translate('catalogAdmin.fieldCanGift') },
    { key: 'canBundle', label: translate('catalogAdmin.fieldCanBundle') },
    { key: 'clubLevel', label: translate('catalogAdmin.fieldClubLevel') },
    { key: 'discountPercent', label: translate('catalogAdmin.fieldDiscount') },
    { key: 'visible', label: translate('catalogAdmin.fieldVisible') },
  ];

  const PAGE_FIELDS = () => [
    { key: 'localization', label: translate('catalogAdmin.fieldLocalization') },
    { key: 'name', label: translate('catalogAdmin.fieldName') },
    { key: 'icon', label: translate('catalogAdmin.fieldIcon') },
    { key: 'layout', label: translate('catalogAdmin.fieldLayout') },
    { key: 'sortOrder', label: translate('catalogAdmin.fieldSortOrder') },
    { key: 'visible', label: translate('catalogAdmin.fieldVisible') },
  ];

  const PRODUCT_FIELDS = () => [
    { key: 'productType', label: translate('catalogAdmin.fieldProductType') },
    { key: 'furnitureDefinitionId', label: translate('catalogAdmin.fieldFurniture') },
    { key: 'extraParam', label: translate('catalogAdmin.fieldExtraParam') },
    { key: 'quantity', label: translate('catalogAdmin.fieldQuantity') },
    { key: 'uniqueSize', label: translate('catalogAdmin.fieldUniqueSize') },
    { key: 'uniqueRemaining', label: translate('catalogAdmin.fieldUniqueRemaining') },
    { key: 'buildersClubEligible', label: translate('catalogAdmin.fieldBuildersClub') },
  ];

  function stageCreatePage() {
    if (!canManage) {
      ops.fail('createPage', translate('common.insufficientRights'));
      return;
    }

    const parentId = currentParentId();

    stage(
      'createPage',
      translate('catalogAdmin.newPage'),
      '/api/v1/operations/catalog/pages',
      Boolean(newPage.localization.trim()),
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
    };
    editPageOpen = true;
  }

  function stageUpdatePage() {
    if (!canManage || !currentPage || !editPageForm) return;

    stage(
      'updatePage',
      translate('catalogAdmin.edit'),
      '/api/v1/operations/catalog/pages/update',
      Boolean(editPageForm.localization.trim()),
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
      diffFields(currentPage, { ...editPageForm, name: editPageForm.name.trim() || null }, PAGE_FIELDS()),
    );
  }

  function stageDeletePage() {
    if (!canManage || !currentPage) return;

    stage(
      'deletePage',
      translate('catalogAdmin.deletePage'),
      '/api/v1/operations/catalog/pages/delete',
      true,
      { pageId: currentPage.id },
      translate('catalogAdmin.deletePageSummary', { id: currentPage.id, name: currentPage.localization }),
      async () => {
        goUp();
      },
    );
  }


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
      Boolean(newOffer.localizationId.trim()) && nonNegative(newOffer.costCredits),
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
      },
      translate('catalogAdmin.createOfferSummary', { name: newOffer.localizationId.trim(), id: currentPage.id }),
      async () => {
        newOfferOpen = false;
        newOffer = emptyOfferForm();
        await loadCurrentPage();
      },
    );
  }

  // The row as it was loaded, kept so the confirm dialog can show what actually changes.
  let editOfferOriginal = null;
  let editProductOriginal = null;

  function startEditOffer(offer) {
    editOfferId = offer.id;
    editOfferOriginal = offer;
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
    };
  }

  function stageUpdateOffer() {
    if (!canManage || !editOfferForm || editOfferId === null) return;

    const currencyTypeId = editOfferForm.currencyTypeId === '' ? null : Number(editOfferForm.currencyTypeId);

    stage(
      'updateOffer',
      translate('catalogAdmin.edit'),
      '/api/v1/operations/catalog/offers/update',
      Boolean(editOfferForm.localizationId.trim()) && nonNegative(editOfferForm.costCredits),
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
      diffFields(
        editOfferOriginal,
        { ...editOfferForm, localizationId: editOfferForm.localizationId.trim(), currencyTypeId },
        OFFER_FIELDS(),
      ),
    );
  }


  function stageDeleteOffer(offer) {
    if (!canManage) return;

    stage(
      'deleteOffer',
      translate('catalogAdmin.deleteOffer'),
      '/api/v1/operations/catalog/offers/delete',
      true,
      { offerId: offer.id },
      translate('catalogAdmin.deleteOfferSummary', { id: offer.id, name: offer.localizationId }),
      async () => {
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
      true,
      {
        offerId: selectedOfferId,
        productType: Number(newProduct.productType),
        furnitureDefinitionId: newProduct.furnitureDefinitionId ? Number(newProduct.furnitureDefinitionId) : null,
        extraParam: newProduct.extraParam.trim() ? newProduct.extraParam.trim() : null,
        quantity: Number(newProduct.quantity) || 1,
        uniqueSize: Number(newProduct.uniqueSize) || 0,
        uniqueRemaining: Number(newProduct.uniqueRemaining) || 0,
        buildersClubEligible: newProduct.buildersClubEligible,
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
    editProductOriginal = product;
    editProductForm = {
      productType: product.productType,
      furnitureDefinitionId: product.furnitureDefinitionEntityId ?? '',
      furnitureName: product.furnitureName || '',
      extraParam: product.extraParam || '',
      quantity: product.quantity,
      uniqueSize: product.uniqueSize,
      uniqueRemaining: product.uniqueRemaining,
      buildersClubEligible: product.buildersClubEligible,
    };
  }

  function stageUpdateProduct() {
    if (!canManage || !editProductForm || editProductId === null) return;

    stage(
      'updateProduct',
      translate('catalogAdmin.edit'),
      '/api/v1/operations/catalog/products/update',
      true,
      {
        productId: editProductId,
        productType: Number(editProductForm.productType),
        furnitureDefinitionId: editProductForm.furnitureDefinitionId ? Number(editProductForm.furnitureDefinitionId) : null,
        extraParam: editProductForm.extraParam.trim() ? editProductForm.extraParam.trim() : null,
        quantity: Number(editProductForm.quantity) || 1,
        uniqueSize: Number(editProductForm.uniqueSize) || 0,
        uniqueRemaining: Number(editProductForm.uniqueRemaining) || 0,
        buildersClubEligible: editProductForm.buildersClubEligible,
      },
      translate('catalogAdmin.updateProductSummary', { id: editProductId }),
      async () => {
        editProductId = null;
        await loadOfferDetail(selectedOfferId);
      },
      diffFields(
        // The loaded product names the definition `furnitureDefinitionEntityId`; the form uses the
        // shorter key the endpoint takes. Aligned here so the diff compares like with like.
        { ...editProductOriginal, furnitureDefinitionId: editProductOriginal?.furnitureDefinitionEntityId ?? '' },
        editProductForm,
        PRODUCT_FIELDS(),
      ),
    );
  }


  function stageDeleteProduct(product) {
    if (!canManage) return;

    stage(
      'deleteProduct',
      translate('catalogAdmin.deleteProduct'),
      '/api/v1/operations/catalog/products/delete',
      true,
      { productId: product.id },
      translate('catalogAdmin.deleteProductSummary', { id: product.id }),
      async () => {
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
    <button type="button" class="ghost-button" onclick={refreshAll} disabled={pagesLoading}>{$t('common.refresh')}</button>
  </div>
  <p class="muted">
    {$t('catalogAdmin.description')}
  </p>

  <div class="catalog-tabs">
    {#each CATALOG_TYPES as ct}
      <button type="button" class="catalog-tab" class:active={catalogType === ct.value} onclick={() => switchCatalogType(ct.value)}>
        <Tag size={14} strokeWidth={2} aria-hidden="true" />
        {$t(ct.key)}
      </button>
    {/each}
  </div>

  <nav class="breadcrumb" aria-label="Catalog page path">
    <button type="button" class="crumb-button" class:active={parentChain.length === 0} onclick={() => drillToBreadcrumb(-1)}>
      <Folder size={14} strokeWidth={2} aria-hidden="true" /> {$t('catalogAdmin.root')}
    </button>
    {#each parentChain as crumb, i}
      <ChevronRight size={14} strokeWidth={2} class="muted" aria-hidden="true" />
      <button type="button" class="crumb-button" class:active={i === parentChain.length - 1} onclick={() => drillToBreadcrumb(i)}>
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
              <img src={currentPage.iconUrl} alt="" loading="lazy" />
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
          <button type="button" class="ghost-button" onclick={startEditPage}>
            <Pencil size={14} strokeWidth={2} aria-hidden="true" /> {$t('catalogAdmin.edit')}
          </button>
        {/if}
      </div>
      <div class="metric-grid compact">
        <StatCard label={$t('catalogAdmin.layout')} value={currentPage.layout}>
          {#snippet icon()}
            <Activity size={15} strokeWidth={2} aria-hidden="true" />
          {/snippet}
        </StatCard>
        <StatCard label={$t('catalogAdmin.icon')}>
          {#snippet icon()}
            <Image size={15} strokeWidth={2} aria-hidden="true" />
          {/snippet}
          {#snippet value()}
                    <strong class="icon-preview">
              {#if currentPage.iconUrl}<img src={currentPage.iconUrl} alt="" loading="lazy" />{/if}
              #{currentPage.icon}
            </strong>
          {/snippet}
        </StatCard>
        <StatCard label={$t('catalogAdmin.sortOrder')} value={currentPage.sortOrder}>
          {#snippet icon()}
            <Hash size={15} strokeWidth={2} aria-hidden="true" />
          {/snippet}
        </StatCard>
        <StatCard label={$t('catalogAdmin.visible')}>
          {#snippet icon()}
            <Activity size={15} strokeWidth={2} aria-hidden="true" />
          {/snippet}
          {#snippet value()}
                    <span>
              <span class="status-badge" class:status-badge--ok={currentPage.visible} class:status-badge--bad={!currentPage.visible}>
                {#if currentPage.visible}<Eye size={12} strokeWidth={2} aria-hidden="true" />{:else}<EyeOff size={12} strokeWidth={2} aria-hidden="true" />{/if}
                {currentPage.visible ? $t('catalogAdmin.visible') : $t('catalogAdmin.hidden')}
              </span>
            </span>
          {/snippet}
        </StatCard>
        <StatCard label={$t('catalogAdmin.imageData')} value={$t('catalogAdmin.lineCount', { count: (currentPage.imageData || []).length })}>
          {#snippet icon()}
            <Hash size={15} strokeWidth={2} aria-hidden="true" />
          {/snippet}
        </StatCard>
        <StatCard label={$t('catalogAdmin.textData')} value={$t('catalogAdmin.lineCount', { count: (currentPage.textData || []).length })}>
          {#snippet icon()}
            <Hash size={15} strokeWidth={2} aria-hidden="true" />
          {/snippet}
        </StatCard>
      </div>


      {#if canManage}
        <div class="catalog-card-detail">
          <div class="op-field">
            <div class="op-pick">
              <button type="button" class="ghost-button danger" onclick={stageDeletePage}>
                <Trash2 size={14} strokeWidth={2} aria-hidden="true" /> {$t('catalogAdmin.deletePage')}
              </button>
            </div>
            <p class="muted">{$t('catalogAdmin.deletePageBlockedNote')}</p>
          </div>
          {#if $ops.errors.deletePage}<p class="empty-state danger" role="alert">{$ops.errors.deletePage}</p>{/if}
          {#if $ops.results.deletePage}
            <OpResult result={$ops.results.deletePage} />
          {/if}
        </div>
      {/if}
    </section>
  {/if}

  <section class="panel">
    <div class="panel-head">
      <h2><Folder size={17} strokeWidth={2} aria-hidden="true" /> {$t('catalogAdmin.subPages')}</h2>
      {#if canManage}
        <button type="button" class="ghost-button" onclick={() => (newPageOpen = !newPageOpen)}>
          <Plus size={14} strokeWidth={2} aria-hidden="true" /> {newPageOpen ? $t('catalogAdmin.cancel') : $t('catalogAdmin.newPage')}
        </button>
      {/if}
    </div>


    {#if pagesLoading}
      <p class="muted">{$t('catalogAdmin.loadingPages')}</p>
    {:else if pagesError}
      <p class="empty-state danger" role="alert">{pagesError}</p>
    {:else if pages.length === 0}
      <p class="empty-state">{$t('catalogAdmin.noSubPages')}</p>
    {:else}
      <div class="catalog-list">
        {#each pages as page (page.id)}
          <button type="button" class="catalog-row" onclick={() => drillInto(page)}>
            <span class="catalog-row-icon">
              {#if page.iconUrl}
                <img src={page.iconUrl} alt="" loading="lazy" />
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
          <button type="button" class="ghost-button" onclick={openNewOffer}>
            <Plus size={14} strokeWidth={2} aria-hidden="true" /> {newOfferOpen ? $t('catalogAdmin.cancel') : $t('catalogAdmin.newOffer')}
          </button>
        {/if}
      </div>


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
                  <button type="button" class="ghost-button" class:active={selectedOfferId === offer.id} onclick={() => toggleOfferDetail(offer.id)}>
                    <Package size={14} strokeWidth={2} aria-hidden="true" /> {offerActionLabel(offer, selectedOfferId, $t)}
                  </button>
                  {#if canManage}
                    <button type="button" class="ghost-button" onclick={() => startEditOffer(offer)}>
                      <Pencil size={14} strokeWidth={2} aria-hidden="true" /> {$t('catalogAdmin.edit')}
                    </button>
                  {/if}
                </div>
              </div>

              {#if offer.productCount === 1 && offer.singleProduct}
                <div class="catalog-row-sub">
                  <span class="catalog-row-icon small">
                    {#if offer.singleProduct.furnitureIconUrl}
                      <img src={offer.singleProduct.furnitureIconUrl} alt="" loading="lazy" />
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


              {#if canManage}
                <div class="catalog-card-detail op-pick">
                  <button type="button" class="ghost-button danger" onclick={() => stageDeleteOffer(offer)}>
                    <Trash2 size={14} strokeWidth={2} aria-hidden="true" /> {$t('catalogAdmin.deleteOffer')}
                  </button>
                </div>
              {/if}

              {#if selectedOfferId === offer.id}
                <div class="catalog-card-detail products-panel">
                  <div class="panel-head">
                    <h3><Package size={15} strokeWidth={2} aria-hidden="true" /> {$t('catalogAdmin.itemsDelivered')}</h3>
                    {#if canManage}
                      <button type="button" class="ghost-button" onclick={() => (newProductOpen = !newProductOpen)}>
                        <Plus size={14} strokeWidth={2} aria-hidden="true" /> {newProductOpen ? $t('catalogAdmin.cancel') : $t('catalogAdmin.addItem')}
                      </button>
                    {/if}
                  </div>

                  {#if offerDetailLoading}
                    <p class="muted">{$t('catalogAdmin.loadingProducts')}</p>
                  {:else if offerDetailError}
                    <p class="empty-state danger" role="alert">{offerDetailError}</p>
                  {:else if offerDetail}

                    {#if offerDetail.products.length === 0}
                      <p class="empty-state">{$t('catalogAdmin.noProducts')}</p>
                    {:else}
                      <div class="catalog-list">
                        {#each offerDetail.products as product (product.id)}
                          <div class="catalog-card">
                            <div class="catalog-row static">
                              <span class="catalog-row-icon">
                                {#if product.furnitureIconUrl}
                                  <img src={product.furnitureIconUrl} alt="" loading="lazy" />
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
                                <button type="button" class="ghost-button" onclick={() => startEditProduct(product)}>
                                  <Pencil size={14} strokeWidth={2} aria-hidden="true" /> {$t('catalogAdmin.edit')}
                                </button>
                              {/if}
                            </div>


                            {#if canManage}
                              <div class="catalog-card-detail op-pick">
                                <button type="button" class="ghost-button danger" onclick={() => stageDeleteProduct(product)}>
                                  <Trash2 size={14} strokeWidth={2} aria-hidden="true" /> {$t('catalogAdmin.deleteProduct')}
                                </button>
                              </div>
                            {/if}
                          </div>
                        {/each}
                      </div>
                    {/if}
                    {#if $ops.errors.deleteProduct}<p class="empty-state danger" role="alert">{$ops.errors.deleteProduct}</p>{/if}
                    {#if $ops.results.deleteProduct}
                      <OpResult result={$ops.results.deleteProduct} />
                    {/if}
                  {/if}
                </div>
              {/if}
            </div>
          {/each}
        </div>
      {/if}
      {#if $ops.errors.deleteOffer}<p class="empty-state danger" role="alert">{$ops.errors.deleteOffer}</p>{/if}
      {#if $ops.results.deleteOffer}
        <OpResult result={$ops.results.deleteOffer} />
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

{#if editPageOpen && editPageForm}
  <Drawer title={$t('catalog.editPage')} eyebrow={$t('catalog.title')} onclose={() => { editPageOpen = false; }}>
    <div class="catalog-card-detail">
      <div class="op-field">
        <label for="edit-page-localization">{$t('catalogAdmin.localizationKeyRequired')}</label>
        <input autocomplete="off" spellcheck="false" id="edit-page-localization" bind:value={editPageForm.localization} />
      </div>
      <div class="op-field">
        <label for="edit-page-name">{$t('catalogAdmin.displayName')}</label>
        <input autocomplete="off" spellcheck="false" id="edit-page-name" bind:value={editPageForm.name} />
      </div>
      <div class="op-field">
        <span class="op-label">{$t('catalogAdmin.icon')}</span>
        <div class="op-pick">
          <button class="ghost-button" type="button" onclick={() => (iconPickerTarget = 'edit')}>
            <Image size={14} strokeWidth={2} aria-hidden="true" /> {$t('catalogAdmin.selectIcon')}
          </button>
          <span class="op-chip">
            {#if iconUrlFor(editPageForm.icon)}<img class="op-sprite" src={iconUrlFor(editPageForm.icon)} alt="" loading="lazy" />{:else}<span class="op-sprite">{editPageForm.icon}</span>{/if}
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
        <input autocomplete="off" spellcheck="false" id="edit-page-sort" type="number" bind:value={editPageForm.sortOrder} />
      </div>
      <div class="op-field">
        <label><input autocomplete="off" spellcheck="false" type="checkbox" bind:checked={editPageForm.visible} /> {$t('catalogAdmin.visible')}</label>
      </div>
      <div class="op-field">
        <label for="edit-page-image-data">{$t('catalogAdmin.imageDataOptional')}</label>
        <textarea id="edit-page-image-data" rows="3" bind:value={editPageForm.imageDataText} placeholder="promo_banner.png"></textarea>
      </div>
      <div class="op-field">
        <label for="edit-page-text-data">{$t('catalogAdmin.textDataOptional')}</label>
        <textarea id="edit-page-text-data" rows="3" bind:value={editPageForm.textDataText} placeholder="Welcome to our shop!"></textarea>
      </div>
      <div class="op-actions">
        <button type="button" onclick={stageUpdatePage} disabled={$ops.busyKeys.updatePage}>{$t('catalogAdmin.save')}</button>
        <button class="ghost-button" type="button" onclick={() => (editPageOpen = false)}>{$t('catalogAdmin.cancel')}</button>
      </div>
      {#if $ops.errors.updatePage}<p class="empty-state danger" role="alert">{$ops.errors.updatePage}</p>{/if}
      {#if $ops.results.updatePage}
        <OpResult result={$ops.results.updatePage} />
      {/if}
    </div>
  </Drawer>
{/if}

{#if newPageOpen}
  <Drawer title={$t('catalog.newPage')} eyebrow={$t('catalog.title')} onclose={() => { newPageOpen = false; }}>
    <div class="catalog-card-detail">
      <div class="op-field">
        <label for="new-page-localization">{$t('catalogAdmin.localizationKeyRequired')}</label>
        <input autocomplete="off" spellcheck="false" id="new-page-localization" bind:value={newPage.localization} placeholder={$t('catalogAdmin.localizationPlaceholder')} />
      </div>
      <div class="op-field">
        <label for="new-page-name">{$t('catalogAdmin.displayName')}</label>
        <input autocomplete="off" spellcheck="false" id="new-page-name" bind:value={newPage.name} />
      </div>
      <div class="op-field">
        <span class="op-label">{$t('catalogAdmin.icon')}</span>
        <div class="op-pick">
          <button class="ghost-button" type="button" onclick={() => (iconPickerTarget = 'new')}>
            <Image size={14} strokeWidth={2} aria-hidden="true" /> {$t('catalogAdmin.selectIcon')}
          </button>
          <span class="op-chip">
            {#if iconUrlFor(newPage.icon)}<img class="op-sprite" src={iconUrlFor(newPage.icon)} alt="" loading="lazy" />{:else}<span class="op-sprite">{newPage.icon}</span>{/if}
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
        <input autocomplete="off" spellcheck="false" id="new-page-sort" type="number" bind:value={newPage.sortOrder} />
      </div>
      <div class="op-field">
        <label><input autocomplete="off" spellcheck="false" type="checkbox" bind:checked={newPage.visible} /> {$t('catalogAdmin.visible')}</label>
      </div>
      <div class="op-field">
        <label for="new-page-image-data">{$t('catalogAdmin.imageDataOptional')}</label>
        <textarea id="new-page-image-data" rows="3" bind:value={newPage.imageDataText} placeholder="promo_banner.png"></textarea>
      </div>
      <div class="op-field">
        <label for="new-page-text-data">{$t('catalogAdmin.textDataOptional')}</label>
        <textarea id="new-page-text-data" rows="3" bind:value={newPage.textDataText} placeholder="Welcome to our shop!"></textarea>
      </div>
      <div class="op-actions">
        <button type="button" onclick={stageCreatePage} disabled={$ops.busyKeys.createPage}>{$t('catalogAdmin.create')}</button>
      </div>
      {#if $ops.errors.createPage}<p class="empty-state danger" role="alert">{$ops.errors.createPage}</p>{/if}
      {#if $ops.results.createPage}
        <OpResult result={$ops.results.createPage} />
      {/if}
    </div>
  </Drawer>
{/if}

{#if newOfferOpen}
  <Drawer title={$t('catalog.newOffer')} eyebrow={$t('catalog.title')} onclose={() => { newOfferOpen = false; }}>
    <div class="catalog-card-detail">
      <div class="op-field">
        <label for="new-offer-localization">{$t('catalogAdmin.localizationIdRequired')}</label>
        <input autocomplete="off" spellcheck="false" id="new-offer-localization" bind:value={newOffer.localizationId} />
        <small class="muted">{$t('catalogAdmin.localizationIdHint')}</small>
      </div>
      <div class="op-field">
        <label for="new-offer-credits">{$t('catalogAdmin.costCredits')}</label>
        <input autocomplete="off" spellcheck="false" id="new-offer-credits" type="number" min="0" bind:value={newOffer.costCredits} />
      </div>
      <div class="op-field">
        <label for="new-offer-currency-amount">{$t('catalogAdmin.costSecondary')}</label>
        <input autocomplete="off" spellcheck="false" id="new-offer-currency-amount" type="number" min="0" bind:value={newOffer.costCurrency} />
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
        <input autocomplete="off" spellcheck="false" id="new-offer-club-level" type="number" min="0" bind:value={newOffer.clubLevel} />
      </div>
      <div class="op-field">
        <label for="new-offer-discount">{$t('catalogAdmin.discountPercent')}</label>
        <input autocomplete="off" spellcheck="false" id="new-offer-discount" type="number" min="0" max="100" bind:value={newOffer.discountPercent} />
      </div>
      <div class="op-field">
        <label><input autocomplete="off" spellcheck="false" type="checkbox" bind:checked={newOffer.canGift} /> {$t('catalogAdmin.canGift')}</label>
      </div>
      <div class="op-field">
        <label><input autocomplete="off" spellcheck="false" type="checkbox" bind:checked={newOffer.canBundle} /> {$t('catalogAdmin.canBundle')}</label>
      </div>
      <div class="op-field">
        <label><input autocomplete="off" spellcheck="false" type="checkbox" bind:checked={newOffer.visible} /> {$t('catalogAdmin.visible')}</label>
      </div>
      <div class="op-actions">
        <button type="button" onclick={stageCreateOffer} disabled={$ops.busyKeys.createOffer}>{$t('catalogAdmin.create')}</button>
      </div>
      {#if $ops.errors.createOffer}<p class="empty-state danger" role="alert">{$ops.errors.createOffer}</p>{/if}
      {#if $ops.results.createOffer}
        <OpResult result={$ops.results.createOffer} />
      {/if}
    </div>
  </Drawer>
{/if}

{#if editOfferId !== null && editOfferForm}
  <Drawer title={$t('catalog.editOffer')} eyebrow={$t('catalog.title')} onclose={() => { editOfferId = null; editOfferForm = null; }}>
    <div class="catalog-card-detail">
      <div class="op-field">
        <label for={`edit-offer-localization-${editOfferId}`}>{$t('catalogAdmin.localizationIdRequired')}</label>
        <input autocomplete="off" spellcheck="false" id={`edit-offer-localization-${editOfferId}`} bind:value={editOfferForm.localizationId} />
      </div>
      <div class="op-field">
        <label for={`edit-offer-credits-${editOfferId}`}>{$t('catalogAdmin.costCredits')}</label>
        <input autocomplete="off" spellcheck="false" id={`edit-offer-credits-${editOfferId}`} type="number" min="0" bind:value={editOfferForm.costCredits} />
      </div>
      <div class="op-field">
        <label for={`edit-offer-currency-amount-${editOfferId}`}>{$t('catalogAdmin.costSecondary')}</label>
        <input autocomplete="off" spellcheck="false" id={`edit-offer-currency-amount-${editOfferId}`} type="number" min="0" bind:value={editOfferForm.costCurrency} />
      </div>
      <div class="op-field">
        <label for={`edit-offer-currency-type-${editOfferId}`}>{$t('catalogAdmin.currencyTypeOptional')}</label>
        <select id={`edit-offer-currency-type-${editOfferId}`} bind:value={editOfferForm.currencyTypeId}>
          <option value="">{$t('catalogAdmin.none')}</option>
          {#each currencyTypes as c}<option value={c.id}>{c.name || c.type} (#{c.id})</option>{/each}
        </select>
      </div>
      <div class="op-field">
        <label for={`edit-offer-club-${editOfferId}`}>{$t('catalogAdmin.clubLevel')}</label>
        <input autocomplete="off" spellcheck="false" id={`edit-offer-club-${editOfferId}`} type="number" min="0" bind:value={editOfferForm.clubLevel} />
      </div>
      <div class="op-field">
        <label for={`edit-offer-discount-${editOfferId}`}>{$t('catalogAdmin.discountPercent')}</label>
        <input autocomplete="off" spellcheck="false" id={`edit-offer-discount-${editOfferId}`} type="number" min="0" max="100" bind:value={editOfferForm.discountPercent} />
      </div>
      <div class="op-field"><label><input autocomplete="off" spellcheck="false" type="checkbox" bind:checked={editOfferForm.canGift} /> {$t('catalogAdmin.canGift')}</label></div>
      <div class="op-field"><label><input autocomplete="off" spellcheck="false" type="checkbox" bind:checked={editOfferForm.canBundle} /> {$t('catalogAdmin.canBundle')}</label></div>
      <div class="op-field"><label><input autocomplete="off" spellcheck="false" type="checkbox" bind:checked={editOfferForm.visible} /> {$t('catalogAdmin.visible')}</label></div>
      <div class="op-actions">
        <button type="button" onclick={stageUpdateOffer} disabled={$ops.busyKeys.updateOffer}>{$t('catalogAdmin.save')}</button>
        <button class="ghost-button" type="button" onclick={() => (editOfferId = null)}>{$t('catalogAdmin.cancel')}</button>
      </div>
      {#if $ops.errors.updateOffer}<p class="empty-state danger" role="alert">{$ops.errors.updateOffer}</p>{/if}
      {#if $ops.results.updateOffer}
        <OpResult result={$ops.results.updateOffer} />
      {/if}
    </div>
  </Drawer>
{/if}

{#if newProductOpen}
  <Drawer title={$t('catalog.newProduct')} eyebrow={$t('catalog.title')} onclose={() => { newProductOpen = false; }}>
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
            onclick={() => pickProductFurniture((f) => (newProduct = { ...newProduct, furnitureDefinitionId: f.id, furnitureName: f.name, furnitureSprite: f.spriteId, furnitureIcon: f.iconUrl }))}
          >
            <Image size={14} strokeWidth={2} aria-hidden="true" /> {$t('common.selectFurniture')}
          </button>
          {#if newProduct.furnitureDefinitionId}
            <span class="op-chip">
              {#if newProduct.furnitureIcon}<img class="op-sprite" src={newProduct.furnitureIcon} alt="" loading="lazy" />{:else}<span class="op-sprite">{newProduct.furnitureSprite}</span>{/if}
              {newProduct.furnitureName} <small>#{newProduct.furnitureDefinitionId}</small>
            </span>
            <button class="ghost-button" type="button" onclick={() => (newProduct = { ...newProduct, furnitureDefinitionId: '', furnitureName: '', furnitureIcon: '', furnitureSprite: '' })}>{$t('catalogAdmin.clear')}</button>
          {:else}
            <span class="muted">{$t('common.noFurnitureSelected')}</span>
          {/if}
        </div>
      </div>
      <div class="op-field">
        <label for="new-product-extra">{$t('catalogAdmin.extraData')}</label>
        <input autocomplete="off" spellcheck="false" id="new-product-extra" bind:value={newProduct.extraParam} placeholder={$t('operations.extraDataPlaceholder')} />
      </div>
      <div class="op-field">
        <label for="new-product-quantity">{$t('catalogAdmin.quantity')}</label>
        <input autocomplete="off" spellcheck="false" id="new-product-quantity" type="number" min="1" bind:value={newProduct.quantity} />
      </div>
      <div class="op-field">
        <label for="new-product-unique-size">{$t('catalogAdmin.uniqueSizeHint')}</label>
        <input autocomplete="off" spellcheck="false" id="new-product-unique-size" type="number" min="0" bind:value={newProduct.uniqueSize} />
      </div>
      <div class="op-field">
        <label for="new-product-unique-remaining">{$t('catalogAdmin.uniqueRemaining')}</label>
        <input autocomplete="off" spellcheck="false" id="new-product-unique-remaining" type="number" min="0" bind:value={newProduct.uniqueRemaining} />
      </div>
      <div class="op-field">
        <label><input autocomplete="off" spellcheck="false" type="checkbox" bind:checked={newProduct.buildersClubEligible} /> {$t('catalogAdmin.bcEligible')}</label>
      </div>
      <div class="op-actions">
        <button type="button" onclick={stageCreateProduct} disabled={$ops.busyKeys.createProduct}>{$t('catalogAdmin.create')}</button>
      </div>
      {#if $ops.errors.createProduct}<p class="empty-state danger" role="alert">{$ops.errors.createProduct}</p>{/if}
      {#if $ops.results.createProduct}
        <OpResult result={$ops.results.createProduct} />
      {/if}
    </div>
  </Drawer>
{/if}

{#if editProductId !== null && editProductForm}
  <Drawer title={$t('catalog.editProduct')} eyebrow={$t('catalog.title')} onclose={() => { editProductId = null; editProductForm = null; }}>
    <div class="catalog-card-detail">
      <div class="op-field">
        <label for={`edit-product-type-${editProductId}`}>{$t('catalogAdmin.productType')}</label>
        <select id={`edit-product-type-${editProductId}`} bind:value={editProductForm.productType}>
          {#each PRODUCT_TYPES as pt}<option value={pt.value}>{pt.label}</option>{/each}
        </select>
      </div>
      <div class="op-field">
        <label for={`edit-product-def-${editProductId}`}>{$t('catalogAdmin.furnitureDefIdOptional')}</label>
        <input autocomplete="off" spellcheck="false" id={`edit-product-def-${editProductId}`} type="number" min="0" bind:value={editProductForm.furnitureDefinitionId} />
      </div>
      <div class="op-field">
        <label for={`edit-product-extra-${editProductId}`}>{$t('catalogAdmin.extraData')}</label>
        <input autocomplete="off" spellcheck="false" id={`edit-product-extra-${editProductId}`} bind:value={editProductForm.extraParam} />
      </div>
      <div class="op-field">
        <label for={`edit-product-qty-${editProductId}`}>{$t('catalogAdmin.quantity')}</label>
        <input autocomplete="off" spellcheck="false" id={`edit-product-qty-${editProductId}`} type="number" min="1" bind:value={editProductForm.quantity} />
      </div>
      <div class="op-field">
        <label for={`edit-product-usize-${editProductId}`}>{$t('catalogAdmin.uniqueSizeHint')}</label>
        <input autocomplete="off" spellcheck="false" id={`edit-product-usize-${editProductId}`} type="number" min="0" bind:value={editProductForm.uniqueSize} />
      </div>
      <div class="op-field">
        <label for={`edit-product-urem-${editProductId}`}>{$t('catalogAdmin.uniqueRemaining')}</label>
        <input autocomplete="off" spellcheck="false" id={`edit-product-urem-${editProductId}`} type="number" min="0" bind:value={editProductForm.uniqueRemaining} />
      </div>
      <div class="op-field"><label><input autocomplete="off" spellcheck="false" type="checkbox" bind:checked={editProductForm.buildersClubEligible} /> {$t('catalogAdmin.bcEligible')}</label></div>
      <div class="op-actions">
        <button type="button" onclick={stageUpdateProduct} disabled={$ops.busyKeys.updateProduct}>{$t('catalogAdmin.save')}</button>
        <button class="ghost-button" type="button" onclick={() => (editProductId = null)}>{$t('catalogAdmin.cancel')}</button>
      </div>
      {#if $ops.errors.updateProduct}<p class="empty-state danger" role="alert">{$ops.errors.updateProduct}</p>{/if}
      {#if $ops.results.updateProduct}
        <OpResult result={$ops.results.updateProduct} />
      {/if}
    </div>
  </Drawer>
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
  danger={$ops.pending?.danger ?? false}
  onconfirm={ops.confirm}
  oncancel={() => ops.cancel()}
/>

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

  .ghost-button.active {
    border-color: var(--accent);
    color: var(--ink);
    background: rgba(var(--accent-rgb), 0.12);
  }

  .ghost-button,
  .crumb-button,
  .catalog-tab,
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
