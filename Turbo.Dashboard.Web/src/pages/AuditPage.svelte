<script>
  import { onMount } from 'svelte';
  import { apiGet } from '../lib/api.js';
  import { compactCorrelation, formatDate, summarizeData } from '../lib/format.js';
  import EntityLink from '../components/EntityLink.svelte';
  import AccessDeniedNotice from '../components/AccessDeniedNotice.svelte';
  import { isPermissionDeniedError } from '../lib/permissions.js';
  import { openPlayer, openItem } from '../lib/session.js';

  let rows = [];
  let error = '';
  let forbidden = false;

  async function refresh() {
    forbidden = false;
    error = '';

    try {
      const data = await apiGet('/api/audit?limit=80');
      rows = data.items || [];
    } catch (err) {
      if (isPermissionDeniedError(err)) {
        forbidden = true;
        error = '';
        rows = [];
        return;
      }

      error = err.message;
      rows = [];
    }
  }

  onMount(refresh);
</script>

<section class="panel">
  <div class="panel-head">
    <h2>Recent audit events</h2>
    <button type="button" on:click={refresh}>Refresh</button>
  </div>

  {#if forbidden}
    <AccessDeniedNotice message="Vous n'avez pas l'autorisation d'accéder au journal d'audit." />
  {:else if error}
    <p class="empty-state danger">{error}</p>
  {/if}

  <table>
    <thead><tr><th>Time</th><th>Category</th><th>Action</th><th>Actor</th><th>Target</th><th>Result</th><th>Data</th><th>CID</th></tr></thead>
    <tbody>
      {#each rows as row}
        <tr>
          <td>{formatDate(row.occurredAt)}</td>
          <td>{row.category}</td>
          <td>{row.action}</td>
          <td><EntityLink id={row.actorPlayerId} label={row.actorName || ''} {openPlayer} {openItem} /></td>
          <td><EntityLink id={row.targetPlayerId} label={row.targetName || ''} {openPlayer} {openItem} /></td>
          <td>{row.result}</td>
          <td class="truncate">{summarizeData(row.data)}</td>
          <td>{compactCorrelation(row.correlationId)}</td>
        </tr>
      {:else}
        <tr><td colspan="8" class="muted">No audit rows.</td></tr>
      {/each}
    </tbody>
  </table>
</section>
