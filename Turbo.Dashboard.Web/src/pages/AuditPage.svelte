<script>
  import { onMount } from 'svelte';
  import { apiGet } from '../lib/api.js';
  import { compactCorrelation, formatDate, summarizeData } from '../lib/format.js';
  import EntityLink from '../components/EntityLink.svelte';
  import AccessDeniedNotice from '../components/AccessDeniedNotice.svelte';
  import { isPermissionDeniedError } from '../lib/permissions.js';
  import { openPlayer, openItem } from '../lib/session.js';

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
    <h2>Recent audit events</h2>
    <button type="button" on:click={refresh} disabled={loading}>Refresh</button>
  </div>

  <form class="toolbar-grid" on:submit|preventDefault={applyFilters}>
    <label>
      Since
      <input type="datetime-local" bind:value={since} />
    </label>
    <label>
      Until
      <input type="datetime-local" bind:value={until} />
    </label>
    <label>
      Actor
      <input type="text" bind:value={actor} placeholder="player id" />
    </label>
    <label>
      Target
      <input type="text" bind:value={target} placeholder="player id" />
    </label>
    <label>
      Category
      <select bind:value={category}>
        {#each categoryOptions as option}
          <option value={option}>{option || 'all categories'}</option>
        {/each}
      </select>
    </label>
    <label>
      Action
      <input type="text" bind:value={action} placeholder="e.g. moderation.kick" />
    </label>
    <label>
      Page size
      <input type="number" min="10" max="500" bind:value={limit} />
    </label>
    <button type="submit">Filter</button>
  </form>

  {#if forbidden}
    <AccessDeniedNotice message="Vous n'avez pas l'autorisation d'accéder au journal d'audit." />
  {:else if error}
    <p class="empty-state danger">{error}</p>
  {:else if loading}
    <p class="muted">Loading audit events...</p>
  {:else}
    <p class="muted">{total} event(s) found.</p>
  {/if}

  <div class="table-wrap">
    <table>
      <thead><tr><th>Time</th><th>Category</th><th>Action</th><th>Actor</th><th>Target</th><th>Result</th><th>Data</th><th>CID</th></tr></thead>
      <tbody>
        {#each rows as row}
          <tr>
            <td>{formatDate(row.occurredAt)}</td>
            <td>
              <span class="category-badge" style={`border-left-color: ${categoryColor(row.category)};`}>
                {row.category}
              </span>
            </td>
            <td><code>{row.action}</code></td>
            <td><EntityLink id={row.actorPlayerId} label={row.actorName || ''} {openPlayer} {openItem} /></td>
            <td><EntityLink id={row.targetPlayerId} label={row.targetName || ''} {openPlayer} {openItem} /></td>
            <td>
              <span class={`status-badge ${resultBadgeClass[row.result] || 'status-badge--unknown'}`}>
                {row.result}
              </span>
            </td>
            <td class="truncate" title={summarizeData(row.data)}>{summarizeData(row.data)}</td>
            <td>{compactCorrelation(row.correlationId)}</td>
          </tr>
        {:else}
          <tr><td colspan="8" class="muted">No audit rows.</td></tr>
        {/each}
      </tbody>
    </table>
  </div>

  {#if total > 0}
    <div class="pagination">
      <button type="button" class="ghost-button" on:click={() => goToPage(page - 1)} disabled={page <= 1 || loading}>
        ← Prev
      </button>
      <span class="muted">page {page} / {totalPages}</span>
      <button type="button" class="ghost-button" on:click={() => goToPage(page + 1)} disabled={page >= totalPages || loading}>
        Next →
      </button>
    </div>
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

  .pagination {
    display: flex;
    align-items: center;
    gap: 12px;
    margin-top: 12px;
  }
</style>
