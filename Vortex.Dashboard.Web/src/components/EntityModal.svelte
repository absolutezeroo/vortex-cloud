<script lang="ts">
  import Modal from './Modal.svelte';
  import { apiGet } from '../lib/api';
  import type { ItemProfile, PlayerProfile } from '../lib/apiTypes';
  import { compactCorrelation, formatDate, summarizeData } from '../lib/format';
  import EntityLink from './EntityLink.svelte';
  import AccessDeniedNotice from './AccessDeniedNotice.svelte';
  import AssetImage from './AssetImage.svelte';
  import CurrencyIcon from './CurrencyIcon.svelte';
  import { currencyChipClass, currencyKindFromName, currencyLabel } from '../lib/currency';
  import { formatNumber } from '../lib/format';
  import { User, Package } from '@lucide/svelte';
  import { isPermissionDeniedError } from '../lib/permissions';
  import { modal, closeModal, openPlayer, openItem } from '../lib/session';
  import { identity } from '../lib/session';
  import Tabs from './Tabs.svelte';
  import PlayerOperationsPanel from './PlayerOperationsPanel.svelte';
  import ConfirmStagedModal from './ConfirmStagedModal.svelte';
  import DropdownMenu from './DropdownMenu.svelte';
  import { createWriteOps } from '../lib/writeOps';
  import { hasDashboardCapability } from '../lib/permissions';
  import {
    MODERATION_OPERATION_CAPABILITIES,
    OPERATION_CAPABILITIES,
  } from '../lib/dashboardPermissions';
  import { t, translate } from '../lib/i18n';

  let loading = $state(false);
  let error = $state('');
  let data = $state<PlayerProfile | ItemProfile | null>(null);
  let currentKey = $state('');
  let forbidden = $state(false);

  // The actions this operator could take on anyone. The panel gates every button on its own
  // capability; this only decides whether offering the tab at all would be a lie.
  let canAct = $derived(
    [...Object.values(OPERATION_CAPABILITIES), ...Object.values(MODERATION_OPERATION_CAPABILITIES)]
      .some((capability) => hasDashboardCapability($identity, capability)),
  );

  /** Which half of the player popup is open. Reset per player -- see the effect below. */
  let tab = $state('identity');

  // Taking an item back lives on the rows that list the items, not behind an id field on the
  // actions tab: the operator is looking at the thing they mean, with its name and its icon.
  const itemOps = createWriteOps(() => load());

  let canTakeItems = $derived(
    hasDashboardCapability($identity, OPERATION_CAPABILITIES.item),
  );

  type InventoryItem = PlayerProfile['inventory']['latest'][number];

  /**
   * What can be done to one item, and what cannot yet.
   *
   * One action per state. A placed item belongs to its room, so the only thing to do with it is
   * send it back to the hand -- through the room grain, so everyone standing there sees it leave.
   * Once it is in a hand, deleting it is an ordinary thing to do. The sequence teaches itself and
   * beats a disabled row explaining why half the menu does nothing.
   */
  function itemMenu(item: InventoryItem) {
    const placed = item.roomEntityId !== null;

    return placed
      ? [{ id: 'pickup', label: $t('entityModal.pickUp') }]
      : [{ id: 'delete', label: $t('entityModal.deleteItem'), danger: true }];
  }

  function runItemAction(action: string, item: InventoryItem) {
    if (!playerProfile) {
      return;
    }

    const named = {
      item: item.itemId,
      name: item.definitionName ?? '-',
      player: playerProfile.name,
    };

    if (action === 'pickup') {
      itemOps.ask(
        '/api/v1/operations/items/pickup',
        { roomId: item.roomEntityId, itemId: item.itemId },
        translate('entityModal.pickUpTitle'),
        translate('entityModal.pickUpSummary', { ...named, room: item.roomEntityId ?? 0 }),
      );

      return;
    }

    itemOps.ask(
      '/api/v1/operations/items/revoke',
      { playerId: playerProfile.id, itemId: item.itemId },
      translate('entityModal.deleteItemTitle'),
      translate('entityModal.deleteItemSummary', named),
      { danger: true },
    );
  }

  // $derived, not const: the labels follow the locale the operator switches to.
  let playerTabs = $derived([
    { id: 'identity', label: $t('entityModal.identity') },
    { id: 'actions', label: $t('playerOps.tabActions') },
  ]);


  async function load() {
    const target = $modal;

    if (!target) return;

    loading = true;
    error = '';
    data = null;
    forbidden = false;

    try {
      // The profile on its own, not the investigation search. That one also assembles the audit
      // trail, the ledger, the chat and the item history — a dozen queries this popup never reads.
      data = target.type === 'item'
        ? await apiGet<ItemProfile>(
            `/api/v1/directory/entity/${encodeURIComponent(target.id)}`,
          )
        : await apiGet<PlayerProfile>(
            `/api/v1/directory/players/${encodeURIComponent(target.id)}/profile`,
          );
    } catch (err) {
      if (isPermissionDeniedError(err)) {
        forbidden = true;
        error = '';
        return;
      }

      error = (err as Error).message;
    } finally {
      loading = false;
    }
  }


  let key = $derived($modal ? `${$modal.type}:${$modal.id}` : '');
  $effect(() => {
    if (key && key !== currentKey) {
      currentKey = key;
      // Opening a different player starts on their identity: landing straight on a ban form for
      // someone whose name you have not read yet is how the wrong account gets sanctioned.
      tab = 'identity';
      void load();
    }
  });
  // The profile endpoint answers the profile itself, so there is no envelope to unwrap and no
  // discriminator to check: a player id that matches nobody comes back null.
  let playerProfile = $derived(
    $modal?.type === 'item' ? null : (data as PlayerProfile | null),
  );
  let itemProfile = $derived($modal?.type === 'item' ? (data as ItemProfile | null) : null);
  let forbiddenMessage = $derived($t($modal?.type === 'item' ? 'entityModal.itemAccessDenied' : 'entityModal.playerAccessDenied'));
</script>

{#if $modal}
  <Modal
    title={$modal.label || `${$modal.type} #${$modal.id}`}
    eyebrow={$modal.type === 'item' ? $t('entityModal.itemInspector') : $t('entityModal.playerInspector')}
    labelledBy="entity-modal-title"
    onclose={closeModal}
  >
    {#snippet header()}
        <button class="ghost-button" type="button" onclick={closeModal}>
        {$t('pickerModal.close')}
      </button>
    {/snippet}

    {#if loading}
      <p class="empty-state">{$t('pickerModal.loading')}</p>
    {:else if forbidden}
      <AccessDeniedNotice message={forbiddenMessage} />
    {:else if error}
      <p class="empty-state danger" role="alert">{$t('entityModal.unableToLoad', { error })}</p>
    {:else if $modal.type === 'player' && playerProfile}
      <div class="profile-headline">
        <AssetImage src={playerProfile.avatarUrl} alt={playerProfile.name} size={56} fallbackIcon={User} />
        <div class="profile-headline-text">
          <strong>{playerProfile.name} #{playerProfile.id}</strong>
          {#if playerProfile.motto}<small>{playerProfile.motto}</small>{/if}
          <small>
            {playerProfile.online ? $t('entityModal.online') : $t('entityModal.offline')} -
            {playerProfile.gender}
          </small>
        </div>
      </div>
      {#if canAct}
        <Tabs tabs={playerTabs} bind:active={tab} />
      {/if}

      {#if tab === 'actions'}
        <PlayerOperationsPanel
          playerId={playerProfile.id}
          playerName={playerProfile.name}
          online={playerProfile.online}
          onDone={load}
        />
      {:else}
      <div class="modal-grid">
        <article>
          <span>{$t('entityModal.created')}</span>
          <strong>{formatDate(playerProfile.createdAt)}</strong>
          <small>{$t('entityModal.updated', { date: formatDate(playerProfile.updatedAt) })}</small>
        </article>
        <article>
          <span>{$t('entityModal.inventory')}</span>
          <strong>{playerProfile.inventory.total}</strong>
          <small>{$t('entityModal.ownedItems')}</small>
        </article>
        <article>
          <span>{$t('entityModal.rooms')}</span>
          <strong>{playerProfile.ownedRooms.total}</strong>
          <small>{$t('entityModal.ownedRooms')}</small>
        </article>
      </div>

      <section class="modal-section">
        <h3>{$t('entityModal.wallets')}</h3>
        <div class="inline-list">
          {#each playerProfile.wallets as wallet}
            <span class={currencyChipClass(currencyKindFromName(wallet.currency, wallet.activityPointType))}>
              <CurrencyIcon kind={currencyKindFromName(wallet.currency, wallet.activityPointType)} size={13} />
              <strong>{formatNumber(wallet.amount)}</strong>
              {currencyLabel(wallet.currency, wallet.activityPointType)}
            </span>
          {:else}
            <span class="muted">{$t('entityModal.noWalletRows')}</span>
          {/each}
        </div>
      </section>

      <section class="modal-section">
        <h3>{$t('entityModal.recentItems')}</h3>
        <div class="table-wrap">
          <table>
          <thead><tr><th>{$t('entityModal.colItem')}</th><th>{$t('entityModal.colDefinition')}</th><th>{$t('entityModal.colRoom')}</th>{#if canTakeItems}<th>{$t('common.actions')}</th>{/if}</tr></thead>
          <tbody>
            {#each playerProfile.inventory.latest as item}
              <tr>
                <td><EntityLink type="item" id={item.itemId} label={`item #${item.itemId}`} {openPlayer} {openItem} /></td>
                <td>
                  <span style="display: inline-flex; align-items: center; gap: 8px;">
                    <AssetImage src={item.furniIconUrl} alt={item.definitionName} size={26} fallbackIcon={Package} />
                    <span>{item.definitionName || '-'}</span>
                  </span>
                </td>
                <td>{item.roomName || $t('entityModal.notPlaced')}</td>
                {#if canTakeItems}
                  <td>
                    <DropdownMenu
                      label={$t('entityModal.itemActions')}
                      align="end"
                      items={itemMenu(item)}
                      onpick={(action) => runItemAction(action, item)}
                    />
                  </td>
                {/if}
              </tr>
            {:else}
              <tr><td colspan={canTakeItems ? 4 : 3} class="muted">{$t('entityModal.noInventorySnapshot')}</td></tr>
            {/each}
          </tbody>
        </table>
        </div>
      </section>

      <section class="modal-section">
        <h3>{$t('entityModal.recentActivity')}</h3>
        <div class="table-wrap">
          <table>
          <thead><tr><th>{$t('entityModal.colTime')}</th><th>{$t('entityModal.colType')}</th><th>{$t('entityModal.colDetails')}</th></tr></thead>
          <tbody>
            {#each playerProfile.timeline.items as entry}
              <tr>
                <td>{formatDate(entry.occurredAt)}</td>
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
        </div>
      </section>
      {/if}
    {:else if $modal.type === 'item' && itemProfile}
      <div class="modal-grid">
        <article>
          <span>{$t('entityModal.item')}</span>
          <strong class="item-identity">
            <AssetImage
              src={itemProfile.snapshot?.furniIconUrl}
              alt={itemProfile.snapshot?.definitionName ?? ''}
              size={32}
              fallbackIcon={Package}
            />
            #{itemProfile.itemId}
          </strong>
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
          <strong>{itemProfile.total}</strong>
          <small>{$t('entityModal.forensicRows')}</small>
        </article>
      </div>

      <section class="modal-section">
        <h3>{$t('entityModal.history')}</h3>
        <div class="table-wrap">
          <table>
          <thead><tr><th>{$t('entityModal.colTime')}</th><th>{$t('entityModal.colEvent')}</th><th>{$t('entityModal.colActor')}</th><th>{$t('entityModal.colRoom')}</th><th>{$t('entityModal.colDetails')}</th></tr></thead>
          <tbody>
            {#each itemProfile.history as row}
              <tr>
                <td>{formatDate(row.occurredAt)}</td>
                <td>{row.eventType || '-'}</td>
                <td>
                  <EntityLink
                    id={row.actorPlayerId}
                    label={row.actorPlayerName}
                    {openPlayer}
                    {openItem}
                  />
                </td>
                <td>{row.roomName || row.roomId || '-'}</td>
                <td>{summarizeData(row.data)}</td>
              </tr>
            {:else}
              <tr><td colspan="5" class="muted">{$t('entityModal.noItemHistory')}</td></tr>
            {/each}
          </tbody>
        </table>
        </div>
      </section>
    {:else}
      <p class="empty-state">{$t('entityModal.noProfileFound')}</p>
    {/if}

    <!-- Stacked on top of the popup, which is what the dialog stack in lib/dialogBehaviour exists
         for: before it, Escape here closed the popup underneath instead of the confirmation. -->
    <ConfirmStagedModal ops={itemOps} eyebrow={$t('entityModal.takeBackTitle')} />
  </Modal>
{/if}

<style>
  /* The sprite sits with the id, the way the player card puts the head with the name -- an item
     is recognised by what it looks like long before its number means anything. */
  .item-identity {
    display: inline-flex;
    align-items: center;
    gap: 10px;
  }

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

