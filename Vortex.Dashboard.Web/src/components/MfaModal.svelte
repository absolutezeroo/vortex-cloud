<script>
  // Second-factor enrolment for the signed-in operator. Enrolment is two steps on purpose: /begin
  // hands back a secret and stores nothing, and only a code computed from it turns it into the
  // account's factor. Walking away from this dialog therefore cannot lock anyone out.
  import QRCode from 'qrcode';
  import { apiGet, apiPost, describeApiError } from '../lib/api.js';
  import { identity } from '../lib/session.js';
  import Modal from './Modal.svelte';
  import { t } from '../lib/i18n.js';

  let { onclose, logout } = $props();

  // Two things an operator does to their own account, one dialog: the second factor and the
  // password. Both re-prove the account rather than trusting the session cookie.
  let tab = $state('mfa');

  let enrolment = $state(null);
  let qr = $state('');
  let code = $state('');
  let busy = $state(false);
  let error = $state('');

  let enabled = $derived($identity?.mfaEnabled === true);

  async function refreshIdentity() {
    identity.set(await apiGet('/api/me'));
  }

  async function run(work) {
    if (busy) return;
    busy = true;
    error = '';

    try {
      await work();
    } catch (e) {
      error = describeApiError(e);
    } finally {
      busy = false;
    }
  }

  const begin = () =>
    run(async () => {
      enrolment = await apiPost('/api/v1/account/mfa/begin', {});
      // Rendered here rather than fetched: a QR of an authenticator secret must never leave the
      // browser, and the dashboard's CSP would refuse the request anyway.
      qr = await QRCode.toDataURL(enrolment.uri, { margin: 1, width: 200 });
      code = '';
    });

  const enable = () =>
    run(async () => {
      await apiPost('/api/v1/account/mfa/enable', { secret: enrolment.secret, code });
      enrolment = null;
      qr = '';
      code = '';
      await refreshIdentity();
    });

  const disable = () =>
    run(async () => {
      await apiPost('/api/v1/account/mfa/disable', { code });
      code = '';
      await refreshIdentity();
    });

  let currentPassword = $state('');
  let newPassword = $state('');
  let newPasswordRepeat = $state('');
  let passwordCode = $state('');
  let passwordDone = $state(null);

  let passwordReady = $derived(
    currentPassword.length > 0 &&
      newPassword.length >= 8 &&
      newPassword === newPasswordRepeat &&
      (!enabled || passwordCode.length === 6)
  );

  const changePassword = () =>
    run(async () => {
      const result = await apiPost('/api/v1/account/password', {
        currentPassword,
        newPassword,
        code: enabled ? passwordCode : undefined,
      });

      // The change signed this session out along with every other one, so there is nothing left to
      // do here but say so and send the operator back to the login screen.
      passwordDone = result.sessionsRevoked ?? 0;
      currentPassword = '';
      newPassword = '';
      newPasswordRepeat = '';
      passwordCode = '';
    });
</script>

<Modal title={$t('account.title')} width={520} column {onclose}>
  <div class="tabs">
    <button type="button" class:active={tab === 'mfa'} onclick={() => (tab = 'mfa')}>
      {$t('mfa.title')}
    </button>
    <button type="button" class:active={tab === 'password'} onclick={() => (tab = 'password')}>
      {$t('account.password')}
    </button>
  </div>

  {#if tab === 'password'}
    {#if passwordDone !== null}
      <p class="state on">{$t('account.changed', { count: passwordDone })}</p>
      <button type="button" onclick={() => logout()}>{$t('account.backToSignIn')}</button>
    {:else}
      <p class="muted">{$t('account.hint')}</p>

      <input
        class="pm-username"
        type="text"
        name="username"
        autocomplete="username"
        value={$identity?.email ?? ''}
        readonly
        tabindex="-1"
        aria-hidden="true"
      />

      <label>
        <span>{$t('account.currentPassword')}</span>
        <input type="password" autocomplete="current-password" bind:value={currentPassword} />
      </label>

      <label>
        <span>{$t('account.newPassword')}</span>
        <input type="password" autocomplete="new-password" bind:value={newPassword} />
      </label>

      <label>
        <span>{$t('account.repeatPassword')}</span>
        <input type="password" autocomplete="new-password" bind:value={newPasswordRepeat} />
      </label>

      {#if enabled}
        <label>
          <span>{$t('mfa.code')}</span>
          <input
            type="text"
            inputmode="numeric"
            maxlength="6"
            autocomplete="one-time-code"
            bind:value={passwordCode}
            placeholder={$t('mfa.codePlaceholder')}
          />
        </label>
      {/if}

      <button type="button" onclick={changePassword} disabled={busy || !passwordReady}>
        {$t('account.change')}
      </button>
    {/if}
  {:else if enabled}
    <p class="state notice notice--success">{$t('mfa.enabled')}</p>
    <p class="muted">{$t('mfa.disableHint')}</p>

    <label>
      <span>{$t('mfa.code')}</span>
      <input
        type="text"
        inputmode="numeric"
        maxlength="6"
        autocomplete="one-time-code"
        bind:value={code}
        placeholder={$t('mfa.codePlaceholder')}
      />
    </label>

    <button type="button" class="danger" onclick={disable} disabled={busy || code.length !== 6}>
      {$t('mfa.disable')}
    </button>
  {:else if enrolment}
    <p class="muted">{$t('mfa.scanHint')}</p>

    {#if qr}
      <img class="qr" src={qr} alt={$t('mfa.qrAlt')} width="200" height="200" />
    {/if}

    <p class="muted">{$t('mfa.manualHint')}</p>
    <code class="secret">{enrolment.secret}</code>

    <label>
      <span>{$t('mfa.code')}</span>
      <input
        type="text"
        inputmode="numeric"
        maxlength="6"
        autocomplete="one-time-code"
        bind:value={code}
        placeholder={$t('mfa.codePlaceholder')}
      />
    </label>

    <button type="button" onclick={enable} disabled={busy || code.length !== 6}>
      {$t('mfa.confirm')}
    </button>
  {:else}
    <p class="state notice notice--warn">{$t('mfa.disabled')}</p>
    <p class="muted">{$t('mfa.enableHint')}</p>

    <button type="button" onclick={begin} disabled={busy}>{$t('mfa.enable')}</button>
  {/if}

  {#if error}
    <p class="empty-state danger" role="alert">{error}</p>
  {/if}
</Modal>

<style>
  .state {
    font-weight: 600;
    margin: 0;
  }

  /* Present for the password manager, absent for everyone else. Not display:none -- a field the
     browser cannot see is a field it will not fill, which puts us back to it hunting elsewhere. */
  .pm-username {
    position: absolute;
    width: 1px;
    height: 1px;
    padding: 0;
    border: 0;
    opacity: 0;
    pointer-events: none;
  }

  .secret {
    display: block;
    font-size: 1.25rem;
    letter-spacing: 0.15em;
    word-break: break-all;
    padding: 12px;
    border-radius: 8px;
    background: var(--surface-2, rgba(255, 255, 255, 0.04));
  }

  .tabs {
    display: flex;
    gap: 4px;
  }

  /* The global tab strip is `white-space: nowrap; flex-shrink: 0`, which is right on a page wide
     enough to hold it. In a 520px dialog "Two-factor authentication" and "Password" together are
     wider than that, and since neither could give, the panel itself grew a horizontal scrollbar. */
  .tabs button {
    flex: 1;
    min-width: 0;
    flex-shrink: 1;
    white-space: normal;
  }

  .tabs button.active {
    /* The selected tab reads as pressed rather than as a second call to action. */
    filter: brightness(1.25);
  }

  .qr {
    align-self: center;
    border-radius: 8px;
    /* The QR is dark-on-light by definition; a white plate keeps it scannable in the dark theme. */
    background: #fff;
    padding: 8px;
  }
</style>
