<script>
  import { onMount } from 'svelte';
  import { apiGet } from '../lib/api.js';
  import { formatNumber } from '../lib/format.js';
  import { isPermissionDeniedError } from '../lib/permissions.js';
  import { openPlayer } from '../lib/session.js';
  import AccessDeniedNotice from '../components/AccessDeniedNotice.svelte';
  import EntityLink from '../components/EntityLink.svelte';
  import LineChart from '../components/LineChart.svelte';
  import StatCard from '../components/StatCard.svelte';
  import { PawPrint, Hash } from '@lucide/svelte';
  import { t } from '../lib/i18n.js';

  const granularities = ['day', 'month', 'year'];

  function granularityLabel(value, translator) {
    return translator(`common.granularity${value.charAt(0).toUpperCase()}${value.slice(1)}`);
  }

  let since = '';
  let until = '';
  let granularity = 'day';
  let loading = false;
  let forbidden = false;
  let error = '';
  let data = null;

  function toLocalDateValue(value) {
    const date = new Date(value);
    return Number.isNaN(date.getTime()) ? '' : date.toISOString().slice(0, 10);
  }

  function setDefaultWindow() {
    const end = new Date();
    const start = new Date(end.getTime() - 30 * 24 * 60 * 60 * 1000);
    since = toLocalDateValue(start);
    until = toLocalDateValue(end);
  }

  async function refresh() {
    loading = true;
    error = '';
    forbidden = false;

    const params = new URLSearchParams({ granularity });
    if (since) params.set('since', new Date(`${since}T00:00:00`).toISOString());
    if (until) params.set('until', new Date(`${until}T23:59:59`).toISOString());

    try {
      data = await apiGet(`/api/v1/pets/stats?${params}`);
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

  $: growthSeries = data
    ? [
        {
          name: $t('petsStats.totalPets'),
          color: 'var(--accent)',
          points: (data.growth || []).map((p) => ({ label: p.label, value: p.petsCreated })),
        },
      ]
    : [];

  onMount(() => {
    setDefaultWindow();
    void refresh();
  });
</script>

<section class="panel">
  <div class="panel-head"><h2>{$t('petsStats.title')}</h2></div>
  <p class="muted">{$t('petsStats.description')}</p>

  <form class="toolbar-grid" on:submit|preventDefault={refresh}>
    <label>
      {$t('common.since')}
      <input type="date" bind:value={since} />
    </label>
    <label>
      {$t('common.until')}
      <input type="date" bind:value={until} />
    </label>
    <label>
      {$t('common.granularity')}
      <select bind:value={granularity}>
        {#each granularities as g}
          <option value={g}>{granularityLabel(g, $t)}</option>
        {/each}
      </select>
    </label>
    <button type="submit" disabled={loading}>{$t('common.refresh')}</button>
  </form>

  {#if loading}
    <p class="muted">{$t('common.loading')}</p>
  {:else if forbidden}
    <AccessDeniedNotice message={$t('petsStats.accessDenied')} />
  {:else if error}
    <p class="empty-state danger">{error}</p>
  {/if}
</section>

{#if data}
  <div class="metric-grid" style="margin-top: 12px;">
    <StatCard label={$t('petsStats.totalPets')} value={formatNumber(data.totals.totalPets)}>
      <PawPrint slot="icon" size={15} strokeWidth={2} aria-hidden="true" />
    </StatCard>
    <StatCard label={$t('petsStats.avgLevel')} value={data.totals.avgLevel}>
      <Hash slot="icon" size={15} strokeWidth={2} aria-hidden="true" />
    </StatCard>
    <StatCard label={$t('petsStats.avgEnergy')} value={data.totals.avgEnergy}>
      <Hash slot="icon" size={15} strokeWidth={2} aria-hidden="true" />
    </StatCard>
    <StatCard label={$t('petsStats.avgNutrition')} value={data.totals.avgNutrition}>
      <Hash slot="icon" size={15} strokeWidth={2} aria-hidden="true" />
    </StatCard>
    <StatCard label={$t('petsStats.breedablePets')} value={formatNumber(data.totals.breedablePets)}>
      <PawPrint slot="icon" size={15} strokeWidth={2} aria-hidden="true" />
    </StatCard>
    <StatCard label={$t('petsStats.bredPets')} value={formatNumber(data.totals.bredPets)}>
      <PawPrint slot="icon" size={15} strokeWidth={2} aria-hidden="true" />
    </StatCard>
  </div>

  <div class="panel" style="margin-top: 12px;">
    <div class="panel-head"><h2>{$t('petsStats.growthChartTitle', { granularity: granularityLabel(granularity, $t) })}</h2></div>
    <LineChart series={growthSeries} valueFormatter={(v) => formatNumber(v)} />
  </div>

  <div class="panel" style="margin-top: 12px;">
    <div class="panel-head"><h2>{$t('petsStats.byTypeTitle')}</h2></div>
    <div class="table-wrap">
      <table>
        <thead><tr><th>{$t('petsStats.colType')}</th><th>{$t('petsStats.colCount')}</th></tr></thead>
        <tbody>
          {#each data.byType || [] as row}
            <tr><td>{row.type}</td><td>{formatNumber(row.count)}</td></tr>
          {:else}
            <tr><td colspan="2" class="muted">{$t('petsStats.noPets')}</td></tr>
          {/each}
        </tbody>
      </table>
    </div>
  </div>

  <div class="panel" style="margin-top: 12px;">
    <div class="panel-head"><h2>{$t('petsStats.byRarityTitle')}</h2></div>
    <div class="table-wrap">
      <table>
        <thead><tr><th>{$t('petsStats.colRarity')}</th><th>{$t('petsStats.colCount')}</th></tr></thead>
        <tbody>
          {#each data.byRarity || [] as row}
            <tr><td>{row.rarityLevel}</td><td>{formatNumber(row.count)}</td></tr>
          {:else}
            <tr><td colspan="2" class="muted">{$t('petsStats.noPets')}</td></tr>
          {/each}
        </tbody>
      </table>
    </div>
  </div>

  <div class="panel" style="margin-top: 12px;">
    <div class="panel-head"><h2>{$t('petsStats.topOwnersTitle')}</h2></div>
    <div class="table-wrap">
      <table>
        <thead><tr><th>{$t('petsStats.colOwner')}</th><th>{$t('petsStats.colPetCount')}</th></tr></thead>
        <tbody>
          {#each data.topOwners || [] as row}
            <tr>
              <td><EntityLink type="player" id={row.ownerId} label={row.ownerName} {openPlayer} /></td>
              <td>{formatNumber(row.petCount)}</td>
            </tr>
          {:else}
            <tr><td colspan="2" class="muted">{$t('petsStats.noPets')}</td></tr>
          {/each}
        </tbody>
      </table>
    </div>
  </div>
{/if}
