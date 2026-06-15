<script>
  import { setToken } from './lib/api.js';
  import AppShell from './components/AppShell.svelte';
  import EntityModal from './components/EntityModal.svelte';
  import OverviewPage from './pages/OverviewPage.svelte';
  import InfrastructurePage from './pages/InfrastructurePage.svelte';
  import InvestigationPage from './pages/InvestigationPage.svelte';
  import EconomyPage from './pages/EconomyPage.svelte';
  import RoomsPage from './pages/RoomsPage.svelte';
  import PacketsPage from './pages/PacketsPage.svelte';
  import IncidentsPage from './pages/IncidentsPage.svelte';
  import AuditPage from './pages/AuditPage.svelte';
  import OperationsPage from './pages/OperationsPage.svelte';

  const routes = [
    { path: '/overview', label: 'Overview', short: 'Live health' },
    { path: '/infrastructure', label: 'Infrastructure', short: 'Runtime and Orleans' },
    { path: '/investigation', label: 'Investigation', short: 'Players and items' },
    { path: '/economy', label: 'Economy', short: 'Ledger' },
    { path: '/rooms', label: 'Room inspector', short: 'Room timeline' },
    { path: '/packets', label: 'Packet center', short: 'Traffic' },
    { path: '/incidents', label: 'Incident center', short: 'Signals' },
    { path: '/audit', label: 'Audit feed', short: 'Security' },
    { path: '/operations', label: 'Operations', short: 'Admin actions' },
  ];

  let token = new URLSearchParams(window.location.search).get('token') || '';
  let currentRoute = normalizeRoute(window.location.pathname);
  let modal = null;

  setToken(token);

  $: globalStatus = token ? 'Connected' : 'Missing token';

  function normalizeRoute(pathname) {
    const path = (pathname || '/overview').replace(/\/+$/, '') || '/overview';
    return routes.some((route) => route.path === path) ? path : '/overview';
  }

  function routeHref(route) {
    return token ? `${route}?token=${encodeURIComponent(token)}` : route;
  }

  function navigate(route) {
    currentRoute = normalizeRoute(route);
    window.history.pushState({}, '', routeHref(currentRoute));
  }

  function openPlayer(id, label = '') {
    modal = { type: 'player', id, label: label || `player #${id}` };
  }

  function openItem(id) {
    modal = { type: 'item', id, label: `item #${id}` };
  }

  window.onpopstate = () => {
    currentRoute = normalizeRoute(window.location.pathname);
  };
</script>

<AppShell {routes} {currentRoute} {token} {globalStatus} {navigate}>
  {#if currentRoute === '/overview'}
    <OverviewPage {openPlayer} {openItem} />
  {:else if currentRoute === '/infrastructure'}
    <InfrastructurePage />
  {:else if currentRoute === '/investigation'}
    <InvestigationPage {openPlayer} {openItem} />
  {:else if currentRoute === '/economy'}
    <EconomyPage {openPlayer} {openItem} />
  {:else if currentRoute === '/rooms'}
    <RoomsPage {openPlayer} {openItem} />
  {:else if currentRoute === '/packets'}
    <PacketsPage />
  {:else if currentRoute === '/incidents'}
    <IncidentsPage />
  {:else if currentRoute === '/audit'}
    <AuditPage {openPlayer} {openItem} />
  {:else if currentRoute === '/operations'}
    <OperationsPage />
  {/if}
</AppShell>

<EntityModal {modal} close={() => (modal = null)} {openPlayer} {openItem} />
