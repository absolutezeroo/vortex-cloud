<script>
  import { apiGet } from '../lib/api.js';
  import { formatDate, summarizeData } from '../lib/format.js';
  import EntityLink from '../components/EntityLink.svelte';

  export let openPlayer;
  export let openItem;

  let roomId = '';
  let data = null;
  let error = '';

  async function load() {
    if (!roomId.trim()) {
      return;
    }

    try {
      error = '';
      data = await apiGet(`/api/room/${encodeURIComponent(roomId.trim())}?limit=120`);
    } catch (err) {
      error = err.message;
      data = null;
    }
  }
</script>

<section class="panel">
  <div class="panel-head">
    <h2>Room timeline</h2>
    <button type="button" on:click={load}>Refresh</button>
  </div>
  <form class="toolbar" on:submit|preventDefault={load}>
    <input bind:value={roomId} placeholder="room id" />
    <button type="submit">Inspect</button>
  </form>

  {#if error}<p class="empty-state danger">{error}</p>{/if}

  {#if data?.room}
    <div class="room-summary">
      <strong>{data.room.name || data.room.roomName} #{data.room.roomId || data.room.id}</strong>
      <span>{data.room.usersNow ?? data.room.roomUsersNow}/{data.room.playersMax ?? data.room.roomPlayersMax} players</span>
      <span>{data.room.modelName || data.room.roomModelName}</span>
      <EntityLink id={data.room.roomOwnerId || data.room.ownerPlayerId} label={data.room.roomOwnerName || ''} {openPlayer} {openItem} />
    </div>
  {/if}

  <table>
    <thead><tr><th>Time</th><th>Event</th><th>Actor</th><th>Target</th><th>Message</th></tr></thead>
    <tbody>
      {#each data?.timeline || [] as row}
        <tr>
          <td>{formatDate(row.createdAt)}</td>
          <td><span class={`event-${row.eventType}`}>{row.eventType}</span></td>
          <td><EntityLink id={row.playerId} label={row.playerName || ''} {openPlayer} {openItem} /></td>
          <td><EntityLink id={row.targetPlayerId} label={row.targetPlayerName || ''} {openPlayer} {openItem} /></td>
          <td>
            {#if row.itemId}
              <EntityLink type="item" id={row.itemId} label={`item #${row.itemId}`} {openPlayer} {openItem} />
            {/if}
            {summarizeData(row.message)}
          </td>
        </tr>
      {:else}
        <tr><td colspan="5" class="muted">No room timeline loaded.</td></tr>
      {/each}
    </tbody>
  </table>
</section>
