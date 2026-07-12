<script>
  import { onMount } from 'svelte';
  import Router, { replace, location } from 'svelte-spa-router';
  import { AlertTriangle, RefreshCw, WifiOff } from '@lucide/svelte';
  import {
    describeApiError,
    getIdentity,
    isAuthError,
    isConnectionError,
    logout as apiLogout,
  } from './lib/api.js';
  import { connectionIssue, identity, deniedRoute } from './lib/session.js';
  import { routes } from './lib/routes.js';
  import { t } from './lib/i18n.js';
  import AppShell from './components/AppShell.svelte';
  import Login from './components/Login.svelte';
  import EntityModal from './components/EntityModal.svelte';

  let status = 'loading'; // 'loading' | 'login' | 'ready' | 'unavailable'
  let bootMessage = '';
  let retryBusy = false;
  let logoutBusy = false;
  let retryTimer = null;
  let identityRequestId = 0;

  onMount(() => {
    loadIdentity();

    const retryWhenVisible = () => {
      if (status === 'unavailable' || $connectionIssue) {
        retryConnection(true);
      }
    };

    window.addEventListener('online', retryWhenVisible);
    window.addEventListener('focus', retryWhenVisible);

    return () => {
      clearRetryTimer();
      window.removeEventListener('online', retryWhenVisible);
      window.removeEventListener('focus', retryWhenVisible);
    };
  });

  async function loadIdentity(options = {}) {
    const silent = options.silent === true;
    const requestId = ++identityRequestId;

    clearRetryTimer();

    if (!silent) {
      status = 'loading';
      bootMessage = '';
    }

    try {
      identity.set(await getIdentity({ timeoutMs: silent ? 5000 : undefined }));
      connectionIssue.set(null);
      status = 'ready';
      bootMessage = '';
    } catch (e) {
      if (requestId !== identityRequestId) {
        return;
      }

      identity.set(null);

      if (isAuthError(e)) {
        connectionIssue.set(null);
        status = 'login';
        bootMessage = '';
        return;
      }

      bootMessage = describeApiError(e);

      if (!silent || status !== 'ready') {
        status = 'unavailable';
      }

      if (isConnectionError(e)) {
        scheduleRetry();
      }
    }
  }

  async function handleLogout() {
    if (logoutBusy) {
      return;
    }

    logoutBusy = true;
    clearRetryTimer();
    identity.set(null);
    deniedRoute.set('');
    status = 'login';
    replace('/');

    try {
      await apiLogout();
      connectionIssue.set(null);
    } catch (e) {
      if (!isConnectionError(e)) {
        bootMessage = describeApiError(e);
      }
    } finally {
      logoutBusy = false;
    }
  }

  async function retryConnection(silent = false) {
    if (retryBusy) {
      return;
    }

    retryBusy = true;

    try {
      await loadIdentity({ silent: silent || status === 'ready' });
    } finally {
      retryBusy = false;
    }
  }

  function scheduleRetry() {
    if (retryTimer !== null) {
      return;
    }

    retryTimer = setTimeout(() => {
      retryTimer = null;
      retryConnection(true);
    }, 5000);
  }

  function clearRetryTimer() {
    if (retryTimer === null) {
      return;
    }

    clearTimeout(retryTimer);
    retryTimer = null;
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
  <div class="boot-screen">
    <section class="boot-panel" aria-live="polite">
      <RefreshCw size={24} class="spin" />
      <div>
        <p class="eyebrow">{$t('nav.brandTitle')}</p>
        <h1>{$t('boot.checkingEmulator')}</h1>
      </div>
    </section>
  </div>
{:else if status === 'unavailable'}
  <div class="boot-screen">
    <section class="boot-panel boot-panel--wide" role="status" aria-live="polite">
      <WifiOff size={30} />
      <div>
        <p class="eyebrow">{$t('boot.unavailableEyebrow')}</p>
        <h1>{$t('boot.connectionPaused')}</h1>
        <p>{bootMessage || $connectionIssue?.message || $t('boot.unreachable')}</p>
      </div>
      <button type="button" on:click={() => retryConnection(false)} disabled={retryBusy}>
        <RefreshCw size={16} class={retryBusy ? 'spin' : ''} />
        <span>{retryBusy ? $t('boot.retrying') : $t('boot.retryNow')}</span>
      </button>
      <small>{$t('boot.autoRetry')}</small>
    </section>
  </div>
{:else if status === 'login'}
  {#if $connectionIssue}
    <div class="connection-banner" role="status">
      <AlertTriangle size={16} />
      <span>{$connectionIssue.message}</span>
      <button type="button" on:click={() => retryConnection(true)} disabled={retryBusy}>
        <RefreshCw size={14} class={retryBusy ? 'spin' : ''} />
        <span>{$t('common.retry')}</span>
      </button>
    </div>
  {/if}

  <Login onAuthenticated={loadIdentity} />
{:else}
  {#if $connectionIssue}
    <div class="connection-banner" role="status">
      <AlertTriangle size={16} />
      <span>{$connectionIssue.message}</span>
      <button type="button" on:click={() => retryConnection(true)} disabled={retryBusy}>
        <RefreshCw size={14} class={retryBusy ? 'spin' : ''} />
        <span>{$t('common.retry')}</span>
      </button>
    </div>
  {/if}

  <AppShell logout={handleLogout} {logoutBusy}>
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
    padding: 24px;
  }

  .boot-panel {
    width: min(420px, 100%);
    display: grid;
    justify-items: center;
    gap: 14px;
    padding: 24px;
    border: 1px solid var(--line);
    border-radius: 14px;
    background: var(--surface);
    color: var(--muted-strong);
    text-align: center;
    box-shadow: var(--panel-shadow);
  }

  .boot-panel--wide {
    width: min(520px, 100%);
  }

  .boot-panel h1 {
    margin: 0;
    color: var(--ink);
    font-size: 1.35rem;
  }

  .boot-panel p {
    margin: 8px 0 0;
    color: var(--muted);
  }

  .boot-panel small {
    color: var(--muted);
  }

  .boot-panel button,
  .connection-banner button {
    display: inline-flex;
    align-items: center;
    justify-content: center;
    gap: 7px;
    border: 1px solid rgba(var(--accent-rgb), 0.34);
    border-radius: 9px;
    background: var(--button-bg);
    color: var(--button-ink);
    padding: 8px 12px;
    font-weight: 700;
  }

  .boot-panel button:disabled,
  .connection-banner button:disabled {
    opacity: 0.62;
    cursor: default;
  }

  .connection-banner {
    position: fixed;
    z-index: 80;
    top: 14px;
    left: 50%;
    width: min(640px, calc(100vw - 28px));
    transform: translateX(-50%);
    display: grid;
    grid-template-columns: auto minmax(0, 1fr) auto;
    align-items: center;
    gap: 10px;
    padding: 10px 12px;
    border: 1px solid var(--warning-border);
    border-radius: 10px;
    background: var(--warning-bg);
    color: var(--muted-strong);
    box-shadow: var(--panel-shadow);
  }

  .connection-banner span {
    min-width: 0;
  }

  :global(.spin) {
    animation: spin 900ms linear infinite;
  }

  @keyframes spin {
    to {
      transform: rotate(360deg);
    }
  }

  @media (max-width: 560px) {
    .connection-banner {
      grid-template-columns: auto minmax(0, 1fr);
    }

    .connection-banner button {
      grid-column: 1 / -1;
      width: 100%;
    }
  }
</style>
