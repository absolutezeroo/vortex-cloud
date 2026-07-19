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
  import { t, translate } from '../lib/i18n.js';

  let query = '';
  let rows = [];
  let player = null;
  let state = '';
  $: if (!state) state = translate('investigation.hint');
  let error = '';
  let forbidden = false;

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
        state = translate('investigation.eventsForPlayer', { count: nextRows.length, term });
      } else if (data.kind === 'correlationId') {
        (data.audit || []).forEach((row) => push(nextRows, { kind: 'audit', ...row }));
        (data.ledger || []).forEach((row) => push(nextRows, { kind: 'ledger', ...row }));
        (data.items || []).forEach((row) => push(nextRows, { kind: 'item', ...row }));
        state = translate('investigation.linkedEventsFor', { count: nextRows.length, term });
      } else {
        state = data.hint || translate('investigation.noStructuredResult');
      }

      rows = nextRows.sort((left, right) => right.sortTime - left.sortTime);
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
    <button type="button" on:click={search}>{$t('investigation.search')}</button>
  </div>
  <form class="toolbar" on:submit|preventDefault={search}>
    <input bind:value={query} placeholder={$t('investigation.searchPlaceholder')} />
    <button type="submit">{$t('investigation.load')}</button>
  </form>
  <p class="muted">{state}</p>

  {#if forbidden}
    <AccessDeniedNotice message={$t('investigation.accessDenied')} />
  {:else if error}
    <p class="empty-state danger">{error}</p>
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
  {/if}

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
</section>

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
