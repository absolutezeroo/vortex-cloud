<script>
  // Overlapping avatar heads with a "+N" overflow, per the kit. For "who is in this room" or "who
  // touched this ticket", where the count matters more than any individual face.
  //
  //   <AvatarStack people={occupants} max={5} />     people: [{ id, name, avatarUrl }]
  import AssetImage from './AssetImage.svelte';
  import { User } from '@lucide/svelte';

  /**
   * @typedef {Object} Props
   * @property {Array<{id?: any, name?: string, avatarUrl?: string}>} [people]
   * @property {number} [max] - faces shown before the rest collapse into +N
   * @property {number} [size]
   */

  /** @type {Props} */
  let { people = [], max = 5, size = 28 } = $props();

  let shown = $derived((people || []).slice(0, max));
  let overflow = $derived(Math.max(0, (people || []).length - shown.length));
</script>

<span class="stack" style:--size={`${size}px`}>
  {#each shown as person, index (person.id ?? index)}
    <span class="slot" title={person.name || ''}>
      <AssetImage src={person.avatarUrl} alt={person.name || ''} {size} fallbackIcon={User} />
    </span>
  {/each}
  {#if overflow}
    <span class="slot more" aria-label={`+${overflow}`}>+{overflow}</span>
  {/if}
</span>

<style>
  .stack {
    display: inline-flex;
    align-items: center;
  }

  /* Each face overlaps the one before it and carries a ring in the page colour, which is what makes
     the overlap read as a stack rather than as a rendering mistake. */
  .slot {
    display: inline-flex;
    margin-left: calc(var(--size) / -3);
    border-radius: 8px;
    box-shadow: 0 0 0 2px var(--surface);
  }

  .slot:first-child {
    margin-left: 0;
  }

  .more {
    display: grid;
    place-items: center;
    width: var(--size);
    height: var(--size);
    border-radius: 8px;
    background: var(--surface-hover);
    color: var(--muted-strong);
    font-size: 0.72rem;
    font-weight: 700;
  }
</style>
