<script lang="ts">
  // The one loading state for the whole dashboard.
  //
  // Every page used to print "Loading…" wherever its markup happened to be, which on a page of stat
  // tiles meant the sentence landed INSIDE the tile row and pushed it around -- the layout jumped
  // on every filter. An overlay does not belong to any one block, so nothing moves: the page it
  // covers stays exactly where the operator left it and comes back unchanged.
  //
  //   <LoadingOverlay show={loading} label={$t('common.loading')} />
  import { t } from '../lib/i18n';

  type Props = {
    show?: boolean;
    /** Defaults to the shared "Loading…" string; pass one when the wait has a name. */
    label?: string;
  };

  let { show = false, label = '' }: Props = $props();
</script>

{#if show}
  <!-- aria-live rather than a role="dialog": this is a status, not something to trap focus in.
       An operator tabbing through a form while it refreshes should keep their place. -->
  <div class="loading-overlay" role="status" aria-live="polite">
    <div class="loading-card">
      <span class="loading-spinner" aria-hidden="true"></span>
      <span>{label || $t('common.loading')}</span>
    </div>
  </div>
{/if}

<style>
  .loading-overlay {
    position: fixed;
    inset: 0;
    z-index: 110;
    display: grid;
    place-items: center;
    background: var(--overlay-veil, rgba(4, 8, 14, 0.55));
    backdrop-filter: blur(2px);
    animation: overlay-in 120ms ease-out;
  }

  .loading-card {
    display: inline-flex;
    align-items: center;
    gap: 12px;
    padding: 14px 18px;
    border: 2px solid var(--line-strong);
    border-radius: 5px;
    background: var(--surface);
    color: var(--ink);
    font-weight: 700;
    text-transform: uppercase;
    font-size: 0.8rem;
    letter-spacing: 0.02em;
    box-shadow: var(--key-drop, 0 10px 30px rgba(0, 0, 0, 0.45));
  }

  .loading-spinner {
    width: 18px;
    height: 18px;
    border-radius: 50%;
    border: 3px solid rgba(var(--accent-rgb), 0.28);
    border-top-color: var(--accent);
    animation: spin 720ms linear infinite;
  }

  @keyframes spin {
    to {
      transform: rotate(360deg);
    }
  }

  @keyframes overlay-in {
    from {
      opacity: 0;
    }
  }

  /* A spinner is decoration; someone who asked the system to stop animating still needs to know the
     page is busy, which the label already says. */
  @media (prefers-reduced-motion: reduce) {
    .loading-spinner {
      animation: none;
    }

    .loading-overlay {
      animation: none;
    }
  }
</style>
