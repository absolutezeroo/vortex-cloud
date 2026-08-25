<script>
  // "Prove it is still you." Shown when the server refuses a critical operation because this session
  // has not verified a second factor recently enough -- minting currency, the staff roster, a console
  // command, a database backup.
  //
  // Mounted once at the app root, like EntityModal, and driven by lib/stepUp.js. It never appears
  // unless a write was actually refused, and with DashboardStepUpMinutes at its default of 0 nothing
  // ever refuses.
  import Modal from './Modal.svelte';
  import { apiPost, describeApiError } from '../lib/api.js';
  import { stepUpRequest, resolveStepUp } from '../lib/stepUp.js';
  import { t } from '../lib/i18n.js';

  let code = $state('');
  let busy = $state(false);
  let error = $state('');

  async function submit() {
    if (busy || code.trim() === '') return;

    busy = true;
    error = '';

    try {
      await apiPost('/api/v1/account/mfa/step-up', { code: code.trim() });
      code = '';
      resolveStepUp(true);
    } catch (e) {
      // A wrong code leaves the dialog open with the field cleared: the operator has six digits that
      // change every thirty seconds, and closing on the first miss means starting the write again.
      error = describeApiError(e);
      code = '';
    } finally {
      busy = false;
    }
  }

  function dismiss() {
    code = '';
    error = '';
    resolveStepUp(false);
  }
</script>

{#if $stepUpRequest}
  <Modal
    title={$t('stepUp.title')}
    width={420}
    labelledBy="step-up-title"
    onclose={dismiss}
  >
    <p>{$t('stepUp.body')}</p>

    <form
      onsubmit={(event) => {
        event.preventDefault();
        submit();
      }}
    >
      <!-- svelte-ignore a11y_autofocus -->
      <input
        type="text"
        inputmode="numeric"
        autocomplete="one-time-code"
        autofocus
        maxlength="8"
        bind:value={code}
        placeholder={$t('stepUp.codePlaceholder')}
        aria-label={$t('stepUp.codeLabel')}
        disabled={busy}
      />
    </form>

    {#if error}<p class="empty-state danger" role="alert">{error}</p>{/if}

    {#snippet actions()}
      <button type="button" onclick={submit} disabled={busy || code.trim() === ''}>
        {busy ? $t('stepUp.verifying') : $t('stepUp.verify')}
      </button>
      <button class="ghost-button" type="button" onclick={dismiss}>
        {$t('common.cancel')}
      </button>
    {/snippet}
  </Modal>
{/if}

<style>
  form {
    margin-top: 12px;
  }

  input {
    width: 100%;
    letter-spacing: 0.35em;
    text-align: center;
    font-size: 1.2rem;
  }
</style>
