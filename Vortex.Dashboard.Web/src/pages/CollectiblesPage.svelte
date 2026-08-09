<script>
  // NFT collections, their items and the collector scores. The one thing worth flagging: an item
  // whose product code matches no furniture definition makes its collection impossible to complete,
  // and nothing else in the hotel would ever say so.
  import { onMount } from 'svelte';
  import { apiGet } from '../lib/api.js';
  import { formatNumber, formatDate } from '../lib/format.js';
  import { isPermissionDeniedError } from '../lib/permissions.js';
  import { openPlayer } from '../lib/session.js';
  import AccessDeniedNotice from '../components/AccessDeniedNotice.svelte';
  import AssetImage from '../components/AssetImage.svelte';
  import EmptyState from '../components/EmptyState.svelte';
  import EntityLink from '../components/EntityLink.svelte';
  import StatCard from '../components/StatCard.svelte';
  import { Gem, Boxes, TriangleAlert, Trophy } from '@lucide/svelte';
  import { t } from '../lib/i18n.js';

  let loading = false;
  let forbidden = false;
  let error = '';
  let data = null;
  let expanded = null;

  async function refresh() {
    loading = true;
    error = '';
    forbidden = false;

    try {
      data = await apiGet('/api/v1/collectibles');
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

  onMount(() => {
    void refresh();
  });
</script>

<section class="panel">
  <div class="panel-head"><h2>{$t('collectibles.title')}</h2></div>
  <p class="muted">{$t('collectibles.description')}</p>
  <div class="toolbar">
    <button type="button" on:click={refresh} disabled={loading}>{$t('common.refresh')}</button>
  </div>

  {#if loading}
    <p class="muted">{$t('common.loading')}</p>
  {:else if forbidden}
    <AccessDeniedNotice message={$t('collectibles.accessDenied')} />
  {:else if error}
    <p class="empty-state danger">{error}</p>
  {/if}
</section>

{#if data}
  <div class="metric-grid" style="margin-top: 12px;">
    <StatCard label={$t('collectibles.collections')} value={formatNumber(data.totals.collections)}>
      <Gem slot="icon" size={15} strokeWidth={2} aria-hidden="true" />
    </StatCard>
    <StatCard label={$t('collectibles.items')} value={formatNumber(data.totals.items)}>
      <Boxes slot="icon" size={15} strokeWidth={2} aria-hidden="true" />
    </StatCard>
    <StatCard label={$t('collectibles.completable')} value={formatNumber(data.totals.completableCollections)}>
      <Gem slot="icon" size={15} strokeWidth={2} aria-hidden="true" />
    </StatCard>
    <StatCard label={$t('collectibles.unresolved')} value={formatNumber(data.totals.unresolvedItems)}>
      <TriangleAlert slot="icon" size={15} strokeWidth={2} aria-hidden="true" />
    </StatCard>
    <StatCard label={$t('collectibles.trackedPlayers')} value={formatNumber(data.totals.trackedPlayers)}>
      <Trophy slot="icon" size={15} strokeWidth={2} aria-hidden="true" />
    </StatCard>
  </div>

  <section class="panel" style="margin-top: 12px;">
    <div class="panel-head"><h2>{$t('collectibles.collectionsTitle')}</h2></div>
    {#if (data.collections || []).length === 0}
      <EmptyState message={$t('collectibles.noCollections')} />
    {:else}
      <div class="table-wrap">
        <table>
          <thead>
            <tr>
              <th>{$t('collectibles.colCollection')}</th>
              <th>{$t('collectibles.colCode')}</th>
              <th>{$t('collectibles.colItems')}</th>
              <th>{$t('collectibles.colScore')}</th>
              <th>{$t('collectibles.colBoost')}</th>
              <th>{$t('collectibles.colStatus')}</th>
              <th>{$t('collectibles.colReleased')}</th>
            </tr>
          </thead>
          <tbody>
            {#each data.collections as collection}
              <tr
                on:click={() => (expanded = expanded === collection.id ? null : collection.id)}
                style="cursor: pointer;"
                class:selected={expanded === collection.id}
              >
                <td>{collection.name}</td>
                <td><code>{collection.collectionCode}</code></td>
                <td>
                  {formatNumber(collection.itemCount)}
                  {#if collection.unresolvedItems > 0}
                    <span class="status-badge status-badge--bad">
                      {$t('collectibles.unresolvedCount', { count: collection.unresolvedItems })}
                    </span>
                  {/if}
                </td>
                <td>{formatNumber(collection.totalScore)}</td>
                <td>{formatNumber(collection.boostScore)}</td>
                <td>{collection.status}</td>
                <td>{collection.releasedAt ? formatDate(collection.releasedAt) : '—'}</td>
              </tr>
              {#if expanded === collection.id}
                <tr>
                  <td colspan="7">
                    <div class="table-wrap">
                      <table>
                        <thead>
                          <tr>
                            <th>{$t('collectibles.colItem')}</th>
                            <th>{$t('collectibles.colRarity')}</th>
                            <th>{$t('collectibles.colItemScore')}</th>
                            <th>{$t('collectibles.colResolved')}</th>
                          </tr>
                        </thead>
                        <tbody>
                          {#each collection.items || [] as item}
                            <tr>
                              <td>
                                <span class="cell">
                                  <AssetImage src={item.iconUrl} alt={item.productCode} size={32} />
                                  <code>{item.productCode}</code>
                                </span>
                              </td>
                              <td>{item.rarity || '—'}</td>
                              <td>{formatNumber(item.score)}</td>
                              <td>
                                {#if item.resolved}
                                  <span class="status-badge status-badge--ok">{$t('collectibles.resolved')}</span>
                                {:else}
                                  <span class="status-badge status-badge--bad">{$t('collectibles.missingFurni')}</span>
                                {/if}
                              </td>
                            </tr>
                          {:else}
                            <tr><td colspan="4" class="muted">{$t('collectibles.noItems')}</td></tr>
                          {/each}
                        </tbody>
                      </table>
                    </div>
                  </td>
                </tr>
              {/if}
            {/each}
          </tbody>
        </table>
      </div>
    {/if}
  </section>

  <section class="panel" style="margin-top: 12px;">
    <div class="panel-head"><h2>{$t('collectibles.leaderboardTitle')}</h2></div>
    <div class="table-wrap">
      <table>
        <thead>
          <tr>
            <th>{$t('collectibles.colPlayer')}</th>
            <th>{$t('collectibles.colCollectorScore')}</th>
          </tr>
        </thead>
        <tbody>
          {#each data.topCollectors || [] as row}
            <tr>
              <td><EntityLink type="player" id={row.playerId} label={row.playerName} {openPlayer} /></td>
              <td>{formatNumber(row.score)}</td>
            </tr>
          {:else}
            <tr><td colspan="2" class="muted">{$t('collectibles.noCollectors')}</td></tr>
          {/each}
        </tbody>
      </table>
    </div>
  </section>
{/if}

<style>
  tr.selected {
    background: var(--surface-raised, rgba(255, 255, 255, 0.04));
  }

  .cell {
    display: inline-flex;
    align-items: center;
    gap: 8px;
  }
</style>
