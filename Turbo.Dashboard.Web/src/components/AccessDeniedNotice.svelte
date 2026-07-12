<script>
  import { t } from '../lib/i18n.js';

  export let title = '';
  export let message = '';
</script>

<section class="access-denied-notice" role="status" aria-live="polite">
  <p class="notice-title">{title || $t('common.accessDeniedTitle')}</p>
  <p>{message || $t('common.accessDeniedDefaultMessage')}</p>
</section>

<style>
  .access-denied-notice {
    border: 1px solid var(--danger-border);
    border-radius: 14px;
    background: var(--danger-bg);
    padding: 16px;
    color: var(--danger);
    display: grid;
    gap: 6px;
  }

  .notice-title {
    margin: 0;
    font-weight: 700;
  }

  .access-denied-notice p {
    margin: 0;
    color: var(--danger);
  }
</style>
