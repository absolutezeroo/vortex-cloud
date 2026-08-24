<script>

  import { onMount } from 'svelte';
  import PickerModal from '../components/PickerModal.svelte';
  import { apiGet } from '../lib/api.js';
  import { formatDate } from '../lib/format.js';
  import EntityLink from '../components/EntityLink.svelte';
  import AccessDeniedNotice from '../components/AccessDeniedNotice.svelte';
  import { isPermissionDeniedError } from '../lib/permissions.js';
  import { openPlayer, openItem } from '../lib/session.js';
  import { t } from '../lib/i18n.js';

  let picking = $state(null);
  let playerName = $state('');
  let player = $state('');
  let rows = $state([]);
  let error = $state('');
  let forbidden = $state(false);

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
      <div class="head-actions">
        <button type="button" onclick={refresh} class="warning">{$t('common.refresh')}</button>
        <button type="button" class="ghost-button" onclick={() => (picking = 'player')}>
          {playerName || (player ? `#${player}` : $t('economy.load'))}
        </button>
      </div>
  </div>
  <p class="muted">
    {$t('economy.description')}
  </p>
</section>

<section class="panel">

</section>

<section class="panel">

  {#if forbidden}
    <AccessDeniedNotice message={$t('economy.accessDenied')} />
  {:else if error}
    <p class="empty-state danger" role="alert">{error}</p>
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

{#if picking}
    {#if picking === 'player'}
      <PickerModal kind="user" onSelect={(item) => { player = String(item.id); playerName = item.name; picking = null; refresh(); }} onClose={() => (picking = null)} />
    {/if}
{/if}
