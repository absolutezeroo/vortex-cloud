<script>
  // In-page tab strip for the admin pages that do several jobs at once. Follows the WAI-ARIA tabs
  // pattern and NN/g's rules for the component: one tab is always selected, switching is instant
  // (the panels are page state, nothing refetches), and labels stay one or two words in sentence
  // case so the strip reads as a set of destinations rather than a sentence.
  //
  // Use it when a page has three or more sections a operator would otherwise scroll past to reach
  // the third thing -- NOT when they need to compare two sections side by side, which is the one
  // case tabs actively make worse.
  //
  //   <Tabs tabs={[{ id: 'prizes', label: 'Prizes', icon: Gift, count: prizes.length }]} bind:active />
  //   {#if active === 'prizes'} ... {/if}

  
  
  /**
   * @typedef {Object} Props
   * @property {any} [tabs] - [{ id, label, icon?, count? }] -- `icon` is a lucide component, `count` a badge.
   * @property {any} [active]
   * @property {(id: string) => void} [onchange] - receives the newly selected tab id
   * @property {string} [storageKey] - Remembers the open tab under this key for the session, so a refresh (or coming back from
another page) lands where the operator left off instead of resetting to the first tab.
   */

  /** @type {Props} */
  let { tabs = [], active = $bindable(tabs[0]?.id ?? ''), storageKey = '', onchange } = $props();

  let buttons = $state([]);

  if (storageKey) {
    try {
      const stored = sessionStorage.getItem(`vortex.tabs.${storageKey}`);
      if (stored && tabs.some((t) => t.id === stored)) active = stored;
    } catch {
      // Private browsing / quota -- the default tab is a fine fallback.
    }
  }

  function select(id) {
    if (id === active) return;

    active = id;
    onchange?.(id);

    if (storageKey) {
      try {
        sessionStorage.setItem(`vortex.tabs.${storageKey}`, id);
      } catch {
        // Best-effort.
      }
    }
  }

  // Arrow keys move between tabs, Home/End jump to the ends -- the behaviour a keyboard user expects
  // from a tablist, and the reason the strip is buttons rather than links.
  function onKeydown(event, index) {
    const last = tabs.length - 1;
    let next = null;

    if (event.key === 'ArrowRight') next = index === last ? 0 : index + 1;
    else if (event.key === 'ArrowLeft') next = index === 0 ? last : index - 1;
    else if (event.key === 'Home') next = 0;
    else if (event.key === 'End') next = last;
    else return;

    event.preventDefault();
    select(tabs[next].id);
    buttons[next]?.focus();
  }

  // A tab whose panel is empty is still worth showing -- hiding it would make the page's shape
  // change under the operator -- but it reads as quieter.
  const isEmpty = (tab) => tab.count === 0;
</script>

<div class="tabs" role="tablist">
  {#each tabs as tab, i (tab.id)}
    <button
      bind:this={buttons[i]}
      type="button"
      role="tab"
      id="tab-{tab.id}"
      class="tab"
      class:active={active === tab.id}
      class:empty={isEmpty(tab)}
      aria-selected={active === tab.id}
      aria-controls="panel-{tab.id}"
      tabindex={active === tab.id ? 0 : -1}
      onclick={() => select(tab.id)}
      onkeydown={(e) => onKeydown(e, i)}
    >
      {#if tab.icon}
        <tab.icon size={14} strokeWidth={2} aria-hidden="true" />
      {/if}
      <span>{tab.label}</span>
      {#if tab.count != null}<span class="tab-count">{tab.count}</span>{/if}
    </button>
  {/each}
</div>

<style>
  .tabs {
    display: flex;
    gap: 4px;
    margin: 10px 0 14px;
    padding: 4px;
    border: 1px solid var(--line);
    border-radius: 12px;
    background: var(--surface-strong);
    overflow-x: auto;
    scrollbar-width: thin;
  }

  .tab {
    border: 1px solid transparent;
    border-radius: 9px;
    background: transparent;
    color: var(--muted);
    padding: 7px 12px;
    font-weight: 700;
    font-size: 0.86rem;
  }

  .tab:hover:not(.active) {
    background: var(--surface-hover);
    color: var(--ink);
  }

  .tab.active {
    background: var(--button-bg);
    color: var(--button-ink);
  }

  /* The tab keeps its place when its panel has nothing in it, but stops competing for attention. */
  .tab.empty:not(.active) {
    opacity: 0.6;
  }

  .tab-count {
    border-radius: 999px;
    padding: 1px 7px;
    background: rgba(var(--muted-rgb), 0.18);
    font-size: 0.74rem;
    font-variant-numeric: tabular-nums;
  }

  .tab.active .tab-count {
    background: rgba(0, 0, 0, 0.18);
  }
</style>
