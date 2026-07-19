<script>
  import { apiGet } from '../lib/api.js';
  import { formatDate, summarizeData } from '../lib/format.js';
  import EntityLink from '../components/EntityLink.svelte';
  import AccessDeniedNotice from '../components/AccessDeniedNotice.svelte';
  import { isPermissionDeniedError } from '../lib/permissions.js';
  import { openPlayer, openItem } from '../lib/session.js';
  import { t } from '../lib/i18n.js';

  let roomId = '';
  let data = null;
  let error = '';
  let forbidden = false;

  async function load() {
    if (!roomId.trim()) {
      return;
    }

    forbidden = false;
    error = '';

    try {
      data = await apiGet(`/api/v1/directory/rooms/${encodeURIComponent(roomId.trim())}?limit=120`);
    } catch (err) {
      if (isPermissionDeniedError(err)) {
        forbidden = true;
        error = '';
        data = null;
        return;
      }

      error = err.message;
      data = null;
    }
  }
</script>

<section class="panel">
  <div class="panel-head">
    <h2>{$t('roomsTimeline.title')}</h2>
    <button type="button" on:click={load}>{$t('common.refresh')}</button>
  </div>
  <form class="toolbar" on:submit|preventDefault={load}>
    <input bind:value={roomId} placeholder={$t('roomsTimeline.roomIdPlaceholder')} />
    <button type="submit">{$t('roomsTimeline.inspect')}</button>
  </form>

  {#if forbidden}
    <AccessDeniedNotice message={$t('roomsTimeline.accessDenied')} />
  {:else if error}
    <p class="empty-state danger">{error}</p>
  {/if}

  {#if data?.room}
    <div class="room-summary">
      <strong>{data.room.name || data.room.roomName} #{data.room.roomId || data.room.id}</strong>
      <span>{data.room.usersNow ?? data.room.roomUsersNow}/{data.room.playersMax ?? data.room.roomPlayersMax} {$t('roomsTimeline.players')}</span>
      <span>{data.room.modelName || data.room.roomModelName}</span>
      <EntityLink id={data.room.roomOwnerId || data.room.ownerPlayerId} label={data.room.roomOwnerName || ''} {openPlayer} {openItem} />
    </div>
  {/if}

  <table>
    <thead><tr><th>{$t('roomsTimeline.colTime')}</th><th>{$t('roomsTimeline.colEvent')}</th><th>{$t('roomsTimeline.colActor')}</th><th>{$t('roomsTimeline.colTarget')}</th><th>{$t('roomsTimeline.colMessage')}</th></tr></thead>
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
        <tr><td colspan="5" class="muted">{$t('roomsTimeline.noTimeline')}</td></tr>
      {/each}
    </tbody>
  </table>
</section>
