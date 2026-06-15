<script>
  import {
    Activity,
    Coins,
    Home,
    Package,
    ScrollText,
    Search,
    Server,
    ShieldAlert,
    Wrench,
  } from '@lucide/svelte';

  export let routes = [];
  export let currentRoute = '/overview';
  export let token = '';
  export let globalStatus = '';
  export let navigate;

  const routeIcons = {
    '/overview': Activity,
    '/infrastructure': Server,
    '/investigation': Search,
    '/rooms': Home,
    '/packets': Package,
    '/economy': Coins,
    '/incidents': ShieldAlert,
    '/audit': ScrollText,
    '/operations': Wrench,
  };

  function href(route) {
    return token ? `${route}?token=${encodeURIComponent(token)}` : route;
  }

  function iconFor(route) {
    return routeIcons[route.path] || Activity;
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
      {#each routes as route}
        {@const Icon = iconFor(route)}
        <a
          href={href(route.path)}
          class:active={currentRoute === route.path}
          on:click|preventDefault={() => navigate(route.path)}
        >
          <span class="nav-icon" aria-hidden="true">
            <Icon size={18} strokeWidth={1.9} />
          </span>
          <span class="nav-copy">
            <span>{route.label}</span>
            <small>{route.short}</small>
          </span>
        </a>
      {/each}
    </nav>
  </aside>

  <section class="workspace">
    <header class="topline">
      <div>
        <p class="eyebrow">Turbo Cloud</p>
        <h1>{routes.find((route) => route.path === currentRoute)?.label || 'Dashboard'}</h1>
      </div>
      <div class="status-pill" class:ok={token}>{globalStatus}</div>
    </header>

    <slot />
  </section>
</main>
