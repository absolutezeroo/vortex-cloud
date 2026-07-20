<script>
  // A currency amount rendered as "sprite + tabular count" — the Habbo purse look, used wherever a
  // price or balance appears. When no currency sprite URL is supplied it falls back to a gold coin
  // glyph (we never fabricate asset URLs; a real sprite can be wired via the `src` prop once a
  // currency-icon template is configured server-side). Numerals use the condensed counter face.
  import { Coins } from '@lucide/svelte';
  import { formatNumber } from '../lib/format.js';

  export let amount = 0;
  export let src = null; // optional currency sprite URL (coins / duckets / diamonds…)
  export let alt = '';
  export let decimals = 0;
  export let suffix = ''; // e.g. "c" for credits, or a currency short name
</script>

<span class="coin">
  {#if src}
    <img {src} {alt} loading="lazy" />
  {:else}
    <Coins size={15} strokeWidth={2} color="var(--gold)" aria-hidden="true" />
  {/if}
  <span>{formatNumber(amount, decimals)}{suffix}</span>
</span>
