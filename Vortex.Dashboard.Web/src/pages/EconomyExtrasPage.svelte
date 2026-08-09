<script>
  // The economy's smaller tables, each of which had no surface at all: LTD series with their raffle
  // outcomes, rentable spaces with who is actually renting them, the currency catalogue with how
  // much of each is held, and the builders-club ladder.
  import { onMount } from 'svelte';
  import { apiGet } from '../lib/api.js';
  import { formatNumber, formatDate, formatDuration } from '../lib/format.js';
  import { isPermissionDeniedError } from '../lib/permissions.js';
  import { openPlayer } from '../lib/session.js';
  import AccessDeniedNotice from '../components/AccessDeniedNotice.svelte';
  import AssetImage from '../components/AssetImage.svelte';
  import EntityLink from '../components/EntityLink.svelte';
  import StatCard from '../components/StatCard.svelte';
  import { Ticket, Store, Coins, Hammer } from '@lucide/svelte';
  import { t } from '../lib/i18n.js';

  let loading = false;
  let forbidden = false;
  let error = '';
  let data = null;

  async function refresh() {
    loading = true;
    error = '';
    forbidden = false;

    try {
      data = await apiGet('/api/v1/economy/extras');
    } catch (err) {
      if (isPermissionDeniedError(err)) {
        forbidden = true;
        data = null;
        return;
      }

      error = err.message;
      data = null;
    } finally {
      loading = false;
    }
  }

  function resultSummary(row) {
    if (!row.entriesByResult || row.entriesByResult.length === 0) return '—';
    return row.entriesByResult.map((e) => `${e.result}: ${formatNumber(e.count)}`).join(', ');
  }

  onMount(() => {
    void refresh();
  });
</script>

<section class="panel">
  <div class="panel-head"><h2>{$t('economyExtras.title')}</h2></div>
  <p class="muted">{$t('economyExtras.description')}</p>
  <div class="toolbar">
    <button type="button" on:click={refresh} disabled={loading}>{$t('common.refresh')}</button>
  </div>

  {#if loading}
    <p class="muted">{$t('common.loading')}</p>
  {:else if forbidden}
    <AccessDeniedNotice message={$t('economyExtras.accessDenied')} />
  {:else if error}
    <p class="empty-state danger">{error}</p>
  {/if}
</section>

{#if data}
  <div class="metric-grid" style="margin-top: 12px;">
    <StatCard
      label={$t('economyExtras.ltdSeries')}
      value={formatNumber(data.totals.ltdSeries)}
      sub={$t('economyExtras.running', { count: formatNumber(data.totals.runningSeries) })}
    >
      <Ticket slot="icon" size={15} strokeWidth={2} aria-hidden="true" />
    </StatCard>
    <StatCard
      label={$t('economyExtras.rentableSpaces')}
      value={formatNumber(data.totals.rentableSpaces)}
      sub={$t('economyExtras.rentedNow', { count: formatNumber(data.totals.rentedNow) })}
    >
      <Store slot="icon" size={15} strokeWidth={2} aria-hidden="true" />
    </StatCard>
    <StatCard label={$t('economyExtras.currencies')} value={formatNumber(data.totals.currencies)}>
      <Coins slot="icon" size={15} strokeWidth={2} aria-hidden="true" />
    </StatCard>
    <StatCard label={$t('economyExtras.buildersClubTiers')} value={formatNumber(data.totals.buildersClubTiers)}>
      <Hammer slot="icon" size={15} strokeWidth={2} aria-hidden="true" />
    </StatCard>
  </div>

  <section class="panel" style="margin-top: 12px;">
    <div class="panel-head"><h2>{$t('economyExtras.ltdTitle')}</h2></div>
    <div class="table-wrap">
      <table>
        <thead>
          <tr>
            <th>{$t('economyExtras.colItem')}</th>
            <th>{$t('economyExtras.colSold')}</th>
            <th>{$t('economyExtras.colCost')}</th>
            <th>{$t('economyExtras.colWindow')}</th>
            <th>{$t('economyExtras.colState')}</th>
            <th>{$t('economyExtras.colEntries')}</th>
            <th>{$t('economyExtras.colEnds')}</th>
          </tr>
        </thead>
        <tbody>
          {#each data.ltdSeries || [] as row}
            <tr>
              <td>
                <span class="cell">
                  <AssetImage src={row.iconUrl} alt={row.productName || ''} size={32} />
                  <span>{row.productName || `#${row.productId}`}</span>
                </span>
              </td>
              <td>{formatNumber(row.sold)} / {formatNumber(row.totalQuantity)}</td>
              <td>{formatNumber(row.costCredits)}</td>
              <td>{formatDuration(row.raffleWindowSeconds)}</td>
              <td>
                {#if row.running}
                  <span class="status-badge status-badge--ok">{$t('economyExtras.stateRunning')}</span>
                {:else if row.hasRaffleFinished}
                  <span class="status-badge status-badge--unknown">{$t('economyExtras.stateFinished')}</span>
                {:else}
                  <span class="status-badge status-badge--warn">{$t('economyExtras.stateIdle')}</span>
                {/if}
                {#if row.pendingEntries > 0}
                  <span class="status-badge status-badge--warn">
                    {$t('economyExtras.pending', { count: row.pendingEntries })}
                  </span>
                {/if}
              </td>
              <td>{resultSummary(row)}</td>
              <td>{row.endsAt ? formatDate(row.endsAt) : '—'}</td>
            </tr>
          {:else}
            <tr><td colspan="7" class="muted">{$t('economyExtras.noSeries')}</td></tr>
          {/each}
        </tbody>
      </table>
    </div>
  </section>

  <section class="panel" style="margin-top: 12px;">
    <div class="panel-head"><h2>{$t('economyExtras.rentableTitle')}</h2></div>
    <p class="muted">{$t('economyExtras.rentableDescription')}</p>
    <div class="table-wrap">
      <table>
        <thead>
          <tr>
            <th>{$t('economyExtras.colFurniture')}</th>
            <th>{$t('economyExtras.colRenter')}</th>
            <th>{$t('economyExtras.colUntil')}</th>
            <th>{$t('economyExtras.colTerms')}</th>
          </tr>
        </thead>
        <tbody>
          {#each data.rentableSpaces || [] as row}
            <tr>
              <td>#{row.furnitureId}</td>
              <td>
                {#if row.renterId}
                  <EntityLink type="player" id={row.renterId} label={row.renterName} {openPlayer} />
                {:else}
                  <span class="muted">{$t('economyExtras.vacant')}</span>
                {/if}
              </td>
              <td>{row.rentedUntil ? formatDate(row.rentedUntil) : '—'}</td>
              <td>
                {#if row.hasTerms}
                  <span class="status-badge status-badge--ok">{$t('economyExtras.termsSet')}</span>
                {:else}
                  <span class="status-badge status-badge--warn">{$t('economyExtras.noTerms')}</span>
                {/if}
              </td>
            </tr>
          {:else}
            <tr><td colspan="4" class="muted">{$t('economyExtras.noRentals')}</td></tr>
          {/each}
        </tbody>
      </table>
    </div>
  </section>

  <section class="panel" style="margin-top: 12px;">
    <div class="panel-head"><h2>{$t('economyExtras.currenciesTitle')}</h2></div>
    <div class="table-wrap">
      <table>
        <thead>
          <tr>
            <th>{$t('economyExtras.colCurrency')}</th>
            <th>{$t('economyExtras.colType')}</th>
            <th>{$t('economyExtras.colPointType')}</th>
            <th>{$t('economyExtras.colEnabled')}</th>
            <th>{$t('economyExtras.colStarting')}</th>
            <th>{$t('economyExtras.colWallets')}</th>
            <th>{$t('economyExtras.colHeld')}</th>
          </tr>
        </thead>
        <tbody>
          {#each data.currencies || [] as row}
            <tr>
              <td>{row.name || `#${row.id}`}</td>
              <td>{row.currencyType}</td>
              <td>{row.activityPointType ?? '—'}</td>
              <td>{row.enabled ? $t('common.yes') : $t('common.no')}</td>
              <td>{formatNumber(row.startingAmount)}</td>
              <td>{formatNumber(row.walletRows)}</td>
              <td>{formatNumber(row.totalHeld)}</td>
            </tr>
          {:else}
            <tr><td colspan="7" class="muted">{$t('economyExtras.noCurrencies')}</td></tr>
          {/each}
        </tbody>
      </table>
    </div>
  </section>

  <section class="panel" style="margin-top: 12px;">
    <div class="panel-head"><h2>{$t('economyExtras.buildersClubTitle')}</h2></div>
    <div class="table-wrap">
      <table>
        <thead>
          <tr>
            <th>{$t('economyExtras.colLevel')}</th>
            <th>{$t('economyExtras.colFurniLimit')}</th>
          </tr>
        </thead>
        <tbody>
          {#each data.buildersClub || [] as row}
            <tr><td>{row.level}</td><td>{formatNumber(row.furniLimit)}</td></tr>
          {:else}
            <tr><td colspan="2" class="muted">{$t('economyExtras.noTiers')}</td></tr>
          {/each}
        </tbody>
      </table>
    </div>
  </section>
{/if}

<style>
  .cell {
    display: inline-flex;
    align-items: center;
    gap: 8px;
  }
</style>
