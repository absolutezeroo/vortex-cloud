<script>
  import { onMount } from 'svelte';
  import { apiGet, apiPost } from '../lib/api.js';
  import { isPermissionDeniedError, hasDashboardCapability } from '../lib/permissions.js';
  import { formatDate, compactCorrelation } from '../lib/format.js';
  import { CAPABILITIES } from '../lib/dashboardPermissions.js';
  import { reasonOk } from '../lib/validation.js';
  import AccessDeniedNotice from '../components/AccessDeniedNotice.svelte';
  import EntityLink from '../components/EntityLink.svelte';
  import { identity, openPlayer, openItem } from '../lib/session.js';

  let loading = false;
  let forbidden = false;
  let error = '';
  let rooms = [];

  // Expanded room id -> occupant list / loading state.
  let expanded = null;
  let occupants = [];
  let occupantsLoading = false;
  let occupantsError = '';

  // The one in-flight/last-confirmed action. Kept open on error so the operator sees why it
  // failed; cleared on success (the room/occupant list refresh is the success feedback).
  let pending = null;
  let lastResult = null;

  $: canManage = hasDashboardCapability($identity, CAPABILITIES.opsRoomsManage);

  function roomId(room) {
    return room.roomId ?? room.RoomId;
  }

  function roomName(room) {
    return room.name ?? room.Name ?? `room #${roomId(room)}`;
  }

  function roomOwnerName(room) {
    return room.ownerName ?? room.OwnerName ?? '';
  }

  function roomOwnerId(room) {
    return room.ownerId ?? room.OwnerId;
  }

  function roomPopulation(room) {
    return room.population ?? room.Population ?? 0;
  }

  function roomUpdatedAt(room) {
    return room.lastUpdatedUtc ?? room.LastUpdatedUtc;
  }

  function occupantId(occupant) {
    return occupant.playerId ?? occupant.PlayerId;
  }

  function occupantName(occupant) {
    return occupant.name ?? occupant.Name;
  }

  async function refresh() {
    loading = true;
    error = '';
    forbidden = false;

    try {
      rooms = await apiGet('/api/v1/directory/rooms/active');
    } catch (err) {
      if (isPermissionDeniedError(err)) {
        forbidden = true;
        rooms = [];
        return;
      }

      error = err.message;
      rooms = [];
    } finally {
      loading = false;
    }
  }

  async function refreshOccupants(id) {
    occupantsLoading = true;
    occupantsError = '';

    try {
      occupants = await apiGet(`/api/v1/directory/rooms/${id}/occupants`);
    } catch (err) {
      occupantsError = isPermissionDeniedError(err) ? 'Droits insuffisants.' : err.code || err.message;
    } finally {
      occupantsLoading = false;
    }
  }

  async function toggleExpand(id) {
    if (expanded === id) {
      expanded = null;
      occupants = [];
      return;
    }

    expanded = id;
    occupants = [];
    await refreshOccupants(id);
  }

  function stageClose(room) {
    lastResult = null;
    pending = {
      kind: 'close',
      roomId: roomId(room),
      reasonInput: '',
      busy: false,
      error: '',
      summary: `Deactivate ${roomName(room)} (#${roomId(room)}). Does not evict occupants by itself.`,
    };
  }

  function stageKick(occupant, forRoomId) {
    lastResult = null;
    pending = {
      kind: 'kick',
      roomId: forRoomId,
      playerId: occupantId(occupant),
      reasonInput: '',
      busy: false,
      error: '',
      summary: `Remove ${occupantName(occupant)} (#${occupantId(occupant)}) from room #${forRoomId}.`,
    };
  }

  function cancel() {
    pending = null;
  }

  async function confirm() {
    if (!pending) {
      return;
    }

    if (!reasonOk(pending.reasonInput)) {
      pending = { ...pending, error: 'Reason needs at least 3 characters.' };
      return;
    }

    pending = { ...pending, busy: true, error: '' };

    const { kind, roomId: rid, playerId, reasonInput } = pending;

    try {
      const result =
        kind === 'close'
          ? await apiPost('/api/v1/operations/rooms/close', { roomId: rid, reason: reasonInput.trim() })
          : await apiPost('/api/v1/operations/rooms/kick', { roomId: rid, playerId, reason: reasonInput.trim() });

      if (!result.ok) {
        pending = { ...pending, busy: false, error: result.message };
        return;
      }

      lastResult = result;
      pending = null;

      if (kind === 'close') {
        await refresh();
      } else {
        await refreshOccupants(rid);
      }
    } catch (err) {
      pending = {
        ...pending,
        busy: false,
        error: isPermissionDeniedError(err) ? 'Droits insuffisants.' : err.code || err.message,
      };
    }
  }

  onMount(() => {
    void refresh();
  });
</script>

<section class="panel">
  <div class="panel-head">
    <h2>Room control</h2>
    <button type="button" class="ghost-button" on:click={refresh} disabled={loading}>Refresh</button>
  </div>
  <p class="muted">
    Currently active rooms. For historical room activity/timeline lookups by id, see Room
    inspector — this page is for live occupancy and moderation actions only.
  </p>

  {#if loading}
    <p class="muted">Loading active rooms...</p>
  {:else if forbidden}
    <AccessDeniedNotice message="Vous n'avez pas l'autorisation de gérer les rooms." />
  {:else if error}
    <p class="empty-state danger">{error}</p>
  {/if}

  {#if lastResult}
    <p class="op-result" class:danger={!lastResult.ok}>
      {lastResult.ok ? '✅' : '❌'} {lastResult.message} - cid
      <code>{compactCorrelation(lastResult.correlationId)}</code>
    </p>
  {/if}

  <table>
    <thead>
      <tr>
        <th>Room</th>
        <th>Owner</th>
        <th>Population</th>
        <th>Last updated</th>
        <th>Actions</th>
      </tr>
    </thead>
    <tbody>
      {#each rooms as room (roomId(room))}
        <tr>
          <td>
            <button class="ghost-button" type="button" on:click={() => toggleExpand(roomId(room))}>
              {expanded === roomId(room) ? '▾' : '▸'} {roomName(room)} <small>#{roomId(room)}</small>
            </button>
          </td>
          <td><EntityLink id={roomOwnerId(room)} label={roomOwnerName(room)} {openPlayer} {openItem} /></td>
          <td>{roomPopulation(room)}</td>
          <td>{formatDate(roomUpdatedAt(room))}</td>
          <td>
            {#if canManage}
              <button type="button" on:click={() => stageClose(room)}>Force-close</button>
            {:else}
              <span class="muted">read-only</span>
            {/if}
          </td>
        </tr>
        {#if expanded === roomId(room)}
          <tr>
            <td colspan="5">
              {#if occupantsLoading}
                <p class="muted">Loading occupants...</p>
              {:else if occupantsError}
                <p class="empty-state danger">{occupantsError}</p>
              {:else}
                <table>
                  <thead><tr><th>Player</th><th>Actions</th></tr></thead>
                  <tbody>
                    {#each occupants as occupant (occupantId(occupant))}
                      <tr>
                        <td>
                          <EntityLink id={occupantId(occupant)} label={occupantName(occupant)} {openPlayer} {openItem} />
                        </td>
                        <td>
                          {#if canManage}
                            <button type="button" class="ghost-button" on:click={() => stageKick(occupant, roomId(room))}>Kick</button>
                          {/if}
                        </td>
                      </tr>
                    {:else}
                      <tr><td colspan="2" class="muted">No occupants.</td></tr>
                    {/each}
                  </tbody>
                </table>
              {/if}
            </td>
          </tr>
        {/if}
      {:else}
        <tr><td colspan="5" class="muted">No active rooms.</td></tr>
      {/each}
    </tbody>
  </table>
</section>

{#if pending}
  <div class="modal-layer">
    <button class="modal-backdrop" type="button" aria-label="Cancel" on:click={cancel}></button>
    <section class="modal-panel" role="dialog" aria-modal="true" style="width: min(460px, 100%)">
      <header class="modal-header">
        <div>
          <p class="eyebrow">Confirm room action</p>
          <h2>{pending.kind === 'close' ? 'Force-close room' : 'Kick from room'}</h2>
        </div>
      </header>
      <p>{pending.summary}</p>
      <div class="op-field">
        <label for="room-action-reason">Reason *</label>
        <input id="room-action-reason" bind:value={pending.reasonInput} placeholder="why this action?" />
      </div>
      {#if pending.error}<p class="empty-state danger">{pending.error}</p>{/if}
      <div class="op-actions">
        <button type="button" on:click={confirm} disabled={pending.busy}>Confirm</button>
        <button class="ghost-button" type="button" on:click={cancel}>Cancel</button>
      </div>
    </section>
  </div>
{/if}
