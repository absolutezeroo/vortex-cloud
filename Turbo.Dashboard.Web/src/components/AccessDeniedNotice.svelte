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
    border: 1px solid rgba(223, 111, 123, 0.34);
    border-radius: 14px;
    background: rgba(223, 111, 123, 0.08);
    padding: 16px;
    color: #f6d2d6;
    display: grid;
    gap: 6px;
  }

  .notice-title {
    margin: 0;
    font-weight: 700;
  }

  .access-denied-notice p {
    margin: 0;
    color: #f7d8dc;
  }
</style>
