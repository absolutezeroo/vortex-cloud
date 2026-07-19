<script>
  import { apiGet } from '../lib/api.js';
  import { compactCorrelation, formatDate, summarizeData } from '../lib/format.js';
  import EntityLink from './EntityLink.svelte';
  import AccessDeniedNotice from './AccessDeniedNotice.svelte';
  import AssetImage from './AssetImage.svelte';
  import { User } from '@lucide/svelte';
  import { isPermissionDeniedError } from '../lib/permissions.js';
  import { modal, closeModal, openPlayer, openItem } from '../lib/session.js';
  import { t } from '../lib/i18n.js';

  let loading = false;
  let error = '';
  let data = null;
  let currentKey = '';
  let forbidden = false;

  $: key = $modal ? `${$modal.type}:${$modal.id}` : '';
  $: if (key && key !== currentKey) {
    currentKey = key;
    void load();
  }

  async function load() {
    loading = true;
    error = '';
    data = null;
    forbidden = false;

    try {
      data = $modal.type === 'item'
        ? await apiGet(`/api/v1/directory/entity/${encodeURIComponent($modal.id)}`)
        : await apiGet(`/api/v1/directory/search?q=${encodeURIComponent($modal.id)}`);
    } catch (err) {
      if (isPermissionDeniedError(err)) {
        forbidden = true;
        error = '';
        return;
      }

      error = err.message;
    } finally {
      loading = false;
    }
  }

  $: playerProfile = data?.kind === 'id' ? data.playerProfile : null;
  $: itemProfile = $modal?.type === 'item' ? data : null;

  $: forbiddenMessage = $t($modal?.type === 'item' ? 'entityModal.itemAccessDenied' : 'entityModal.playerAccessDenied');
</script>

{#if $modal}
  <div class="modal-layer">
    <button class="modal-backdrop" type="button" aria-label="Close" on:click={closeModal}></button>
    <section class="modal-panel" role="dialog" aria-modal="true">
      <header class="modal-header">
        <div>
          <p class="eyebrow">{$modal.type === 'item' ? $t('entityModal.itemInspector') : $t('entityModal.playerInspector')}</p>
          <h2>{$modal.label || `${$modal.type} #${$modal.id}`}</h2>
        </div>
        <button class="ghost-button" type="button" on:click={closeModal}>{$t('pickerModal.close')}</button>
      </header>

      {#if loading}
        <p class="empty-state">{$t('pickerModal.loading')}</p>
      {:else if forbidden}
        <AccessDeniedNotice message={forbiddenMessage} />
      {:else if error}
        <p class="empty-state danger">{$t('entityModal.unableToLoad', { error })}</p>
      {:else if $modal.type === 'player' && playerProfile}
        <div class="profile-headline">
          <AssetImage src={playerProfile.avatarUrl} alt={playerProfile.name} size={56} fallbackIcon={User} />
          <div class="profile-headline-text">
            <strong>{playerProfile.name} #{playerProfile.id}</strong>
            {#if playerProfile.motto}<small>{playerProfile.motto}</small>{/if}
            <small>{playerProfile.status} - {playerProfile.gender}</small>
          </div>
        </div>
        <div class="modal-grid">
          <article>
            <span>{$t('entityModal.created')}</span>
            <strong>{formatDate(playerProfile.createdAt)}</strong>
            <small>{$t('entityModal.updated', { date: formatDate(playerProfile.updatedAt) })}</small>
          </article>
          <article>
            <span>{$t('entityModal.inventory')}</span>
            <strong>{playerProfile.inventory?.total || 0}</strong>
            <small>{$t('entityModal.ownedItems')}</small>
          </article>
          <article>
            <span>{$t('entityModal.rooms')}</span>
            <strong>{playerProfile.ownedRooms?.total || 0}</strong>
            <small>{$t('entityModal.ownedRooms')}</small>
          </article>
        </div>

        <section class="modal-section">
          <h3>{$t('entityModal.wallets')}</h3>
          <div class="inline-list">
            {#each playerProfile.wallets || [] as wallet}
              <span>{wallet.currency}: <strong>{wallet.amount}</strong></span>
            {:else}
              <span class="muted">{$t('entityModal.noWalletRows')}</span>
            {/each}
          </div>
        </section>

        <section class="modal-section">
          <h3>{$t('entityModal.recentItems')}</h3>
          <table>
            <thead><tr><th>{$t('entityModal.colItem')}</th><th>{$t('entityModal.colDefinition')}</th><th>{$t('entityModal.colRoom')}</th></tr></thead>
            <tbody>
              {#each playerProfile.inventory?.latest || [] as item}
                <tr>
                  <td><EntityLink type="item" id={item.itemId} label={`item #${item.itemId}`} {openPlayer} {openItem} /></td>
                  <td>{item.definitionName || '-'}</td>
                  <td>{item.roomName || $t('entityModal.notPlaced')}</td>
                </tr>
              {:else}
                <tr><td colspan="3" class="muted">{$t('entityModal.noInventorySnapshot')}</td></tr>
              {/each}
            </tbody>
          </table>
        </section>

        <section class="modal-section">
          <h3>{$t('entityModal.recentActivity')}</h3>
          <table>
            <thead><tr><th>{$t('entityModal.colTime')}</th><th>{$t('entityModal.colType')}</th><th>{$t('entityModal.colDetails')}</th></tr></thead>
            <tbody>
              {#each playerProfile.timeline?.items || [] as entry}
                <tr>
                  <td>{formatDate(entry.occurredAt || entry.OccurredAt)}</td>
                  <td>{entry.eventType || $t('entityModal.item')}</td>
                  <td>
                    <EntityLink type="item" id={entry.itemId} label={`item #${entry.itemId}`} {openPlayer} {openItem} />
                    <span class="muted"> {entry.roomName || ''} {compactCorrelation(entry.correlationId)}</span>
                  </td>
                </tr>
              {:else}
                <tr><td colspan="3" class="muted">{$t('entityModal.noItemActivity')}</td></tr>
              {/each}
            </tbody>
          </table>
        </section>
      {:else if $modal.type === 'item' && itemProfile}
        <div class="modal-grid">
          <article>
            <span>{$t('entityModal.item')}</span>
            <strong>#{itemProfile.itemId}</strong>
            <small>{itemProfile.snapshot?.definitionName || $t('entityModal.unknownDefinition')}</small>
          </article>
          <article>
            <span>{$t('entityModal.owner')}</span>
            {#if itemProfile.snapshot?.ownerPlayerId}
              <strong>
                <EntityLink
                  id={itemProfile.snapshot.ownerPlayerId}
                  label={itemProfile.snapshot.ownerName || `player #${itemProfile.snapshot.ownerPlayerId}`}
                  {openPlayer}
                  {openItem}
                />
              </strong>
            {:else}
              <strong>{$t('entityModal.unknown')}</strong>
            {/if}
          </article>
          <article>
            <span>{$t('entityModal.room')}</span>
            <strong>{itemProfile.snapshot?.roomName || $t('entityModal.notPlaced')}</strong>
            <small>{itemProfile.snapshot?.roomId ? `#${itemProfile.snapshot.roomId}` : '-'}</small>
          </article>
          <article>
            <span>{$t('entityModal.events')}</span>
            <strong>{itemProfile.total || 0}</strong>
            <small>{$t('entityModal.forensicRows')}</small>
          </article>
        </div>

        <section class="modal-section">
          <h3>{$t('entityModal.history')}</h3>
          <table>
            <thead><tr><th>{$t('entityModal.colTime')}</th><th>{$t('entityModal.colEvent')}</th><th>{$t('entityModal.colActor')}</th><th>{$t('entityModal.colRoom')}</th><th>{$t('entityModal.colDetails')}</th></tr></thead>
            <tbody>
              {#each itemProfile.history || [] as row}
                <tr>
                  <td>{formatDate(row.occurredAt || row.OccurredAt)}</td>
                  <td>{row.eventType || '-'}</td>
                  <td>
                    <EntityLink
                      id={row.actorPlayerId || row.ActorPlayerId}
                      label={row.actorPlayerName || row.actorName || ''}
                      {openPlayer}
                      {openItem}
                    />
                  </td>
                  <td>{row.roomName || row.RoomId || '-'}</td>
                  <td>{summarizeData(row.data || row.Data)}</td>
                </tr>
              {:else}
                <tr><td colspan="5" class="muted">{$t('entityModal.noItemHistory')}</td></tr>
              {/each}
            </tbody>
          </table>
        </section>
      {:else}
        <p class="empty-state">{$t('entityModal.noProfileFound')}</p>
      {/if}
    </section>
  </div>
{/if}

<style>
  .profile-headline {
    display: flex;
    align-items: center;
    gap: 12px;
    margin-bottom: 12px;
  }

  .profile-headline-text {
    display: grid;
    gap: 2px;
    min-width: 0;
  }

  .profile-headline-text small {
    color: var(--muted);
  }
</style>

