<script>
  // The one pager. Emits a `change` event with the new 1-based page. Renders a compact window with
  // first/last + ellipses and an optional "start–end of total" count. Use this everywhere a list is
  // paged so Audit / Moderation / Furniture (which each hand-rolled the same block) and the pages
  // that currently dump everything all look and behave identically. Labels are props so callers can
  // pass translated strings ($t).
  import { createEventDispatcher } from 'svelte';

  export let page = 1;
  export let pageCount = 1;
  export let total = null; // grand total, enables the "start–end of total" count
  export let pageSize = null; // enables the shown range in that count
  export let label = 'éléments';
  export let prevLabel = 'Précédent';
  export let nextLabel = 'Suivant';
  export let pageWord = 'Page';
  export let disabled = false; // e.g. bind to `loading` to freeze the pager during a fetch

  const dispatch = createEventDispatcher();

  function go(target) {
    const next = Math.min(Math.max(1, target), Math.max(1, pageCount));
    if (next !== page) dispatch('change', next);
  }

  // Compact page window: always show 1 and last, the current page and its neighbours, with
  // ellipses filling the gaps. Small page counts (<= 7) render every page.
  function buildPages(cur, count) {
    if (count <= 7) return Array.from({ length: count }, (_, i) => i + 1);
    const wanted = [1, count, cur, cur - 1, cur + 1].filter((n) => n >= 1 && n <= count);
    const uniqueSorted = [...new Set(wanted)].sort((a, b) => a - b);
    const out = [];
    let prev = 0;
    for (const n of uniqueSorted) {
      if (n - prev > 1) out.push('…');
      out.push(n);
      prev = n;
    }
    return out;
  }

  $: pages = buildPages(page, pageCount);
  $: rangeStart = pageSize ? (page - 1) * pageSize + 1 : null;
  $: rangeEnd =
    pageSize != null && total != null
      ? Math.min(page * pageSize, total)
      : pageSize != null
        ? page * pageSize
        : null;
</script>

<div class="pager">
  {#if total != null}
    <span class="pager-count">
      {#if rangeStart}<b>{rangeStart}–{rangeEnd}</b> /
      {/if}<b>{total}</b> {label}
    </span>
  {:else}
    <span class="pager-count">{pageWord} <b>{page}</b> / <b>{pageCount}</b></span>
  {/if}

  <div class="pager-pages">
    <button type="button" on:click={() => go(page - 1)} disabled={disabled || page <= 1} aria-label={prevLabel}>‹</button>
    {#each pages as p}
      {#if p === '…'}
        <span class="pager-ellipsis" aria-hidden="true">…</span>
      {:else}
        <button type="button" class:active={p === page} disabled={disabled} aria-current={p === page ? 'page' : undefined} on:click={() => go(p)}>{p}</button>
      {/if}
    {/each}
    <button type="button" on:click={() => go(page + 1)} disabled={disabled || page >= pageCount} aria-label={nextLabel}>›</button>
  </div>
</div>
