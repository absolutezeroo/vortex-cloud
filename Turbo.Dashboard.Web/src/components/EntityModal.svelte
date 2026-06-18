<script>
  import { apiGet } from '../lib/api.js';
  import { compactCorrelation, formatDate, summarizeData } from '../lib/format.js';
  import EntityLink from './EntityLink.svelte';
  import AccessDeniedNotice from './AccessDeniedNotice.svelte';
  import { isPermissionDeniedError } from '../lib/permissions.js';
  import { modal, closeModal, openPlayer, openItem } from '../lib/session.js';

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
        ? await apiGet(`/api/item/${encodeURIComponent($modal.id)}`)
        : await apiGet(`/api/search?q=${encodeURIComponent($modal.id)}`);
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

  $: forbiddenMessage =
    $modal?.type === 'item'
      ? "Vous n'avez pas la permission d'afficher ce détail d'objet."
      : "Vous n'avez pas la permission d'afficher cette fiche joueur.";
</script>

{#if $modal}
  <div class="modal-layer">
    <button class="modal-backdrop" type="button" aria-label="Close" on:click={closeModal}></button>
    <section class="modal-panel" role="dialog" aria-modal="true">
      <header class="modal-header">
        <div>
          <p class="eyebrow">{$modal.type === 'item' ? 'Item inspector' : 'Player inspector'}</p>
          <h2>{$modal.label || `${$modal.type} #${$modal.id}`}</h2>
        </div>
        <button class="ghost-button" type="button" on:click={closeModal}>Close</button>
      </header>

      {#if loading}
        <p class="empty-state">Loading...</p>
      {:else if forbidden}
        <AccessDeniedNotice message={forbiddenMessage} />
      {:else if error}
        <p class="empty-state danger">Unable to load: {error}</p>
      {:else if $modal.type === 'player' && playerProfile}
        <div class="modal-grid">
          <article>
            <span>Identity</span>
            <strong>{playerProfile.name} #{playerProfile.id}</strong>
            <small>{playerProfile.status} - {playerProfile.gender}</small>
          </article>
          <article>
            <span>Created</span>
            <strong>{formatDate(playerProfile.createdAt)}</strong>
            <small>Updated {formatDate(playerProfile.updatedAt)}</small>
          </article>
          <article>
            <span>Inventory</span>
            <strong>{playerProfile.inventory?.total || 0}</strong>
            <small>owned items</small>
          </article>
          <article>
            <span>Rooms</span>
            <strong>{playerProfile.ownedRooms?.total || 0}</strong>
            <small>owned rooms</small>
          </article>
        </div>

        <section class="modal-section">
          <h3>Wallets</h3>
          <div class="inline-list">
            {#each playerProfile.wallets || [] as wallet}
              <span>{wallet.currency}: <strong>{wallet.amount}</strong></span>
            {:else}
              <span class="muted">No wallet rows.</span>
            {/each}
          </div>
        </section>

        <section class="modal-section">
          <h3>Recent items</h3>
          <table>
            <thead><tr><th>Item</th><th>Definition</th><th>Room</th></tr></thead>
            <tbody>
              {#each playerProfile.inventory?.latest || [] as item}
                <tr>
                  <td><EntityLink type="item" id={item.itemId} label={`item #${item.itemId}`} {openPlayer} {openItem} /></td>
                  <td>{item.definitionName || '-'}</td>
                  <td>{item.roomName || 'not placed'}</td>
                </tr>
              {:else}
                <tr><td colspan="3" class="muted">No inventory snapshot.</td></tr>
              {/each}
            </tbody>
          </table>
        </section>

        <section class="modal-section">
          <h3>Recent activity</h3>
          <table>
            <thead><tr><th>Time</th><th>Type</th><th>Details</th></tr></thead>
            <tbody>
              {#each playerProfile.timeline?.items || [] as entry}
                <tr>
                  <td>{formatDate(entry.occurredAt || entry.OccurredAt)}</td>
                  <td>{entry.eventType || 'item'}</td>
                  <td>
                    <EntityLink type="item" id={entry.itemId} label={`item #${entry.itemId}`} {openPlayer} {openItem} />
                    <span class="muted"> {entry.roomName || ''} {compactCorrelation(entry.correlationId)}</span>
                  </td>
                </tr>
              {:else}
                <tr><td colspan="3" class="muted">No item activity.</td></tr>
              {/each}
            </tbody>
          </table>
        </section>
      {:else if $modal.type === 'item' && itemProfile}
        <div class="modal-grid">
          <article>
            <span>Item</span>
            <strong>#{itemProfile.itemId}</strong>
            <small>{itemProfile.snapshot?.definitionName || 'unknown definition'}</small>
          </article>
          <article>
            <span>Owner</span>
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
              <strong>unknown</strong>
            {/if}
          </article>
          <article>
            <span>Room</span>
            <strong>{itemProfile.snapshot?.roomName || 'not placed'}</strong>
            <small>{itemProfile.snapshot?.roomId ? `#${itemProfile.snapshot.roomId}` : '-'}</small>
          </article>
          <article>
            <span>Events</span>
            <strong>{itemProfile.total || 0}</strong>
            <small>forensic rows</small>
          </article>
        </div>

        <section class="modal-section">
          <h3>History</h3>
          <table>
            <thead><tr><th>Time</th><th>Event</th><th>Actor</th><th>Room</th><th>Details</th></tr></thead>
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
                <tr><td colspan="5" class="muted">No item history.</td></tr>
              {/each}
            </tbody>
          </table>
        </section>
      {:else}
        <p class="empty-state">No profile found.</p>
      {/if}
    </section>
  </div>
{/if}


