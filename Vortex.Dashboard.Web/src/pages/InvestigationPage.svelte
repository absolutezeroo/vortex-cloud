<script>

  import { onMount } from 'svelte';
  import { apiGet } from '../lib/api.js';
  import { compactCorrelation, formatDate, summarizeData } from '../lib/format.js';
  import EntityLink from '../components/EntityLink.svelte';
  import AccessDeniedNotice from '../components/AccessDeniedNotice.svelte';
  import AssetImage from '../components/AssetImage.svelte';
  import { User } from '@lucide/svelte';
  import { isPermissionDeniedError } from '../lib/permissions.js';
  import { openPlayer, openItem } from '../lib/session.js';
  import PickerModal from '../components/PickerModal.svelte';
  import PlayerOperationsPanel from '../components/PlayerOperationsPanel.svelte';
  import Tabs from '../components/Tabs.svelte';
  import { Wrench, ScrollText } from '@lucide/svelte';
  import { readParam, writeParams } from '../lib/urlState.js';
  import { t, translate } from '../lib/i18n.js';

  // ?player= makes this page linkable, and is how the command palette hands a player over.
  let query = $state(readParam('player'));
  let rows = $state([]);
  let player = $state(null);
  let summary = $state('');
  $effect(() => {
    if (!summary) summary = translate('investigation.hint');
  });
  let error = $state('');
  let forbidden = $state(false);
  // The search box takes an id or a correlation id; a name is what an operator actually remembers,
  // and the shared picker already knows how to find one. Picking fills the box and runs the search,
  // so both paths land on the same page state.
  let picking = $state(false);
  let active = $state('actions');

  // $derived, not const: translate() reads the locale once, and a const would keep the tab strip in
  // whatever language the page happened to mount in after the operator switches.
  let tabs = $derived([
    { id: 'actions', label: $t('playerOps.tabActions'), icon: Wrench },
    { id: 'timeline', label: $t('investigation.tabTimeline'), icon: ScrollText },
  ]);

  function pick(chosen) {
    query = String(chosen.id);
    search();
  }

  onMount(() => {
    if (query.trim()) search();
  });

  function push(rows, row) {
    rows.push({
      time: row.occurredAt || row.OccurredAt || row.createdAt || row.CreatedAt,
      sortTime: Date.parse(row.occurredAt || row.OccurredAt || row.createdAt || row.CreatedAt || '') || 0,
      ...row,
    });
  }

  async function search() {
    const term = query.trim();
    if (!term) {
      return;
    }

    forbidden = false;
    error = '';
    player = null;
    try {
      const data = await apiGet(`/api/v1/directory/search?q=${encodeURIComponent(term)}`);
      const nextRows = [];

      if (data.kind === 'id') {
        player = data.playerProfile || null;
        (data.asActor || []).forEach((row) => push(nextRows, { kind: 'audit', ...row }));
        (data.ledger || []).forEach((row) => push(nextRows, { kind: 'ledger', playerId: term, ...row }));
        (data.itemHistory || []).forEach((row) => push(nextRows, { kind: 'item', ...row }));
        summary = translate('investigation.eventsForPlayer', { count: nextRows.length, term });
      } else if (data.kind === 'correlationId') {
        (data.audit || []).forEach((row) => push(nextRows, { kind: 'audit', ...row }));
        (data.ledger || []).forEach((row) => push(nextRows, { kind: 'ledger', ...row }));
        (data.items || []).forEach((row) => push(nextRows, { kind: 'item', ...row }));
        summary = translate('investigation.linkedEventsFor', { count: nextRows.length, term });
      } else {
        summary = data.hint || translate('investigation.noStructuredResult');
      }

      rows = nextRows.sort((left, right) => right.sortTime - left.sortTime);
      // Only a player search is worth putting in the URL: a correlation id is a one-off lookup, not
      // a place someone comes back to.
      writeParams({ player: player ? String(player.id) : '' });
    } catch (err) {
      if (isPermissionDeniedError(err)) {
        forbidden = true;
        rows = [];
        player = null;
        error = '';
        return;
      }

      error = err.message;
      rows = [];
      player = null;
    }
  }
</script>

<section class="panel">
  <div class="panel-head">
    <h2>{$t('investigation.title')}</h2>
    <button type="button" onclick={search}>{$t('investigation.search')}</button>
  </div>
  <form class="toolbar" onsubmit={(event) => { event.preventDefault(); search(); }}>
    <input autocomplete="off" spellcheck="false" bind:value={query} placeholder={$t('investigation.searchPlaceholder')} />
    <button type="submit">{$t('investigation.load')}</button>
    <button type="button" class="ghost-button" onclick={() => (picking = true)}>
      {$t('investigation.findByName')}
    </button>
  </form>
  <p class="muted">{summary}</p>

  {#if forbidden}
    <AccessDeniedNotice message={$t('investigation.accessDenied')} />
  {:else if error}
    <p class="empty-state danger" role="alert">{error}</p>
  {/if}

  {#if player}
    <div class="player-headline">
      <AssetImage src={player.avatarUrl} alt={player.name} size={56} fallbackIcon={User} />
      <div class="player-headline-text">
        <strong>{player.name} #{player.id}</strong>
        {#if player.motto}<small class="muted">{player.motto}</small>{/if}
        <small class="muted">{player.status} - {player.gender}</small>
      </div>
    </div>

    <Tabs {tabs} bind:active storageKey="player" />
  {/if}

  {#if player && active === 'actions'}
    <PlayerOperationsPanel
      playerId={player.id}
      playerName={player.name}
      online={player.online}
      onDone={search}
    />
  {/if}

  <!-- With no player loaded there is no tab strip, so the timeline is simply what the page shows. -->
  {#if !player || active === 'timeline'}
    <table>
    <thead><tr><th>{$t('investigation.colTime')}</th><th>{$t('investigation.colType')}</th><th>{$t('investigation.colActor')}</th><th>{$t('investigation.colDetails')}</th></tr></thead>
    <tbody>
      {#each rows as row}
        <tr>
          <td>{formatDate(row.time)}</td>
          <td>{row.kind}</td>
          <td>
            {#if row.kind === 'item' && row.itemId}
              <EntityLink type="item" id={row.itemId} label={`item #${row.itemId}`} {openPlayer} {openItem} />
            {:else}
              <EntityLink id={row.actorPlayerId || row.playerId || row.fromOwnerId || query} label={row.actorPlayerName || row.playerName || row.fromOwnerName || ''} {openPlayer} {openItem} />
            {/if}
          </td>
          <td>
            {row.category || row.eventType || row.action || row.currency || $t('investigation.event')}
            {#if row.itemId}
              - <EntityLink type="item" id={row.itemId} label={`item #${row.itemId}`} {openPlayer} {openItem} />
            {/if}
            <span class="muted">{compactCorrelation(row.correlationId)} {summarizeData(row.data || row.Data)}</span>
          </td>
        </tr>
      {:else}
        <tr><td colspan="4" class="muted">{$t('investigation.noRows')}</td></tr>
      {/each}
    </tbody>
  </table>
  {/if}
</section>

{#if picking}
  <PickerModal
    kind="user"
    title={$t('investigation.findByName')}
    onSelect={pick}
    onClose={() => (picking = false)}
  />
{/if}

<style>
  .player-headline {
    display: flex;
    align-items: center;
    gap: 12px;
    margin: 4px 0 12px;
  }

  .player-headline-text {
    display: grid;
    gap: 2px;
    min-width: 0;
  }
</style>
