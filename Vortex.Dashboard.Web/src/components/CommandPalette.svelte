<script>
  // Ctrl/Cmd+K from anywhere. The sidebar's search box only ever filtered the sidebar, so finding a
  // player meant: guess which page owns players, go there, find its search box, type the name again.
  // This asks the three directories and the nav at once and jumps straight to the answer.
  //
  // Mounted once, in AppShell -- it is a window-level shortcut, not a page feature.
  import { push } from 'svelte-spa-router';
  import { Search, CornerDownLeft, House, User, Package, Compass } from '@lucide/svelte';
  import { apiGet } from '../lib/api.js';
  import { NAV } from '../lib/routes.js';
  import { identity, openItem } from '../lib/session.js';
  import { hasDashboardCapability } from '../lib/permissions.js';
  import { t, translate } from '../lib/i18n.js';

  let open = $state(false);
  let query = $state('');
  let cursor = $state(0);
  let input = $state();
  let remote = $state([]);
  let searching = $state(false);

  // Bumped on every keystroke so a slow response for "ab" cannot overwrite the results for "abcd".
  let requestId = 0;
  let debounce = null;

  const KIND_ICONS = { nav: Compass, player: User, room: House, furniture: Package };

  // Nav entries are matched locally against the label the operator actually reads, and filtered by
  // the same capabilities the sidebar uses -- the palette must not offer a page it cannot open.
  let navMatches = $derived.by(() => {
    const term = query.trim().toLowerCase();
    if (!term) return [];

    return NAV.filter((item) => hasDashboardCapability($identity, item.caps))
      .map((item) => ({ kind: 'nav', id: item.path, label: translate(item.labelKey), hint: item.group }))
      .filter((item) => item.label.toLowerCase().includes(term) || item.id.includes(term))
      .slice(0, 5);
  });

  let results = $derived([...navMatches, ...remote]);

  $effect(() => {
    // Re-clamp rather than reset: the list shrinks as results arrive and a stale cursor would
    // otherwise point past the end.
    if (cursor > results.length - 1) cursor = Math.max(0, results.length - 1);
  });

  async function searchRemote(term) {
    const id = ++requestId;

    if (!term) {
      remote = [];
      searching = false;
      return;
    }

    searching = true;

    // One failing directory (a capability the operator lacks, most often) must not blank the other
    // two, so each is settled on its own.
    const [players, rooms, furniture] = await Promise.all([
      apiGet(`/api/v1/directory/players?q=${encodeURIComponent(term)}&limit=4`).catch(() => null),
      apiGet(`/api/v1/directory/rooms?q=${encodeURIComponent(term)}&limit=4`).catch(() => null),
      apiGet(`/api/v1/directory/furniture?q=${encodeURIComponent(term)}&limit=4`).catch(() => null),
    ]);

    if (id !== requestId) return;

    remote = [
      ...(players?.items || []).map((p) => ({
        kind: 'player',
        id: p.id,
        label: p.name,
        hint: `#${p.id}`,
        online: p.online,
      })),
      ...(rooms?.items || []).map((r) => ({
        kind: 'room',
        id: r.id,
        label: r.name,
        hint: r.ownerName ? `#${r.id} - ${r.ownerName}` : `#${r.id}`,
      })),
      ...(furniture?.items || []).map((f) => ({
        kind: 'furniture',
        id: f.id,
        label: f.name,
        hint: f.logic ? `#${f.id} - ${f.logic}` : `#${f.id}`,
      })),
    ];

    searching = false;
  }

  $effect(() => {
    const term = query.trim();

    clearTimeout(debounce);
    debounce = setTimeout(() => searchRemote(term), 180);

    return () => clearTimeout(debounce);
  });

  function show() {
    open = true;
    query = '';
    remote = [];
    cursor = 0;
    queueMicrotask(() => input?.focus());
  }

  function hide() {
    open = false;
    query = '';
    remote = [];
  }

  function choose(entry) {
    if (!entry) return;

    hide();

    if (entry.kind === 'nav') {
      push(entry.id);
    } else if (entry.kind === 'player') {
      // The player page reads ?player= on mount, so this lands on a loaded profile, not a form.
      push(`/investigation?player=${entry.id}`);
    } else if (entry.kind === 'room') {
      push(`/rooms?room=${entry.id}`);
    } else {
      // A definition, not an owned item -- the furniture admin page is where one is looked at.
      push(`/furniture-definitions?q=${encodeURIComponent(entry.label)}`);
    }
  }

  function onWindowKeydown(event) {
    if ((event.ctrlKey || event.metaKey) && event.key.toLowerCase() === 'k') {
      event.preventDefault();
      open ? hide() : show();
      return;
    }

    if (!open) return;

    if (event.key === 'Escape') {
      event.preventDefault();
      hide();
    } else if (event.key === 'ArrowDown') {
      event.preventDefault();
      cursor = results.length ? (cursor + 1) % results.length : 0;
    } else if (event.key === 'ArrowUp') {
      event.preventDefault();
      cursor = results.length ? (cursor - 1 + results.length) % results.length : 0;
    } else if (event.key === 'Enter') {
      event.preventDefault();
      choose(results[cursor]);
    }
  }
</script>

<svelte:window onkeydown={onWindowKeydown} />

{#if open}
  <div class="cp-layer">
    <button class="cp-backdrop" type="button" aria-label={$t('common.close')} tabindex="-1" onclick={hide}
    ></button>

    <div class="cp-panel" role="dialog" aria-modal="true" aria-label={$t('palette.title')}>
      <div class="cp-search">
        <Search size={16} strokeWidth={2} aria-hidden="true" />
        <input
          bind:this={input}
          bind:value={query}
          class="bare"
          type="text"
          autocomplete="off"
          spellcheck="false"
          placeholder={$t('palette.placeholder')}
          aria-label={$t('palette.placeholder')}
        />
        <kbd>esc</kbd>
      </div>

      <div class="cp-results" role="listbox" aria-label={$t('palette.title')}>
        {#each results as entry, index (entry.kind + entry.id)}
          {@const Icon = KIND_ICONS[entry.kind]}
          <button
            type="button"
            role="option"
            aria-selected={index === cursor}
            class="cp-row"
            class:active={index === cursor}
            onmouseenter={() => (cursor = index)}
            onclick={() => choose(entry)}
          >
            <span class="cp-ico"><Icon size={15} strokeWidth={2} aria-hidden="true" /></span>
            <span class="cp-main">
              <strong>{entry.label}</strong>
              <small>{$t(`palette.kind_${entry.kind}`)} · {entry.hint}</small>
            </span>
            {#if entry.kind === 'player'}
              <span class="cp-dot" class:on={entry.online} aria-hidden="true"></span>
            {/if}
            {#if index === cursor}
              <CornerDownLeft size={14} strokeWidth={2} aria-hidden="true" />
            {/if}
          </button>
        {:else}
          <p class="empty-state" role="status">
            {#if !query.trim()}
              {$t('palette.hint')}
            {:else if searching}
              {$t('palette.searching')}
            {:else}
              {$t('palette.noResults')}
            {/if}
          </p>
        {/each}
      </div>
    </div>
  </div>
{/if}

<style>
  .cp-layer {
    position: fixed;
    inset: 0;
    z-index: 120;
    display: grid;
    justify-items: center;
    align-content: start;
    padding: 12vh 16px 16px;
  }

  .cp-backdrop {
    position: absolute;
    inset: 0;
    border: 0;
    background: var(--overlay-bg);
  }

  .cp-panel {
    position: relative;
    width: min(620px, 100%);
    display: grid;
    gap: 8px;
    padding: 10px;
    border: 1px solid var(--line-strong);
    border-radius: 10px;
    background: var(--surface);
    box-shadow: 0 26px 70px rgba(0, 0, 0, 0.46);
  }

  .cp-search {
    display: flex;
    align-items: center;
    gap: 9px;
    padding: 0 4px;
    color: var(--muted);
  }

  .cp-search input {
    flex: 1;
    min-width: 0;
    border: 0;
    background: transparent;
    color: var(--ink);
    padding: 9px 0;
    font-size: 1rem;
  }

  .cp-search input:focus-visible {
    outline: none;
  }

  .cp-search kbd {
    border: 1px solid var(--line);
    border-radius: 8px;
    padding: 1px 6px;
    font-size: 0.7rem;
  }

  .cp-results {
    display: grid;
    gap: 4px;
    max-height: 54vh;
    overflow: auto;
    overscroll-behavior: contain;
  }

  .cp-row {
    display: flex;
    align-items: center;
    gap: 10px;
    width: 100%;
    text-align: left;
    border: 1px solid transparent;
    border-radius: 8px;
    background: transparent;
    color: var(--ink);
    padding: 7px 9px;
  }

  .cp-row.active {
    border-color: rgba(var(--accent-rgb), 0.34);
    background: var(--surface-hover);
  }

  .cp-ico {
    flex: 0 0 auto;
    display: grid;
    place-items: center;
    width: 26px;
    height: 26px;
    border-radius: 8px;
    background: var(--input-bg);
    color: var(--accent);
  }

  .cp-main {
    display: grid;
    gap: 1px;
    min-width: 0;
    flex: 1;
  }

  .cp-main strong {
    overflow: hidden;
    text-overflow: ellipsis;
    white-space: nowrap;
  }

  .cp-main small {
    color: var(--muted);
  }

  .cp-dot {
    width: 8px;
    height: 8px;
    flex: 0 0 auto;
    border-radius: 999px;
    background: var(--muted);
  }

  .cp-dot.on {
    background: var(--ok);
  }
</style>
