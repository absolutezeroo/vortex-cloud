<script>
  import { onMount } from 'svelte';
  import { apiGet } from '../lib/api.js';
  import { formatNumber } from '../lib/format.js';
  import { isPermissionDeniedError } from '../lib/permissions.js';
  import AccessDeniedNotice from '../components/AccessDeniedNotice.svelte';
  import AssetImage from '../components/AssetImage.svelte';
  import { Package } from '@lucide/svelte';
  import { t } from '../lib/i18n.js';

  let loading = false;
  let forbidden = false;
  let error = '';
  let data = null;

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
    <button type="button" on:click={refresh} disabled={loading}>{$t('common.refresh')}</button>
  </div>
  <p class="muted">{$t('wiredStats.description')}</p>

  {#if loading}
    <p class="muted">{$t('common.loading')}</p>
  {:else if forbidden}
    <AccessDeniedNotice message={$t('wiredStats.accessDenied')} />
  {:else if error}
    <p class="empty-state danger">{error}</p>
  {/if}
</section>

{#if data}
  <div class="metric-grid" style="margin-top: 12px;">
    <article><span>{$t('wiredStats.totalWiredPlaced')}</span><strong>{formatNumber(data.totals.totalWiredPlaced)}</strong></article>
    <article><span>{$t('wiredStats.roomsWithWired')}</span><strong>{formatNumber(data.totals.roomsWithWired)}</strong></article>
  </div>

  <div class="panel" style="margin-top: 12px;">
    <div class="panel-head"><h2>{$t('wiredStats.byCategoryTitle')}</h2></div>
    <div class="table-wrap">
      <table>
        <thead><tr><th>{$t('wiredStats.colCategory')}</th><th>{$t('wiredStats.colCount')}</th></tr></thead>
        <tbody>
          {#each data.byCategory || [] as row}
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
    <div class="table-wrap">
      <table>
        <thead><tr><th>{$t('wiredStats.colLogic')}</th><th>{$t('wiredStats.colCount')}</th></tr></thead>
        <tbody>
          {#each data.byLogic || [] as row}
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
    <div class="table-wrap">
      <table>
        <thead><tr><th>{$t('wiredStats.colRoom')}</th><th>{$t('wiredStats.colWiredCount')}</th></tr></thead>
        <tbody>
          {#each data.topRooms || [] as row}
            <tr><td>{row.roomName}</td><td>{formatNumber(row.wiredCount)}</td></tr>
          {:else}
            <tr><td colspan="2" class="muted">{$t('wiredStats.noWired')}</td></tr>
          {/each}
        </tbody>
      </table>
    </div>
  </div>
{/if}
