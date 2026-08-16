<script>
  import OpResult from '../components/OpResult.svelte';
  import { onMount } from 'svelte';
  import { apiGet } from '../lib/api.js';
  import { createWriteOps } from '../lib/writeOps.js';
  import { isPermissionDeniedError, hasDashboardCapability } from '../lib/permissions.js';
  import { formatDate, compactCorrelation } from '../lib/format.js';
  import { CAPABILITIES } from '../lib/dashboardPermissions.js';
  import { ChevronDown, ChevronRight } from '@lucide/svelte';
  import AccessDeniedNotice from '../components/AccessDeniedNotice.svelte';
  import ConfirmReasonModal from '../components/ConfirmReasonModal.svelte';
  import EntityLink from '../components/EntityLink.svelte';
  import { identity, openPlayer, openItem } from '../lib/session.js';
  import { t, translate } from '../lib/i18n.js';

  let loading = $state(false);
  let forbidden = $state(false);
  let error = $state('');
  let rooms = $state([]);

  // Expanded room id -> occupant list / loading state.
  let expanded = $state(null);
  let occupants = $state([]);
  let occupantsLoading = $state(false);
  let occupantsError = $state('');

  // The one in-flight/last-confirmed action, staged through the shared reason modal: it stays open
  // on error so the operator sees why it failed and closes on success (the room/occupant list
  // refresh is the success feedback). Each staged action carries its own refresh as `onSuccess`,
  // since closing a room reloads the room list while a kick only reloads that room's occupants.
  const ops = createWriteOps();

  let canManage = $derived(hasDashboardCapability($identity, CAPABILITIES.opsRoomsManage));

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
      occupantsError = isPermissionDeniedError(err) ? translate('common.insufficientRights') : err.code || err.message;
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
    ops.ask(
      '/api/v1/operations/rooms/close',
      { roomId: roomId(room) },
      translate('roomControl.forceCloseRoom'),
      translate('roomControl.deactivateSummary', { room: roomName(room), id: roomId(room) }),
      { danger: true, onSuccess: refresh },
    );
  }

  function stageKick(occupant, forRoomId) {
    ops.ask(
      '/api/v1/operations/rooms/kick',
      { roomId: forRoomId, playerId: occupantId(occupant) },
      translate('roomControl.kickFromRoom'),
      translate('roomControl.removeSummary', {
        occupant: occupantName(occupant),
        id: occupantId(occupant),
        roomId: forRoomId,
      }),
      { danger: true, onSuccess: () => refreshOccupants(forRoomId) },
    );
  }

  onMount(() => {
    void refresh();
  });
</script>

<section class="panel">
  <div class="panel-head">
    <h2>{$t('roomControl.title')}</h2>
    <button type="button" class="ghost-button" onclick={refresh} disabled={loading}>{$t('common.refresh')}</button>
  </div>
  <p class="muted">
    {$t('roomControl.description')}
  </p>

  {#if loading}
    <p class="muted">{$t('roomControl.loadingRooms')}</p>
  {:else if forbidden}
    <AccessDeniedNotice message={$t('roomControl.accessDenied')} />
  {:else if error}
    <p class="empty-state danger">{error}</p>
  {/if}

  {#if $ops.result}
    <OpResult result={$ops.result} />
  {/if}

  <table>
    <thead>
      <tr>
        <th>{$t('roomControl.colRoom')}</th>
        <th>{$t('roomControl.colOwner')}</th>
        <th>{$t('roomControl.colPopulation')}</th>
        <th>{$t('roomControl.colLastUpdated')}</th>
        <th>{$t('roomControl.colActions')}</th>
      </tr>
    </thead>
    <tbody>
      {#each rooms as room (roomId(room))}
        <tr>
          <td>
            <button class="ghost-button" type="button" onclick={() => toggleExpand(roomId(room))}>
              {#if expanded === roomId(room)}<ChevronDown size={14} strokeWidth={2} aria-hidden="true" />{:else}<ChevronRight size={14} strokeWidth={2} aria-hidden="true" />{/if} {roomName(room)} <small>#{roomId(room)}</small>
            </button>
          </td>
          <td><EntityLink id={roomOwnerId(room)} label={roomOwnerName(room)} {openPlayer} {openItem} /></td>
          <td>{roomPopulation(room)}</td>
          <td>{formatDate(roomUpdatedAt(room))}</td>
          <td>
            {#if canManage}
              <button type="button" onclick={() => stageClose(room)}>{$t('roomControl.forceClose')}</button>
            {:else}
              <span class="muted">{$t('roomControl.readOnly')}</span>
            {/if}
          </td>
        </tr>
        {#if expanded === roomId(room)}
          <tr>
            <td colspan="5">
              {#if occupantsLoading}
                <p class="muted">{$t('roomControl.loadingOccupants')}</p>
              {:else if occupantsError}
                <p class="empty-state danger">{occupantsError}</p>
              {:else}
                <table>
                  <thead><tr><th>{$t('roomControl.colPlayer')}</th><th>{$t('roomControl.colActions')}</th></tr></thead>
                  <tbody>
                    {#each occupants as occupant (occupantId(occupant))}
                      <tr>
                        <td>
                          <EntityLink id={occupantId(occupant)} label={occupantName(occupant)} {openPlayer} {openItem} />
                        </td>
                        <td>
                          {#if canManage}
                            <button type="button" class="ghost-button" onclick={() => stageKick(occupant, roomId(room))}>{$t('roomControl.kick')}</button>
                          {/if}
                        </td>
                      </tr>
                    {:else}
                      <tr><td colspan="2" class="muted">{$t('roomControl.noOccupants')}</td></tr>
                    {/each}
                  </tbody>
                </table>
              {/if}
            </td>
          </tr>
        {/if}
      {:else}
        <tr><td colspan="5" class="muted">{$t('roomControl.noActiveRooms')}</td></tr>
      {/each}
    </tbody>
  </table>
</section>

<ConfirmReasonModal
  open={Boolean($ops.pending)}
  title={$ops.pending?.title ?? ''}
  changes={$ops.pending?.changes ?? []}
  noteOnly={$ops.pending?.noteOnly ?? false}
  summary={$ops.pending?.summary ?? ''}
  confirmLabel={$ops.pending?.title ?? $t('common.confirm')}
  busy={$ops.busy}
  error={$ops.error}
  danger={$ops.pending?.danger ?? false}
  onconfirm={ops.confirm}
  oncancel={() => ops.cancel()}
/>
