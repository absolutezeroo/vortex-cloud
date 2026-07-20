<script>
  import { describeApiError, isConnectionError, login } from '../lib/api.js';
  import { t, translate } from '../lib/i18n.js';

  export let onAuthenticated;

  let email = '';
  let password = '';
  let error = '';
  let busy = false;

  async function submit() {
    if (busy) return;
    error = '';
    busy = true;
    try {
      await login(email, password);
      await onAuthenticated();
    } catch (e) {
      if (isConnectionError(e)) {
        error = describeApiError(e);
      } else if (e.status === 403) {
        error = translate('login.noAccess');
      } else if (e.status === 429) {
        error = describeApiError(e);
      } else {
        error = translate('login.invalidCredentials');
      }
    } finally {
      busy = false;
    }
  }
</script>

<div class="login-screen">
  <form class="login-card" on:submit|preventDefault={submit}>
    <div class="login-brand">
      <span class="brand-mark" aria-hidden="true">
        <svg width="20" height="20" viewBox="0 0 24 24" fill="none">
          <path d="M3 11 12 4l9 7v8a1 1 0 0 1-1 1h-5v-6H9v6H4a1 1 0 0 1-1-1z" fill="currentColor" />
        </svg>
      </span>
      <div>
        <strong>{$t('nav.brandTitle')}</strong>
        <small>{$t('nav.brandSubtitle')}</small>
      </div>
    </div>

    <label>
      <span>{$t('login.email')}</span>
      <input type="email" bind:value={email} autocomplete="username" required />
    </label>

    <label>
      <span>{$t('login.password')}</span>
      <input type="password" bind:value={password} autocomplete="current-password" required />
    </label>

    {#if error}
      <p class="login-error">{error}</p>
    {/if}

    <button type="submit" disabled={busy}>{busy ? $t('login.signingIn') : $t('login.signIn')}</button>
  </form>
</div>

<style>
  .login-screen {
    min-height: 100vh;
    display: flex;
    align-items: center;
    justify-content: center;
    padding: 24px;
  }

  .login-card {
    width: 100%;
    max-width: 360px;
    display: flex;
    flex-direction: column;
    gap: 16px;
    padding: 28px;
    border-radius: 16px;
    background: var(--surface);
    border: 1px solid var(--line);
    box-shadow: var(--shadow);
  }

  .login-brand {
    display: flex;
    align-items: center;
    gap: 12px;
    margin-bottom: 4px;
  }

  .login-brand strong {
    display: block;
  }

  .login-brand small {
    color: var(--muted);
  }

  .brand-mark {
    display: grid;
    place-items: center;
    width: 40px;
    height: 40px;
    border-radius: 11px;
    background: linear-gradient(160deg, var(--gold-strong), #e0870f);
    box-shadow: 0 3px 0 #a9640a, 0 8px 16px rgba(0, 0, 0, 0.28);
    color: var(--gold-ink);
    font-weight: 800;
  }

  .brand-mark svg {
    display: block;
  }

  label {
    display: flex;
    flex-direction: column;
    gap: 6px;
    font-size: 13px;
    color: var(--muted);
  }

  input {
    padding: 10px 12px;
    border-radius: 10px;
    border: 1px solid var(--line-strong);
    background: var(--input-bg);
    color: var(--ink);
    font-size: 14px;
    outline: none;
  }

  input:focus {
    border-color: rgba(var(--accent-rgb), 0.58);
    box-shadow: 0 0 0 3px rgba(var(--accent-rgb), 0.12);
  }

  button {
    margin-top: 4px;
    padding: 11px 12px;
    border: 1px solid transparent;
    border-radius: 10px;
    background: var(--button-bg);
    color: var(--button-ink);
    font-weight: 700;
    cursor: pointer;
    box-shadow: 0 1px 2px rgba(0, 0, 0, 0.18);
    transition: filter 140ms ease;
  }

  button:hover:not(:disabled) {
    filter: brightness(1.06);
  }

  button:disabled {
    opacity: 0.6;
    cursor: default;
  }

  .login-error {
    margin: 0;
    color: var(--danger);
    font-size: 13px;
  }
</style>
