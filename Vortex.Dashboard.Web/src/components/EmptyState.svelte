<script>
  // Unified empty / loading / error block. Replaces the historical mix of a bare muted <p>, a
  // dashed box, and a colspan table row. `kind` drives the icon + colour; `message` is the copy.
  // Uses the global .empty-state class so it themes with everything else.
  import { Inbox, LoaderCircle, TriangleAlert } from '@lucide/svelte';

  /**
   * @typedef {Object} Props
   * @property {string} [kind] - 'empty' | 'loading' | 'error'
   * @property {string} [message]
   */

  /** @type {Props} */
  let { kind = 'empty', message = '' } = $props();

  const ICONS = { empty: Inbox, loading: LoaderCircle, error: TriangleAlert };
  let Icon = $derived(ICONS[kind] || Inbox);
</script>

<p class="empty-state" class:danger={kind === 'error'}>
  <span class="es-ico" class:spin={kind === 'loading'}>
    <Icon size={16} strokeWidth={2} aria-hidden="true" />
  </span>
  <span>{message}</span>
</p>

<style>
  .empty-state {
    display: flex;
    align-items: center;
    gap: 8px;
  }

  .es-ico {
    display: inline-flex;
    flex: 0 0 auto;
  }

  .spin {
    animation: es-spin 0.9s linear infinite;
  }

  @keyframes es-spin {
    to {
      transform: rotate(360deg);
    }
  }

  @media (prefers-reduced-motion: reduce) {
    .spin {
      animation: none;
    }
  }
</style>
