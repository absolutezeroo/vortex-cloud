<script lang="ts">
  // The filter row every list gets: the page declares its fields, this draws them, keeps them in the
  // URL, and shows what is currently on as chips you can take off one at a time.
  //
  //   <FilterBar fields={FIELDS} bind:values />
  //
  // See lib/filters.ts for the field shape and why this exists. Two behaviours are the point and are
  // not per page: a set filter is in the address bar (so the list can be linked to), and a set
  // filter is visible (so "why is this table empty" is answerable without opening the controls).
  import Chip from './Chip.svelte';
  import PickerModal from './PickerModal.svelte';
  import { writeFilterValues, hasActiveFilters } from '../lib/filters';
  import type { FilterField, FilterValues } from '../lib/filters';
  import type { PickerRow } from '../lib/pickers/directories';
  import { t } from '../lib/i18n';

  type Props = {
    fields: FilterField[];
    values: FilterValues;
    /** Runs after any change, for pages that reload rather than filter what they already hold. */
    onchange?: () => void;
    /** False keeps the values out of the address bar — for a filter row inside a dialog. */
    urlSync?: boolean;
  };

  let { fields, values = $bindable(), onchange, urlSync = true }: Props = $props();

  /**
   * Names for the ids picked this session.
   *
   * An entity filter stores an id, because that is what the endpoint takes. Arriving from a link
   * there is no name to show and the chip reads `#4312`, which is honest; picking one from the
   * modal fills this in and it reads the name from then on. Resolving ids to names on load would be
   * a request per chip to make a label prettier.
   */
  let pickedLabels = $state<Record<string, string>>({});

  /** The field whose picker is open, or null. */
  let picking = $state<FilterField | null>(null);

  function commit() {
    if (urlSync) {
      writeFilterValues(values);
    }

    onchange?.();
  }

  function set(field: FilterField, value: string) {
    values = { ...values, [field.id]: value };
    commit();
  }

  function clearOne(field: FilterField) {
    delete pickedLabels[field.id];
    pickedLabels = { ...pickedLabels };
    set(field, '');
  }

  function clearAll() {
    values = Object.fromEntries(Object.keys(values).map((id) => [id, '']));
    pickedLabels = {};
    commit();
  }

  function pick(row: PickerRow) {
    const field = picking;

    if (!field) {
      return;
    }

    pickedLabels = { ...pickedLabels, [field.id]: String(row.name ?? row.id) };
    picking = null;
    set(field, String(row.id));
  }

  const anyLabel = (field: FilterField) => field.anyLabel || $t('filters.any');

  /** What a chip says: the field's label, then the value as the operator would recognise it. */
  function chipValue(field: FilterField): string {
    const value = values[field.id];

    if (field.kind === 'bool') {
      return $t('filters.on');
    }

    if (field.kind === 'entity') {
      return pickedLabels[field.id] || `#${value}`;
    }

    if (field.kind === 'select') {
      return field.options?.find((option) => option.value === value)?.label ?? value;
    }

    return value;
  }

  let active = $derived(fields.filter((field) => values[field.id]));
</script>

<form class="toolbar-grid" onsubmit={(event) => { event.preventDefault(); commit(); }}>
  {#each fields as field (field.id)}
    <label class:wide={field.wide} class:filter-field={field.kind === 'bool'}>
      {#if field.kind === 'bool'}
        <input
          type="checkbox"
          autocomplete="off"
          checked={values[field.id] === 'true'}
          onchange={(event) => set(field, event.currentTarget.checked ? 'true' : '')}
        />
        {field.label}
      {:else}
        {field.label}
        {#if field.kind === 'select'}
          <select
            value={values[field.id]}
            onchange={(event) => set(field, event.currentTarget.value)}
          >
            <option value="">{anyLabel(field)}</option>
            {#each field.options ?? [] as option (option.value)}
              <option value={option.value}>{option.label}</option>
            {/each}
          </select>
        {:else if field.kind === 'entity'}
          <button type="button" class="picker-button" onclick={() => (picking = field)}>
            {values[field.id] ? chipValue(field) : anyLabel(field)}
          </button>
        {:else}
          <!-- change, not input: these values are read straight into a request key, and a request
               per keystroke is what made the older filter rows feel heavy. Enter submits the form,
               which commits too. -->
          <input
            autocomplete="off"
            spellcheck="false"
            type={field.kind === 'number' ? 'number' : field.kind === 'date' ? 'date' : 'search'}
            value={values[field.id]}
            placeholder={field.placeholder ?? ''}
            onchange={(event) => set(field, event.currentTarget.value)}
          />
        {/if}
      {/if}
    </label>
  {/each}
</form>

{#if hasActiveFilters(values)}
  <!-- The answer to "why is this list empty". Without it an operator who set a filter two minutes
       ago and scrolled reads an empty table as a broken page. -->
  <div class="filter-chips">
    {#each active as field (field.id)}
      <Chip label={field.label} onremove={() => clearOne(field)}>
        <span class="chip-label">{field.label}</span>
        {chipValue(field)}
      </Chip>
    {/each}
    <!-- ghost-button, not `linklike`: that class only removes the key's shadow and leaves the rest
         to the page's own scoped styles, which a shared component does not have. Unstyled, it came
         out as a full blue key sitting next to the chips it is meant to be quieter than. -->
    <button type="button" class="ghost-button" onclick={clearAll}>{$t('filters.clearAll')}</button>
  </div>
{/if}

{#if picking}
  <PickerModal
    kind={picking.picker ?? 'user'}
    title={picking.label}
    onSelect={pick}
    onClose={() => (picking = null)}
  />
{/if}

<style>
  .wide {
    grid-column: span 2;
  }

  .filter-chips {
    display: flex;
    align-items: center;
    flex-wrap: wrap;
    gap: 6px;
    margin-top: 10px;
  }

  /* The field's name in the chip, quieter than its value: what is filtered is the value, the label
     is only there to say which control it came from. */
  .chip-label {
    color: var(--muted);
    text-transform: uppercase;
    font-size: 0.62rem;
    letter-spacing: 0.04em;
  }
</style>
