<script>
  import { location, push } from 'svelte-spa-router';
  import {
    Activity,
    Ban,
    Box,
    Coins,
    DoorOpen,
    Gavel,
    Home,
    LineChart,
    LogOut,
    MessageCircleWarning,
    Package,
    ScrollText,
    Search,
    Server,
    ShieldAlert,
    ShoppingCart,
    Sparkles,
    Store,
    Terminal,
    Ticket,
    Wrench,
    Lock,
  } from '@lucide/svelte';
  import { identity } from '../lib/session.js';
  import { NAV, hasRouteAccess } from '../lib/routes.js';
  import { reasonSuggestions } from '../lib/reasonHistory.js';
  import { theme, setTheme, THEMES } from '../lib/theme.js';

  export let logout;
  export let logoutBusy = false;

  // Keep in sync with the `group` field on NAV entries (routes.js).
  const GROUP_ORDER = ['Live', 'Investigate', 'Act', 'Dev'];

  const routeIcons = {
    '/overview': Activity,
    '/infrastructure': Server,
    '/investigation': Search,
    '/rooms': Home,
    '/packets': Package,
    '/economy': Coins,
    '/incidents': ShieldAlert,
    '/audit': ScrollText,
    '/moderation': Gavel,
    '/api-explorer': Terminal,
    '/operations': Wrench,
    '/moderation-actions': Ban,
    '/cfh': MessageCircleWarning,
    '/room-control': DoorOpen,
    '/vouchers': Ticket,
    '/catalog': Store,
    '/furniture-definitions': Box,
    '/economy-trends': LineChart,
    '/marketplace': ShoppingCart,
    '/subscriptions': Sparkles,
  };

  let query = '';

  // Re-evaluate access whenever the identity changes (login / logout / role swap).
  $: items = NAV.map((item) => ({ ...item, allowed: hasRouteAccess(item, $identity) }));
  $: filteredItems = filterItems(items, query);
  $: groups = GROUP_ORDER.map((name) => ({
    name,
    items: filteredItems.filter((item) => (item.group || 'Other') === name),
  })).filter((group) => group.items.length > 0);
  $: email = $identity?.email || '';
  $: activeLabel = NAV.find((item) => item.path === $location)?.label || 'Dashboard';

  function filterItems(list, q) {
    const needle = q.trim().toLowerCase();

    if (!needle) {
      return list;
    }

    return list.filter(
      (item) =>
        item.label.toLowerCase().includes(needle) || item.short.toLowerCase().includes(needle),
    );
  }

  function iconFor(item) {
    return routeIcons[item.path] || Activity;
  }

  function go(item) {
    if (item.allowed) {
      push(item.path);
    }
  }
</script>

<main class="app-shell">
  <aside class="sidebar">
    <div class="brand">
      <span class="brand-mark">T</span>
      <div>
        <strong>Turbo Ops</strong>
        <small>Observability center</small>
      </div>
    </div>

    <div class="nav-search">
      <Search size={15} strokeWidth={1.9} aria-hidden="true" />
      <input
        type="search"
        placeholder="Filter sections..."
        aria-label="Filter dashboard sections"
        bind:value={query}
      />
    </div>

    <nav aria-label="Dashboard sections">
      {#each groups as group}
        <p class="nav-group-label">{group.name}</p>
        {#each group.items as item}
          {@const Icon = iconFor(item)}
          <a
            href={`#${item.path}`}
            class:active={$location === item.path}
            class:disabled={!item.allowed}
            aria-disabled={!item.allowed}
            tabindex={item.allowed ? 0 : -1}
            on:click|preventDefault={() => go(item)}
          >
            <span class="nav-icon" aria-hidden="true">
              <Icon size={18} strokeWidth={1.9} />
            </span>
            <span class="nav-copy">
              {#if !item.allowed}
                <span class="nav-lock" aria-hidden="true">
                  <Lock size={13} />
                </span>
              {/if}
              <span>{item.label}</span>
              <small>{item.short}</small>
            </span>
          </a>
        {/each}
      {/each}
      {#if groups.length === 0}
        <p class="nav-empty">No section matches "{query}".</p>
      {/if}
    </nav>
  </aside>

  <section class="workspace">
    <header class="topline">
      <div>
        <p class="eyebrow">Turbo Cloud</p>
        <h1>{activeLabel}</h1>
      </div>
      <div class="session-area">
        <div class="theme-switch" role="radiogroup" aria-label="Dashboard theme">
          {#each THEMES as t}
            <button
              type="button"
              role="radio"
              aria-checked={$theme === t.value}
              class:active={$theme === t.value}
              on:click={() => setTheme(t.value)}
            >
              {t.label}
            </button>
          {/each}
        </div>
        <div class="status-pill ok">{email}</div>
        <button
          type="button"
          class="logout-btn"
          title="Sign out"
          disabled={logoutBusy}
          aria-busy={logoutBusy}
          on:click={() => logout()}
        >
          <LogOut size={16} strokeWidth={1.9} />
          <span>{logoutBusy ? 'Signing out...' : 'Sign out'}</span>
        </button>
      </div>
    </header>

    <slot />
  </section>

  <datalist id="reason-history">
    {#each $reasonSuggestions as suggestion}
      <option value={suggestion}></option>
    {/each}
  </datalist>
</main>

<style>
  .nav-search {
    display: flex;
    align-items: center;
    gap: 8px;
    padding: 8px 10px;
    border: 1px solid var(--line);
    border-radius: 10px;
    background: var(--input-bg);
    color: var(--muted);
  }

  .nav-search input {
    flex: 1;
    min-width: 0;
    border: 0;
    background: transparent;
    color: var(--ink);
    outline: none;
    font-size: 0.86rem;
  }

  .nav-search input::placeholder {
    color: var(--muted);
  }

  .nav-search:focus-within {
    border-color: rgba(var(--accent-rgb), 0.58);
    box-shadow: 0 0 0 3px rgba(var(--accent-rgb), 0.12);
  }

  .nav-group-label {
    margin: 10px 4px 2px;
    color: var(--muted);
    text-transform: uppercase;
    font-size: 0.68rem;
    font-weight: 700;
    letter-spacing: 0.04em;
  }

  .sidebar nav > .nav-group-label:first-child {
    margin-top: 0;
  }

  .nav-empty {
    margin: 8px 4px;
    color: var(--muted);
    font-size: 0.82rem;
  }

  .session-area {
    display: flex;
    align-items: center;
    gap: 10px;
  }

  .theme-switch {
    display: inline-flex;
    gap: 2px;
    padding: 3px;
    border: 1px solid var(--line-strong);
    border-radius: 10px;
    background: var(--surface-strong);
  }

  .theme-switch button {
    border: 0;
    border-radius: 7px;
    background: transparent;
    color: var(--muted);
    padding: 6px 11px;
    font-size: 12px;
    font-weight: 700;
    cursor: pointer;
  }

  .theme-switch button:hover {
    color: var(--ink);
  }

  .theme-switch button.active {
    background: var(--surface-raised);
    color: var(--ink);
    box-shadow: inset 0 0 0 1px var(--line-strong);
  }

  .logout-btn {
    display: inline-flex;
    align-items: center;
    gap: 6px;
    padding: 7px 12px;
    border-radius: 10px;
    border: 1px solid var(--line-strong);
    background: var(--surface-strong);
    color: inherit;
    cursor: pointer;
    font-size: 13px;
  }

  .logout-btn:hover {
    background: var(--surface-hover);
  }

  .logout-btn:disabled {
    opacity: 0.62;
    cursor: default;
  }

  .disabled {
    opacity: 0.5;
    cursor: not-allowed;
    pointer-events: none;
  }

  .disabled:hover {
    background: transparent;
    transform: none;
    border-color: transparent;
  }

  .nav-copy {
    display: grid;
    gap: 2px;
  }

  .nav-lock {
    margin-right: 2px;
    display: inline-flex;
    align-items: center;
    color: var(--warning);
  }
</style>
