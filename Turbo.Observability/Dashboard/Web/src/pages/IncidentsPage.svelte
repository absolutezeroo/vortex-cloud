<script>
  import { onMount } from 'svelte';
  import { apiGet } from '../lib/api.js';
  import { formatDate } from '../lib/format.js';

  let data = null;
  let error = '';

  async function refresh() {
    try {
      error = '';
      data = await apiGet('/api/incidents');
    } catch (err) {
      error = err.message;
    }
  }

  onMount(refresh);
</script>

<section class="panel">
  <div class="panel-head">
    <h2>Incident signals</h2>
    <button type="button" on:click={refresh}>Refresh</button>
  </div>
  {#if error}<p class="empty-state danger">{error}</p>{/if}
  <div class="incident-banner" class:critical={data?.overall === 'Critical'} class:degraded={data?.overall === 'Degraded'}>
    <strong>{data?.overall || '-'}</strong>
    <span>{formatDate(data?.generatedAt)}</span>
  </div>
  <div class="incident-list">
    {#each data?.signals || [] as signal}
      <article>
        <strong>{signal.name || signal.code}</strong>
        <span>{signal.status}</span>
        <p>{signal.summary}</p>
      </article>
    {:else}
      <p class="muted">No active incident signal.</p>
    {/each}
  </div>
</section>
