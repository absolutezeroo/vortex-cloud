<script>
  import { onMount } from 'svelte';
  import { apiGet } from '../lib/api.js';
  import { compactCorrelation, formatDate, summarizeData } from '../lib/format.js';
  import EntityLink from '../components/EntityLink.svelte';
  import AccessDeniedNotice from '../components/AccessDeniedNotice.svelte';
  import Pagination from '../components/Pagination.svelte';
  import { isPermissionDeniedError } from '../lib/permissions.js';
  import { openPlayer, openItem } from '../lib/session.js';
  import { t } from '../lib/i18n.js';

  const categoryOptions = [
    '',
    'Auth',
    'Staff',
    'Moderation',
    'Economy',
    'Item',
    'Room',
    'Security',
    'Social',
    'System',
    'RentableSpace',
  ];

  const categoryColors = {
    Auth: 'var(--accent)',
    Staff: '#9f6ce1',
    Moderation: 'var(--danger)',
    Economy: 'var(--ok)',
    Item: 'var(--warning)',
    Room: '#e0995e',
    Security: '#df6f7b',
    Social: '#4fb3bf',
    System: '#64748b',
    RentableSpace: '#4fae8a',
    other: '#64748b',
  };

  const resultBadgeClass = {
    Success: 'status-badge--ok',
    Denied: 'status-badge--warn',
    Failed: 'status-badge--bad',
  };

  let since = '';
  let until = '';
  let actor = '';
  let target = '';
  let category = '';
  let action = '';
  let limit = 50;
  let page = 1;

  let rows = [];
  let total = 0;
  let loading = false;
  let error = '';
  let forbidden = false;

  $: totalPages = Math.max(1, Math.ceil(total / limit));

  function categoryColor(value) {
    return categoryColors[value] || categoryColors.other;
  }

  // Takes the translator function itself (not just a locale flag) so template call sites can pass
  // $t explicitly and stay reactive -- see the ApiExplorerPage `filtered` reactivity note: a
  // helper that reads $t only inside its body, with $t absent from the template expression's own
  // text, is invisible to Svelte's per-expression dependency tracking and won't re-render on
  // locale change.
  function categoryLabel(value, translator) {
    return value ? translator(`audit.categories.${value}`) : translator('audit.allCategories');
  }

  function resultLabel(value, translator) {
    if (value === 'Success') return translator('common.resultSuccess');
    if (value === 'Denied') return translator('common.resultDenied');
    if (value === 'Failed') return translator('common.resultFailed');
    return value;
  }

  function buildParams() {
    const params = new URLSearchParams({ limit: String(limit), page: String(page) });

    if (since) params.set('since', new Date(since).toISOString());
    if (until) params.set('until', new Date(until).toISOString());
    if (actor.trim()) params.set('actor', actor.trim());
    if (target.trim()) params.set('target', target.trim());
    if (category) params.set('category', category);
    if (action.trim()) params.set('action', action.trim());

    return params;
  }

  async function refresh() {
    loading = true;
    error = '';
    forbidden = false;

    try {
      const data = await apiGet(`/api/v1/forensics/audit?${buildParams()}`);
      rows = data.items || [];
      total = data.total || 0;
    } catch (err) {
      if (isPermissionDeniedError(err)) {
        forbidden = true;
        rows = [];
        total = 0;
        return;
      }

      error = err.message;
      rows = [];
      total = 0;
    } finally {
      loading = false;
    }
  }

  function applyFilters() {
    page = 1;
    void refresh();
  }

  function goToPage(next) {
    page = Math.min(totalPages, Math.max(1, next));
    void refresh();
  }

  onMount(refresh);
</script>

<section class="panel">
  <div class="panel-head">
    <h2>{$t('audit.title')}</h2>
    <button type="button" on:click={refresh} disabled={loading}>{$t('common.refresh')}</button>
  </div>

  <form class="toolbar-grid" on:submit|preventDefault={applyFilters}>
    <label>
      {$t('audit.since')}
      <input type="datetime-local" bind:value={since} />
    </label>
    <label>
      {$t('audit.until')}
      <input type="datetime-local" bind:value={until} />
    </label>
    <label>
      {$t('audit.actor')}
      <input type="text" bind:value={actor} placeholder={$t('audit.playerIdPlaceholder')} />
    </label>
    <label>
      {$t('audit.target')}
      <input type="text" bind:value={target} placeholder={$t('audit.playerIdPlaceholder')} />
    </label>
    <label>
      {$t('audit.category')}
      <select bind:value={category}>
        {#each categoryOptions as option}
          <option value={option}>{categoryLabel(option, $t)}</option>
        {/each}
      </select>
    </label>
    <label>
      {$t('audit.action')}
      <input type="text" bind:value={action} placeholder={$t('audit.actionPlaceholder')} />
    </label>
    <label>
      {$t('audit.pageSize')}
      <input type="number" min="10" max="500" bind:value={limit} />
    </label>
    <button type="submit">{$t('common.filter')}</button>
  </form>

  {#if forbidden}
    <AccessDeniedNotice message={$t('audit.accessDenied')} />
  {:else if error}
    <p class="empty-state danger">{error}</p>
  {:else if loading}
    <p class="muted">{$t('audit.loadingEvents')}</p>
  {:else}
    <p class="muted">{$t('audit.eventsFound', { count: total })}</p>
  {/if}

  <div class="table-wrap">
    <table>
      <thead><tr><th>{$t('audit.colTime')}</th><th>{$t('audit.colCategory')}</th><th>{$t('audit.colAction')}</th><th>{$t('audit.colActor')}</th><th>{$t('audit.colTarget')}</th><th>{$t('audit.colResult')}</th><th>{$t('audit.colData')}</th><th>{$t('audit.colCid')}</th></tr></thead>
      <tbody>
        {#each rows as row}
          <tr>
            <td>{formatDate(row.occurredAt)}</td>
            <td>
              <span class="category-badge" style={`border-left-color: ${categoryColor(row.category)};`}>
                {categoryLabel(row.category, $t)}
              </span>
            </td>
            <td><code>{row.action}</code></td>
            <td><EntityLink id={row.actorPlayerId} label={row.actorName || ''} {openPlayer} {openItem} /></td>
            <td><EntityLink id={row.targetPlayerId} label={row.targetName || ''} {openPlayer} {openItem} /></td>
            <td>
              <span class={`status-badge ${resultBadgeClass[row.result] || 'status-badge--unknown'}`}>
                {resultLabel(row.result, $t)}
              </span>
            </td>
            <td class="truncate" title={summarizeData(row.data)}>{summarizeData(row.data)}</td>
            <td>{compactCorrelation(row.correlationId)}</td>
          </tr>
        {:else}
          <tr><td colspan="8" class="muted">{$t('audit.noRows')}</td></tr>
        {/each}
      </tbody>
    </table>
  </div>

  {#if total > 0}
    <Pagination
      page={page}
      pageCount={totalPages}
      pageWord={$t('common.page')}
      prevLabel={$t('common.prev')}
      nextLabel={$t('common.next')}
      disabled={loading}
      on:change={(e) => goToPage(e.detail)}
    />
  {/if}
</section>

<style>
  .category-badge {
    display: inline-flex;
    align-items: center;
    border-left: 3px solid var(--muted);
    padding: 2px 8px;
    font-size: 0.8rem;
    color: var(--muted-strong);
  }
</style>
