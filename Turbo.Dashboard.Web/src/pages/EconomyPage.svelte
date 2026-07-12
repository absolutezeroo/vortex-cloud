<script>
  import { onMount } from 'svelte';
  import { apiGet } from '../lib/api.js';
  import { formatDate } from '../lib/format.js';
  import EntityLink from '../components/EntityLink.svelte';
  import AccessDeniedNotice from '../components/AccessDeniedNotice.svelte';
  import { isPermissionDeniedError } from '../lib/permissions.js';
  import { openPlayer, openItem } from '../lib/session.js';
  import { t } from '../lib/i18n.js';

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
      const data = await apiGet(`/api/v1/economy/ledger?${params}`);
      rows = data.items || [];
    } catch (err) {
      if (isPermissionDeniedError(err)) {
        forbidden = true;
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
    <h2>{$t('economy.title')}</h2>
    <button type="button" on:click={refresh}>{$t('common.refresh')}</button>
  </div>
  <p class="muted">
    {$t('economy.description')}
  </p>
  <form class="toolbar" on:submit|preventDefault={refresh}>
    <input bind:value={player} placeholder={$t('audit.playerIdPlaceholder')} />
    <button type="submit">{$t('economy.load')}</button>
  </form>

  {#if forbidden}
    <AccessDeniedNotice message={$t('economy.accessDenied')} />
  {:else if error}
    <p class="empty-state danger">{error}</p>
  {/if}

  <table>
    <thead><tr><th>{$t('economy.colTime')}</th><th>{$t('economy.colPlayer')}</th><th>{$t('economy.colCurrency')}</th><th>{$t('economy.colDelta')}</th><th>{$t('economy.colAfter')}</th><th>{$t('economy.colReason')}</th></tr></thead>
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
        <tr><td colspan="6" class="muted">{$t('economy.noRows')}</td></tr>
      {/each}
    </tbody>
  </table>
</section>
