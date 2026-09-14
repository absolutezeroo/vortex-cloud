<script lang="ts">
  /**
   * The content column of a `generic` widget -- a caption, a body, a button -- edited as rows
   * instead of as `caption,landing.view.jan26cf.header;bodytext,...`.
   *
   * Two things this does that a text field cannot:
   *
   *   - It names the arguments. The client reads them as an untyped array, so nothing in the file
   *     says the second field of `catalogbutton` is a catalogue page; that only exists in the AS3
   *     source, and it is what the label and the hint here carry.
   *   - It edits the words, not the key. `caption` points at a key in external_flash_texts, and an
   *     operator writing a promo wants to type the headline once, in the place they are writing the
   *     promo.
   *
   * Four element types arrive from the server marked unverified: real types whose later arguments
   * nobody has read off the client. Those are edited as the raw comma list, because a made-up label
   * is worse than the string.
   */
  import type { HotelViewElement, HotelViewVocabulary } from '../../lib/apiTypes';
  import { blankElement, labeller } from '../../lib/hotelView';
  import { t } from '../../lib/i18n';
  import Button from '../Button.svelte';
  import { ChevronDown, ChevronUp, Plus, Trash2 } from '@lucide/svelte';

  type Props = {
    elements: HotelViewElement[];
    vocabulary: HotelViewVocabulary;
    disabled?: boolean;
    onchange: (elements: HotelViewElement[]) => void;
  };

  let { elements, vocabulary, disabled = false, onchange }: Props = $props();

  let adding = $state('');

  let types = $derived([...vocabulary.elementTypes].sort((a, b) => a.type.localeCompare(b.type)));
  let label = $derived(labeller($t));

  const shapeOf = (type: string) => vocabulary.elementTypes.find((e) => e.type === type);

  function replace(index: number, next: HotelViewElement) {
    onchange(elements.map((element, i) => (i === index ? next : element)));
  }

  function setArg(index: number, position: number, value: string) {
    const element = elements[index];
    const args = [...element.args];
    while (args.length <= position) args.push('');
    args[position] = value;
    replace(index, { ...element, args });
  }

  /** The raw form: every argument as one comma list, for the types whose shape is not known. */
  function setRaw(index: number, value: string) {
    replace(index, { ...elements[index], args: value === '' ? [] : value.split(',') });
  }

  function retype(index: number, type: string) {
    const shape = shapeOf(type);
    if (!shape) return;
    // The arguments are positional and the types disagree about what sits where, so they are not
    // carried across: a caption key surviving into `spacing` would become a height.
    replace(index, blankElement(shape));
  }

  function move(index: number, by: number) {
    const target = index + by;
    if (target < 0 || target >= elements.length) return;
    const next = [...elements];
    [next[index], next[target]] = [next[target], next[index]];
    onchange(next);
  }

  function add() {
    const shape = shapeOf(adding);
    if (!shape) return;
    onchange([...elements, blankElement(shape)]);
    adding = '';
  }
</script>

<div class="elements">
  {#each elements as element, index (index)}
    {@const shape = shapeOf(element.type)}
    <div class="element" class:unknown={!shape}>
      <div class="element-head">
        <select
          value={element.type}
          {disabled}
          aria-label={$t('hotelView.elementType')}
          onchange={(e) => retype(index, e.currentTarget.value)}
        >
          {#if !shape}
            <option value={element.type}>{element.type}</option>
          {/if}
          {#each types as type (type.type)}
            <option value={type.type}>{type.type}</option>
          {/each}
        </select>

        <div class="element-actions">
          <Button
            variant="ghost"
            size="sm"
            disabled={disabled || index === 0}
            ariaLabel={$t('hotelView.moveUp')}
            onclick={() => move(index, -1)}><ChevronUp size={14} /></Button
          >
          <Button
            variant="ghost"
            size="sm"
            disabled={disabled || index === elements.length - 1}
            ariaLabel={$t('hotelView.moveDown')}
            onclick={() => move(index, 1)}><ChevronDown size={14} /></Button
          >
          <Button
            variant="danger"
            size="sm"
            {disabled}
            ariaLabel={$t('common.delete')}
            onclick={() => onchange(elements.filter((_, i) => i !== index))}
            ><Trash2 size={14} /></Button
          >
        </div>
      </div>

      <p class="element-help">
        {label(`hotelView.element.${element.type}`, shape ? '' : $t('hotelView.elementUnknown'))}
      </p>

      {#if shape && shape.verified}
        <div class="fields">
          {#each shape.arguments as argument, position (argument.name)}
            <label class="field" class:wide={argument.kind === 'text'}>
              <span>
                {label(`hotelView.arg.${argument.name}`, argument.name)}
                {#if argument.optional}<em>{$t('hotelView.optional')}</em>{/if}
              </span>

              {#if argument.kind === 'bool'}
                <select
                  value={element.args[position] ?? ''}
                  {disabled}
                  onchange={(e) => setArg(index, position, e.currentTarget.value)}
                >
                  <option value="">{$t('hotelView.unset')}</option>
                  <option value="true">{$t('common.yes')}</option>
                  <option value="false">{$t('common.no')}</option>
                </select>
              {:else}
                <input
                  type={argument.kind === 'int' || argument.kind === 'number' ? 'number' : 'text'}
                  value={element.args[position] ?? ''}
                  {disabled}
                  placeholder={label(`hotelView.kind.${argument.kind}`)}
                  oninput={(e) => setArg(index, position, e.currentTarget.value)}
                />
              {/if}
            </label>

            {#if argument.kind === 'text' && position === 0}
              <label class="field wide">
                <span>{$t('hotelView.words')}</span>
                <input
                  type="text"
                  value={element.text ?? ''}
                  {disabled}
                  placeholder={$t('hotelView.wordsPlaceholder')}
                  oninput={(e) => replace(index, { ...element, text: e.currentTarget.value })}
                />
              </label>
            {/if}
          {/each}
        </div>
      {:else}
        <label class="field wide">
          <span>{$t('hotelView.rawArgs')}</span>
          <input
            type="text"
            value={element.args.join(',')}
            {disabled}
            oninput={(e) => setRaw(index, e.currentTarget.value)}
          />
        </label>
      {/if}
    </div>
  {/each}

  <div class="add">
    <select bind:value={adding} {disabled} aria-label={$t('hotelView.addElement')}>
      <option value="">{$t('hotelView.addElement')}</option>
      {#each types as type (type.type)}
        <option value={type.type}>{type.type}</option>
      {/each}
    </select>
    <Button variant="ghost" size="sm" disabled={disabled || adding === ''} onclick={add}>
      <Plus size={14} />{$t('hotelView.add')}
    </Button>
  </div>
</div>

<style>
  .elements {
    display: flex;
    flex-direction: column;
    gap: 0.75rem;
  }

  .element {
    border: 1px solid var(--line);
    border-radius: 8px;
    padding: 0.65rem 0.75rem;
    background: var(--surface-raised);
  }

  /* A type the server does not know is a column the client will abandon whole. Say so loudly. */
  .element.unknown {
    border-color: var(--danger);
  }

  .element-head {
    display: flex;
    align-items: center;
    justify-content: space-between;
    gap: 0.5rem;
  }

  .element-actions {
    display: flex;
    gap: 0.25rem;
  }

  .element-help {
    margin: 0.35rem 0 0.5rem;
    font-size: 0.78rem;
    color: var(--muted);
  }

  .fields {
    display: grid;
    grid-template-columns: repeat(auto-fit, minmax(150px, 1fr));
    gap: 0.5rem;
  }

  .field {
    display: flex;
    flex-direction: column;
    gap: 0.2rem;
    min-width: 0;
  }

  .field.wide {
    grid-column: 1 / -1;
  }

  .field span {
    font-size: 0.74rem;
    color: var(--muted);
    text-transform: uppercase;
    letter-spacing: 0.03em;
  }

  .field em {
    font-style: normal;
    opacity: 0.65;
  }

  .add {
    display: flex;
    gap: 0.5rem;
    align-items: center;
  }
</style>
