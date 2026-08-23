<script>
  // Second-factor enrolment for the signed-in operator. Enrolment is two steps on purpose: /begin
  // hands back a secret and stores nothing, and only a code computed from it turns it into the
  // account's factor. Walking away from this dialog therefore cannot lock anyone out.
  import QRCode from 'qrcode';
  import { apiGet, apiPost, describeApiError } from '../lib/api.js';
  import { identity } from '../lib/session.js';
  import Modal from './Modal.svelte';
  import { t } from '../lib/i18n.js';

  let { onclose } = $props();

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
</script>

<Modal title={$t('mfa.title')} width={520} column {onclose}>
  {#if enabled}
    <p class="state on">{$t('mfa.enabled')}</p>
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
    <p class="state off">{$t('mfa.disabled')}</p>
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

  .state.on {
    color: var(--ok);
  }

  .state.off {
    color: var(--warning);
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

  .qr {
    align-self: center;
    border-radius: 8px;
    /* The QR is dark-on-light by definition; a white plate keeps it scannable in the dark theme. */
    background: #fff;
    padding: 8px;
  }
</style>
