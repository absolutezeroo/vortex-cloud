<script>
  import { login } from '../lib/api.js';

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
      error =
        e.status === 403
          ? 'This account has no dashboard access.'
          : 'Invalid email or password.';
    } finally {
      busy = false;
    }
  }
</script>

<div class="login-screen">
  <form class="login-card" on:submit|preventDefault={submit}>
    <div class="login-brand">
      <span class="brand-mark">T</span>
      <div>
        <strong>Turbo Ops</strong>
        <small>Observability center</small>
      </div>
    </div>

    <label>
      <span>Email</span>
      <input type="email" bind:value={email} autocomplete="username" required />
    </label>

    <label>
      <span>Password</span>
      <input type="password" bind:value={password} autocomplete="current-password" required />
    </label>

    {#if error}
      <p class="login-error">{error}</p>
    {/if}

    <button type="submit" disabled={busy}>{busy ? 'Signing in…' : 'Sign in'}</button>
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
    background: rgba(255, 255, 255, 0.04);
    border: 1px solid rgba(255, 255, 255, 0.08);
    box-shadow: 0 24px 60px rgba(0, 0, 0, 0.35);
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
    opacity: 0.6;
  }

  .brand-mark {
    display: inline-flex;
    align-items: center;
    justify-content: center;
    width: 36px;
    height: 36px;
    border-radius: 10px;
    background: #4f7cff;
    color: #fff;
    font-weight: 700;
  }

  label {
    display: flex;
    flex-direction: column;
    gap: 6px;
    font-size: 13px;
    opacity: 0.85;
  }

  input {
    padding: 10px 12px;
    border-radius: 10px;
    border: 1px solid rgba(255, 255, 255, 0.12);
    background: rgba(0, 0, 0, 0.25);
    color: inherit;
    font-size: 14px;
  }

  button {
    margin-top: 4px;
    padding: 11px 12px;
    border: 0;
    border-radius: 10px;
    background: #4f7cff;
    color: #fff;
    font-weight: 600;
    cursor: pointer;
  }

  button:disabled {
    opacity: 0.6;
    cursor: default;
  }

  .login-error {
    margin: 0;
    color: #ff6b6b;
    font-size: 13px;
  }
</style>
