<script>
  // Renders whatever lib/toasts.js is holding. Mounted once, in AppShell.
  //
  // aria-live="polite" on the region rather than role="alert" on each toast: a burst of writes
  // finishing at once should be read after the current sentence, not interrupt four times.
  import { CircleCheck, Info, TriangleAlert, CircleX, X } from '@lucide/svelte';
  import { toasts, dismiss } from '../lib/toasts.js';
  import { t } from '../lib/i18n.js';

  const ICONS = { success: CircleCheck, info: Info, warning: TriangleAlert, error: CircleX };
</script>

<div class="toast-host" role="status" aria-live="polite">
  {#each $toasts as entry (entry.id)}
    {@const Icon = ICONS[entry.kind] || Info}
    <div class="toast {entry.kind}">
      <span class="ico"><Icon size={17} strokeWidth={2.2} aria-hidden="true" /></span>
      <span class="body">
        {#if entry.title}<strong>{entry.title}</strong>{/if}
        <span>{entry.message}</span>
      </span>
      <button type="button" onclick={() => dismiss(entry.id)} aria-label={$t('common.close')}>
        <X size={14} strokeWidth={2.4} aria-hidden="true" />
      </button>
    </div>
  {/each}
</div>

<style>
  .toast-host {
    position: fixed;
    z-index: 130;
    top: 16px;
    right: 16px;
    display: grid;
    gap: 8px;
    width: min(380px, calc(100vw - 32px));
    /* The host spans the corner permanently; only the toasts themselves take pointer events, so it
       never swallows a click on the page behind it. */
    pointer-events: none;
  }

  .toast {
    pointer-events: auto;
    display: flex;
    align-items: start;
    gap: 10px;
    border: 1px solid var(--line-strong);
    border-radius: 8px;
    padding: 10px 11px;
    color: var(--ink);
    box-shadow: var(--shadow);
  }

  .toast.success { background: var(--success-bg); border-color: var(--success-border); }
  .toast.info { background: var(--info-bg); border-color: var(--info-border); }
  .toast.warning { background: var(--warning-bg); border-color: var(--warning-border); }
  .toast.error { background: var(--danger-bg); border-color: var(--danger-border); }

  .toast.success .ico { color: var(--ok); }
  .toast.info .ico { color: var(--accent-strong); }
  .toast.warning .ico { color: var(--warning); }
  .toast.error .ico { color: var(--danger); }

  .ico {
    flex: 0 0 auto;
    display: inline-flex;
    padding-top: 1px;
  }

  .body {
    display: grid;
    gap: 2px;
    min-width: 0;
    flex: 1;
    overflow-wrap: anywhere;
  }

  button {
    flex: 0 0 auto;
    display: grid;
    place-items: center;
    width: 20px;
    height: 20px;
    border: 0;
    border-radius: 8px;
    background: transparent;
    color: var(--muted-strong);
    padding: 0;
  }

  button:hover {
    background: rgba(var(--muted-rgb), 0.18);
    color: var(--ink);
  }
</style>
