<script>
  // Targeted-offer promo image / icon field. When an image template (containing the literal `{file}`)
  // is configured, the operator supplies just a filename behind a read-only base prefix and gets a
  // live preview; the bound `value` always stays the FULL URL that is stored on the wire. A pasted
  // full URL (http/https) is kept verbatim as a passthrough, and when no template is configured the
  // field degrades to a plain full-URL text input.
  import { Image } from '@lucide/svelte';
  import AssetImage from './AssetImage.svelte';
  import { t } from '../lib/i18n.js';

  export let id = '';
  export let label = '';
  export let value = '';
  export let imageTemplate = null;
  export let previewAlt = '';
  /** Available images for the gallery picker: [{ file, url, thumbUrl }]. Empty hides the picker. */
  export let images = [];

  let browseOpen = false;

  const isHttp = (candidate) => /^https?:\/\//i.test((candidate || '').trim());

  function pick(image) {
    value = image.url;
    browseOpen = false;
  }

  $: v = value || '';
  $: prefix = imageTemplate ? imageTemplate.split('{file}')[0] : '';
  $: suffix = imageTemplate ? imageTemplate.split('{file}')[1] ?? '' : '';

  // A stored value "lives under" the template when it starts with the base prefix (and matches any
  // trailing suffix). Such values are edited as just their filename; anything else is a foreign full
  // URL that we must not mangle.
  $: underBase =
    Boolean(imageTemplate) &&
    v.trim() !== '' &&
    v.startsWith(prefix) &&
    (suffix === '' || v.endsWith(suffix));

  // Filename mode applies when a template exists AND the value is empty or already under the base.
  $: filenameMode = Boolean(imageTemplate) && (v.trim() === '' || underBase);

  $: filename = underBase
    ? v.slice(prefix.length, suffix ? v.length - suffix.length : v.length)
    : '';

  function onFilenameInput(event) {
    const raw = event.target.value.trim();
    if (raw === '') {
      value = '';
    } else if (isHttp(raw)) {
      value = raw; // Pasted a full URL into the filename field -> passthrough verbatim.
    } else {
      value = imageTemplate.replace('{file}', raw);
    }
  }

  function onFullUrlInput(event) {
    value = event.target.value;
  }
</script>

<div class="op-field">
  <label for={id}>{label}</label>

  {#if filenameMode}
    <div class="filename-row">
      {#if prefix}<span class="filename-prefix" title={imageTemplate}>{prefix}</span>{/if}
      <input
        {id}
        value={filename}
        on:input={onFilenameInput}
        placeholder={$t('targetedOffers.filenamePlaceholder')}
      />
    </div>
    <small class="muted">{$t('targetedOffers.filenameHint')}</small>
  {:else}
    <input {id} value={v} on:input={onFullUrlInput} placeholder="https://..." />
  {/if}

  <div class="field-actions">
    {#if images.length}
      <button type="button" class="ghost-button" on:click={() => (browseOpen = true)}>
        <Image size={14} strokeWidth={2} aria-hidden="true" />
        {$t('targetedOffers.browseImages')}
      </button>
    {/if}
    {#if v}
      <AssetImage src={v} alt={previewAlt} size={40} />
      <small class="muted">{$t('targetedOffers.previewLabel')}</small>
    {/if}
  </div>
</div>

{#if browseOpen}
  <div class="modal-layer">
    <button
      class="modal-backdrop"
      type="button"
      aria-label="Close"
      on:click={() => (browseOpen = false)}
    ></button>
    <section
      class="modal-panel gallery-panel"
      role="dialog"
      aria-modal="true"
      style="width: min(720px, 100%)"
    >
      <header class="modal-header">
        <div>
          <p class="eyebrow">{$t('targetedOffers.imagesCount', { count: images.length })}</p>
          <h2>{$t('targetedOffers.pickImage')}</h2>
        </div>
      </header>
      <div class="gallery-grid">
        {#each images as image (image.file)}
          <button
            type="button"
            class="gallery-item"
            class:selected={v === image.url}
            on:click={() => pick(image)}
            title={image.file}
          >
            <AssetImage src={image.thumbUrl || image.url} alt={image.file} size={80} />
            <span class="gallery-name">{image.file}</span>
          </button>
        {/each}
      </div>
      <div class="op-actions">
        <button type="button" class="ghost-button" on:click={() => (browseOpen = false)}>
          {$t('common.cancel')}
        </button>
      </div>
    </section>
  </div>
{/if}

<style>
  .filename-row {
    display: flex;
    align-items: stretch;
    gap: 0;
    border: 1px solid var(--line-strong);
    border-radius: 9px;
    overflow: hidden;
    background: var(--input-bg);
  }

  .filename-row input {
    flex: 1 1 auto;
    min-width: 0;
    border: none;
    border-radius: 0;
    background: transparent;
  }

  .filename-prefix {
    flex: 0 0 auto;
    max-width: 55%;
    display: inline-flex;
    align-items: center;
    padding: 0 8px;
    font-size: 0.78rem;
    color: var(--muted);
    background: var(--surface-strong);
    border-right: 1px solid var(--line-strong);
    white-space: nowrap;
    overflow: hidden;
    text-overflow: ellipsis;
    direction: rtl;
    text-align: left;
  }

  .preview-row {
    display: flex;
    align-items: center;
    gap: 8px;
    margin-top: 8px;
  }

  .field-actions {
    display: flex;
    align-items: center;
    gap: 8px;
    margin-top: 8px;
    flex-wrap: wrap;
  }

  .gallery-panel {
    max-height: 80vh;
    display: flex;
    flex-direction: column;
  }

  .gallery-grid {
    display: grid;
    grid-template-columns: repeat(auto-fill, minmax(120px, 1fr));
    gap: 10px;
    overflow-y: auto;
    padding: 4px;
    margin: 8px 0;
  }

  .gallery-item {
    display: flex;
    flex-direction: column;
    align-items: center;
    gap: 6px;
    padding: 8px;
    border: 1px solid var(--line);
    border-radius: 10px;
    background: var(--surface-strong);
    cursor: pointer;
    color: inherit;
    font: inherit;
  }

  .gallery-item:hover {
    border-color: var(--accent);
  }

  .gallery-item.selected {
    border-color: var(--accent);
    background: rgba(var(--accent-rgb), 0.12);
  }

  .gallery-name {
    font-size: 0.72rem;
    color: var(--muted);
    max-width: 100%;
    overflow: hidden;
    text-overflow: ellipsis;
    white-space: nowrap;
  }
</style>
