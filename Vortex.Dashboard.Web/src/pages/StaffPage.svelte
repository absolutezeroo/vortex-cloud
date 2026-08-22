<script>
  // Who can do what — and now, who gets to do what. The two cross-checks are still the point of the
  // read half: a role granting a capability string the code no longer declares grants nothing, and a
  // declared capability no role grants is a feature nobody can reach. The write half closes the
  // loop: the capability editor only offers keys that exist, so the first failure cannot be created
  // here at all.
  import { onMount } from 'svelte';
  import { SvelteSet } from 'svelte/reactivity';
  import { apiGet } from '../lib/api.js';
  import { formatNumber, formatDate, formatDuration } from '../lib/format.js';
  import { isPermissionDeniedError, hasDashboardCapability } from '../lib/permissions.js';
  import { CAPABILITIES } from '../lib/dashboardPermissions.js';
  import { identity } from '../lib/session.js';
  import { createWriteOps } from '../lib/writeOps.js';
  import AccessDeniedNotice from '../components/AccessDeniedNotice.svelte';
  import Drawer from '../components/Drawer.svelte';
  import PageHeader from '../components/PageHeader.svelte';
  import AssetImage from '../components/AssetImage.svelte';
  import ConfirmReasonModal from '../components/ConfirmReasonModal.svelte';
  import EmptyState from '../components/EmptyState.svelte';
  import OpResult from '../components/OpResult.svelte';
  import StatCard from '../components/StatCard.svelte';
  import Tabs from '../components/Tabs.svelte';
  import { ShieldCheck, KeyRound, Users, Gavel, User } from '@lucide/svelte';
  import { t } from '../lib/i18n.js';

  let loading = $state(false);
  let forbidden = $state(false);
  let error = $state('');
  let data = $state(null);
  let expanded = $state(null);

  // These sections are independent jobs that were stacked vertically, so reaching the last one
  // meant scrolling past every other. Nothing here is read against anything else -- which is
  // both what makes tabs right and what would have made them wrong.
  let tab = $state('roles');

  let roleForm = $state({ key: '', name: '' });
  let roleDraft = $state(null); // { id, key, name }
  let capabilityDraft = $state(null); // { roleId, selected: Set }
  let presetForm = $state(emptyPreset());
  let presetDraft = $state(null);

  // One modal drives every write, so the audited reason cannot be skipped on any of the small forms.
  // createWriteOps owns that whole cycle (stage -> confirm with reason -> remember it -> refresh);
  // the callback below is only the page's own "and clear the drafts" step.
  const ops = createWriteOps(async () => {
    roleDraft = null;
    capabilityDraft = null;
    presetDraft = null;
    roleForm = { key: '', name: '' };
    presetForm = emptyPreset();
    await refresh();
    if (accountResults.length > 0) await searchAccounts();
  });


  let accountQuery = $state('');
  let accountResults = $state([]);
  let accountSearching = $state(false);
  let assignRoleId = $state(0);

  let canManage = $derived(hasDashboardCapability($identity, CAPABILITIES.opsStaffManage));

  function emptyPreset() {
    return { kind: 0, presetIndex: 0, name: '', durationSeconds: null, message: '' };
  }

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

  async function searchAccounts() {
    accountSearching = true;

    try {
      const params = new URLSearchParams();
      if (accountQuery.trim()) params.set('q', accountQuery.trim());
      const response = await apiGet(`/api/v1/staff/accounts?${params}`);
      accountResults = response.items || [];
    } catch (err) {
      error = err.message;
    } finally {
      accountSearching = false;
    }
  }

  const ask = (endpoint, body, title, summary) => ops.ask(endpoint, body, title, summary);

  function startCapabilityEdit(role) {
    expanded = role.id;
    // SvelteSet, not Set: $state proxies this object but NOT a Set held inside it, so the
    // add/delete below would mutate without signalling and no checkbox would move.
    capabilityDraft = { roleId: role.id, role, selected: new SvelteSet(role.capabilities) };
  }

  function toggleCapability(key) {
    if (!capabilityDraft) return;

    if (capabilityDraft.selected.has(key)) {
      capabilityDraft.selected.delete(key);
    } else {
      capabilityDraft.selected.add(key);
    }
  }

  function toggleArea(group, on) {
    if (!capabilityDraft) return;

    for (const key of group.capabilities) {
      if (on) capabilityDraft.selected.add(key);
      else capabilityDraft.selected.delete(key);
    }
  }

  function saveCapabilities(role) {
    ask(
      '/api/v1/operations/staff/roles/capabilities',
      { roleId: role.id, capabilities: [...capabilityDraft.selected] },
      $t('staff.saveCapabilities'),
      $t('staff.saveCapabilitiesSummary', {
        role: role.name,
        count: capabilityDraft.selected.size,
      })
    );
  }

  function roleName(id) {
    return (data?.roles || []).find((r) => r.id === id)?.name ?? `#${id}`;
  }

  onMount(() => {
    void refresh();
  });
</script>

<section class="panel">
  <PageHeader title={$t('staff.title')} description={$t('staff.description')} />
  <p class="muted">{$t('staff.description')}</p>
  <div class="toolbar">
    <button type="button" onclick={refresh} disabled={loading}>{$t('common.refresh')}</button>
    {#if !canManage}
      <span class="muted">{$t('staff.readOnly')}</span>
    {/if}
  </div>

  {#if loading}
    <p class="muted">{$t('common.loading')}</p>
  {:else if forbidden}
    <AccessDeniedNotice message={$t('staff.accessDenied')} />
  {:else if error}
    <p class="empty-state danger" role="alert">{error}</p>
  {/if}

  {#if $ops.result}
    <OpResult result={$ops.result} />
  {/if}
</section>

{#if data}
  <div class="metric-grid" style="margin-top: 12px;">
    <StatCard label={$t('staff.roles')} value={formatNumber(data.totals.roleCount)}>
      {#snippet icon()}
        <ShieldCheck size={15} strokeWidth={2} aria-hidden="true" />
      {/snippet}
    </StatCard>
    <StatCard label={$t('staff.accounts')} value={formatNumber(data.totals.staffAccounts)}>
      {#snippet icon()}
        <Users size={15} strokeWidth={2} aria-hidden="true" />
      {/snippet}
    </StatCard>
    <StatCard
      label={$t('staff.capabilities')}
      value={formatNumber(data.totals.declaredCapabilities)}
      sub={$t('staff.grantedCapabilities', { count: formatNumber(data.totals.grantedCapabilities) })}
    >
      {#snippet icon()}
        <KeyRound size={15} strokeWidth={2} aria-hidden="true" />
      {/snippet}
    </StatCard>
    <StatCard label={$t('staff.presets')} value={formatNumber(data.totals.presetCount)}>
      {#snippet icon()}
        <Gavel size={15} strokeWidth={2} aria-hidden="true" />
      {/snippet}
    </StatCard>
    <StatCard label={$t('staff.activeBans')} value={formatNumber(data.totals.activeBans)}>
      {#snippet icon()}
        <Gavel size={15} strokeWidth={2} aria-hidden="true" />
      {/snippet}
    </StatCard>
  </div>

  <Tabs
    bind:active={tab}
    storageKey="staff"
    tabs={[
      { id: 'roles', label: $t('staff.tabRoles'), icon: ShieldCheck, count: data.roles?.length },
      { id: 'people', label: $t('staff.tabPeople'), icon: Users, count: data.staff?.length },
      { id: 'presets', label: $t('staff.tabPresets'), icon: Gavel, count: data.presets?.length },
    ]}
  />

  {#if tab === 'roles'}
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
            {#if canManage}<th></th>{/if}
          </tr>
        </thead>
        <tbody>
          {#each data.roles || [] as role}
            <tr class:selected={expanded === role.id}>
              <td>
                <button type="button" class="link-cell" onclick={() => (expanded = expanded === role.id ? null : role.id)}>
                  {role.name}
                </button>
              </td>
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
              {#if canManage}
                <td class="row-actions">
                  <button type="button" class="ghost-button" onclick={() => startCapabilityEdit(role)}>
                    {$t('staff.editCapabilities')}
                  </button>
                  <button type="button" class="ghost-button" onclick={() => (roleDraft = { ...role })}>
                    {$t('staff.rename')}
                  </button>
                  <button
                    type="button"
                    class="ghost-button danger"
                    onclick={() =>
                      ask(
                        '/api/v1/operations/staff/roles/delete',
                        { roleId: role.id },
                        $t('staff.deleteRole'),
                        $t('staff.deleteRoleSummary', { role: role.name })
                      )}
                  >
                    {$t('staff.delete')}
                  </button>
                </td>
              {/if}
            </tr>


            {#if expanded === role.id}
              <tr>
                <td colspan={canManage ? 6 : 5}>
                  {#if role.unknownCapabilities.length > 0}
                    <p class="empty-state danger" role="alert">
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
            <tr><td colspan={canManage ? 6 : 5} class="muted">{$t('staff.noRoles')}</td></tr>
          {/each}
        </tbody>
      </table>
    </div>

    {#if canManage}
      <form
        class="inline-form"
        onsubmit={(event) => {
          event.preventDefault();
          ask(
            '/api/v1/operations/staff/roles',
            { key: roleForm.key, name: roleForm.name },
            $t('staff.addRole'),
            $t('staff.addRoleSummary', { role: roleForm.name })
          );
        }}
      >
        <label>
          {$t('staff.colKey')}
          <input autocomplete="off" spellcheck="false" bind:value={roleForm.key} placeholder="moderator" />
        </label>
        <label>
          {$t('staff.colRole')}
          <input autocomplete="off" spellcheck="false" bind:value={roleForm.name} placeholder={$t('staff.rolePlaceholder')} />
        </label>
        <button type="submit" disabled={!roleForm.key.trim() || !roleForm.name.trim()}>
          {$t('staff.addRole')}
        </button>
      </form>
    {/if}
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
  {/if}

  {#if tab === 'people'}
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
              <td>
                {#each account.players || [] as habbo}
                  <span class="habbo-cell">
                    <AssetImage src={habbo.avatarUrl} alt={habbo.name} size={28} fallbackIcon={User} />
                    <span>{habbo.name}</span>
                  </span>
                {:else}
                  <span class="muted">—</span>
                {/each}
              </td>
              <td>{(account.roles || []).join(', ')}</td>
              <td>{formatDate(account.createdAt)}</td>
            </tr>
          {:else}
            <tr><td colspan="4" class="muted">{$t('staff.noStaff')}</td></tr>
          {/each}
        </tbody>
      </table>
    </div>

    {#if canManage}
      <h3 class="subhead">{$t('staff.assignTitle')}</h3>
      <p class="muted">{$t('staff.assignDescription')}</p>
      <form class="inline-form" onsubmit={(event) => { event.preventDefault(); searchAccounts(); }}>
        <label>
          {$t('staff.searchAccount')}
          <input autocomplete="off" spellcheck="false" bind:value={accountQuery} placeholder={$t('staff.searchAccountPlaceholder')} />
        </label>
        <button type="submit" disabled={accountSearching}>{$t('staff.search')}</button>
        <label>
          {$t('staff.colRole')}
          <select bind:value={assignRoleId}>
            <option value={0}>{$t('staff.pickRole')}</option>
            {#each data.roles || [] as role}
              <option value={role.id}>{role.name}</option>
            {/each}
          </select>
        </label>
      </form>

      {#if accountResults.length > 0}
        <div class="table-wrap">
          <table>
            <thead>
              <tr>
                <th>{$t('staff.colEmail')}</th>
                <th>{$t('staff.colPlayers')}</th>
                <th>{$t('staff.colRoles')}</th>
                <th></th>
              </tr>
            </thead>
            <tbody>
              {#each accountResults as account}
                <tr>
                  <td>{account.email}</td>
                  <td>{(account.playerNames || []).join(', ') || '—'}</td>
                  <td>
                    {#each account.roleIds || [] as id}
                      <span class="chip">
                        {roleName(id)}
                        <button
                          type="button"
                          class="chip-x"
                          title={$t('staff.revoke')}
                          onclick={() =>
                            ask(
                              '/api/v1/operations/staff/assignments/delete',
                              { accountId: account.id, roleId: id },
                              $t('staff.revoke'),
                              $t('staff.revokeSummary', { role: roleName(id), email: account.email })
                            )}
                        >
                          ×
                        </button>
                      </span>
                    {:else}
                      <span class="muted">—</span>
                    {/each}
                  </td>
                  <td>
                    <button
                      type="button"
                      class="ghost-button"
                      disabled={!assignRoleId || (account.roleIds || []).includes(assignRoleId)}
                      onclick={() =>
                        ask(
                          '/api/v1/operations/staff/assignments',
                          { accountId: account.id, roleId: assignRoleId },
                          $t('staff.assign'),
                          $t('staff.assignSummary', {
                            role: roleName(assignRoleId),
                            email: account.email,
                          })
                        )}
                    >
                      {$t('staff.assign')}
                    </button>
                  </td>
                </tr>
              {/each}
            </tbody>
          </table>
        </div>
      {/if}
    {/if}
  </section>
  {/if}

  {#if tab === 'presets'}
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
            {#if canManage}<th></th>{/if}
          </tr>
        </thead>
        <tbody>
          {#each data.presets || [] as preset}
            <tr>
              <td>{preset.kind}</td>
              <td>{preset.presetIndex}</td>
              <td>{preset.name}</td>
              <td>{preset.permanent ? $t('common.permanent') : formatDuration(preset.durationSeconds)}</td>
              <td class="truncate">{preset.message || '—'}</td>
              {#if canManage}
                <td class="row-actions">
                  <button
                    type="button"
                    class="ghost-button"
                    onclick={() =>
                      (presetDraft = {
                        id: preset.id,
                        kind: (data.presetKinds || []).find((k) => k.label === preset.kind)?.value ?? 0,
                        presetIndex: preset.presetIndex,
                        name: preset.name,
                        durationSeconds: preset.durationSeconds,
                        message: preset.message || '',
                      })}
                  >
                    {$t('staff.edit')}
                  </button>
                  <button
                    type="button"
                    class="ghost-button danger"
                    onclick={() =>
                      ask(
                        '/api/v1/operations/staff/presets/delete',
                        { presetId: preset.id },
                        $t('staff.deletePreset'),
                        $t('staff.deletePresetSummary', { name: preset.name })
                      )}
                  >
                    {$t('staff.delete')}
                  </button>
                </td>
              {/if}
            </tr>
          {:else}
            <tr><td colspan={canManage ? 6 : 5} class="muted">{$t('staff.noPresets')}</td></tr>
          {/each}
        </tbody>
      </table>
    </div>

    {#if canManage}
      <form
        class="inline-form"
        onsubmit={(event) => {
          event.preventDefault();
          ask(
            '/api/v1/operations/staff/presets',
            {
              kind: Number(presetForm.kind),
              presetIndex: Number(presetForm.presetIndex),
              name: presetForm.name,
              durationSeconds: presetForm.durationSeconds ? Number(presetForm.durationSeconds) : null,
              message: presetForm.message,
            },
            $t('staff.addPreset'),
            $t('staff.addPresetSummary', { name: presetForm.name })
          );
        }}
      >
        <label>
          {$t('staff.colKind')}
          <select bind:value={presetForm.kind}>
            {#each data.presetKinds || [] as kind}
              <option value={kind.value}>{kind.label}</option>
            {/each}
          </select>
        </label>
        <label>
          {$t('staff.colIndex')}
          <input autocomplete="off" spellcheck="false" type="number" bind:value={presetForm.presetIndex} min="0" />
        </label>
        <label>
          {$t('staff.colPresetName')}
          <input autocomplete="off" spellcheck="false" bind:value={presetForm.name} />
        </label>
        <label>
          {$t('staff.durationSeconds')}
          <input autocomplete="off" spellcheck="false" type="number" bind:value={presetForm.durationSeconds} min="0" placeholder={$t('common.permanent')} />
        </label>
        <label>
          {$t('staff.colMessage')}
          <input autocomplete="off" spellcheck="false" bind:value={presetForm.message} />
        </label>
        <button type="submit" disabled={!presetForm.name.trim()}>{$t('staff.addPreset')}</button>
      </form>
    {/if}
  </section>
  {/if}
{/if}

{#if roleDraft}
  <Drawer title={$t('staff.rolesTitle')} eyebrow={$t('staff.title')} onclose={() => (roleDraft = null)}>
    <form
      class="inline-form"
      onsubmit={(event) => {
        event.preventDefault();
        ask(
          '/api/v1/operations/staff/roles/update',
          { roleId: roleDraft.id, key: roleDraft.key, name: roleDraft.name },
          $t('staff.updateRole'),
          $t('staff.updateRoleSummary', { role: roleDraft.name })
        );
      }}
    >
      <label>
        {$t('staff.colKey')}
        <input autocomplete="off" spellcheck="false" bind:value={roleDraft.key} required />
      </label>
      <label>
        {$t('staff.colRole')}
        <input autocomplete="off" spellcheck="false" bind:value={roleDraft.name} required />
      </label>
      <button type="submit">{$t('staff.save')}</button>
      <button type="button" class="ghost-button" onclick={() => (roleDraft = null)}>
        {$t('staff.cancel')}
      </button>
    </form>
  </Drawer>
{/if}

{#if capabilityDraft}
  <Drawer title={$t('staff.capabilitiesTitle')} eyebrow={$t('staff.title')} onclose={() => (capabilityDraft = null)}>
    <p class="muted">{$t('staff.capabilityEditorHint')}</p>
    <label class="wildcard-row">
      <input autocomplete="off" spellcheck="false"
        type="checkbox"
        checked={capabilityDraft.selected.has(data.wildcard)}
        onchange={() => toggleCapability(data.wildcard)}
      />
      <code>{data.wildcard}</code>
      <span class="muted">{$t('staff.wildcardHint')}</span>
    </label>
    {#each data.allCapabilities || [] as group}
      <div class="cap-area">
        <div class="cap-area-head">
          <strong>{group.area}</strong>
          <button type="button" class="ghost-button" onclick={() => toggleArea(group, true)}>
            {$t('staff.selectAll')}
          </button>
          <button type="button" class="ghost-button" onclick={() => toggleArea(group, false)}>
            {$t('staff.selectNone')}
          </button>
        </div>
        <div class="cap-grid">
          {#each group.capabilities as cap}
            <label class="cap-check">
              <input autocomplete="off" spellcheck="false"
                type="checkbox"
                checked={capabilityDraft.selected.has(cap)}
                onchange={() => toggleCapability(cap)}
              />
              <code>{cap}</code>
            </label>
          {/each}
        </div>
      </div>
    {/each}
    <div class="editor-actions">
      <button type="button" onclick={() => saveCapabilities(capabilityDraft.role)}>
        {$t('staff.saveCapabilities')}
      </button>
      <button type="button" class="ghost-button" onclick={() => (capabilityDraft = null)}>
        {$t('staff.cancel')}
      </button>
      <span class="muted">
        {$t('staff.selectedCount', { count: capabilityDraft.selected.size })}
      </span>
    </div>
  </Drawer>
{/if}

{#if presetDraft}
  <Drawer title={$t('staff.presetsTitle')} eyebrow={$t('staff.title')} onclose={() => (presetDraft = null)}>
    <form
      class="inline-form"
      onsubmit={(event) => {
        event.preventDefault();
        ask(
          '/api/v1/operations/staff/presets/update',
          {
            presetId: presetDraft.id,
            kind: Number(presetDraft.kind),
            presetIndex: Number(presetDraft.presetIndex),
            name: presetDraft.name,
            durationSeconds: presetDraft.durationSeconds
              ? Number(presetDraft.durationSeconds)
              : null,
            message: presetDraft.message,
          },
          $t('staff.updatePreset'),
          $t('staff.updatePresetSummary', { name: presetDraft.name })
        );
      }}
    >
      <label>
        {$t('staff.colKind')}
        <select bind:value={presetDraft.kind}>
          {#each data.presetKinds || [] as kind}
            <option value={kind.value}>{kind.label}</option>
          {/each}
        </select>
      </label>
      <label>
        {$t('staff.colIndex')}
        <input autocomplete="off" spellcheck="false" type="number" bind:value={presetDraft.presetIndex} min="0" />
      </label>
      <label>
        {$t('staff.colPresetName')}
        <input autocomplete="off" spellcheck="false" bind:value={presetDraft.name} required />
      </label>
      <label>
        {$t('staff.durationSeconds')}
        <input autocomplete="off" spellcheck="false" type="number" bind:value={presetDraft.durationSeconds} min="0" placeholder={$t('common.permanent')} />
      </label>
      <label>
        {$t('staff.colMessage')}
        <input autocomplete="off" spellcheck="false" bind:value={presetDraft.message} />
      </label>
      <button type="submit">{$t('staff.save')}</button>
      <button type="button" class="ghost-button" onclick={() => (presetDraft = null)}>
        {$t('staff.cancel')}
      </button>
    </form>
  </Drawer>
{/if}

<ConfirmReasonModal
  open={Boolean($ops.pending)}
  title={$ops.pending?.title ?? ''}
  changes={$ops.pending?.changes ?? []}
  noteOnly={$ops.pending?.noteOnly ?? false}
  summary={$ops.pending?.summary ?? ''}
  confirmLabel={$ops.pending?.title ?? $t('common.confirm')}
  busy={$ops.busy}
  error={$ops.error}
  onconfirm={ops.confirm}
  oncancel={() => ops.cancel()}
/>

<style>
  .habbo-cell {
    display: inline-flex;
    align-items: center;
    gap: 6px;
    margin-right: 8px;
  }

  tr.selected {
    background: var(--surface-raised, rgba(255, 255, 255, 0.04));
  }

  .link-cell {
    background: none;
    border: 0;
    padding: 0;
    color: inherit;
    font: inherit;
    cursor: pointer;
    text-decoration: underline dotted;
  }

  .cap-list {
    display: flex;
    flex-wrap: wrap;
    gap: 6px;
  }

  .chip {
    display: inline-flex;
    align-items: center;
    gap: 4px;
    padding: 2px 6px;
    border-radius: 6px;
    background: var(--surface-raised, rgba(255, 255, 255, 0.06));
    font-size: 0.78rem;
  }

  .chip-x {
    border: 0;
    background: none;
    color: var(--danger, #e06c6c);
    cursor: pointer;
    font-size: 0.9rem;
    line-height: 1;
    padding: 0 2px;
  }

  .cap-area {
    margin-top: 10px;
  }

  .cap-area-head {
    display: flex;
    align-items: center;
    gap: 8px;
    margin-bottom: 4px;
  }

  .cap-grid {
    display: grid;
    grid-template-columns: repeat(auto-fit, minmax(260px, 1fr));
    gap: 2px 12px;
  }

  .cap-check,
  .wildcard-row {
    display: flex;
    align-items: center;
    gap: 6px;
    font-size: 0.8rem;
  }

  .wildcard-row {
    margin: 8px 0;
  }

  .editor-actions {
    display: flex;
    align-items: center;
    gap: 10px;
    margin-top: 12px;
  }

</style>
