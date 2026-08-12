<script>
  // Shown while a lazily-loaded page chunk is in flight (see lib/routes.js). Deliberately quiet: on a
  // local network the chunk usually arrives in a frame or two, and a full-blown skeleton flashing in
  // and out is more distracting than a thin placeholder.
  import { RefreshCw } from '@lucide/svelte';
  import { t } from '../lib/i18n.js';
</script>

<section class="panel route-loading" aria-live="polite" aria-busy="true">
  <RefreshCw size={16} class="spin" />
  <span>{$t('boot.loadingPage')}</span>
</section>

<style>
  .route-loading {
    display: flex;
    align-items: center;
    gap: 10px;
    color: var(--muted);
  }
</style>
