<script>
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
  import { apiGet, apiPost } from '../lib/api.js';
  import { isPermissionDeniedError, hasDashboardCapability } from '../lib/permissions.js';
  import { compactCorrelation } from '../lib/format.js';
  import { CAPABILITIES } from '../lib/dashboardPermissions.js';
  import { reasonOk } from '../lib/validation.js';
  import { rememberReason } from '../lib/reasonHistory.js';
  import AccessDeniedNotice from '../components/AccessDeniedNotice.svelte';
  import AssetImage from '../components/AssetImage.svelte';
  import OfferImageField from '../components/OfferImageField.svelte';
  import { identity } from '../lib/session.js';
  import { t, translate } from '../lib/i18n.js';

  function emptyOfferForm() {
    return {
      identifier: '', offerType: 0, title: '', description: '', imageUrl: '', iconImageUrl: '',
      productCode: '', priceInCredits: 0, priceInActivityPoints: 0, activityPointType: 0,
      purchaseLimit: 0, expiresAt: '', active: true, sortOrder: 0, reason: '',
    };
  }

  function emptyProductForm() {
    return { productCode: '', furnitureDefinitionId: '', quantity: 1, reason: '' };
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

  let activeOnly = false;

  // Admin-form metadata loaded once: the configured promo-image base template (drives the
  // filename-only image inputs) and the currency types for the activity-point picker.
  let imageTemplate = null;
  let currencyTypes = [];
  // Activity points can be paid in any non-Credits currency; Credits are handled by the separate
  // credits price field, so excluding them here avoids offering the same currency twice.
  $: activityPointCurrencyTypes = currencyTypes.filter((c) => c.type !== 'Credits');

  let offers = [];
  let loading = false;
  let error = '';
  let forbidden = false;

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

  let deleteOfferReason = {};
  let deleteProductReason = {};

  let results = {};
  let errors = {};
  let busy = {};
  let pending = null;

  $: canManage = hasDashboardCapability($identity, CAPABILITIES.opsTargetedOffersManage);

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

  function stage(id, title, endpoint, valid, body, summary, onSuccess) {
    errors = { ...errors, [id]: '' };

    if (!valid) {
      errors = { ...errors, [id]: translate('targetedOffers.fillFields') };
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
      reason: form.reason.trim(),
    };

    return offerId === null ? body : { offerId, ...body };
  }

  function stageCreateOffer() {
    if (!canManage) return;

    stage(
      'createOffer',
      translate('targetedOffers.newOffer'),
      '/api/operations/targeted-offers',
      Boolean(newOffer.identifier.trim()) && reasonOk(newOffer.reason),
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
    errors = { ...errors, updateOffer: '' };

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
        reason: '',
      };
    } catch (err) {
      editOfferId = null;
      errors = {
        ...errors,
        updateOffer: isPermissionDeniedError(err) ? translate('common.insufficientRights') : err.code || err.message,
      };
    }
  }

  function stageUpdateOffer() {
    if (!canManage || !editOfferForm || editOfferId === null) return;

    stage(
      'updateOffer',
      translate('targetedOffers.edit'),
      '/api/operations/targeted-offers/update',
      Boolean(editOfferForm.identifier.trim()) && reasonOk(editOfferForm.reason),
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

  function stageDeleteOffer(offer) {
    if (!canManage) return;

    stage(
      'deleteOffer',
      translate('targetedOffers.deleteOffer'),
      '/api/operations/targeted-offers/delete',
      reasonOk(deleteOfferReason[offer.id]),
      { offerId: offer.id, reason: (deleteOfferReason[offer.id] || '').trim() },
      translate('targetedOffers.deleteOfferSummary', { id: offer.id, name: offer.identifier }),
      async () => {
        deleteOfferReason = { ...deleteOfferReason, [offer.id]: '' };
        if (selectedOfferId === offer.id) {
          selectedOfferId = null;
          offerDetail = null;
        }
        await loadOffers();
      },
    );
  }

  function stageCreateProduct() {
    if (!canManage || selectedOfferId === null) return;

    stage(
      'createProduct',
      translate('targetedOffers.addProduct'),
      '/api/operations/targeted-offers/products',
      Boolean(newProduct.productCode.trim()) && reasonOk(newProduct.reason),
      {
        offerId: selectedOfferId,
        productCode: newProduct.productCode.trim(),
        furnitureDefinitionId: newProduct.furnitureDefinitionId ? Number(newProduct.furnitureDefinitionId) : null,
        quantity: Number(newProduct.quantity) || 1,
        reason: newProduct.reason.trim(),
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
      reason: '',
    };
  }

  function stageUpdateProduct() {
    if (!canManage || !editProductForm || editProductId === null) return;

    stage(
      'updateProduct',
      translate('targetedOffers.edit'),
      '/api/operations/targeted-offers/products/update',
      Boolean(editProductForm.productCode.trim()) && reasonOk(editProductForm.reason),
      {
        productId: editProductId,
        productCode: editProductForm.productCode.trim(),
        furnitureDefinitionId: editProductForm.furnitureDefinitionId ? Number(editProductForm.furnitureDefinitionId) : null,
        quantity: Number(editProductForm.quantity) || 1,
        reason: editProductForm.reason.trim(),
      },
      translate('targetedOffers.updateProductSummary', { id: editProductId }),
      async () => {
        editProductId = null;
        editProductForm = null;
        await loadOfferDetail(selectedOfferId);
      },
    );
  }

  function stageDeleteProduct(product) {
    if (!canManage) return;

    stage(
      'deleteProduct',
      translate('targetedOffers.deleteProduct'),
      '/api/operations/targeted-offers/products/delete',
      reasonOk(deleteProductReason[product.id]),
      { productId: product.id, reason: (deleteProductReason[product.id] || '').trim() },
      translate('targetedOffers.deleteProductSummary', { id: product.id }),
      async () => {
        deleteProductReason = { ...deleteProductReason, [product.id]: '' };
        await loadOfferDetail(selectedOfferId);
        await loadOffers();
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
        <input type="checkbox" bind:checked={activeOnly} on:change={loadOffers} />
        {$t('targetedOffers.activeOnlyLabel')}
      </label>
      <button type="button" class="ghost-button" on:click={refreshAll} disabled={loading}>{$t('common.refresh')}</button>
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
        <button type="button" class="ghost-button" on:click={() => (newOfferOpen = !newOfferOpen)}>
          <Plus size={14} strokeWidth={2} aria-hidden="true" /> {newOfferOpen ? $t('targetedOffers.cancel') : $t('targetedOffers.newOffer')}
        </button>
      {/if}
    </div>

    {#if newOfferOpen}
      <div class="catalog-card-detail">
        <div class="op-field">
          <label for="new-offer-identifier">{$t('targetedOffers.identifierRequired')}</label>
          <input id="new-offer-identifier" bind:value={newOffer.identifier} placeholder={$t('targetedOffers.identifierPlaceholder')} />
        </div>
        <div class="op-field">
          <label for="new-offer-title">{$t('targetedOffers.offerTitle')}</label>
          <input id="new-offer-title" bind:value={newOffer.title} />
        </div>
        <div class="op-field">
          <label for="new-offer-type">{$t('targetedOffers.offerType')}</label>
          <input id="new-offer-type" type="number" min="0" bind:value={newOffer.offerType} />
        </div>
        <div class="op-field">
          <label for="new-offer-description">{$t('targetedOffers.descriptionLabel')}</label>
          <textarea id="new-offer-description" rows="3" bind:value={newOffer.description}></textarea>
        </div>
        <div class="op-field">
          <label for="new-offer-product-code">{$t('targetedOffers.productCode')}</label>
          <input id="new-offer-product-code" bind:value={newOffer.productCode} />
        </div>
        <OfferImageField
          id="new-offer-image"
          label={$t('targetedOffers.imageUrl')}
          {imageTemplate}
          previewAlt={newOffer.title || newOffer.identifier}
          bind:value={newOffer.imageUrl}
        />
        <OfferImageField
          id="new-offer-icon"
          label={$t('targetedOffers.iconImageUrl')}
          {imageTemplate}
          previewAlt={newOffer.title || newOffer.identifier}
          bind:value={newOffer.iconImageUrl}
        />
        <div class="op-field">
          <label for="new-offer-credits">{$t('targetedOffers.priceInCredits')}</label>
          <input id="new-offer-credits" type="number" min="0" bind:value={newOffer.priceInCredits} />
        </div>
        <div class="op-field">
          <label for="new-offer-points">{$t('targetedOffers.priceInActivityPoints')}</label>
          <input id="new-offer-points" type="number" min="0" bind:value={newOffer.priceInActivityPoints} />
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
          <input id="new-offer-limit" type="number" min="0" bind:value={newOffer.purchaseLimit} />
        </div>
        <div class="op-field">
          <label for="new-offer-expires">{$t('targetedOffers.expiresAt')}</label>
          <input id="new-offer-expires" type="datetime-local" bind:value={newOffer.expiresAt} />
          <small class="muted">{$t('targetedOffers.expiresAtHint')}</small>
        </div>
        <div class="op-field">
          <label for="new-offer-sort">{$t('targetedOffers.sortOrder')}</label>
          <input id="new-offer-sort" type="number" bind:value={newOffer.sortOrder} />
        </div>
        <div class="op-field">
          <label><input type="checkbox" bind:checked={newOffer.active} /> {$t('targetedOffers.activeLabel')}</label>
        </div>
        <div class="op-field">
          <label for="new-offer-reason">{$t('common.reasonRequired')}</label>
          <input id="new-offer-reason" bind:value={newOffer.reason} placeholder={$t('targetedOffers.reasonOfferPlaceholder')} list="reason-history" />
        </div>
        <div class="op-actions">
          <button type="button" on:click={stageCreateOffer} disabled={busy.createOffer}>{$t('targetedOffers.create')}</button>
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

    {#if loading}
      <p class="muted">{$t('common.loading')}</p>
    {:else if error}
      <p class="empty-state danger">{error}</p>
    {:else if offers.length === 0}
      <p class="empty-state">{$t('targetedOffers.noOffers')}</p>
    {:else}
      <div class="catalog-list">
        {#each offers as offer (offer.id)}
          <div class="catalog-card">
            <div class="catalog-row static">
              <AssetImage src={offer.imageUrl} alt={offer.title || offer.identifier} size={38} fallbackIcon={Sparkles} />
              <span class="catalog-row-main">
                <strong>{offer.title || offer.identifier}</strong>
                <small class="muted">{offer.identifier} - #{offer.id}{offer.productCode ? ` - ${offer.productCode}` : ''}</small>
              </span>
              <span class="catalog-row-meta">
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
              </span>
              <div class="op-actions">
                <button type="button" class="ghost-button" class:active={selectedOfferId === offer.id} on:click={() => toggleOfferDetail(offer.id)}>
                  <Package size={14} strokeWidth={2} aria-hidden="true" /> {offerProductsLabel(offer, selectedOfferId, $t)}
                </button>
                {#if canManage}
                  <button type="button" class="ghost-button" on:click={() => startEditOffer(offer)}>
                    <Pencil size={14} strokeWidth={2} aria-hidden="true" /> {$t('targetedOffers.edit')}
                  </button>
                {/if}
              </div>
            </div>

            {#if editOfferId === offer.id}
              {#if editOfferForm}
                <div class="catalog-card-detail">
                  <div class="op-field">
                    <label for={`edit-offer-identifier-${offer.id}`}>{$t('targetedOffers.identifierRequired')}</label>
                    <input id={`edit-offer-identifier-${offer.id}`} bind:value={editOfferForm.identifier} />
                  </div>
                  <div class="op-field">
                    <label for={`edit-offer-title-${offer.id}`}>{$t('targetedOffers.offerTitle')}</label>
                    <input id={`edit-offer-title-${offer.id}`} bind:value={editOfferForm.title} />
                  </div>
                  <div class="op-field">
                    <label for={`edit-offer-type-${offer.id}`}>{$t('targetedOffers.offerType')}</label>
                    <input id={`edit-offer-type-${offer.id}`} type="number" min="0" bind:value={editOfferForm.offerType} />
                  </div>
                  <div class="op-field">
                    <label for={`edit-offer-description-${offer.id}`}>{$t('targetedOffers.descriptionLabel')}</label>
                    <textarea id={`edit-offer-description-${offer.id}`} rows="3" bind:value={editOfferForm.description}></textarea>
                  </div>
                  <div class="op-field">
                    <label for={`edit-offer-product-code-${offer.id}`}>{$t('targetedOffers.productCode')}</label>
                    <input id={`edit-offer-product-code-${offer.id}`} bind:value={editOfferForm.productCode} />
                  </div>
                  <OfferImageField
                    id={`edit-offer-image-${offer.id}`}
                    label={$t('targetedOffers.imageUrl')}
                    {imageTemplate}
                    previewAlt={editOfferForm.title || editOfferForm.identifier}
                    bind:value={editOfferForm.imageUrl}
                  />
                  <OfferImageField
                    id={`edit-offer-icon-${offer.id}`}
                    label={$t('targetedOffers.iconImageUrl')}
                    {imageTemplate}
                    previewAlt={editOfferForm.title || editOfferForm.identifier}
                    bind:value={editOfferForm.iconImageUrl}
                  />
                  <div class="op-field">
                    <label for={`edit-offer-credits-${offer.id}`}>{$t('targetedOffers.priceInCredits')}</label>
                    <input id={`edit-offer-credits-${offer.id}`} type="number" min="0" bind:value={editOfferForm.priceInCredits} />
                  </div>
                  <div class="op-field">
                    <label for={`edit-offer-points-${offer.id}`}>{$t('targetedOffers.priceInActivityPoints')}</label>
                    <input id={`edit-offer-points-${offer.id}`} type="number" min="0" bind:value={editOfferForm.priceInActivityPoints} />
                  </div>
                  <div class="op-field">
                    <label for={`edit-offer-point-type-${offer.id}`}>{$t('targetedOffers.activityPointType')}</label>
                    <select id={`edit-offer-point-type-${offer.id}`} bind:value={editOfferForm.activityPointType}>
                      <option value={0}>{$t('targetedOffers.activityPointTypeNone')}</option>
                      {#each activityPointCurrencyTypes as currency (currency.id)}
                        <option value={currency.activityPointType}>{currency.name}</option>
                      {/each}
                    </select>
                  </div>
                  <div class="op-field">
                    <label for={`edit-offer-limit-${offer.id}`}>{$t('targetedOffers.purchaseLimit')}</label>
                    <input id={`edit-offer-limit-${offer.id}`} type="number" min="0" bind:value={editOfferForm.purchaseLimit} />
                  </div>
                  <div class="op-field">
                    <label for={`edit-offer-expires-${offer.id}`}>{$t('targetedOffers.expiresAt')}</label>
                    <input id={`edit-offer-expires-${offer.id}`} type="datetime-local" bind:value={editOfferForm.expiresAt} />
                    <small class="muted">{$t('targetedOffers.expiresAtHint')}</small>
                  </div>
                  <div class="op-field">
                    <label for={`edit-offer-sort-${offer.id}`}>{$t('targetedOffers.sortOrder')}</label>
                    <input id={`edit-offer-sort-${offer.id}`} type="number" bind:value={editOfferForm.sortOrder} />
                  </div>
                  <div class="op-field">
                    <label><input type="checkbox" bind:checked={editOfferForm.active} /> {$t('targetedOffers.activeLabel')}</label>
                  </div>
                  <div class="op-field">
                    <label for={`edit-offer-reason-${offer.id}`}>{$t('common.reasonRequired')}</label>
                    <input id={`edit-offer-reason-${offer.id}`} bind:value={editOfferForm.reason} placeholder={$t('common.reasonPlaceholderChange')} list="reason-history" />
                  </div>
                  <div class="op-actions">
                    <button type="button" on:click={stageUpdateOffer} disabled={busy.updateOffer}>{$t('targetedOffers.save')}</button>
                    <button class="ghost-button" type="button" on:click={() => { editOfferId = null; editOfferForm = null; }}>{$t('targetedOffers.cancel')}</button>
                  </div>
                  {#if errors.updateOffer}<p class="empty-state danger">{errors.updateOffer}</p>{/if}
                  {#if results.updateOffer}
                    <p class="op-result" class:danger={!results.updateOffer.ok}>
                      {results.updateOffer.ok ? '✅' : '❌'} {results.updateOffer.message} - cid
                      <code>{compactCorrelation(results.updateOffer.correlationId)}</code>
                    </p>
                  {/if}
                </div>
              {:else if errors.updateOffer}
                <div class="catalog-card-detail"><p class="empty-state danger">{errors.updateOffer}</p></div>
              {/if}
            {/if}

            {#if canManage}
              <div class="catalog-card-detail op-pick">
                <input bind:value={deleteOfferReason[offer.id]} placeholder={$t('targetedOffers.deleteOfferReasonPlaceholder')} list="reason-history" style="flex: 1;" />
                <button type="button" class="ghost-button danger" on:click={() => stageDeleteOffer(offer)}>
                  <Trash2 size={14} strokeWidth={2} aria-hidden="true" /> {$t('targetedOffers.deleteOffer')}
                </button>
              </div>
            {/if}

            {#if selectedOfferId === offer.id}
              <div class="catalog-card-detail products-panel">
                <div class="panel-head">
                  <h3><Package size={15} strokeWidth={2} aria-hidden="true" /> {$t('targetedOffers.bundleProducts')}</h3>
                  {#if canManage}
                    <button type="button" class="ghost-button" on:click={() => (newProductOpen = !newProductOpen)}>
                      <Plus size={14} strokeWidth={2} aria-hidden="true" /> {newProductOpen ? $t('targetedOffers.cancel') : $t('targetedOffers.addProduct')}
                    </button>
                  {/if}
                </div>

                {#if offerDetailLoading}
                  <p class="muted">{$t('targetedOffers.loadingProducts')}</p>
                {:else if offerDetailError}
                  <p class="empty-state danger">{offerDetailError}</p>
                {:else if offerDetail}
                  {#if newProductOpen}
                    <div class="catalog-card-detail">
                      <div class="op-field">
                        <label for="new-product-code">{$t('targetedOffers.productCodeRequired')}</label>
                        <input id="new-product-code" bind:value={newProduct.productCode} />
                      </div>
                      <div class="op-field">
                        <label for="new-product-def">{$t('targetedOffers.furnitureDefIdOptional')}</label>
                        <input id="new-product-def" type="number" min="0" bind:value={newProduct.furnitureDefinitionId} />
                      </div>
                      <div class="op-field">
                        <label for="new-product-quantity">{$t('targetedOffers.quantity')}</label>
                        <input id="new-product-quantity" type="number" min="1" bind:value={newProduct.quantity} />
                      </div>
                      <div class="op-field">
                        <label for="new-product-reason">{$t('common.reasonRequired')}</label>
                        <input id="new-product-reason" bind:value={newProduct.reason} placeholder={$t('targetedOffers.reasonProductPlaceholder')} list="reason-history" />
                      </div>
                      <div class="op-actions">
                        <button type="button" on:click={stageCreateProduct} disabled={busy.createProduct}>{$t('targetedOffers.create')}</button>
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
                    <p class="empty-state">{$t('targetedOffers.noProducts')}</p>
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
                              <strong>{product.furnitureName || product.productCode}</strong>
                              <small class="muted">{product.productCode}{product.furnitureDefinitionEntityId ? ` - #${product.furnitureDefinitionEntityId}` : ''}</small>
                            </span>
                            <span class="catalog-row-meta">
                              <span class="op-chip" title={$t('targetedOffers.quantity')}>x{product.quantity}</span>
                            </span>
                            {#if canManage}
                              <button type="button" class="ghost-button" on:click={() => startEditProduct(product)}>
                                <Pencil size={14} strokeWidth={2} aria-hidden="true" /> {$t('targetedOffers.edit')}
                              </button>
                            {/if}
                          </div>

                          {#if editProductId === product.id && editProductForm}
                            <div class="catalog-card-detail">
                              <div class="op-field">
                                <label for={`edit-product-code-${product.id}`}>{$t('targetedOffers.productCodeRequired')}</label>
                                <input id={`edit-product-code-${product.id}`} bind:value={editProductForm.productCode} />
                              </div>
                              <div class="op-field">
                                <label for={`edit-product-def-${product.id}`}>{$t('targetedOffers.furnitureDefIdOptional')}</label>
                                <input id={`edit-product-def-${product.id}`} type="number" min="0" bind:value={editProductForm.furnitureDefinitionId} />
                              </div>
                              <div class="op-field">
                                <label for={`edit-product-qty-${product.id}`}>{$t('targetedOffers.quantity')}</label>
                                <input id={`edit-product-qty-${product.id}`} type="number" min="1" bind:value={editProductForm.quantity} />
                              </div>
                              <div class="op-field">
                                <label for={`edit-product-reason-${product.id}`}>{$t('common.reasonRequired')}</label>
                                <input id={`edit-product-reason-${product.id}`} bind:value={editProductForm.reason} placeholder={$t('common.reasonPlaceholderChange')} list="reason-history" />
                              </div>
                              <div class="op-actions">
                                <button type="button" on:click={stageUpdateProduct} disabled={busy.updateProduct}>{$t('targetedOffers.save')}</button>
                                <button class="ghost-button" type="button" on:click={() => { editProductId = null; editProductForm = null; }}>{$t('targetedOffers.cancel')}</button>
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
                              <input bind:value={deleteProductReason[product.id]} placeholder={$t('targetedOffers.deleteProductReasonPlaceholder')} list="reason-history" style="flex: 1;" />
                              <button type="button" class="ghost-button danger" on:click={() => stageDeleteProduct(product)}>
                                <Trash2 size={14} strokeWidth={2} aria-hidden="true" /> {$t('targetedOffers.deleteProduct')}
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

{#if pending}
  <div class="modal-layer">
    <button class="modal-backdrop" type="button" aria-label="Cancel" on:click={cancelPending}></button>
    <section class="modal-panel" role="dialog" aria-modal="true" style="width: min(460px, 100%)">
      <header class="modal-header">
        <div>
          <p class="eyebrow">{$t('targetedOffers.confirmEyebrow')}</p>
          <h2>{pending.title}</h2>
        </div>
      </header>
      <p>{pending.summary}</p>
      <p class="muted">{$t('vouchers.reasonLabel', { reason: pending.reason })}</p>
      <div class="op-actions">
        <button type="button" on:click={confirmPending}>{$t('common.confirm')}</button>
        <button class="ghost-button" type="button" on:click={cancelPending}>{$t('targetedOffers.cancel')}</button>
      </div>
    </section>
  </div>
{/if}

<style>
  .head-actions {
    display: flex;
    align-items: center;
    gap: 12px;
    flex-wrap: wrap;
  }

  .active-toggle {
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

  .ghost-button.active {
    border-color: var(--accent);
    color: var(--ink);
    background: rgba(var(--accent-rgb), 0.12);
  }

  .ghost-button,
  .op-actions button {
    display: inline-flex;
    align-items: center;
    gap: 6px;
  }

  .panel-head {
    flex-wrap: wrap;
    row-gap: 8px;
  }

  .panel-head h2,
  .panel-head h3 {
    display: flex;
    align-items: center;
    gap: 8px;
  }

  .panel-head h3 {
    margin: 0;
    font-size: 0.95rem;
  }

  .products-panel > .panel-head {
    margin-bottom: 10px;
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
</style>
