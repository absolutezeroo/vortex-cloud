<script>
  import { location, push } from 'svelte-spa-router';
  import {
    Activity,
    Ban,
    BarChart3,
    Box,
    Cable,
    ChevronDown,
    ChevronRight,
    Coins,
    DoorOpen,
    Gavel,
    Home,
    LineChart,
    LogOut,
    MessageCircleWarning,
    Package,
    PawPrint,
    ScrollText,
    Search,
    Server,
    ShieldAlert,
    ShoppingBag,
    ShoppingCart,
    Sparkles,
    Store,
    Terminal,
    Ticket,
    Users,
    Wrench,
    Lock,
  } from '@lucide/svelte';
  import { identity } from '../lib/session.js';
  import { NAV, hasRouteAccess } from '../lib/routes.js';
  import { reasonSuggestions } from '../lib/reasonHistory.js';
  import { theme, setTheme, THEMES } from '../lib/theme.js';
  import { t, locale, setLocale, LOCALES } from '../lib/i18n.js';

  export let logout;
  export let logoutBusy = false;

  // Keep in sync with the `group` field on NAV entries (routes.js).
  const GROUP_ORDER = ['Live', 'Investigate', 'Stats', 'Act', 'Dev'];

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
    '/groups-stats': Users,
    '/pets-stats': PawPrint,
    '/cfh-stats': BarChart3,
    '/catalog-purchases': ShoppingBag,
    '/wired-stats': Cable,
  };

  let query = '';

  // Which nav groups are collapsed, keyed by the stable GROUP_ORDER id (not the translated label,
  // which changes with $locale and would silently reset saved state on a language switch).
  // Persisted so a collapse choice survives reloads.
  const COLLAPSE_STORAGE_KEY = 'vortex-dashboard-nav-collapsed';

  function loadCollapsedGroups() {
    try {
      const raw = localStorage.getItem(COLLAPSE_STORAGE_KEY);
      return raw ? new Set(JSON.parse(raw)) : new Set();
    } catch {
      return new Set();
    }
  }

  let collapsedGroups = loadCollapsedGroups();

  function toggleGroup(id) {
    if (collapsedGroups.has(id)) {
      collapsedGroups.delete(id);
    } else {
      collapsedGroups.add(id);
    }
    collapsedGroups = collapsedGroups;

    try {
      localStorage.setItem(COLLAPSE_STORAGE_KEY, JSON.stringify([...collapsedGroups]));
    } catch {
      // Ignore storage failures (private browsing, quota) -- collapse still works this session.
    }
  }

  // Takes `groupsCollapsed` explicitly (not read from the outer `collapsedGroups` closure) so the
  // `{@const}` call site below stays reactive -- see the ApiExplorerPage `filtered` reactivity note:
  // a value read only inside a called function's body is invisible to Svelte's per-block dirty
  // tracking, so a toggle would mutate state but never trigger a re-render without this.
  // A collapsed group still expands while actively searching, so filter results stay visible.
  function isCollapsed(group, q, groupsCollapsed) {
    return groupsCollapsed.has(group.id) && !q.trim();
  }

  // Re-evaluate access whenever the identity changes (login / logout / role swap). label/short are
  // resolved here (not read directly off NAV) so they re-translate whenever $locale changes too --
  // $t is referenced directly in this statement's own expression for that reason (see the Svelte
  // reactivity note on ApiExplorerPage's `filtered` for why that matters).
  $: items = NAV.map((item) => ({
    ...item,
    label: $t(item.labelKey),
    short: $t(item.shortKey),
    allowed: hasRouteAccess(item, $identity),
  }));
  $: groupLabels = {
    Live: $t('nav.groupLive'),
    Investigate: $t('nav.groupInvestigate'),
    Stats: $t('nav.groupStats'),
    Act: $t('nav.groupAct'),
    Dev: $t('nav.groupDev'),
  };
  $: filteredItems = filterItems(items, query);
  $: groups = GROUP_ORDER.map((name) => ({
    id: name,
    name: groupLabels[name] || name,
    items: filteredItems.filter((item) => (item.group || 'Other') === name),
  })).filter((group) => group.items.length > 0);
  $: email = $identity?.email || '';
  $: activeLabel = items.find((item) => item.path === $location)?.label || $t('nav.dashboardFallback');

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
      <span class="brand-mark" aria-hidden="true">
        <svg width="22" height="22" viewBox="0 0 24 24" fill="none">
          <path d="M3 11 12 4l9 7v8a1 1 0 0 1-1 1h-5v-6H9v6H4a1 1 0 0 1-1-1z" fill="currentColor" />
        </svg>
      </span>
      <div>
        <strong>{$t('nav.brandTitle')}</strong>
        <small>{$t('nav.brandSubtitle')}</small>
      </div>
    </div>

    <div class="nav-search">
      <Search size={15} strokeWidth={1.9} aria-hidden="true" />
      <input
        type="search"
        placeholder={$t('nav.searchPlaceholder')}
        aria-label={$t('nav.searchPlaceholder')}
        bind:value={query}
      />
    </div>

    <nav aria-label="Dashboard sections">
      {#each groups as group}
        {@const collapsed = isCollapsed(group, query, collapsedGroups)}
        <button
          type="button"
          class="nav-group-label"
          on:click={() => toggleGroup(group.id)}
          aria-expanded={!collapsed}
        >
          <svelte:component this={collapsed ? ChevronRight : ChevronDown} size={13} strokeWidth={2.2} aria-hidden="true" />
          <span>{group.name}</span>
        </button>
        {#if !collapsed}
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
        {/if}
      {/each}
      {#if groups.length === 0}
        <p class="nav-empty">{$t('nav.noMatch', { query })}</p>
      {/if}
    </nav>
  </aside>

  <section class="workspace">
    <header class="topline">
      <div>
        <p class="eyebrow">Vortex Cloud</p>
        <h1>{activeLabel}</h1>
      </div>
      <div class="session-area">
        <div class="locale-switch" role="radiogroup" aria-label="Dashboard language">
          {#each LOCALES as loc}
            <button
              type="button"
              role="radio"
              aria-checked={$locale === loc.value}
              class:active={$locale === loc.value}
              on:click={() => setLocale(loc.value)}
            >
              {loc.label}
            </button>
          {/each}
        </div>
        <div class="theme-switch" role="radiogroup" aria-label="Dashboard theme">
          {#each THEMES as themeOption}
            <button
              type="button"
              role="radio"
              aria-checked={$theme === themeOption.value}
              class:active={$theme === themeOption.value}
              on:click={() => setTheme(themeOption.value)}
            >
              {themeOption.label}
            </button>
          {/each}
        </div>
        <div class="status-pill ok">{email}</div>
        <button
          type="button"
          class="logout-btn"
          title={$t('common.signOut')}
          disabled={logoutBusy}
          aria-busy={logoutBusy}
          on:click={() => logout()}
        >
          <LogOut size={16} strokeWidth={1.9} />
          <span>{logoutBusy ? $t('common.signingOut') : $t('common.signOut')}</span>
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
    display: flex;
    align-items: center;
    gap: 5px;
    width: 100%;
    margin: 10px 0 2px;
    padding: 4px;
    background: none;
    border: none;
    color: var(--muted);
    text-transform: uppercase;
    font-size: 0.68rem;
    font-weight: 700;
    letter-spacing: 0.04em;
    cursor: pointer;
    border-radius: 6px;
  }

  .nav-group-label:hover {
    color: var(--ink);
    background: var(--surface-hover, var(--surface));
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
    justify-content: flex-end;
    flex-wrap: wrap;
    gap: 10px;
  }

  .theme-switch,
  .locale-switch {
    display: inline-flex;
    gap: 2px;
    padding: 3px;
    border: 1px solid var(--line-strong);
    border-radius: 10px;
    background: var(--surface-strong);
  }

  .theme-switch button,
  .locale-switch button {
    border: 0;
    border-radius: 7px;
    background: transparent;
    color: var(--muted);
    padding: 6px 11px;
    font-size: 12px;
    font-weight: 700;
    cursor: pointer;
  }

  .theme-switch button:hover,
  .locale-switch button:hover {
    color: var(--ink);
  }

  .theme-switch button.active,
  .locale-switch button.active {
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
