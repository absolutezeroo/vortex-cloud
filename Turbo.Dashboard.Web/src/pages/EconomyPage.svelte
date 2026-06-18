<script>
  import { onMount } from 'svelte';
  import { apiGet } from '../lib/api.js';
  import { formatDate } from '../lib/format.js';
  import EntityLink from '../components/EntityLink.svelte';
  import AccessDeniedNotice from '../components/AccessDeniedNotice.svelte';
  import { isPermissionDeniedError } from '../lib/permissions.js';
  import { openPlayer, openItem } from '../lib/session.js';

  let player = '';
  let rows = [];
  let error = '';
  let forbidden = false;

  async function refresh() {
    const params = new URLSearchParams({ limit: '80' });
    if (player.trim()) {
      params.set('player', player.trim());
    }

    forbidden = false;
    error = '';

    try {
      const data = await apiGet(`/api/economy?${params}`);
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
    <h2>Economy ledger</h2>
    <button type="button" on:click={refresh}>Refresh</button>
  </div>
  <form class="toolbar" on:submit|preventDefault={refresh}>
    <input bind:value={player} placeholder="player id" />
    <button type="submit">Load</button>
  </form>

  {#if forbidden}
    <AccessDeniedNotice message="Vous n'avez pas l'autorisation d'accéder au ledger économie." />
  {:else if error}
    <p class="empty-state danger">{error}</p>
  {/if}

  <table>
    <thead><tr><th>Time</th><th>Player</th><th>Currency</th><th>Delta</th><th>After</th><th>Reason</th></tr></thead>
    <tbody>
      {#each rows as row}
        <tr>
          <td>{formatDate(row.occurredAt)}</td>
          <td><EntityLink id={row.playerId} label={row.playerName || ''} {openPlayer} {openItem} /></td>
          <td>{row.currency}</td>
          <td class:positive={Number(row.delta) > 0} class:negative={Number(row.delta) < 0}>{row.delta}</td>
          <td>{row.balanceAfter}</td>
          <td>{row.reason}</td>
        </tr>
      {:else}
        <tr><td colspan="6" class="muted">No economy rows.</td></tr>
      {/each}
    </tbody>
  </table>
</section>
