<script>
  // The search box above a table that is already loaded in full. Says how many rows survived, so a
  // filter that matches nothing reads as "nothing matches" rather than as a table that failed to
  // load.
  //
  //   <TableFilter bind:query shown={view.length} total={rows.length} />
  import { Search } from '@lucide/svelte';
  import { t } from '../lib/i18n.js';

  /**
   * @typedef {Object} Props
   * @property {string} [query]
   * @property {number} [shown] - rows after filtering
   * @property {number} [total] - rows before filtering
   * @property {string} [placeholder]
   */

  /** @type {Props} */
  let { query = $bindable(''), shown = 0, total = 0, placeholder = '' } = $props();
</script>

<div class="table-filter">
  <span class="tf-input">
    <Search size={14} strokeWidth={2} aria-hidden="true" />
    <input
      class="bare"
      type="search"
      name="table-filter"
      autocomplete="off"
      spellcheck="false"
      bind:value={query}
      placeholder={placeholder || $t('tableFilter.placeholder')}
      aria-label={placeholder || $t('tableFilter.placeholder')}
    />
  </span>
  <small class="muted">
    {query.trim() ? $t('tableFilter.countFiltered', { shown, total }) : $t('tableFilter.count', { total })}
  </small>
</div>

<style>
  .table-filter {
    display: flex;
    align-items: center;
    gap: 10px;
    margin-bottom: 8px;
  }

  .tf-input {
    display: flex;
    align-items: center;
    gap: 7px;
    flex: 1;
    min-width: 0;
    max-width: 320px;
    padding: 0 9px;
    border: 1px solid var(--line-strong);
    border-radius: 8px;
    background: var(--input-bg);
    color: var(--muted);
  }

  .tf-input input {
    flex: 1;
    min-width: 0;
    border: 0;
    background: transparent;
    color: var(--ink);
    padding: 7px 0;
  }

  .tf-input input:focus-visible {
    outline: none;
  }

  .table-filter small {
    margin-left: auto;
    white-space: nowrap;
  }
</style>
