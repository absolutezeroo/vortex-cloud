<script>
  // The actions you can drop on the canvas, grouped and searchable.
  //
  // Sixty-seven of them now, which is exactly why this is a searchable palette and not a select:
  // an operator looking for "the one about guilds" scans a group, and one who knows the name types
  // it. The counts are there so a group that is empty for this hotel says so.
  import { t } from '../../lib/i18n.js';

  /** @type {{ actions: any[], canManage: boolean, onadd: (action: string) => void }} */
  let { actions, canManage, onadd } = $props();

  let query = $state('');

  // The families the vocabulary already falls into. An action that matches none lands in "other"
  // rather than being hidden, because a palette that silently omits something is worse than an
  // untidy group.
  const GROUPS = [
    { key: 'rooms', match: /room|walk_on|doorbell|rate_/ },
    { key: 'items', match: /item|furni|present|mystery/ },
    { key: 'social', match: /friend|respect|messenger|chat|dance|wave|group|forum/ },
    { key: 'economy', match: /buy|spend|marketplace|club|raffle|voucher|offer|trade/ },
    { key: 'collectibles', match: /mint|nft|vault|habbicon/ },
    { key: 'progression', match: /quest|achievement|poll|quiz|task|badge|login|figure|motto|name|effect|outfit|clothing|preference/ },
    { key: 'pets', match: /pet/ },
  ];

  function groupOf(name) {
    return GROUPS.find((g) => g.match.test(name))?.key ?? 'other';
  }

  let grouped = $derived.by(() => {
    const term = query.trim().toLowerCase();
    const matching = actions.filter((a) => !term || a.name.toLowerCase().includes(term));
    const byGroup = new Map();

    for (const action of matching) {
      const key = groupOf(action.name);

      byGroup.set(key, [...(byGroup.get(key) ?? []), action]);
    }

    return [...byGroup.entries()].sort((a, b) => a[0].localeCompare(b[0]));
  });
</script>

<aside class="palette">
  <input
    type="search"
    bind:value={query}
    placeholder={$t('rewardTracks.searchActions')}
    aria-label={$t('rewardTracks.searchActions')}
  />

  <div class="palette-list">
    {#each grouped as [group, items] (group)}
      <div class="palette-group">
        <span class="palette-group-name">{$t(`rewardTracks.group_${group}`)}</span>
        <span class="palette-group-count">{items.length}</span>
      </div>
      {#each items as action (action.name)}
        <button
          type="button"
          class="palette-item"
          class:inert={!action.wired}
          disabled={!canManage}
          title={action.wired ? action.name : $t('rewardTracks.notWiredHint')}
          onclick={() => onadd(action.name)}
        >
          {action.name}
        </button>
      {/each}
    {:else}
      <p class="palette-empty">{$t('rewardTracks.noActionMatches')}</p>
    {/each}
  </div>
</aside>

<style>
  .palette {
    flex: 0 0 232px;
    display: flex;
    flex-direction: column;
    gap: 8px;
    padding: 12px;
    border-right: 2px solid var(--line-strong);
    background: var(--surface);
    overflow: hidden;
  }

  .palette-list {
    flex: 1 1 auto;
    overflow-y: auto;
    display: flex;
    flex-direction: column;
    gap: 1px;
  }

  .palette-group {
    display: flex;
    justify-content: space-between;
    margin-top: 10px;
    padding: 5px 8px;
    border-radius: 4px;
    background: rgba(var(--accent-rgb), 0.22);
    font-size: 0.7rem;
    font-weight: 700;
    letter-spacing: 0.07em;
    text-transform: uppercase;
  }

  .palette-group-count {
    color: var(--muted);
  }

  .palette-item {
    padding: 5px 8px;
    border: 0;
    border-radius: 4px;
    background: transparent;
    color: var(--ink);
    font-size: 0.76rem;
    text-align: left;
    cursor: pointer;
  }

  .palette-item:hover:not(:disabled) {
    background: var(--surface-hover);
  }

  /* An action nothing raises: still offered, because content may already name it, but visibly not
     the same thing as one that works. */
  .inert {
    color: var(--muted);
    font-style: italic;
  }

  .palette-empty {
    margin: 12px 0 0;
    color: var(--muted);
    font-size: 0.76rem;
  }
</style>
