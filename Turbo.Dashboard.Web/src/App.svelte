<script>
  import { onMount } from 'svelte';
  import Router, { replace, location } from 'svelte-spa-router';
  import { getIdentity, logout as apiLogout } from './lib/api.js';
  import { identity, deniedRoute } from './lib/session.js';
  import { routes } from './lib/routes.js';
  import AppShell from './components/AppShell.svelte';
  import Login from './components/Login.svelte';
  import EntityModal from './components/EntityModal.svelte';

  let status = 'loading'; // 'loading' | 'login' | 'ready'

  onMount(loadIdentity);

  async function loadIdentity() {
    status = 'loading';
    try {
      identity.set(await getIdentity());
      status = 'ready';
    } catch {
      identity.set(null);
      status = 'login';
    }
  }

  async function handleLogout() {
    try {
      await apiLogout();
    } catch {
      // ignore — clear the session locally regardless
    }

    identity.set(null);
    status = 'login';
  }

  // A capability guard that rejects a navigation surfaces as conditionsFailed; remember the target
  // and route the user to the shared access-denied view instead of a blank screen.
  function handleConditionsFailed(event) {
    deniedRoute.set(event.detail?.userData?.route || event.detail?.route || '');
    replace('/access-denied');
  }

  // Normalise the empty root hash to the overview entry point once authenticated.
  $: if (status === 'ready' && ($location === '/' || $location === '')) {
    replace('/overview');
  }
</script>

{#if status === 'loading'}
  <div class="boot-screen">Loading...</div>
{:else if status === 'login'}
  <Login onAuthenticated={loadIdentity} />
{:else}
  <AppShell logout={handleLogout}>
    <Router {routes} on:conditionsFailed={handleConditionsFailed} />
  </AppShell>

  <EntityModal />
{/if}

<style>
  .boot-screen {
    min-height: 100vh;
    display: flex;
    align-items: center;
    justify-content: center;
    opacity: 0.6;
  }
</style>
