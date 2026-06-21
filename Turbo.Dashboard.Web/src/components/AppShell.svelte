<script>
  import { location, push } from 'svelte-spa-router';
  import {
    Activity,
    Coins,
    Home,
    LogOut,
    Package,
    ScrollText,
    Search,
    Server,
    ShieldAlert,
    Wrench,
    Lock,
  } from '@lucide/svelte';
  import { identity } from '../lib/session.js';
  import { NAV, hasRouteAccess } from '../lib/routes.js';

  export let logout;
  export let logoutBusy = false;

  const routeIcons = {
    '/overview': Activity,
    '/infrastructure': Server,
    '/investigation': Search,
    '/rooms': Home,
    '/packets': Package,
    '/economy': Coins,
    '/incidents': ShieldAlert,
    '/audit': ScrollText,
    '/moderation': ShieldAlert,
    '/api-explorer': ScrollText,
    '/operations': Wrench,
  };

  // Re-evaluate access whenever the identity changes (login / logout / role swap).
  $: items = NAV.map((item) => ({ ...item, allowed: hasRouteAccess(item, $identity) }));
  $: email = $identity?.email || '';
  $: activeLabel = NAV.find((item) => item.path === $location)?.label || 'Dashboard';

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

    <nav aria-label="Dashboard sections">
      {#each items as item}
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
    </nav>
  </aside>

  <section class="workspace">
    <header class="topline">
      <div>
        <p class="eyebrow">Turbo Cloud</p>
        <h1>{activeLabel}</h1>
      </div>
      <div class="session-area">
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
</main>

<style>
  .session-area {
    display: flex;
    align-items: center;
    gap: 10px;
  }

  .logout-btn {
    display: inline-flex;
    align-items: center;
    gap: 6px;
    padding: 7px 12px;
    border-radius: 10px;
    border: 1px solid rgba(255, 255, 255, 0.12);
    background: rgba(255, 255, 255, 0.04);
    color: inherit;
    cursor: pointer;
    font-size: 13px;
  }

  .logout-btn:hover {
    background: rgba(255, 255, 255, 0.08);
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
