<script lang="ts">
  /**
   * A slot's campaign schedule: "from this moment, show this one".
   *
   * The client never resolves this itself. It sends the whole string to the server and renders
   * whatever code comes back, and the server answers with the latest entry whose date has passed.
   * That rule is repeated here -- the row marked live is the one a player is seeing right now -- so
   * an operator does not have to run the hotel to find out which of six dated entries is active.
   *
   * The dates are local, not UTC: they are compared against the server's own clock.
   */
  import type { HotelViewScheduleEntry } from '../../lib/apiTypes';
  import { t } from '../../lib/i18n';
  import Button from '../Button.svelte';
  import Chip from '../Chip.svelte';
  import { Plus, Trash2 } from '@lucide/svelte';

  type Props = {
    schedule: HotelViewScheduleEntry[];
    /** Campaign codes that exist, offered as suggestions; a code may still be typed freely. */
    codes: string[];
    disabled?: boolean;
    onchange: (schedule: HotelViewScheduleEntry[]) => void;
  };

  let { schedule, codes, disabled = false, onchange }: Props = $props();

  const listId = `hotelview-codes-${Math.random().toString(36).slice(2, 8)}`;

  /**
   * Which entry is live, by the server's rule: the latest start that has already passed.
   *
   * Entries whose date does not parse are skipped rather than aborting the schedule, which is also
   * what the handler does -- showing an operator a schedule the server reads differently would be
   * worse than showing none.
   */
  let liveIndex = $derived.by(() => {
    const now = Date.now();
    let best = -1;
    let latest = -Infinity;

    schedule.forEach((entry, index) => {
      const at = Date.parse(entry.startsAt.replace(' ', 'T'));
      if (Number.isNaN(at) || at > now || at < latest) return;
      latest = at;
      best = index;
    });

    return best;
  });

  function replace(index: number, next: HotelViewScheduleEntry) {
    onchange(schedule.map((entry, i) => (i === index ? next : entry)));
  }

  const sorted = () =>
    onchange([...schedule].sort((a, b) => a.startsAt.localeCompare(b.startsAt)));
</script>

<div class="schedule">
  <datalist id={listId}>
    {#each codes as code (code)}<option value={code}></option>{/each}
  </datalist>

  {#each schedule as entry, index (index)}
    <div class="hv-row" class:live={index === liveIndex}>
      <input
        type="datetime-local"
        value={entry.startsAt.replace(' ', 'T')}
        {disabled}
        aria-label={$t('hotelView.startsAt')}
        oninput={(e) =>
          replace(index, { ...entry, startsAt: e.currentTarget.value.replace('T', ' ').slice(0, 16) })}
      />

      <input
        type="text"
        list={listId}
        value={entry.code}
        {disabled}
        placeholder={$t('hotelView.codePlaceholder')}
        aria-label={$t('hotelView.campaignCode')}
        oninput={(e) => replace(index, { ...entry, code: e.currentTarget.value.trim() })}
      />

      {#if index === liveIndex}
        <Chip label={$t('hotelView.live')} tone="success" />
      {:else if entry.code === ''}
        <Chip label={$t('hotelView.switchesOff')} tone="warning" />
      {:else}
        <span class="spacer"></span>
      {/if}

      <Button
        variant="danger"
        size="sm"
        {disabled}
        ariaLabel={$t('common.delete')}
        onclick={() => onchange(schedule.filter((_, i) => i !== index))}><Trash2 size={14} /></Button
      >
    </div>
  {/each}

  <div class="actions">
    <Button
      variant="ghost"
      size="sm"
      {disabled}
      onclick={() => onchange([...schedule, { startsAt: '', code: '' }])}
    >
      <Plus size={14} />{$t('hotelView.addSchedule')}
    </Button>
    <Button variant="ghost" size="sm" disabled={disabled || schedule.length < 2} onclick={sorted}>
      {$t('hotelView.sortByDate')}
    </Button>
  </div>

  {#if schedule.length > 0 && liveIndex === -1}
    <p class="note">{$t('hotelView.nothingLiveYet')}</p>
  {/if}
</div>

<style>
  .schedule {
    display: flex;
    flex-direction: column;
    gap: 0.4rem;
  }

  /* `hv-row`, not `row`: `.row` is a global class and Svelte's scoping does not stop a global rule
     from matching an element that also carries the scoped class. */
  .hv-row {
    display: grid;
    grid-template-columns: minmax(150px, 1fr) minmax(110px, 1fr) auto auto;
    gap: 0.4rem;
    align-items: center;
    padding: 0.25rem 0.35rem;
    border-radius: 6px;
    border: 1px solid transparent;
  }

  .hv-row.live {
    border-color: var(--success-border);
    background: var(--success-bg);
  }

  .spacer {
    display: block;
  }

  .actions {
    display: flex;
    gap: 0.5rem;
  }

  .note {
    margin: 0.2rem 0 0;
    font-size: 0.78rem;
    color: var(--muted);
  }

  @media (max-width: 620px) {
    .hv-row {
      grid-template-columns: 1fr auto;
    }
  }
</style>
