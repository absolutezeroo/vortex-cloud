<script>
  // Targeted-offer promo image / icon field. When an image template (containing the literal `{file}`)
  // is configured, the operator supplies just a filename behind a read-only base prefix and gets a
  // live preview; the bound `value` always stays the FULL URL that is stored on the wire. A pasted
  // full URL (http/https) is kept verbatim as a passthrough, and when no template is configured the
  // field degrades to a plain full-URL text input.
  import AssetImage from './AssetImage.svelte';
  import { t } from '../lib/i18n.js';

  export let id = '';
  export let label = '';
  export let value = '';
  export let imageTemplate = null;
  export let previewAlt = '';

  const isHttp = (candidate) => /^https?:\/\//i.test((candidate || '').trim());

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

  {#if v}
    <div class="preview-row">
      <AssetImage src={v} alt={previewAlt} size={48} />
      <small class="muted">{$t('targetedOffers.previewLabel')}</small>
    </div>
  {/if}
</div>

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
</style>
