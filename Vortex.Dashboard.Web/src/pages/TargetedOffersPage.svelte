<script>
  import ConfirmStagedModal from '../components/ConfirmStagedModal.svelte';
  import OpResult from '../components/OpResult.svelte';
  import { onMount } from 'svelte';
  import {
    Clock,
    Coins,
    Eye,
    EyeOff,
    Image,
    Package,
    Pencil,
    Plus,
    Sparkles,
    Target,
    Trash2,
    Users,
  } from '@lucide/svelte';
  import { apiGet } from '../lib/api.js';
  import { createWriteOps } from '../lib/writeOps.js';
  import { isPermissionDeniedError, hasDashboardCapability } from '../lib/permissions.js';
  import { CAPABILITIES } from '../lib/dashboardPermissions.js';
  import AccessDeniedNotice from '../components/AccessDeniedNotice.svelte';
  import Drawer from '../components/Drawer.svelte';
  import AssetImage from '../components/AssetImage.svelte';
  import OfferImageField from '../components/OfferImageField.svelte';
  import ConfirmReasonModal from '../components/ConfirmReasonModal.svelte';
  import { identity } from '../lib/session.js';
  import { t, translate } from '../lib/i18n.js';

  function emptyOfferForm() {
    return {
      identifier: '', offerType: 0, title: '', description: '', imageUrl: '', iconImageUrl: '',
      productCode: '', priceInCredits: 0, priceInActivityPoints: 0, activityPointType: 0,
      purchaseLimit: 0, expiresAt: '', active: true, sortOrder: 0,
    };
  }

  function emptyProductForm() {
    return { productCode: '', furnitureDefinitionId: '', quantity: 1 };
  }

  // The API returns/consumes ExpiresAt as an ISO instant (or null); <input type="datetime-local">
  // wants a local `yyyy-MM-ddThh:mm` string with no zone. These two helpers bridge the pair -- an
  // empty field means "no expiry" (null on the wire), matching the nullable DateTime? server-side.
  function toDateTimeLocal(iso) {
    if (!iso) return '';
    const date = new Date(iso);
    if (Number.isNaN(date.getTime())) return '';
    const pad = (n) => String(n).padStart(2, '0');
    return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}T${pad(date.getHours())}:${pad(date.getMinutes())}`;
  }

  function fromDateTimeLocal(value) {
    if (!value) return null;
    const date = new Date(value);
    return Number.isNaN(date.getTime()) ? null : date.toISOString();
  }

  let activeOnly = $state(false);

  // Admin-form metadata loaded once: the configured promo-image base template (drives the
  // filename-only image inputs) and the currency types for the activity-point picker.
  let imageTemplate = $state(null);
  let currencyTypes = $state([]);
  // Available promo images (from the asset folder) for the gallery picker, so operators pick a real
  // image instead of typing a filename blind. Empty when the picker is unconfigured.
  let offerImages = $state([]);
  // Activity points can be paid in any non-Credits currency; Credits are handled by the separate
  // credits price field, so excluding them here avoids offering the same currency twice.
  let activityPointCurrencyTypes = $derived(currencyTypes.filter((c) => c.type !== 'Credits'));

  let offers = $state([]);
  let loading = $state(false);
  let error = $state('');
  let forbidden = $state(false);

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

  // Nothing here asks the operator for a reason: createWriteOps builds the audited sentence from the
  // action itself and the confirm dialog takes an optional note. Two stores rather than one so
  // staging an edit cannot open the delete dialog (and vice versa); each form passes a `key` so its
  // busy state, error and OpResult land next to it instead of in one banner for the whole page.
  const ops = createWriteOps();
  const deleteOps = createWriteOps();

  let canManage = $derived(hasDashboardCapability($identity, CAPABILITIES.opsTargetedOffersManage));

  async function loadOffers() {
    loading = true;
    error = '';
    forbidden = false;

    try {
      const data = await apiGet(`/api/targeted-offers?activeOnly=${activeOnly ? 'true' : 'false'}`);
      offers = data.items || [];
    } catch (err) {
      if (isPermissionDeniedError(err)) {
        forbidden = true;
        offers = [];
        return;
      }

      error = err.message;
      offers = [];
    } finally {
      loading = false;
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
      offerDetail = await apiGet(`/api/targeted-offers/${offerId}`);
    } catch (err) {
      offerDetailError = isPermissionDeniedError(err) ? translate('common.insufficientRights') : err.code || err.message;
    } finally {
      offerDetailLoading = false;
    }
  }

  // Toggles the inline bundle-products panel under the clicked offer's own card -- clicking the same
  // offer's action button again collapses it instead of re-fetching.
  async function toggleOfferDetail(offerId) {
    if (selectedOfferId === offerId) {
      selectedOfferId = null;
      offerDetail = null;
      return;
    }

    await loadOfferDetail(offerId);
  }

  function offerProductsLabel(offer, expandedOfferId, translator) {
    if (expandedOfferId === offer.id) {
      return translator('targetedOffers.hideProducts');
    }

    return translator('targetedOffers.productsCount', { count: offer.productCount });
  }

  async function refreshAll() {
    await loadOffers();
    if (selectedOfferId) {
      await loadOfferDetail(selectedOfferId);
    }
  }

  const stage = (id, title, endpoint, valid, body, summary, onSuccess) =>
    ops.ask(endpoint, body, title, summary, {
      key: id,
      valid,
      invalidMessage: translate('targetedOffers.fillFields'),
      onSuccess,
    });

  // Both create and update take the same field set; update additionally carries the offerId. Field
  // order is irrelevant on the wire (System.Text.Json binds by name), so a single builder is safe.
  function buildOfferBody(form, offerId) {
    const body = {
      identifier: form.identifier.trim(),
      offerType: Number(form.offerType) || 0,
      title: form.title.trim(),
      description: form.description.trim(),
      imageUrl: form.imageUrl.trim(),
      iconImageUrl: form.iconImageUrl.trim(),
      productCode: form.productCode.trim(),
      priceInCredits: Number(form.priceInCredits) || 0,
      priceInActivityPoints: Number(form.priceInActivityPoints) || 0,
      activityPointType: Number(form.activityPointType) || 0,
      purchaseLimit: Number(form.purchaseLimit) || 0,
      expiresAt: fromDateTimeLocal(form.expiresAt),
      active: form.active,
      sortOrder: Number(form.sortOrder) || 0,
    };

    return offerId === null ? body : { offerId, ...body };
  }

  function stageCreateOffer() {
    if (!canManage) return;

    stage(
      'createOffer',
      translate('targetedOffers.newOffer'),
      '/api/operations/targeted-offers',
      Boolean(newOffer.identifier.trim()),
      buildOfferBody(newOffer, null),
      translate('targetedOffers.createOfferSummary', { name: newOffer.identifier.trim() }),
      async () => {
        newOfferOpen = false;
        newOffer = emptyOfferForm();
        await loadOffers();
      },
    );
  }

  // The list rows omit description/imageUrl/iconImageUrl (only the detail endpoint has them), so the
  // edit form is populated from a fresh detail fetch rather than the list row.
  async function startEditOffer(offer) {
    editOfferId = offer.id;
    editOfferForm = null;
    ops.clear('updateOffer');

    try {
      const detail = await apiGet(`/api/targeted-offers/${offer.id}`);
      editOfferForm = {
        identifier: detail.identifier || '',
        offerType: detail.offerType ?? 0,
        title: detail.title || '',
        description: detail.description || '',
        imageUrl: detail.imageUrl || '',
        iconImageUrl: detail.iconImageUrl || '',
        productCode: detail.productCode || '',
        priceInCredits: detail.priceInCredits ?? 0,
        priceInActivityPoints: detail.priceInActivityPoints ?? 0,
        activityPointType: detail.activityPointType ?? 0,
        purchaseLimit: detail.purchaseLimit ?? 0,
        expiresAt: toDateTimeLocal(detail.expiresAt),
        active: detail.active,
        sortOrder: detail.sortOrder ?? 0,
      };
    } catch (err) {
      editOfferId = null;
      ops.fail(
        'updateOffer',
        isPermissionDeniedError(err) ? translate('common.insufficientRights') : err.code || err.message,
      );
    }
  }

  function stageUpdateOffer() {
    if (!canManage || !editOfferForm || editOfferId === null) return;

    stage(
      'updateOffer',
      translate('targetedOffers.edit'),
      '/api/operations/targeted-offers/update',
      Boolean(editOfferForm.identifier.trim()),
      buildOfferBody(editOfferForm, editOfferId),
      translate('targetedOffers.updateOfferSummary', { id: editOfferId }),
      async () => {
        const id = editOfferId;
        editOfferId = null;
        editOfferForm = null;
        await loadOffers();
        if (selectedOfferId === id) {
          await loadOfferDetail(id);
        }
      },
    );
  }

  function stageCreateProduct() {
    if (!canManage || selectedOfferId === null) return;

    stage(
      'createProduct',
      translate('targetedOffers.addProduct'),
      '/api/operations/targeted-offers/products',
      Boolean(newProduct.productCode.trim()),
      {
        offerId: selectedOfferId,
        productCode: newProduct.productCode.trim(),
        furnitureDefinitionId: newProduct.furnitureDefinitionId ? Number(newProduct.furnitureDefinitionId) : null,
        quantity: Number(newProduct.quantity) || 1,
      },
      translate('targetedOffers.addProductSummary', { id: selectedOfferId }),
      async () => {
        newProductOpen = false;
        newProduct = emptyProductForm();
        await loadOfferDetail(selectedOfferId);
        await loadOffers();
      },
    );
  }

  function startEditProduct(product) {
    editProductId = product.id;
    editProductForm = {
      productCode: product.productCode || '',
      furnitureDefinitionId: product.furnitureDefinitionEntityId ?? '',
      quantity: product.quantity,
    };
  }

  function stageUpdateProduct() {
    if (!canManage || !editProductForm || editProductId === null) return;

    stage(
      'updateProduct',
      translate('targetedOffers.edit'),
      '/api/operations/targeted-offers/products/update',
      Boolean(editProductForm.productCode.trim()),
      {
        productId: editProductId,
        productCode: editProductForm.productCode.trim(),
        furnitureDefinitionId: editProductForm.furnitureDefinitionId ? Number(editProductForm.furnitureDefinitionId) : null,
        quantity: Number(editProductForm.quantity) || 1,
      },
      translate('targetedOffers.updateProductSummary', { id: editProductId }),
      async () => {
        editProductId = null;
        editProductForm = null;
        await loadOfferDetail(selectedOfferId);
      },
    );
  }

  // On a server refusal (offer_has_purchases and friends) createWriteOps keeps the modal open with
  // the message, so the operator can react without re-opening it.
  function openDeleteOffer(offer) {
    if (!canManage) return;

    deleteOps.ask(
      '/api/operations/targeted-offers/delete',
      { offerId: offer.id },
      translate('targetedOffers.deleteOffer'),
      translate('targetedOffers.deleteOfferSummary', { id: offer.id, name: offer.identifier }),
      {
        key: 'deleteOffer',
        danger: true,
        onSuccess: async () => {
          if (selectedOfferId === offer.id) {
            selectedOfferId = null;
            offerDetail = null;
          }

          await loadOffers();
        },
      },
    );
  }

  function openDeleteProduct(product) {
    if (!canManage) return;

    deleteOps.ask(
      '/api/operations/targeted-offers/products/delete',
      { productId: product.id },
      translate('targetedOffers.deleteProduct'),
      translate('targetedOffers.deleteProductSummary', { id: product.id }),
      {
        key: 'deleteProduct',
        danger: true,
        onSuccess: async () => {
          await loadOfferDetail(selectedOfferId);
          await loadOffers();
        },
      },
    );
  }

  // Form metadata is optional polish: if it fails to load the form still works with plain full-URL
  // image inputs and the "none" activity-point option, so failures are swallowed rather than surfaced.
  async function loadFormMeta() {
    try {
      const meta = await apiGet('/api/targeted-offers/form-meta');
      imageTemplate = meta.imageTemplate ?? null;
      currencyTypes = meta.currencyTypes || [];
    } catch {
      imageTemplate = null;
      currencyTypes = [];
    }

    try {
      const gallery = await apiGet('/api/targeted-offers/images');
      offerImages = gallery.items || [];
    } catch {
      offerImages = [];
    }
  }

  onMount(() => {
    loadOffers();
    loadFormMeta();
  });
</script>

<section class="panel">
  <div class="panel-head">
    <h2>{$t('targetedOffers.title')}</h2>
    <div class="head-actions">
      <label class="active-toggle">
        <input autocomplete="off" spellcheck="false" type="checkbox" bind:checked={activeOnly} onchange={loadOffers} />
        {$t('targetedOffers.activeOnlyLabel')}
      </label>
      <button type="button" class="ghost-button" onclick={refreshAll} disabled={loading}>{$t('common.refresh')}</button>
    </div>
  </div>
  <p class="muted">{$t('targetedOffers.description')}</p>
</section>

{#if forbidden}
  <AccessDeniedNotice message={$t('targetedOffers.accessDenied')} />
{:else}
  <section class="panel">
    <div class="panel-head">
      <h2><Target size={17} strokeWidth={2} aria-hidden="true" /> {$t('targetedOffers.offersHeading')}</h2>
      {#if canManage}
        <button type="button" class="ghost-button" onclick={() => (newOfferOpen = !newOfferOpen)}>
          <Plus size={14} strokeWidth={2} aria-hidden="true" /> {newOfferOpen ? $t('targetedOffers.cancel') : $t('targetedOffers.newOffer')}
        </button>
      {/if}
    </div>


    {#if loading}
      <p class="muted">{$t('common.loading')}</p>
    {:else if error}
      <p class="empty-state danger" role="alert">{error}</p>
    {:else if offers.length === 0}
      <p class="empty-state">{$t('targetedOffers.noOffers')}</p>
    {:else}
      <div class="catalog-list">
        {#each offers as offer (offer.id)}
          <div class="catalog-card">
            <div class="offer-head">
              <AssetImage src={offer.imageUrl} alt={offer.title || offer.identifier} size={44} fallbackIcon={Sparkles} />
              <span class="catalog-row-main">
                <strong>{offer.title || offer.identifier}</strong>
                <small class="muted">{offer.identifier} - #{offer.id}{offer.productCode ? ` - ${offer.productCode}` : ''}</small>
              </span>
              <div class="op-actions offer-actions">
                <button type="button" class="ghost-button" class:active={selectedOfferId === offer.id} onclick={() => toggleOfferDetail(offer.id)}>
                  <Package size={14} strokeWidth={2} aria-hidden="true" /> {offerProductsLabel(offer, selectedOfferId, $t)}
                </button>
                {#if canManage}
                  <button type="button" class="ghost-button" onclick={() => startEditOffer(offer)}>
                    <Pencil size={14} strokeWidth={2} aria-hidden="true" /> {$t('targetedOffers.edit')}
                  </button>
                {/if}
              </div>
            </div>
            <div class="offer-meta">
              <span class="cost-chip"><Coins size={12} strokeWidth={2} aria-hidden="true" /> {offer.priceInCredits}c{offer.priceInActivityPoints > 0 ? ` + ${offer.priceInActivityPoints}pt` : ''}</span>
              <span class="op-chip" title={$t('targetedOffers.bundleProducts')}><Package size={12} strokeWidth={2} aria-hidden="true" /> {offer.productCount}</span>
              <span class="op-chip" title={$t('targetedOffers.buyers')}><Users size={12} strokeWidth={2} aria-hidden="true" /> {offer.buyerCount}</span>
              {#if offer.expired}
                <span class="status-badge status-badge--warn"><Clock size={12} strokeWidth={2} aria-hidden="true" /> {$t('targetedOffers.expiredLabel')}</span>
              {/if}
              <span class="status-badge" class:status-badge--ok={offer.active} class:status-badge--bad={!offer.active}>
                {#if offer.active}<Eye size={12} strokeWidth={2} aria-hidden="true" />{:else}<EyeOff size={12} strokeWidth={2} aria-hidden="true" />{/if}
                {offer.active ? $t('targetedOffers.activeLabel') : $t('targetedOffers.inactive')}
              </span>
            </div>


            {#if canManage}
              <div class="catalog-card-detail delete-bar">
                <button type="button" class="ghost-button danger" onclick={() => openDeleteOffer(offer)}>
                  <Trash2 size={14} strokeWidth={2} aria-hidden="true" /> {$t('targetedOffers.deleteOffer')}
                </button>
              </div>
            {/if}

            {#if selectedOfferId === offer.id}
              <div class="catalog-card-detail products-panel">
                <div class="panel-head">
                  <h3><Package size={15} strokeWidth={2} aria-hidden="true" /> {$t('targetedOffers.bundleProducts')}</h3>
                  {#if canManage}
                    <button type="button" class="ghost-button" onclick={() => (newProductOpen = !newProductOpen)}>
                      <Plus size={14} strokeWidth={2} aria-hidden="true" /> {newProductOpen ? $t('targetedOffers.cancel') : $t('targetedOffers.addProduct')}
                    </button>
                  {/if}
                </div>

                {#if offerDetailLoading}
                  <p class="muted">{$t('targetedOffers.loadingProducts')}</p>
                {:else if offerDetailError}
                  <p class="empty-state danger" role="alert">{offerDetailError}</p>
                {:else if offerDetail}

                  {#if offerDetail.products.length === 0}
                    <p class="empty-state">{$t('targetedOffers.noProducts')}</p>
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
                              <strong>{product.furnitureName || product.productCode}</strong>
                              <small class="muted">{product.productCode}{product.furnitureDefinitionEntityId ? ` - #${product.furnitureDefinitionEntityId}` : ''}</small>
                            </span>
                            <span class="catalog-row-meta">
                              <span class="op-chip" title={$t('targetedOffers.quantity')}>x{product.quantity}</span>
                            </span>
                            {#if canManage}
                              <button type="button" class="ghost-button" onclick={() => startEditProduct(product)}>
                                <Pencil size={14} strokeWidth={2} aria-hidden="true" /> {$t('targetedOffers.edit')}
                              </button>
                            {/if}
                          </div>


                          {#if canManage}
                            <div class="catalog-card-detail delete-bar">
                              <button type="button" class="ghost-button danger" onclick={() => openDeleteProduct(product)}>
                                <Trash2 size={14} strokeWidth={2} aria-hidden="true" /> {$t('targetedOffers.deleteProduct')}
                              </button>
                            </div>
                          {/if}
                        </div>
                      {/each}
                    </div>
                  {/if}
                  {#if $deleteOps.errors.deleteProduct}<p class="empty-state danger" role="alert">{$deleteOps.errors.deleteProduct}</p>{/if}
                  {#if $deleteOps.results.deleteProduct}
                    <OpResult result={$deleteOps.results.deleteProduct} />
                  {/if}
                {/if}
              </div>
            {/if}
          </div>
        {/each}
      </div>
    {/if}
    {#if $deleteOps.errors.deleteOffer}<p class="empty-state danger" role="alert">{$deleteOps.errors.deleteOffer}</p>{/if}
    {#if $deleteOps.results.deleteOffer}
      <OpResult result={$deleteOps.results.deleteOffer} />
    {/if}
  </section>
{/if}

<ConfirmStagedModal {ops} eyebrow={$t('targetedOffers.confirmEyebrow')} />

{#if newOfferOpen}
  <Drawer title={$t('targetedOffers.newOffer')} eyebrow={$t('targetedOffers.title')} onclose={() => { newOfferOpen = false; }}>
    <div class="catalog-card-detail">
      <div class="op-field">
        <label for="new-offer-identifier">{$t('targetedOffers.identifierRequired')}</label>
        <input autocomplete="off" spellcheck="false" id="new-offer-identifier" bind:value={newOffer.identifier} placeholder={$t('targetedOffers.identifierPlaceholder')} />
      </div>
      <div class="op-field">
        <label for="new-offer-title">{$t('targetedOffers.offerTitle')}</label>
        <input autocomplete="off" spellcheck="false" id="new-offer-title" bind:value={newOffer.title} />
      </div>
      <div class="op-field">
        <label for="new-offer-type">{$t('targetedOffers.offerType')}</label>
        <input autocomplete="off" spellcheck="false" id="new-offer-type" type="number" min="0" bind:value={newOffer.offerType} />
      </div>
      <div class="op-field">
        <label for="new-offer-description">{$t('targetedOffers.descriptionLabel')}</label>
        <textarea id="new-offer-description" rows="3" bind:value={newOffer.description}></textarea>
      </div>
      <div class="op-field">
        <label for="new-offer-product-code">{$t('targetedOffers.productCode')}</label>
        <input autocomplete="off" spellcheck="false" id="new-offer-product-code" bind:value={newOffer.productCode} />
      </div>
      <OfferImageField
        id="new-offer-image"
        label={$t('targetedOffers.imageUrl')}
        {imageTemplate}
        previewAlt={newOffer.title || newOffer.identifier}
        images={offerImages}
        bind:value={newOffer.imageUrl}
      />
      <OfferImageField
        id="new-offer-icon"
        label={$t('targetedOffers.iconImageUrl')}
        {imageTemplate}
        previewAlt={newOffer.title || newOffer.identifier}
        images={offerImages}
        bind:value={newOffer.iconImageUrl}
      />
      <div class="op-field">
        <label for="new-offer-credits">{$t('targetedOffers.priceInCredits')}</label>
        <input autocomplete="off" spellcheck="false" id="new-offer-credits" type="number" min="0" bind:value={newOffer.priceInCredits} />
      </div>
      <div class="op-field">
        <label for="new-offer-points">{$t('targetedOffers.priceInActivityPoints')}</label>
        <input autocomplete="off" spellcheck="false" id="new-offer-points" type="number" min="0" bind:value={newOffer.priceInActivityPoints} />
      </div>
      <div class="op-field">
        <label for="new-offer-point-type">{$t('targetedOffers.activityPointType')}</label>
        <select id="new-offer-point-type" bind:value={newOffer.activityPointType}>
          <option value={0}>{$t('targetedOffers.activityPointTypeNone')}</option>
          {#each activityPointCurrencyTypes as currency (currency.id)}
            <option value={currency.activityPointType}>{currency.name}</option>
          {/each}
        </select>
      </div>
      <div class="op-field">
        <label for="new-offer-limit">{$t('targetedOffers.purchaseLimit')}</label>
        <input autocomplete="off" spellcheck="false" id="new-offer-limit" type="number" min="0" bind:value={newOffer.purchaseLimit} />
      </div>
      <div class="op-field">
        <label for="new-offer-expires">{$t('targetedOffers.expiresAt')}</label>
        <input autocomplete="off" spellcheck="false" id="new-offer-expires" type="datetime-local" bind:value={newOffer.expiresAt} />
        <small class="muted">{$t('targetedOffers.expiresAtHint')}</small>
      </div>
      <div class="op-field">
        <label for="new-offer-sort">{$t('targetedOffers.sortOrder')}</label>
        <input autocomplete="off" spellcheck="false" id="new-offer-sort" type="number" bind:value={newOffer.sortOrder} />
      </div>
      <div class="op-field">
        <label><input autocomplete="off" spellcheck="false" type="checkbox" bind:checked={newOffer.active} /> {$t('targetedOffers.activeLabel')}</label>
      </div>
      <div class="op-actions">
        <button type="button" onclick={stageCreateOffer} disabled={$ops.busyKeys.createOffer}>{$t('targetedOffers.create')}</button>
      </div>
      {#if $ops.errors.createOffer}<p class="empty-state danger" role="alert">{$ops.errors.createOffer}</p>{/if}
      {#if $ops.results.createOffer}
        <OpResult result={$ops.results.createOffer} />
      {/if}
    </div>
  </Drawer>
{/if}

{#if editOfferId !== null}
  <Drawer title={$t('targetedOffers.editOffer')} eyebrow={$t('targetedOffers.title')} onclose={() => { editOfferId = null; editOfferForm = null; }}>
    {#if editOfferForm}
      <div class="catalog-card-detail">
        <div class="op-field">
          <label for={`edit-offer-identifier-${editOfferForm.id}`}>{$t('targetedOffers.identifierRequired')}</label>
          <input autocomplete="off" spellcheck="false" id={`edit-offer-identifier-${editOfferForm.id}`} bind:value={editOfferForm.identifier} />
        </div>
        <div class="op-field">
          <label for={`edit-offer-title-${editOfferForm.id}`}>{$t('targetedOffers.offerTitle')}</label>
          <input autocomplete="off" spellcheck="false" id={`edit-offer-title-${editOfferForm.id}`} bind:value={editOfferForm.title} />
        </div>
        <div class="op-field">
          <label for={`edit-offer-type-${editOfferForm.id}`}>{$t('targetedOffers.offerType')}</label>
          <input autocomplete="off" spellcheck="false" id={`edit-offer-type-${editOfferForm.id}`} type="number" min="0" bind:value={editOfferForm.offerType} />
        </div>
        <div class="op-field">
          <label for={`edit-offer-description-${editOfferForm.id}`}>{$t('targetedOffers.descriptionLabel')}</label>
          <textarea id={`edit-offer-description-${editOfferForm.id}`} rows="3" bind:value={editOfferForm.description}></textarea>
        </div>
        <div class="op-field">
          <label for={`edit-offer-product-code-${editOfferForm.id}`}>{$t('targetedOffers.productCode')}</label>
          <input autocomplete="off" spellcheck="false" id={`edit-offer-product-code-${editOfferForm.id}`} bind:value={editOfferForm.productCode} />
        </div>
        <OfferImageField
          id={`edit-offer-image-${editOfferForm.id}`}
          label={$t('targetedOffers.imageUrl')}
          {imageTemplate}
          previewAlt={editOfferForm.title || editOfferForm.identifier}
          images={offerImages}
          bind:value={editOfferForm.imageUrl}
        />
        <OfferImageField
          id={`edit-offer-icon-${editOfferForm.id}`}
          label={$t('targetedOffers.iconImageUrl')}
          {imageTemplate}
          previewAlt={editOfferForm.title || editOfferForm.identifier}
          images={offerImages}
          bind:value={editOfferForm.iconImageUrl}
        />
        <div class="op-field">
          <label for={`edit-offer-credits-${editOfferForm.id}`}>{$t('targetedOffers.priceInCredits')}</label>
          <input autocomplete="off" spellcheck="false" id={`edit-offer-credits-${editOfferForm.id}`} type="number" min="0" bind:value={editOfferForm.priceInCredits} />
        </div>
        <div class="op-field">
          <label for={`edit-offer-points-${editOfferForm.id}`}>{$t('targetedOffers.priceInActivityPoints')}</label>
          <input autocomplete="off" spellcheck="false" id={`edit-offer-points-${editOfferForm.id}`} type="number" min="0" bind:value={editOfferForm.priceInActivityPoints} />
        </div>
        <div class="op-field">
          <label for={`edit-offer-point-type-${editOfferForm.id}`}>{$t('targetedOffers.activityPointType')}</label>
          <select id={`edit-offer-point-type-${editOfferForm.id}`} bind:value={editOfferForm.activityPointType}>
            <option value={0}>{$t('targetedOffers.activityPointTypeNone')}</option>
            {#each activityPointCurrencyTypes as currency (currency.id)}
              <option value={currency.activityPointType}>{currency.name}</option>
            {/each}
          </select>
        </div>
        <div class="op-field">
          <label for={`edit-offer-limit-${editOfferForm.id}`}>{$t('targetedOffers.purchaseLimit')}</label>
          <input autocomplete="off" spellcheck="false" id={`edit-offer-limit-${editOfferForm.id}`} type="number" min="0" bind:value={editOfferForm.purchaseLimit} />
        </div>
        <div class="op-field">
          <label for={`edit-offer-expires-${editOfferForm.id}`}>{$t('targetedOffers.expiresAt')}</label>
          <input autocomplete="off" spellcheck="false" id={`edit-offer-expires-${editOfferForm.id}`} type="datetime-local" bind:value={editOfferForm.expiresAt} />
          <small class="muted">{$t('targetedOffers.expiresAtHint')}</small>
        </div>
        <div class="op-field">
          <label for={`edit-offer-sort-${editOfferForm.id}`}>{$t('targetedOffers.sortOrder')}</label>
          <input autocomplete="off" spellcheck="false" id={`edit-offer-sort-${editOfferForm.id}`} type="number" bind:value={editOfferForm.sortOrder} />
        </div>
        <div class="op-field">
          <label><input autocomplete="off" spellcheck="false" type="checkbox" bind:checked={editOfferForm.active} /> {$t('targetedOffers.activeLabel')}</label>
        </div>
        <div class="op-actions">
          <button type="button" onclick={stageUpdateOffer} disabled={$ops.busyKeys.updateOffer}>{$t('targetedOffers.save')}</button>
          <button class="ghost-button" type="button" onclick={() => { editOfferId = null; editOfferForm = null; }}>{$t('targetedOffers.cancel')}</button>
        </div>
        {#if $ops.errors.updateOffer}<p class="empty-state danger" role="alert">{$ops.errors.updateOffer}</p>{/if}
        {#if $ops.results.updateOffer}
          <OpResult result={$ops.results.updateOffer} />
        {/if}
      </div>
    {:else if $ops.errors.updateOffer}
      <div class="catalog-card-detail"><p class="empty-state danger" role="alert">{$ops.errors.updateOffer}</p></div>
    {/if}
  </Drawer>
{/if}

{#if newProductOpen}
  <Drawer title={$t('targetedOffers.newProduct')} eyebrow={$t('targetedOffers.title')} onclose={() => { newProductOpen = false; }}>
    <div class="catalog-card-detail">
      <div class="op-field">
        <label for="new-product-code">{$t('targetedOffers.productCodeRequired')}</label>
        <input autocomplete="off" spellcheck="false" id="new-product-code" bind:value={newProduct.productCode} />
      </div>
      <div class="op-field">
        <label for="new-product-def">{$t('targetedOffers.furnitureDefIdOptional')}</label>
        <input autocomplete="off" spellcheck="false" id="new-product-def" type="number" min="0" bind:value={newProduct.furnitureDefinitionId} />
      </div>
      <div class="op-field">
        <label for="new-product-quantity">{$t('targetedOffers.quantity')}</label>
        <input autocomplete="off" spellcheck="false" id="new-product-quantity" type="number" min="1" bind:value={newProduct.quantity} />
      </div>
      <div class="op-actions">
        <button type="button" onclick={stageCreateProduct} disabled={$ops.busyKeys.createProduct}>{$t('targetedOffers.create')}</button>
      </div>
      {#if $ops.errors.createProduct}<p class="empty-state danger" role="alert">{$ops.errors.createProduct}</p>{/if}
      {#if $ops.results.createProduct}
        <OpResult result={$ops.results.createProduct} />
      {/if}
    </div>
  </Drawer>
{/if}

{#if editProductId !== null}
  <Drawer title={$t('targetedOffers.editProduct')} eyebrow={$t('targetedOffers.title')} onclose={() => { editProductId = null; editProductForm = null; }}>
    <div class="catalog-card-detail">
      <div class="op-field">
        <label for={`edit-product-code-${editProductForm.id}`}>{$t('targetedOffers.productCodeRequired')}</label>
        <input autocomplete="off" spellcheck="false" id={`edit-product-code-${editProductForm.id}`} bind:value={editProductForm.productCode} />
      </div>
      <div class="op-field">
        <label for={`edit-product-def-${editProductForm.id}`}>{$t('targetedOffers.furnitureDefIdOptional')}</label>
        <input autocomplete="off" spellcheck="false" id={`edit-product-def-${editProductForm.id}`} type="number" min="0" bind:value={editProductForm.furnitureDefinitionId} />
      </div>
      <div class="op-field">
        <label for={`edit-product-qty-${editProductForm.id}`}>{$t('targetedOffers.quantity')}</label>
        <input autocomplete="off" spellcheck="false" id={`edit-product-qty-${editProductForm.id}`} type="number" min="1" bind:value={editProductForm.quantity} />
      </div>
      <div class="op-actions">
        <button type="button" onclick={stageUpdateProduct} disabled={$ops.busyKeys.updateProduct}>{$t('targetedOffers.save')}</button>
        <button class="ghost-button" type="button" onclick={() => { editProductId = null; editProductForm = null; }}>{$t('targetedOffers.cancel')}</button>
      </div>
      {#if $ops.errors.updateProduct}<p class="empty-state danger" role="alert">{$ops.errors.updateProduct}</p>{/if}
      {#if $ops.results.updateProduct}
        <OpResult result={$ops.results.updateProduct} />
      {/if}
    </div>
  </Drawer>
{/if}

<ConfirmReasonModal
  open={Boolean($deleteOps.pending)}
  title={$deleteOps.pending?.title ?? ''}
  changes={$deleteOps.pending?.changes ?? []}
  noteOnly={$deleteOps.pending?.noteOnly ?? false}
  summary={$deleteOps.pending?.summary ?? ''}
  confirmLabel={$deleteOps.pending?.title ?? $t('common.confirm')}
  busy={$deleteOps.busy}
  error={$deleteOps.error}
  danger={$deleteOps.pending?.danger ?? false}
  onconfirm={deleteOps.confirm}
  oncancel={() => deleteOps.cancel()}
/>

<style>
  .active-toggle {
    display: inline-flex;
    align-items: center;
    gap: 6px;
    font-size: 0.85rem;
    color: var(--muted);
    white-space: nowrap;
  }

  .ghost-button.active {
    border-color: var(--accent);
    color: var(--ink);
    background: rgba(var(--accent-rgb), 0.12);
  }

  .ghost-button,
  /* Offer card laid out as a column: a header line (thumbnail + title + actions) with the status
     chips on their own line beneath, instead of everything crammed into one wrapping row. */
  .offer-head .catalog-row-main {
    flex: 1 1 160px;
    min-width: 120px;
  }

  .offer-meta {
    display: flex;
    flex-wrap: wrap;
    align-items: center;
    gap: 6px;
    padding: 0 12px 10px 68px;
  }

  .offer-meta > .op-chip,
  .offer-meta > .status-badge,
  .offer-meta > .cost-chip {
    height: 24px;
    box-sizing: border-box;
  }

  .delete-bar {
    display: flex;
    justify-content: flex-end;
  }

  .panel-head h2,
  .panel-head h3 {
    margin: 0;
    font-size: 0.95rem;
  }

  .products-panel > .panel-head {
    margin-bottom: 10px;
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
    font: inherit;
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

  .catalog-row-meta {
    display: flex;
    align-items: center;
    gap: 6px;
    flex: 0 1 auto;
    flex-wrap: wrap;
  }

  .catalog-row-meta > .op-chip,
  .catalog-row-meta > .status-badge,
  .catalog-row-meta > .cost-chip {
    height: 24px;
    box-sizing: border-box;
  }

</style>
