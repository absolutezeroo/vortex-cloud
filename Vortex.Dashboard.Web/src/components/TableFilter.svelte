<script lang="ts">
  // The search box above a table that is already loaded in full. Says how many rows survived, so a
  // filter that matches nothing reads as "nothing matches" rather than as a table that failed to
  // load.
  //
  //   <TableFilter bind:query shown={view.length} total={rows.length} />
  import { Search } from '@lucide/svelte';
  import { t } from '../lib/i18n';

  type Props = {
    query?: string;
    /** rows after filtering */
    shown?: number;
    /** rows before filtering */
    total?: number;
    placeholder?: string;
  };

  let { query = $bindable(''), shown = 0, total = 0, placeholder = '' }: Props = $props();
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

  /* The same field the rest of the dashboard draws, with room for the magnifier inside it. The
     input itself is .bare so it opts out of the global rule; this wrapper has to carry that look
     instead, and it used to carry an approximation of it -- different tokens, no ring, no insets --
     which is why the one search box on a page looked like it came from somewhere else. */
  .tf-input {
    display: flex;
    align-items: center;
    gap: 7px;
    flex: 1;
    min-width: 0;
    max-width: 320px;
    padding: 0 10px;
    border: 1px solid var(--field-border);
    border-radius: 8px;
    background: var(--field-bg);
    box-shadow: 0 0 0 1px var(--field-ring), inset 0 1px 0 var(--field-border-top),
      inset 0 -1px 0 var(--field-border-bottom);
    color: var(--field-ink);
    transition: border-color 140ms ease, box-shadow 140ms ease;
  }

  .tf-input:focus-within {
    border-color: var(--accent);
    box-shadow: 0 0 0 2px #0a325d, inset 0 1px 0 var(--field-border-top);
  }

  .tf-input input {
    flex: 1;
    min-width: 0;
    border: 0;
    background: transparent;
    color: var(--field-ink);
    padding: 9px 0;
  }

  .tf-input input:focus-visible {
    outline: none;
  }

  .table-filter small {
    margin-left: auto;
    white-space: nowrap;
  }
</style>
