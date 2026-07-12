<script>
  export let title = 'Droits insuffisants';
  export let message =
    'Vous n\'avez pas les permissions requises pour afficher cette section.';
</script>

<section class="access-denied-notice" role="status" aria-live="polite">
  <p class="notice-title">{title}</p>
  <p>{message}</p>
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
