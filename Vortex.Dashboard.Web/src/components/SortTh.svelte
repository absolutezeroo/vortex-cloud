<script>
  // A sortable table header. A button inside the <th> rather than a click handler on the <th>
  // itself, so it is reachable by keyboard and announced as what it is; aria-sort tells a screen
  // reader which column the table is currently ordered by.
  //
  //   <SortTh label={$t('x.colCount')} key="count" bind:sort />
  import { ChevronDown, ChevronUp, ChevronsUpDown } from '@lucide/svelte';
  import { toggleSort } from '../lib/tableView.js';

  /**
   * @typedef {Object} Props
   * @property {string} label
   * @property {string} key - the row field this column orders by
   * @property {{ key: string, dir: string }} sort - shared with the other headers of the table
   * @property {string} [initialDir] - 'desc' for counts, 'asc' for names
   */

  /** @type {Props} */
  let { label, key, sort = $bindable({ key: '', dir: 'desc' }), initialDir = 'desc' } = $props();

  let activeDir = $derived(sort.key === key ? sort.dir : '');
</script>

<th aria-sort={activeDir === 'asc' ? 'ascending' : activeDir === 'desc' ? 'descending' : 'none'}>
  <button type="button" class:active={Boolean(activeDir)} onclick={() => (sort = toggleSort(sort, key, initialDir))}>
    <span>{label}</span>
    {#if activeDir === 'asc'}
      <ChevronUp size={13} strokeWidth={2.4} aria-hidden="true" />
    {:else if activeDir === 'desc'}
      <ChevronDown size={13} strokeWidth={2.4} aria-hidden="true" />
    {:else}
      <ChevronsUpDown size={13} strokeWidth={2} aria-hidden="true" />
    {/if}
  </button>
</th>

<style>
  th {
    padding: 0;
  }

  button {
    display: flex;
    align-items: center;
    gap: 5px;
    width: 100%;
    border: 0;
    border-radius: 0;
    background: transparent;
    color: inherit;
    font: inherit;
    text-transform: inherit;
    letter-spacing: inherit;
    padding: 9px 8px;
    text-align: left;
  }

  /* The unsorted chevrons are a hint, not a state: they only show up when the header is worth
     clicking, otherwise every column shouts for attention at once. */
  button :global(svg) {
    opacity: 0;
    flex: 0 0 auto;
  }

  button:hover :global(svg),
  button:focus-visible :global(svg),
  button.active :global(svg) {
    opacity: 1;
  }

  button.active {
    color: var(--ink);
  }
</style>
