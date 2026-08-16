<script>
  // A currency amount rendered as "sprite + tabular count" — the Habbo purse look, used wherever a
  // price or balance appears. When no currency sprite URL is supplied it falls back to a gold coin
  // glyph (we never fabricate asset URLs; a real sprite can be wired via the `src` prop once a
  // currency-icon template is configured server-side). Numerals use the condensed counter face.
  import { Coins } from '@lucide/svelte';
  import { formatNumber } from '../lib/format.js';

  /**
   * @typedef {Object} Props
   * @property {number} [amount]
   * @property {any} [src] - optional currency sprite URL (coins / duckets / diamonds…)
   * @property {string} [alt]
   * @property {number} [decimals]
   * @property {string} [suffix] - e.g. "c" for credits, or a currency short name
   */

  /** @type {Props} */
  let {
    amount = 0,
    src = null,
    alt = '',
    decimals = 0,
    suffix = ''
  } = $props();
</script>

<span class="coin">
  {#if src}
    <img {src} {alt} loading="lazy" />
  {:else}
    <Coins size={15} strokeWidth={2} color="var(--gold)" aria-hidden="true" />
  {/if}
  <span>{formatNumber(amount, decimals)}{suffix}</span>
</span>
