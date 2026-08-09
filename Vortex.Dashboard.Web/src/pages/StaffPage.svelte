<script>
  // Who can do what. The two cross-checks are the point of the page: a role granting a capability
  // string that no longer exists grants nothing at all, and a declared capability no role grants is
  // a feature nobody in the hotel can reach.
  import { onMount } from 'svelte';
  import { apiGet } from '../lib/api.js';
  import { formatNumber, formatDate, formatDuration } from '../lib/format.js';
  import { isPermissionDeniedError } from '../lib/permissions.js';
  import AccessDeniedNotice from '../components/AccessDeniedNotice.svelte';
  import EmptyState from '../components/EmptyState.svelte';
  import StatCard from '../components/StatCard.svelte';
  import { ShieldCheck, KeyRound, Users, Gavel } from '@lucide/svelte';
  import { t } from '../lib/i18n.js';

  let loading = false;
  let forbidden = false;
  let error = '';
  let data = null;
  let expanded = null;

  async function refresh() {
    loading = true;
    error = '';
    forbidden = false;

    try {
      data = await apiGet('/api/v1/staff');
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
  <div class="panel-head"><h2>{$t('staff.title')}</h2></div>
  <p class="muted">{$t('staff.description')}</p>
  <div class="toolbar">
    <button type="button" on:click={refresh} disabled={loading}>{$t('common.refresh')}</button>
  </div>

  {#if loading}
    <p class="muted">{$t('common.loading')}</p>
  {:else if forbidden}
    <AccessDeniedNotice message={$t('staff.accessDenied')} />
  {:else if error}
    <p class="empty-state danger">{error}</p>
  {/if}
</section>

{#if data}
  <div class="metric-grid" style="margin-top: 12px;">
    <StatCard label={$t('staff.roles')} value={formatNumber(data.totals.roleCount)}>
      <ShieldCheck slot="icon" size={15} strokeWidth={2} aria-hidden="true" />
    </StatCard>
    <StatCard label={$t('staff.accounts')} value={formatNumber(data.totals.staffAccounts)}>
      <Users slot="icon" size={15} strokeWidth={2} aria-hidden="true" />
    </StatCard>
    <StatCard
      label={$t('staff.capabilities')}
      value={formatNumber(data.totals.declaredCapabilities)}
      sub={$t('staff.grantedCapabilities', { count: formatNumber(data.totals.grantedCapabilities) })}
    >
      <KeyRound slot="icon" size={15} strokeWidth={2} aria-hidden="true" />
    </StatCard>
    <StatCard label={$t('staff.presets')} value={formatNumber(data.totals.presetCount)}>
      <Gavel slot="icon" size={15} strokeWidth={2} aria-hidden="true" />
    </StatCard>
    <StatCard label={$t('staff.activeBans')} value={formatNumber(data.totals.activeBans)}>
      <Gavel slot="icon" size={15} strokeWidth={2} aria-hidden="true" />
    </StatCard>
  </div>

  <section class="panel" style="margin-top: 12px;">
    <div class="panel-head"><h2>{$t('staff.rolesTitle')}</h2></div>
    <div class="table-wrap">
      <table>
        <thead>
          <tr>
            <th>{$t('staff.colRole')}</th>
            <th>{$t('staff.colKey')}</th>
            <th>{$t('staff.colCapabilities')}</th>
            <th>{$t('staff.colHolders')}</th>
            <th>{$t('staff.colIssues')}</th>
          </tr>
        </thead>
        <tbody>
          {#each data.roles || [] as role}
            <tr
              on:click={() => (expanded = expanded === role.id ? null : role.id)}
              style="cursor: pointer;"
              class:selected={expanded === role.id}
            >
              <td>{role.name}</td>
              <td><code>{role.key}</code></td>
              <td>
                {formatNumber(role.capabilityCount)}
                {#if role.wildcard}
                  <span class="status-badge status-badge--warn">{$t('staff.wildcard')}</span>
                {/if}
              </td>
              <td>{formatNumber(role.holders)}</td>
              <td>
                {#if role.unknownCapabilities.length > 0}
                  <span class="status-badge status-badge--bad">
                    {$t('staff.unknownCount', { count: role.unknownCapabilities.length })}
                  </span>
                {:else}
                  <span class="status-badge status-badge--ok">{$t('staff.ok')}</span>
                {/if}
              </td>
            </tr>
            {#if expanded === role.id}
              <tr>
                <td colspan="5">
                  {#if role.unknownCapabilities.length > 0}
                    <p class="empty-state danger">
                      {$t('staff.unknownExplained')}
                      {#each role.unknownCapabilities as cap}<code class="chip">{cap}</code>{/each}
                    </p>
                  {/if}
                  <div class="cap-list">
                    {#each role.capabilities as cap}
                      <code class="chip">{cap}</code>
                    {:else}
                      <span class="muted">{$t('staff.noCapabilities')}</span>
                    {/each}
                  </div>
                </td>
              </tr>
            {/if}
          {:else}
            <tr><td colspan="5" class="muted">{$t('staff.noRoles')}</td></tr>
          {/each}
        </tbody>
      </table>
    </div>
  </section>

  <section class="panel" style="margin-top: 12px;">
    <div class="panel-head"><h2>{$t('staff.ungrantedTitle')}</h2></div>
    <p class="muted">{$t('staff.ungrantedDescription')}</p>
    {#if data.wildcardExists}
      <EmptyState message={$t('staff.wildcardCovers')} />
    {:else if (data.ungrantedCapabilities || []).length === 0}
      <EmptyState message={$t('staff.ungrantedNone')} />
    {:else}
      <div class="cap-list">
        {#each data.ungrantedCapabilities as cap}
          <code class="chip">{cap}</code>
        {/each}
      </div>
    {/if}
  </section>

  <section class="panel" style="margin-top: 12px;">
    <div class="panel-head"><h2>{$t('staff.staffTitle')}</h2></div>
    <div class="table-wrap">
      <table>
        <thead>
          <tr>
            <th>{$t('staff.colEmail')}</th>
            <th>{$t('staff.colPlayers')}</th>
            <th>{$t('staff.colRoles')}</th>
            <th>{$t('staff.colCreated')}</th>
          </tr>
        </thead>
        <tbody>
          {#each data.staff || [] as account}
            <tr>
              <td>{account.email}</td>
              <td>{(account.playerNames || []).join(', ') || '—'}</td>
              <td>{(account.roles || []).join(', ')}</td>
              <td>{formatDate(account.createdAt)}</td>
            </tr>
          {:else}
            <tr><td colspan="4" class="muted">{$t('staff.noStaff')}</td></tr>
          {/each}
        </tbody>
      </table>
    </div>
  </section>

  <section class="panel" style="margin-top: 12px;">
    <div class="panel-head"><h2>{$t('staff.presetsTitle')}</h2></div>
    <p class="muted">{$t('staff.presetsDescription')}</p>
    <div class="table-wrap">
      <table>
        <thead>
          <tr>
            <th>{$t('staff.colKind')}</th>
            <th>{$t('staff.colIndex')}</th>
            <th>{$t('staff.colPresetName')}</th>
            <th>{$t('staff.colDuration')}</th>
            <th>{$t('staff.colMessage')}</th>
          </tr>
        </thead>
        <tbody>
          {#each data.presets || [] as preset}
            <tr>
              <td>{preset.kind}</td>
              <td>{preset.presetIndex}</td>
              <td>{preset.name}</td>
              <td>
                {preset.permanent ? $t('common.permanent') : formatDuration(preset.durationSeconds)}
              </td>
              <td class="truncate">{preset.message || '—'}</td>
            </tr>
          {:else}
            <tr><td colspan="5" class="muted">{$t('staff.noPresets')}</td></tr>
          {/each}
        </tbody>
      </table>
    </div>
  </section>
{/if}

<style>
  tr.selected {
    background: var(--surface-raised, rgba(255, 255, 255, 0.04));
  }

  .cap-list {
    display: flex;
    flex-wrap: wrap;
    gap: 6px;
  }

  .chip {
    padding: 2px 6px;
    border-radius: 6px;
    background: var(--surface-raised, rgba(255, 255, 255, 0.06));
    font-size: 0.78rem;
  }
</style>
