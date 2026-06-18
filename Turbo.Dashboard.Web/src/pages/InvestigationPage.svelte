<script>
  import { onMount } from 'svelte';
  import { apiGet } from '../lib/api.js';
  import { compactCorrelation, formatDate, summarizeData } from '../lib/format.js';
  import EntityLink from '../components/EntityLink.svelte';
  import AccessDeniedNotice from '../components/AccessDeniedNotice.svelte';
  import { isPermissionDeniedError } from '../lib/permissions.js';
  import { openPlayer, openItem } from '../lib/session.js';

  let query = '';
  let rows = [];
  let state = 'Enter a player id, item id or correlation id.';
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
    try {
      const data = await apiGet(`/api/v1/directory/search?q=${encodeURIComponent(term)}`);
      const nextRows = [];

      if (data.kind === 'id') {
        (data.asActor || []).forEach((row) => push(nextRows, { kind: 'audit', ...row }));
        (data.ledger || []).forEach((row) => push(nextRows, { kind: 'ledger', playerId: term, ...row }));
        (data.itemHistory || []).forEach((row) => push(nextRows, { kind: 'item', ...row }));
        state = `${nextRows.length} events for player ${term}`;
      } else if (data.kind === 'correlationId') {
        (data.audit || []).forEach((row) => push(nextRows, { kind: 'audit', ...row }));
        (data.ledger || []).forEach((row) => push(nextRows, { kind: 'ledger', ...row }));
        (data.items || []).forEach((row) => push(nextRows, { kind: 'item', ...row }));
        state = `${nextRows.length} linked events for ${term}`;
      } else {
        state = data.hint || 'No structured result.';
      }

      rows = nextRows.sort((left, right) => right.sortTime - left.sortTime);
    } catch (err) {
      if (isPermissionDeniedError(err)) {
        forbidden = true;
        rows = [];
        error = '';
        return;
      }

      error = err.message;
      rows = [];
    }
  }
</script>

<section class="panel">
  <div class="panel-head">
    <h2>Timeline search</h2>
    <button type="button" on:click={search}>Search</button>
  </div>
  <form class="toolbar" on:submit|preventDefault={search}>
    <input bind:value={query} placeholder="player id / item id / correlation id" />
    <button type="submit">Load</button>
  </form>
  <p class="muted">{state}</p>

  {#if forbidden}
    <AccessDeniedNotice message="Vous n'avez pas l'autorisation d'exécuter des recherches d'investigation." />
  {:else if error}
    <p class="empty-state danger">{error}</p>
  {/if}

  <table>
    <thead><tr><th>Time</th><th>Type</th><th>Actor</th><th>Details</th></tr></thead>
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
            {row.category || row.eventType || row.action || row.currency || 'event'}
            {#if row.itemId}
              - <EntityLink type="item" id={row.itemId} label={`item #${row.itemId}`} {openPlayer} {openItem} />
            {/if}
            <span class="muted">{compactCorrelation(row.correlationId)} {summarizeData(row.data || row.Data)}</span>
          </td>
        </tr>
      {:else}
        <tr><td colspan="4" class="muted">No timeline rows.</td></tr>
      {/each}
    </tbody>
  </table>
</section>
