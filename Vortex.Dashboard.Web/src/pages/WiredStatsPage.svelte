<script>
  import { onMount } from 'svelte';
  import { apiGet } from '../lib/api.js';
  import { formatNumber } from '../lib/format.js';
  import { isPermissionDeniedError } from '../lib/permissions.js';
  import AccessDeniedNotice from '../components/AccessDeniedNotice.svelte';
  import AssetImage from '../components/AssetImage.svelte';
  import StatCard from '../components/StatCard.svelte';
  import { Package, Cable, DoorOpen } from '@lucide/svelte';
  import TableFilter from '../components/TableFilter.svelte';
  import SortTh from '../components/SortTh.svelte';
  import { filterRows, sortRows } from '../lib/tableView.js';
  import { t } from '../lib/i18n.js';

  let loading = $state(false);
  let forbidden = $state(false);
  let error = $state('');
  let data = $state(null);

  // One filter + sort per table: byLogic alone is one row per registered logic key, which is the
  // table you cannot read by eye.
  let categoryQuery = $state('');
  let categorySort = $state({ key: '', dir: 'desc' });
  let logicQuery = $state('');
  let logicSort = $state({ key: '', dir: 'desc' });
  let roomQuery = $state('');
  let roomSort = $state({ key: '', dir: 'desc' });

  let categoryRows = $derived(data?.byCategory || []);
  let logicRows = $derived(data?.byLogic || []);
  let roomRows = $derived(data?.topRooms || []);

  let categoryView = $derived(sortRows(filterRows(categoryRows, categoryQuery), categorySort));
  let logicView = $derived(sortRows(filterRows(logicRows, logicQuery), logicSort));
  let roomView = $derived(sortRows(filterRows(roomRows, roomQuery), roomSort));

  const categoryKeys = {
    trigger: 'wiredStats.categoryTrigger',
    condition: 'wiredStats.categoryCondition',
    action: 'wiredStats.categoryAction',
    variable: 'wiredStats.categoryVariable',
    selector: 'wiredStats.categorySelector',
    addon: 'wiredStats.categoryAddon',
    other: 'wiredStats.categoryOther',
  };

  function categoryLabel(category, translator) {
    return translator(categoryKeys[category] || 'wiredStats.categoryOther');
  }

  async function refresh() {
    loading = true;
    error = '';
    forbidden = false;

    try {
      data = await apiGet('/api/v1/wired/stats');
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
  <div class="panel-head">
    <h2>{$t('wiredStats.title')}</h2>
    <button type="button" onclick={refresh} disabled={loading}>{$t('common.refresh')}</button>
  </div>
  <p class="muted">{$t('wiredStats.description')}</p>

  {#if loading}
    <p class="muted">{$t('common.loading')}</p>
  {:else if forbidden}
    <AccessDeniedNotice message={$t('wiredStats.accessDenied')} />
  {:else if error}
    <p class="empty-state danger" role="alert">{error}</p>
  {/if}
</section>

{#if data}
  <div class="metric-grid" style="margin-top: 12px;">
    <StatCard label={$t('wiredStats.totalWiredPlaced')} value={formatNumber(data.totals.totalWiredPlaced)}>
      {#snippet icon()}
        <Cable size={15} strokeWidth={2} aria-hidden="true" />
      {/snippet}
    </StatCard>
    <StatCard label={$t('wiredStats.roomsWithWired')} value={formatNumber(data.totals.roomsWithWired)}>
      {#snippet icon()}
        <DoorOpen size={15} strokeWidth={2} aria-hidden="true" />
      {/snippet}
    </StatCard>
  </div>

  <div class="panel" style="margin-top: 12px;">
    <div class="panel-head"><h2>{$t('wiredStats.byCategoryTitle')}</h2></div>
    <TableFilter bind:query={categoryQuery} shown={categoryView.length} total={categoryRows.length} />
    <div class="table-wrap">
      <table>
        <thead><tr>
          <SortTh label={$t('wiredStats.colCategory')} key="category" bind:sort={categorySort} initialDir="asc" />
          <SortTh label={$t('wiredStats.colCount')} key="count" bind:sort={categorySort} />
        </tr></thead>
        <tbody>
          {#each categoryView as row}
            <tr><td>{categoryLabel(row.category, $t)}</td><td>{formatNumber(row.count)}</td></tr>
          {:else}
            <tr><td colspan="2" class="muted">{$t('wiredStats.noWired')}</td></tr>
          {/each}
        </tbody>
      </table>
    </div>
  </div>

  <div class="panel" style="margin-top: 12px;">
    <div class="panel-head"><h2>{$t('wiredStats.byLogicTitle')}</h2></div>
    <TableFilter bind:query={logicQuery} shown={logicView.length} total={logicRows.length} />
    <div class="table-wrap">
      <table>
        <thead><tr>
          <SortTh label={$t('wiredStats.colLogic')} key="logic" bind:sort={logicSort} initialDir="asc" />
          <SortTh label={$t('wiredStats.colCount')} key="count" bind:sort={logicSort} />
        </tr></thead>
        <tbody>
          {#each logicView as row}
            <tr><td><span style="display: inline-flex; align-items: center; gap: 8px;"><AssetImage src={row.furniIconUrl} alt={row.logic} size={26} fallbackIcon={Package} /><code>{row.logic}</code></span></td><td>{formatNumber(row.count)}</td></tr>
          {:else}
            <tr><td colspan="2" class="muted">{$t('wiredStats.noWired')}</td></tr>
          {/each}
        </tbody>
      </table>
    </div>
  </div>

  <div class="panel" style="margin-top: 12px;">
    <div class="panel-head"><h2>{$t('wiredStats.topRoomsTitle')}</h2></div>
    <TableFilter bind:query={roomQuery} shown={roomView.length} total={roomRows.length} />
    <div class="table-wrap">
      <table>
        <thead><tr>
          <SortTh label={$t('wiredStats.colRoom')} key="roomName" bind:sort={roomSort} initialDir="asc" />
          <SortTh label={$t('wiredStats.colWiredCount')} key="wiredCount" bind:sort={roomSort} />
        </tr></thead>
        <tbody>
          {#each roomView as row}
            <tr><td>{row.roomName}</td><td>{formatNumber(row.wiredCount)}</td></tr>
          {:else}
            <tr><td colspan="2" class="muted">{$t('wiredStats.noWired')}</td></tr>
          {/each}
        </tbody>
      </table>
    </div>
  </div>
{/if}
