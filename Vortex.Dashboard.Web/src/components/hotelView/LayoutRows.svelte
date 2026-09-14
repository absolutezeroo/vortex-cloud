<script lang="ts">
  /**
   * Where a widget's illustration and text column sit -- the nine keys the client's layout switch
   * accepts, and nothing else.
   *
   * A tenth key is not an error anywhere: it falls through the switch in silence and the picture
   * stays where it was. So the key is a closed list here rather than a text field, and `bitmap.uri`
   * shows what it points at, because an operator choosing promo art is choosing a picture.
   */
  import type { HotelViewLayoutValue, HotelViewVocabulary } from '../../lib/apiTypes';
  import { labeller, resolveAssetUrl } from '../../lib/hotelView';
  import { t } from '../../lib/i18n';
  import AssetImage from '../AssetImage.svelte';
  import Button from '../Button.svelte';
  import { Plus, Trash2 } from '@lucide/svelte';

  type Props = {
    layout: HotelViewLayoutValue[];
    vocabulary: HotelViewVocabulary;
    placeholders: Record<string, string>;
    disabled?: boolean;
    onchange: (layout: HotelViewLayoutValue[]) => void;
  };

  let { layout, vocabulary, placeholders, disabled = false, onchange }: Props = $props();

  let label = $derived(labeller($t));
  let unused = $derived(
    vocabulary.layoutKeys.filter((key) => !layout.some((value) => value.key === key)),
  );

  function replace(index: number, next: HotelViewLayoutValue) {
    onchange(layout.map((value, i) => (i === index ? next : value)));
  }
</script>

<div class="layout">
  {#each layout as value, index (index)}
    <div class="hv-row">
      <select
        value={value.key}
        {disabled}
        aria-label={$t('hotelView.layoutKey')}
        onchange={(e) => replace(index, { ...value, key: e.currentTarget.value })}
      >
        {#if !vocabulary.layoutKeys.includes(value.key)}
          <option value={value.key}>{value.key}</option>
        {/if}
        {#each vocabulary.layoutKeys as key (key)}
          <option value={key} disabled={key !== value.key && !unused.includes(key)}>{key}</option>
        {/each}
      </select>

      <input
        type={value.key === 'bitmap.uri' ? 'text' : 'number'}
        value={value.value}
        {disabled}
        oninput={(e) => replace(index, { ...value, value: e.currentTarget.value })}
      />

      {#if value.key === 'bitmap.uri'}
        <AssetImage src={resolveAssetUrl(value.value, placeholders)} alt={value.value} size={36} />
      {:else}
        <span class="spacer"></span>
      {/if}

      <Button
        variant="danger"
        size="sm"
        {disabled}
        ariaLabel={$t('common.delete')}
        onclick={() => onchange(layout.filter((_, i) => i !== index))}><Trash2 size={14} /></Button
      >

      <p class="help">{label(`hotelView.layoutHelp.${value.key}`)}</p>
    </div>
  {/each}

  <Button
    variant="ghost"
    size="sm"
    disabled={disabled || unused.length === 0}
    onclick={() => onchange([...layout, { key: unused[0], value: '' }])}
  >
    <Plus size={14} />{$t('hotelView.addLayout')}
  </Button>
</div>

<style>
  .layout {
    display: flex;
    flex-direction: column;
    gap: 0.4rem;
    align-items: flex-start;
  }

  .hv-row {
    display: grid;
    grid-template-columns: minmax(130px, 1fr) minmax(110px, 1fr) auto auto;
    gap: 0.4rem;
    align-items: center;
    width: 100%;
  }

  .help {
    grid-column: 1 / -1;
    margin: 0 0 0.3rem;
    font-size: 0.75rem;
    color: var(--muted);
  }

  .spacer {
    display: block;
  }

  @media (max-width: 620px) {
    .hv-row {
      grid-template-columns: 1fr auto;
    }
  }
</style>
