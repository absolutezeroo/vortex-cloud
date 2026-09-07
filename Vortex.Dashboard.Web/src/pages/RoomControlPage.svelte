<script lang="ts">
  import OpResult from '../components/OpResult.svelte';
  import { onMount } from 'svelte';
  import { apiGet, describeApiError } from '../lib/api';
  import { createWriteOps } from '../lib/writeOps';
  import { isPermissionDeniedError, hasDashboardCapability } from '../lib/permissions';
  import { formatDate } from '../lib/format';
  import { CAPABILITIES } from '../lib/dashboardPermissions';
  import { ChevronDown, ChevronRight } from '@lucide/svelte';
  import AccessDeniedNotice from '../components/AccessDeniedNotice.svelte';
  import ConfirmReasonModal from '../components/ConfirmReasonModal.svelte';
  import EntityLink from '../components/EntityLink.svelte';
  import { identity, openPlayer, openItem } from '../lib/session';
  import TableFilter from '../components/TableFilter.svelte';
  // Filter only: the rows are read through accessors (roomName/roomPopulation), so there are no
  // stable field names for a key-based sort to name.
  import { filterRows } from '../lib/tableView';
  import { t, translate } from '../lib/i18n';
  import type { RoomOccupantSnapshot, RoomSummaryDto } from '../lib/apiTypes';

  let loading = $state(false);
  let forbidden = $state(false);
  let error = $state('');
  let rooms = $state<RoomSummaryDto[]>([]);

  let roomQuery = $state('');
  let roomView = $derived(filterRows(rooms, roomQuery));

  // Expanded room id -> occupant list / loading state.
  let expanded = $state<number | null>(null);
  let occupants = $state<RoomOccupantSnapshot[]>([]);
  let occupantsLoading = $state(false);
  let occupantsError = $state('');

  // The one in-flight/last-confirmed action, staged through the shared reason modal: it stays open
  // on error so the operator sees why it failed and closes on success (the room/occupant list
  // refresh is the success feedback). Each staged action carries its own refresh as `onSuccess`,
  // since closing a room reloads the room list while a kick only reloads that room's occupants.
  const ops = createWriteOps();

  let canManage = $derived(hasDashboardCapability($identity, CAPABILITIES.opsRoomsManage));

  async function refresh() {
    loading = true;
    error = '';
    forbidden = false;

    try {
      rooms = await apiGet<RoomSummaryDto[]>('/api/v1/directory/rooms/active');
    } catch (err) {
      if (isPermissionDeniedError(err)) {
        forbidden = true;
        rooms = [];
        return;
      }

      error = (err as Error).message;
      rooms = [];
    } finally {
      loading = false;
    }
  }

  async function refreshOccupants(id: number) {
    occupantsLoading = true;
    occupantsError = '';

    try {
      occupants = await apiGet<RoomOccupantSnapshot[]>(
        `/api/v1/directory/rooms/${id}/occupants`,
      );
    } catch (err) {
      occupantsError = isPermissionDeniedError(err)
        ? translate('common.insufficientRights')
        : describeApiError(err);
    } finally {
      occupantsLoading = false;
    }
  }

  async function toggleExpand(id: number) {
    if (expanded === id) {
      expanded = null;
      occupants = [];
      return;
    }

    expanded = id;
    occupants = [];
    await refreshOccupants(id);
  }

  function stageClose(room: RoomSummaryDto) {
    ops.ask(
      '/api/v1/operations/rooms/close',
      { roomId: room.roomId },
      translate('roomControl.forceCloseRoom'),
      translate('roomControl.deactivateSummary', { room: room.name, id: room.roomId }),
      { danger: true, onSuccess: refresh },
    );
  }

  function stageKick(occupant: RoomOccupantSnapshot, forRoomId: number) {
    ops.ask(
      '/api/v1/operations/rooms/kick',
      { roomId: forRoomId, playerId: occupant.playerId },
      translate('roomControl.kickFromRoom'),
      translate('roomControl.removeSummary', {
        occupant: occupant.name,
        id: occupant.playerId,
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
      <button type="button" class="warning" onclick={refresh} disabled={loading}>{$t('common.refresh')}</button>
  </div>
  <p class="muted">
    {$t('roomControl.description')}
  </p>

  {#if loading}
    <p class="muted">{$t('roomControl.loadingRooms')}</p>
  {:else if forbidden}
    <AccessDeniedNotice message={$t('roomControl.accessDenied')} />
  {:else if error}
    <p class="empty-state danger" role="alert">{error}</p>
  {/if}

  {#if $ops.result}
    <OpResult result={$ops.result} />
  {/if}

</section>

<section class="panel">
  <TableFilter bind:query={roomQuery} shown={roomView.length} total={rooms.length} />

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
      {#each roomView as room (room.roomId)}
        <tr>
          <td>
            <button class="ghost-button" type="button" onclick={() => toggleExpand(room.roomId)}>
              {#if expanded === room.roomId}
                <ChevronDown size={15} strokeWidth={2} aria-hidden="true" />
              {:else}
                <ChevronRight size={15} strokeWidth={2} aria-hidden="true" />
              {/if}
              {room.name} <small>#{room.roomId}</small>
            </button>
          </td>
          <td><EntityLink id={room.ownerId} label={room.ownerName} {openPlayer} {openItem} /></td>
          <td>{room.population}</td>
          <td>{formatDate(room.lastUpdatedUtc)}</td>
          <td>
            {#if canManage}
              <button type="button" onclick={() => stageClose(room)}>{$t('roomControl.forceClose')}</button>
            {:else}
              <span class="muted">{$t('roomControl.readOnly')}</span>
            {/if}
          </td>
        </tr>
        {#if expanded === room.roomId}
          <tr>
            <td colspan="5">
              {#if occupantsLoading}
                <p class="muted">{$t('roomControl.loadingOccupants')}</p>
              {:else if occupantsError}
                <p class="empty-state danger" role="alert">{occupantsError}</p>
              {:else}
                <table>
                  <thead><tr><th>{$t('roomControl.colPlayer')}</th><th>{$t('roomControl.colActions')}</th></tr></thead>
                  <tbody>
                    {#each occupants as occupant (occupant.playerId)}
                      <tr>
                        <td>
                          <EntityLink id={occupant.playerId} label={occupant.name} {openPlayer} {openItem} />
                        </td>
                        <td>
                          {#if canManage}
                            <button type="button" class="ghost-button" onclick={() => stageKick(occupant, room.roomId)}>{$t('roomControl.kick')}</button>
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
