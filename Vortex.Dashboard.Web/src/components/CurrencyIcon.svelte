<script>
  // The hotel's own 17x17 currency sprite, shown inside a price or reward pill.
  //
  // Imported rather than fetched from a URL: these live in the front-end source tree, so Vite emits
  // them beside the script and stylesheet with a content hash, the dashboard serves them from its
  // own origin, and there is nothing to configure and nothing to add to the CSP. It also means a
  // missing file is a build error rather than a broken image in production.
  //
  // A currency with no sprite yet falls back to the generic lucide glyph, which is why the chip's
  // colour and its written label both stay: the icon is a third carrier, never the only one.
  import { Coins } from '@lucide/svelte';
  import { CURRENCY_KIND } from '../lib/currency.js';
  import creditIcon from '../assets/images/ui_currency_icon_credit.png';
  import ducketIcon from '../assets/images/ui_currency_icon_ducket.png';
  import diamondIcon from '../assets/images/ui_currency_icon_diamond.png';
  import emeraldIcon from '../assets/images/ui_currency_icon_emerald.png';
  import silverIcon from '../assets/images/ui_currency_icon_silver.png';

  /** The files are named for one coin, the currencies for many -- so the plural maps to the singular. */
  const ICONS = {
    [CURRENCY_KIND.credits]: creditIcon,
    [CURRENCY_KIND.duckets]: ducketIcon,
    [CURRENCY_KIND.diamonds]: diamondIcon,
    [CURRENCY_KIND.emeralds]: emeraldIcon,
    [CURRENCY_KIND.silver]: silverIcon,
  };

  export let kind = CURRENCY_KIND.points;
  /** Drawn at the sprite's own size by default; anything else scales it with pixels kept crisp. */
  export let size = 17;

  $: src = ICONS[kind] ?? null;
</script>

{#if src}
  <img class="currency-icon" {src} alt="" width={size} height={size} aria-hidden="true" />
{:else}
  <Coins size={size - 4} strokeWidth={2} aria-hidden="true" />
{/if}

<style>
  /* Sized in the markup so the layout is right before the image loads, and never stretched by the
     flex row it sits in. Nearest-neighbour keeps a 17px pixel sprite sharp if it is ever scaled. */
  .currency-icon {
    flex: 0 0 auto;
    object-fit: contain;
    image-rendering: pixelated;
  }
</style>
