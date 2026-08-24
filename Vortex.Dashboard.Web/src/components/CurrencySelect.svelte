<script>
  // The currencies a reward may actually be paid in, read from the wallet's own table.
  //
  // Quests and achievement levels store their reward currency as a single int: negative means
  // credits, anything else is an activity-point type. Typed by hand that is a guess — nothing on
  // screen says that 5 is diamonds, and nothing says that the number just entered has no
  // `currency_types` row in this hotel, which makes the grant a silent no-op and the reward a
  // promise the player never gets. This is that list. The server refuses the same mistake on save;
  // this is so the operator does not make it.
  //
  // Silver and emeralds are deliberately absent: the reward int has no way to name them, so
  // offering them here would promise a reward the encoding cannot carry.
  import { onMount } from 'svelte';
  import { apiGet } from '../lib/api.js';
  import { isPermissionDeniedError } from '../lib/permissions.js';
  import { currencyKindFromRewardType } from '../lib/currency.js';
  import CurrencyIcon from './CurrencyIcon.svelte';
  import { t } from '../lib/i18n.js';

  /**
   * @typedef {Object} Props
   * @property {string} [id] - Id for the control, so a page's <label for> still points at it.
   * @property {number} [value] - The stored reward type: negative for credits, else the point type.
   * @property {boolean} [credits] - Offer credits. False where the caller grants activity points
   *   only, so that -1 is not a choice the operator can make by accident.
   */

  /** @type {Props} */
  let { id = '', value = $bindable(-1), credits = true } = $props();

  let options = $state([]);
  /** Falls back to the raw number box when the list cannot be read — a narrower capability than
   *  this page's own should not cost the operator the field entirely. */
  let unavailable = $state(false);

  /** The reward int a currency row is named by; null for currencies the encoding cannot express. */
  function rewardTypeFor(row) {
    if (row.type === 'Credits') return -1;
    if (row.type === 'ActivityPoints') return row.activityPointType ?? 0;

    return null;
  }

  onMount(async () => {
    try {
      const data = await apiGet('/api/v1/catalog/currency-types');

      options = (data?.items ?? [])
        .map((row) => ({ row, rewardType: rewardTypeFor(row) }))
        .filter((entry) => entry.rewardType !== null && (credits || entry.rewardType >= 0))
        .map((entry) => ({ value: entry.rewardType, label: entry.row.name || String(entry.rewardType) }));

      unavailable = options.length === 0;
    } catch (err) {
      unavailable = true;

      if (!isPermissionDeniedError(err)) {
        console.warn('currency list unavailable', err);
      }
    }
  });

  let selectedKind = $derived(currencyKindFromRewardType(value));
  /** A stored reward pointing at a currency that is gone: kept visible rather than silently
   *  snapped to another one, because that would rewrite the row on the next save. */
  let missing = $derived(
    !unavailable && options.length > 0 && !options.some((option) => option.value === Number(value))
  );
</script>

{#if unavailable}
  <input autocomplete="off" spellcheck="false" {id} type="number" bind:value />
  <small class="muted">{$t('currency.selectHint')}</small>
{:else}
  <span class="currency-select">
    <CurrencyIcon kind={selectedKind} />
    <select
      {id}
      value={Number(value)}
      onchange={(event) => (value = Number(event.currentTarget.value))}
    >
      {#if missing}
        <option value={Number(value)}>{$t('currency.selectMissing', { type: value })}</option>
      {/if}
      {#each options as option (option.value)}
        <option value={option.value}>{option.label}</option>
      {/each}
    </select>
  </span>
  {#if missing}
    <small class="muted">{$t('currency.selectMissingHint')}</small>
  {/if}
{/if}
