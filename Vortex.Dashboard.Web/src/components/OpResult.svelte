<script>
  // The single result line for every mutating action (create/update/delete/ban/kick…). Replaces the
  // block that was copy-pasted ~30 times across the Act pages — including its `✅`/`❌` emoji, the one
  // spot in the app that used OS-native emoji among an otherwise all-lucide icon set. Renders a
  // lucide check/cross tinted by the shared `.op-result` colours, the message, and the short
  // correlation id. Pass `onCopy` to show a "copy correlation id" button. Guards on `result` so
  // callers can drop it in without an outer {#if}.
  import { CircleCheck, CircleX } from '@lucide/svelte';
  import { compactCorrelation } from '../lib/format.js';
  import { describeOpError } from '../lib/opErrors.js';
  import { t } from '../lib/i18n.js';

  /**
   * @typedef {Object} Props
   * @property {any} [result]
   * @property {any} [onCopy] - (correlationId) => void — shows a copy button when provided
   * @property {string} [copyLabel]
   */

  /** @type {Props} */
  let { result = null, onCopy = null, copyLabel = 'Copy' } = $props();

  // On success the server sends the literal "ok", which is not a sentence to show an operator. On a
  // refusal it sends the domain's own code (offer_has_products); both need translating before they
  // reach the screen. Depends on $t so the line re-renders when the locale changes.
  let message = $derived(result
    ? result.ok
      ? $t('common.resultSuccess')
      : describeOpError(result.message)
    : '');
</script>

{#if result}
  <p class="op-result" class:danger={!result.ok}>
    {#if result.ok}
      <CircleCheck size={16} strokeWidth={2} aria-hidden="true" />
    {:else}
      <CircleX size={16} strokeWidth={2} aria-hidden="true" />
    {/if}
    <span class="op-result-message">{message}</span>
    {#if result.correlationId}
      <code class="op-result-cid">cid {compactCorrelation(result.correlationId)}</code>
    {/if}
    {#if onCopy && result.correlationId}
      <button class="ghost-button op-result-copy" type="button" onclick={() => onCopy(result.correlationId)}>{copyLabel}</button>
    {/if}
  </p>
{/if}

<style>
  .op-result-message {
    flex: 1;
    min-width: 0;
  }

  .op-result-cid {
    opacity: 0.8;
  }

  .op-result-copy {
    padding: 4px 9px;
    font-size: 0.78rem;
  }
</style>
