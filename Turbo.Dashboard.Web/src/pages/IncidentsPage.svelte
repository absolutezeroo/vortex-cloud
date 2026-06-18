<script>
  import { onMount } from 'svelte';
  import { apiGet } from '../lib/api.js';
  import { formatDate } from '../lib/format.js';
  import AccessDeniedNotice from '../components/AccessDeniedNotice.svelte';
  import { isPermissionDeniedError } from '../lib/permissions.js';

  let data = null;
  let error = '';
  let forbidden = false;

  async function refresh() {
    forbidden = false;
    error = '';

    try {
      data = await apiGet('/api/incidents');
    } catch (err) {
      if (isPermissionDeniedError(err)) {
        forbidden = true;
        error = '';
        return;
      }

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

  {#if forbidden}
    <AccessDeniedNotice message="Vous n'avez pas l'autorisation d'accéder aux incidents." />
  {:else if error}
    <p class="empty-state danger">{error}</p>
  {/if}

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
