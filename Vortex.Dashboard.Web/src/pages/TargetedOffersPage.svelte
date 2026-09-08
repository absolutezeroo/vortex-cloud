<script lang="ts">
  import ConfirmStagedModal from '../components/ConfirmStagedModal.svelte';
  import LoadingOverlay from '../components/LoadingOverlay.svelte';
  import OpResult from '../components/OpResult.svelte';
  import { onMount } from 'svelte';
  import {
    Clock,
    Coins,
    Eye,
    EyeOff,
    Image,
    Package,
    Sparkles,
    Target,
    Users,
  } from '@lucide/svelte';
  import { apiGet, describeApiError } from '../lib/api';
  import { createWriteOps } from '../lib/writeOps';
  import { isPermissionDeniedError, hasDashboardCapability } from '../lib/permissions';
  import { CAPABILITIES } from '../lib/dashboardPermissions';
  import AccessDeniedNotice from '../components/AccessDeniedNotice.svelte';
  import Drawer from '../components/Drawer.svelte';
  import AssetImage from '../components/AssetImage.svelte';
  import OfferImageField from '../components/OfferImageField.svelte';
  import PickerModal from '../components/PickerModal.svelte';
  import ConfirmReasonModal from '../components/ConfirmReasonModal.svelte';
  import { identity } from '../lib/session';
  import { t, translate, type Translator } from '../lib/i18n';
  import type {
    TargetedOfferCurrency,
    TargetedOfferDetail,
    TargetedOfferFormMeta,
    TargetedOfferImage,
    TargetedOfferImageList,
    TargetedOfferList,
    TargetedOfferProduct,
    TargetedOfferRow,
  } from '../lib/apiTypes';
  import type { PickerRow } from '../lib/pickers/directories';

  /**
   * The offer form. `expiresAt` is a local datetime-local string, not the instant the API takes;
   * the two helpers below bridge the pair, and an empty field means no expiry.
   */
  type OfferForm = {
    identifier: string;
    offerType: number | string;
    title: string;
    description: string;
    imageUrl: string;
    iconImageUrl: string;
    productCode: string;
    priceInCredits: number | string;
    priceInActivityPoints: number | string;
    activityPointType: number | string;
    purchaseLimit: number | string;
    expiresAt: string;
    active: boolean;
    sortOrder: number | string;
  };

  /** One item of an offer's bundle, as the editor holds it. */
  type ProductForm = {
    productCode: string;
    furnitureDefinitionId: number | string;
    quantity: number | string;
  };

  function emptyOfferForm(): OfferForm {
    return {
      identifier: '', offerType: 0, title: '', description: '', imageUrl: '', iconImageUrl: '',
      productCode: '', priceInCredits: 0, priceInActivityPoints: 0, activityPointType: 0,
      purchaseLimit: 0, expiresAt: '', active: true, sortOrder: 0,
    };
  }

  function emptyProductForm(): ProductForm {
    return { productCode: '', furnitureDefinitionId: '', quantity: 1 };
  }

  // The API returns/consumes ExpiresAt as an ISO instant (or null); <input type="datetime-local">
  // wants a local `yyyy-MM-ddThh:mm` string with no zone. These two helpers bridge the pair -- an
  // empty field means "no expiry" (null on the wire), matching the nullable DateTime? server-side.
  function toDateTimeLocal(iso: string | null | undefined) {
    if (!iso) return '';
    const date = new Date(iso);
    if (Number.isNaN(date.getTime())) return '';
    const pad = (n: number) => String(n).padStart(2, '0');
    return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}T${pad(date.getHours())}:${pad(date.getMinutes())}`;
  }

  function fromDateTimeLocal(value: string) {
    if (!value) return null;
    const date = new Date(value);
    return Number.isNaN(date.getTime()) ? null : date.toISOString();
  }

  let activeOnly = $state(false);

  // Admin-form metadata loaded once: the configured promo-image base template (drives the
  // filename-only image inputs) and the currency types for the activity-point picker.
  let imageTemplate = $state<string | null>(null);
  let currencyTypes = $state<TargetedOfferCurrency[]>([]);
  // Available promo images (from the asset folder) for the gallery picker, so operators pick a real
  // image instead of typing a filename blind. Empty when the picker is unconfigured.
  let offerImages = $state<TargetedOfferImage[]>([]);
  // Activity points can be paid in any non-Credits currency; Credits are handled by the separate
  // credits price field, so excluding them here avoids offering the same currency twice.
  let activityPointCurrencyTypes = $derived(currencyTypes.filter((c) => c.type !== 'Credits'));

  let offers = $state<TargetedOfferRow[]>([]);
  let loading = $state(false);
  let error = $state('');
  let forbidden = $state(false);

  let newOfferOpen = $state(false);
  let newOffer = $state(emptyOfferForm());
  let editOfferId = $state<number | null>(null);
  let editOfferForm = $state<OfferForm | null>(null);

  let selectedOfferId = $state<number | null>(null);
  let offerDetail = $state<TargetedOfferDetail | null>(null);
  let offerDetailLoading = $state(false);
  let offerDetailError = $state('');

  let newProductOpen = $state(false);
  let newProduct = $state(emptyProductForm());
  let editProductId = $state<number | null>(null);
  let editProductForm = $state<ProductForm | null>(null);

  // A product is a furniture, and the id of a furniture is not something anyone knows by heart --
  // it was a bare number field, so the operator had to go find the id somewhere else first. The
  // picker fills both halves at once: the code the offer is delivered under, and the definition it
  // points at, which is the pair that has to agree.
  /** What to do with the picked furniture: whichever form opened the picker fills itself. */
  let furniPicker = $state<((row: PickerRow) => void) | null>(null);

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
      const data = await apiGet<TargetedOfferList>(
        `/api/v1/targeted-offers?activeOnly=${activeOnly ? 'true' : 'false'}`,
      );
      offers = data.items || [];
    } catch (err) {
      if (isPermissionDeniedError(err)) {
        forbidden = true;
        offers = [];
        return;
      }

      error = (err as Error).message;
      offers = [];
    } finally {
      loading = false;
    }
  }

  async function loadOfferDetail(offerId: number) {
    selectedOfferId = offerId;
    offerDetail = null;
    offerDetailError = '';
    offerDetailLoading = true;
    newProductOpen = false;
    editProductId = null;

    try {
      offerDetail = await apiGet<TargetedOfferDetail>(`/api/v1/targeted-offers/${offerId}`);
    } catch (err) {
      offerDetailError = isPermissionDeniedError(err)
        ? translate('common.insufficientRights')
        : describeApiError(err);
    } finally {
      offerDetailLoading = false;
    }
  }

  // Toggles the inline bundle-products panel under the clicked offer's own card -- clicking the same
  // offer's action button again collapses it instead of re-fetching.
  async function toggleOfferDetail(offerId: number) {
    if (selectedOfferId === offerId) {
      selectedOfferId = null;
      offerDetail = null;
      return;
    }

    await loadOfferDetail(offerId);
  }

  function offerProductsLabel(
    offer: TargetedOfferRow,
    expandedOfferId: number | null,
    translator: Translator,
  ) {
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

  const stage = (
    id: string,
    title: string,
    endpoint: string,
    valid: boolean,
    body: Record<string, unknown>,
    summary: string,
    onSuccess: () => void | Promise<void>,
  ) =>
    ops.ask(endpoint, body, title, summary, {
      key: id,
      valid,
      invalidMessage: translate('targetedOffers.fillFields'),
      onSuccess,
    });

  // Both create and update take the same field set; update additionally carries the offerId. Field
  // order is irrelevant on the wire (System.Text.Json binds by name), so a single builder is safe.
  function buildOfferBody(form: OfferForm, offerId: number | null) {
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
      '/api/v1/operations/targeted-offers',
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
  async function startEditOffer(offer: TargetedOfferRow) {
    editOfferId = offer.id;
    editOfferForm = null;
    ops.clear('updateOffer');

    try {
      const detail = await apiGet<TargetedOfferDetail>(`/api/v1/targeted-offers/${offer.id}`);
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
        isPermissionDeniedError(err) ? translate('common.insufficientRights') : describeApiError(err),
      );
    }
  }

  function stageUpdateOffer() {
    if (!canManage || !editOfferForm || editOfferId === null) return;

    stage(
      'updateOffer',
      translate('targetedOffers.edit'),
      '/api/v1/operations/targeted-offers/update',
      Boolean(editOfferForm.identifier.trim()),
      buildOfferBody(editOfferForm, editOfferId),
      translate('targetedOffers.updateOfferSummary', { id: editOfferId }),
      async () => {
        const id = editOfferId;
        editOfferId = null;
        editOfferForm = null;
        await loadOffers();
        if (id !== null && selectedOfferId === id) {
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
      '/api/v1/operations/targeted-offers/products',
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
        await loadOfferDetail(selectedOfferId!);
        await loadOffers();
      },
    );
  }

  function startEditProduct(product: TargetedOfferProduct) {
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
      '/api/v1/operations/targeted-offers/products/update',
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
        await loadOfferDetail(selectedOfferId!);
      },
    );
  }

  // On a server refusal (offer_has_purchases and friends) createWriteOps keeps the modal open with
  // the message, so the operator can react without re-opening it.
  function openDeleteOffer(offer: TargetedOfferRow) {
    if (!canManage) return;

    deleteOps.ask(
      '/api/v1/operations/targeted-offers/delete',
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

  function openDeleteProduct(product: TargetedOfferProduct) {
    if (!canManage) return;

    deleteOps.ask(
      '/api/v1/operations/targeted-offers/products/delete',
      { productId: product.id },
      translate('targetedOffers.deleteProduct'),
      translate('targetedOffers.deleteProductSummary', { id: product.id }),
      {
        key: 'deleteProduct',
        danger: true,
        onSuccess: async () => {
          await loadOfferDetail(selectedOfferId!);
          await loadOffers();
        },
      },
    );
  }

  // Form metadata is optional polish: if it fails to load the form still works with plain full-URL
  // image inputs and the "none" activity-point option, so failures are swallowed rather than surfaced.
  async function loadFormMeta() {
    try {
      const meta = await apiGet<TargetedOfferFormMeta>('/api/v1/targeted-offers/form-meta');
      imageTemplate = meta.imageTemplate ?? null;
      currencyTypes = meta.currencyTypes || [];
    } catch {
      imageTemplate = null;
      currencyTypes = [];
    }

    try {
      const gallery = await apiGet<TargetedOfferImageList>('/api/v1/targeted-offers/images');
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
      <button type="button" class="warning" onclick={refreshAll} disabled={loading}>{$t('common.refresh')}</button>
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
        <button type="button" class="success" onclick={() => (newOfferOpen = true)}>
          {$t('targetedOffers.newOffer')}
        </button>
      {/if}
    </div>


    {#if error}
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
                  {offerProductsLabel(offer, selectedOfferId, $t)}
                </button>
                {#if canManage}
                  <button type="button" class="ghost-button" onclick={() => startEditOffer(offer)}>
                    {$t('targetedOffers.edit')}
                  </button>
                  <button type="button" class="ghost-button danger" onclick={() => openDeleteOffer(offer)}>
                    {$t('targetedOffers.deleteOffer')}
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



            {#if selectedOfferId === offer.id}
              <div class="catalog-card-detail products-panel">
                <div class="panel-head">
                  <h3><Package size={15} strokeWidth={2} aria-hidden="true" /> {$t('targetedOffers.bundleProducts')}</h3>
                  {#if canManage}
                    <button type="button" class="success" onclick={() => (newProductOpen = true)}>
                      {$t('targetedOffers.addProduct')}
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
                              <!-- Same wrapper as the offer row above: loose in the row the pair
                                   sat wherever the chips left them instead of at the row's end. -->
                              <div class="op-actions offer-actions">
                                <button type="button" class="ghost-button" onclick={() => startEditProduct(product)}>
                                  {$t('targetedOffers.edit')}
                                </button>
                                <button type="button" class="ghost-button danger" onclick={() => openDeleteProduct(product)}>
                                  {$t('targetedOffers.deleteProduct')}
                                </button>
                              </div>
                            {/if}
                          </div>


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
      {#if $ops.errors.createOffer}<p class="empty-state danger" role="alert">{$ops.errors.createOffer}</p>{/if}
      {#if $ops.results.createOffer}
        <OpResult result={$ops.results.createOffer} />
      {/if}
    </div>
  
    {#snippet actions()}
      <button type="button" onclick={stageCreateOffer} disabled={$ops.busyKeys.createOffer} class="success">{$t('targetedOffers.create')}</button>
    {/snippet}
  </Drawer>
{/if}

{#if editOfferId !== null}
  <Drawer title={$t('targetedOffers.editOffer')} eyebrow={$t('targetedOffers.title')} onclose={() => { editOfferId = null; editOfferForm = null; }}>
    {#if editOfferForm}
      <div class="catalog-card-detail">
        <div class="op-field">
          <label for={`edit-offer-identifier-${editOfferId}`}>{$t('targetedOffers.identifierRequired')}</label>
          <input autocomplete="off" spellcheck="false" id={`edit-offer-identifier-${editOfferId}`} bind:value={editOfferForm.identifier} />
        </div>
        <div class="op-field">
          <label for={`edit-offer-title-${editOfferId}`}>{$t('targetedOffers.offerTitle')}</label>
          <input autocomplete="off" spellcheck="false" id={`edit-offer-title-${editOfferId}`} bind:value={editOfferForm.title} />
        </div>
        <div class="op-field">
          <label for={`edit-offer-type-${editOfferId}`}>{$t('targetedOffers.offerType')}</label>
          <input autocomplete="off" spellcheck="false" id={`edit-offer-type-${editOfferId}`} type="number" min="0" bind:value={editOfferForm.offerType} />
        </div>
        <div class="op-field">
          <label for={`edit-offer-description-${editOfferId}`}>{$t('targetedOffers.descriptionLabel')}</label>
          <textarea id={`edit-offer-description-${editOfferId}`} rows="3" bind:value={editOfferForm.description}></textarea>
        </div>
        <div class="op-field">
          <label for={`edit-offer-product-code-${editOfferId}`}>{$t('targetedOffers.productCode')}</label>
          <input autocomplete="off" spellcheck="false" id={`edit-offer-product-code-${editOfferId}`} bind:value={editOfferForm.productCode} />
        </div>
        <OfferImageField
          id={`edit-offer-image-${editOfferId}`}
          label={$t('targetedOffers.imageUrl')}
          {imageTemplate}
          previewAlt={editOfferForm.title || editOfferForm.identifier}
          images={offerImages}
          bind:value={editOfferForm.imageUrl}
        />
        <OfferImageField
          id={`edit-offer-icon-${editOfferId}`}
          label={$t('targetedOffers.iconImageUrl')}
          {imageTemplate}
          previewAlt={editOfferForm.title || editOfferForm.identifier}
          images={offerImages}
          bind:value={editOfferForm.iconImageUrl}
        />
        <div class="op-field">
          <label for={`edit-offer-credits-${editOfferId}`}>{$t('targetedOffers.priceInCredits')}</label>
          <input autocomplete="off" spellcheck="false" id={`edit-offer-credits-${editOfferId}`} type="number" min="0" bind:value={editOfferForm.priceInCredits} />
        </div>
        <div class="op-field">
          <label for={`edit-offer-points-${editOfferId}`}>{$t('targetedOffers.priceInActivityPoints')}</label>
          <input autocomplete="off" spellcheck="false" id={`edit-offer-points-${editOfferId}`} type="number" min="0" bind:value={editOfferForm.priceInActivityPoints} />
        </div>
        <div class="op-field">
          <label for={`edit-offer-point-type-${editOfferId}`}>{$t('targetedOffers.activityPointType')}</label>
          <select id={`edit-offer-point-type-${editOfferId}`} bind:value={editOfferForm.activityPointType}>
            <option value={0}>{$t('targetedOffers.activityPointTypeNone')}</option>
            {#each activityPointCurrencyTypes as currency (currency.id)}
              <option value={currency.activityPointType}>{currency.name}</option>
            {/each}
          </select>
        </div>
        <div class="op-field">
          <label for={`edit-offer-limit-${editOfferId}`}>{$t('targetedOffers.purchaseLimit')}</label>
          <input autocomplete="off" spellcheck="false" id={`edit-offer-limit-${editOfferId}`} type="number" min="0" bind:value={editOfferForm.purchaseLimit} />
        </div>
        <div class="op-field">
          <label for={`edit-offer-expires-${editOfferId}`}>{$t('targetedOffers.expiresAt')}</label>
          <input autocomplete="off" spellcheck="false" id={`edit-offer-expires-${editOfferId}`} type="datetime-local" bind:value={editOfferForm.expiresAt} />
          <small class="muted">{$t('targetedOffers.expiresAtHint')}</small>
        </div>
        <div class="op-field">
          <label for={`edit-offer-sort-${editOfferId}`}>{$t('targetedOffers.sortOrder')}</label>
          <input autocomplete="off" spellcheck="false" id={`edit-offer-sort-${editOfferId}`} type="number" bind:value={editOfferForm.sortOrder} />
        </div>
        <div class="op-field">
          <label><input autocomplete="off" spellcheck="false" type="checkbox" bind:checked={editOfferForm.active} /> {$t('targetedOffers.activeLabel')}</label>
        </div>
        {#if $ops.errors.updateOffer}<p class="empty-state danger" role="alert">{$ops.errors.updateOffer}</p>{/if}
        {#if $ops.results.updateOffer}
          <OpResult result={$ops.results.updateOffer} />
        {/if}
      </div>
    {:else if $ops.errors.updateOffer}
      <div class="catalog-card-detail"><p class="empty-state danger" role="alert">{$ops.errors.updateOffer}</p></div>
    {/if}
  
    {#snippet actions()}
      <button type="button" onclick={stageUpdateOffer} disabled={$ops.busyKeys.updateOffer}>{$t('targetedOffers.save')}</button>
      <button class="ghost-button" type="button" onclick={() => { editOfferId = null; editOfferForm = null; }}>{$t('targetedOffers.cancel')}</button>
    {/snippet}
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
        <div class="op-pick">
          <input autocomplete="off" spellcheck="false" id="new-product-def" type="number" min="0" bind:value={newProduct.furnitureDefinitionId} />
          <button
            type="button"
            class="ghost-button"
            onclick={() =>
              (furniPicker = (item) => {
                newProduct.furnitureDefinitionId = Number(item.id);
                newProduct.productCode = item.name;
              })}>{$t('targetedOffers.pick')}</button>
        </div>
      </div>
      <div class="op-field">
        <label for="new-product-quantity">{$t('targetedOffers.quantity')}</label>
        <input autocomplete="off" spellcheck="false" id="new-product-quantity" type="number" min="1" bind:value={newProduct.quantity} />
      </div>
      {#if $ops.errors.createProduct}<p class="empty-state danger" role="alert">{$ops.errors.createProduct}</p>{/if}
      {#if $ops.results.createProduct}
        <OpResult result={$ops.results.createProduct} />
      {/if}
    </div>
  
    {#snippet actions()}
      <button type="button" onclick={stageCreateProduct} disabled={$ops.busyKeys.createProduct} class="success">{$t('targetedOffers.create')}</button>
    {/snippet}
  </Drawer>
{/if}

{#if editProductForm}
  <Drawer title={$t('targetedOffers.editProduct')} eyebrow={$t('targetedOffers.title')} onclose={() => { editProductId = null; editProductForm = null; }}>
    <div class="catalog-card-detail">
      <div class="op-field">
        <label for={`edit-product-code-${editProductId}`}>{$t('targetedOffers.productCodeRequired')}</label>
        <input autocomplete="off" spellcheck="false" id={`edit-product-code-${editProductId}`} bind:value={editProductForm.productCode} />
      </div>
      <div class="op-field">
        <label for={`edit-product-def-${editProductId}`}>{$t('targetedOffers.furnitureDefIdOptional')}</label>
        <div class="op-pick">
          <input autocomplete="off" spellcheck="false" id={`edit-product-def-${editProductId}`} type="number" min="0" bind:value={editProductForm.furnitureDefinitionId} />
          <button
            type="button"
            class="ghost-button"
            onclick={() =>
              (furniPicker = (item) => {
                if (!editProductForm) return;

                editProductForm.furnitureDefinitionId = Number(item.id);
                editProductForm.productCode = item.name;
              })}>{$t('targetedOffers.pick')}</button>
        </div>
      </div>
      <div class="op-field">
        <label for={`edit-product-qty-${editProductId}`}>{$t('targetedOffers.quantity')}</label>
        <input autocomplete="off" spellcheck="false" id={`edit-product-qty-${editProductId}`} type="number" min="1" bind:value={editProductForm.quantity} />
      </div>
      {#if $ops.errors.updateProduct}<p class="empty-state danger" role="alert">{$ops.errors.updateProduct}</p>{/if}
      {#if $ops.results.updateProduct}
        <OpResult result={$ops.results.updateProduct} />
      {/if}
    </div>
  
    {#snippet actions()}
      <button type="button" onclick={stageUpdateProduct} disabled={$ops.busyKeys.updateProduct}>{$t('targetedOffers.save')}</button>
      <button class="ghost-button" type="button" onclick={() => { editProductId = null; editProductForm = null; }}>{$t('targetedOffers.cancel')}</button>
    {/snippet}
  </Drawer>
{/if}

{#if furniPicker}
  <PickerModal
    kind="furniture"
    title={$t('targetedOffers.pickFurniture')}
    onSelect={(item: PickerRow) => {
      furniPicker?.(item);
      furniPicker = null;
    }}
    onClose={() => (furniPicker = null)}
    canSelect={canManage}
  />
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

<LoadingOverlay show={loading} />

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

  /* Offer card laid out as a column: a header line (thumbnail + title + actions) with the status
     chips on their own line beneath, instead of everything crammed into one wrapping row.
     `.ghost-button` used to be part of this selector, which handed `flex: 1 1 160px` to every
     Edit/Delete button on the page: they grew to fill the row and wrapped onto a second line. */
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
    border-radius: 8px;
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
