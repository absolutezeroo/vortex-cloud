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
  import PickerModal from '../components/PickerModal.svelte';
  import Pagination from '../components/Pagination.svelte';
  import TableFilter from '../components/TableFilter.svelte';
  import CurrencyIcon from '../components/CurrencyIcon.svelte';
  import { currencyChipClass, currencyKindFromName, currencyLabel } from '../lib/currency.js';
  import { formatNumber } from '../lib/format.js';
  import { filterRows } from '../lib/tableView.js';
  import PlayerOperationsPanel from '../components/PlayerOperationsPanel.svelte';
  import Tabs from '../components/Tabs.svelte';
  import { Wrench, ScrollText } from '@lucide/svelte';
  import { readParam, readNumberParam, writeParams } from '../lib/urlState.js';
  import { t, translate } from '../lib/i18n.js';

  // ?player= makes this page linkable, and is how the command palette hands a player over.
  let query = $state(readParam('player'));
  let rows = $state([]);
  let player = $state(null);
  let summary = $state('');
  $effect(() => {
    if (!summary) summary = translate('investigation.hint');
  });
  let error = $state('');
  let forbidden = $state(false);
  // The search box takes an id or a correlation id; a name is what an operator actually remembers,
  // and the shared picker already knows how to find one. Picking fills the box and runs the search,
  // so both paths land on the same page state.
  let picking = $state(null);

  // The timeline is paged client-side: the search already returns the whole window, so paging
  // is a reading aid, not another round trip.
  const PAGE_SIZE = 25;
  let page = $state(readNumberParam('page', 1));

  // The search returns the whole window in one go, so narrowing it is a reading operation, not
  // another request: kind, free text and a date range, all applied to what is already here.
  let kindFilter = $state(readParam('kind'));
  let textFilter = $state('');
  let fromFilter = $state('');
  let toFilter = $state('');

  let visibleRows = $derived(
    filterRows(
      rows.filter((row) => {
        if (kindFilter && row.kind !== kindFilter) return false;
        if (fromFilter && row.sortTime < Date.parse(fromFilter)) return false;
        // An end date with no time means the whole of that day.
        if (toFilter && row.sortTime > Date.parse(toFilter) + 86_399_000) return false;

        return true;
      }),
      textFilter,
    ),
  );

  let reasonByCorrelation = $derived(
    new Map(
      rows
        .filter((row) => row.kind !== 'ledger' && row.correlationId && (row.action || row.eventType))
        .map((row) => [row.correlationId, row.action || row.eventType]),
    ),
  );

  let pageCount = $derived(Math.max(1, Math.ceil(visibleRows.length / PAGE_SIZE)));
  let pageRows = $derived(visibleRows.slice((page - 1) * PAGE_SIZE, page * PAGE_SIZE));

  // Narrowing the list moves the ground under the pager, so it goes back to the first page.
  function narrowed() {
    page = 1;
    writeParams({ page: '', kind: kindFilter || '' });
  }
  let active = $state('actions');

  // $derived, not const: translate() reads the locale once, and a const would keep the tab strip in
  // whatever language the page happened to mount in after the operator switches.
  let tabs = $derived([
    { id: 'actions', label: $t('playerOps.tabActions'), icon: Wrench },
    { id: 'timeline', label: $t('investigation.tabTimeline'), icon: ScrollText },
  ]);

  function pick(chosen) {
    query = String(chosen.id);
    picking = null;
    search();
  }

  onMount(() => {
    if (query.trim()) search();
  });

  function push(rows, row) {
    rows.push({
      time: row.occurredAt || row.OccurredAt || row.createdAt || row.CreatedAt,
      sortTime: Date.parse(row.occurredAt || row.OccurredAt || row.createdAt || row.CreatedAt || '') || 0,
      ...row,
    });
  }

  // A contract trade can deposit and withdraw in the same movement; the direction is the headline and
  // the detail line below spells out both halves, so a two-way row is not mislabelled into one word.
  function chestAction(row) {
    return row.depositFurniCount + row.depositCoinsCount > 0 ? 'chest.deposit' : 'chest.withdraw';
  }

  function chestDetail(row) {
    return [
      row.depositFurniCount ? `+${row.depositFurniCount} furni` : '',
      row.withdrawFurniCount ? `-${row.withdrawFurniCount} furni` : '',
      row.depositCoinsCount ? `+${row.depositCoinsCount} coins` : '',
      row.withdrawCoinsCount ? `-${row.withdrawCoinsCount} coins` : '',
      row.definitionInfo,
      row.roomName ? `@ ${row.roomName}` : '',
    ]
      .filter(Boolean)
      .join(' ');
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
      // limit applies per source, and the noisy ones (room entries, sessions) would otherwise fill
      // the default 50 on their own and push everything rarer -- a badge, an achievement, a chest
      // movement -- off a timeline that pages client-side and so never asks for a second window.
      const data = await apiGet(`/api/v1/directory/search?q=${encodeURIComponent(term)}&limit=200`);
      const nextRows = [];

      if (data.kind === 'id') {
        player = data.playerProfile || null;
        (data.asActor || []).forEach((row) => push(nextRows, { kind: 'audit', ...row }));
        (data.ledger || []).forEach((row) => push(nextRows, { kind: 'ledger', playerId: term, ...row }));
        (data.itemHistory || []).forEach((row) => push(nextRows, { kind: 'item', ...row }));
        (data.chats || []).forEach((row) =>
          push(nextRows, { kind: 'chat', playerId: term, action: 'chat.said', data: row.message, ...row })
        );
        (data.chestMoves || []).forEach((row) =>
          push(nextRows, {
            kind: 'chest',
            playerId: term,
            action: chestAction(row),
            data: chestDetail(row),
            ...row,
          })
        );
        summary = player
          ? translate('investigation.eventsForPlayer', { count: nextRows.length, term })
          : translate('investigation.eventsForId', { count: nextRows.length, term });
      } else if (data.kind === 'correlationId') {
        (data.audit || []).forEach((row) => push(nextRows, { kind: 'audit', ...row }));
        (data.ledger || []).forEach((row) => push(nextRows, { kind: 'ledger', ...row }));
        (data.items || []).forEach((row) => push(nextRows, { kind: 'item', ...row }));
        summary = translate('investigation.linkedEventsFor', { count: nextRows.length, term });
      } else {
        summary = data.hint || translate('investigation.noStructuredResult');
      }

      rows = nextRows.sort((left, right) => right.sortTime - left.sortTime);
      // Only a player search is worth putting in the URL: a correlation id is a one-off lookup, not
      // a place someone comes back to.
      // Keep the page only while it still exists in the new result set.
      if (page > Math.ceil(nextRows.length / PAGE_SIZE)) page = 1;

      writeParams({
        player: player ? String(player.id) : '',
        page: page > 1 ? page : '',
      });
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
    <button type="button" onclick={search} class="warning">{$t('common.refresh')}</button>
  </div>
  <p class="muted">{$t('investigation.description')}</p>
</section>

<section class="panel">
  <form class="toolbar" onsubmit={(event) => { event.preventDefault(); search(); }}>
    <input autocomplete="off" spellcheck="false" bind:value={query} placeholder={$t('investigation.searchPlaceholder')} />
    <button type="submit">{$t('investigation.load')}</button>
    <button type="button" class="ghost-button" onclick={() => (picking = 'user')}>
      {$t('investigation.findPlayer')}
    </button>
    <button type="button" class="ghost-button" onclick={() => (picking = 'furniture')}>
      {$t('investigation.findItem')}
    </button>
  </form>

</section>

<section class="panel">
  <p class="muted">{summary}</p>

  {#if forbidden}
    <AccessDeniedNotice message={$t('investigation.accessDenied')} />
  {:else if error}
    <p class="empty-state danger" role="alert">{error}</p>
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

    <Tabs {tabs} bind:active storageKey="player" />
  {/if}

  {#if player && active === 'actions'}
    <PlayerOperationsPanel
      playerId={player.id}
      playerName={player.name}
      online={player.online}
      onDone={search}
    />
  {/if}

</section>

<!-- With no player loaded there is no tab strip, so the timeline is simply what the page shows. -->
{#if !player || active === 'timeline'}
  <section class="panel" style="margin-top: 12px;">
    <div class="timeline-filters">
      <label>
        {$t('investigation.filterKind')}
        <select bind:value={kindFilter} onchange={narrowed}>
          <option value="">{$t('investigation.filterKindAll')}</option>
          <option value="audit">{$t('investigation.kindAudit')}</option>
          <option value="ledger">{$t('investigation.kindLedger')}</option>
          <option value="item">{$t('investigation.kindItem')}</option>
        </select>
      </label>
      <label>
        {$t('common.since')}
        <input autocomplete="off" spellcheck="false" type="date" bind:value={fromFilter} onchange={narrowed} />
      </label>
      <label>
        {$t('common.until')}
        <input autocomplete="off" spellcheck="false" type="date" bind:value={toFilter} onchange={narrowed} />
      </label>
    </div>

  </section>

  <section class="panel" style="margin-top: 12px;">
    <TableFilter bind:query={textFilter} shown={visibleRows.length} total={rows.length} />

    <table>
    <thead><tr><th>{$t('investigation.colTime')}</th><th>{$t('investigation.colType')}</th><th>{$t('investigation.colActor')}</th><th>{$t('investigation.colDetails')}</th></tr></thead>
    <tbody>
      {#each pageRows as row}
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
            {#if row.kind === 'ledger'}
              <div class="ledger-move">
                <span class={currencyChipClass(currencyKindFromName(row.currency, row.activityPointType))}>
                  <CurrencyIcon kind={currencyKindFromName(row.currency, row.activityPointType)} size={13} />
                  {row.delta > 0 ? '+' : ''}{formatNumber(row.delta ?? 0)}
                </span>
                <b>{currencyLabel(row.currency, row.activityPointType)}</b>
                {#if row.balanceAfter != null}
                  <span class="muted">{$t('investigation.balanceAfter', { balance: formatNumber(row.balanceAfter) })}</span>
                {/if}
              </div>
              <div class="ledger-why">
                {reasonByCorrelation.get(row.correlationId) ?? $t('investigation.noReason')}
              </div>
            {:else}
              {row.action || row.eventType || row.category || $t('investigation.event')}
            {/if}
            {#if row.roomId}
              - <EntityLink type="room" id={row.roomId} label={row.roomName || ''} {openPlayer} {openItem} />
            {/if}
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

    {#if pageCount > 1}
      <Pagination
        page={page}
        pageCount={pageCount}
        total={visibleRows.length}
        pageSize={PAGE_SIZE}
        pageWord={$t('common.page')}
        prevLabel={$t('common.prev')}
        nextLabel={$t('common.next')}
        onchange={(next) => {
          page = next;
          writeParams({ page: next > 1 ? next : '' });
        }}
      />
    {/if}
  </section>
{/if}

{#if picking}
  <PickerModal
    kind={picking}
    title={picking === 'furniture' ? $t('investigation.findItem') : $t('investigation.findPlayer')}
    onSelect={pick}
    onClose={() => (picking = null)}
  />
{/if}

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
