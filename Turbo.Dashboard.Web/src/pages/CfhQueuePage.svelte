<script>
  import { onMount } from 'svelte';
  import { apiGet, apiPost } from '../lib/api.js';
  import { isPermissionDeniedError, hasDashboardCapability } from '../lib/permissions.js';
  import { formatDuration, compactCorrelation } from '../lib/format.js';
  import { CAPABILITIES } from '../lib/dashboardPermissions.js';
  import { reasonOk, positive } from '../lib/validation.js';
  import AccessDeniedNotice from '../components/AccessDeniedNotice.svelte';
  import EntityLink from '../components/EntityLink.svelte';
  import { identity, openPlayer, openItem } from '../lib/session.js';

  const closeReasons = [
    { value: 1, label: 'Useless' },
    { value: 2, label: 'Sanctioned' },
    { value: 3, label: 'Resolved' },
  ];

  let loading = false;
  let forbidden = false;
  let error = '';
  let queue = [];

  // Row-scoped action state, keyed by issueId.
  let rowBusy = {};
  let rowError = {};

  // Inline "ban reported player" panel — opened per row, closed after confirm/cancel.
  let banDraft = null;
  let banResult = null;
  let banBusy = false;
  let banError = '';

  $: canManage = hasDashboardCapability($identity, CAPABILITIES.opsCfhManage);
  $: canBan = hasDashboardCapability($identity, CAPABILITIES.opsBanAccount);

  async function refresh() {
    loading = true;
    error = '';
    forbidden = false;

    try {
      queue = await apiGet('/api/v1/operations/cfh/queue');
    } catch (err) {
      if (isPermissionDeniedError(err)) {
        forbidden = true;
        queue = [];
        return;
      }

      error = err.message;
      queue = [];
    } finally {
      loading = false;
    }
  }

  async function pick(issueId) {
    rowBusy = { ...rowBusy, [issueId]: true };
    rowError = { ...rowError, [issueId]: '' };

    try {
      await apiPost('/api/v1/operations/cfh/pick', { issueIds: [issueId] });
      await refresh();
    } catch (err) {
      rowError = {
        ...rowError,
        [issueId]: isPermissionDeniedError(err) ? 'Droits insuffisants.' : err.code || err.message,
      };
    } finally {
      rowBusy = { ...rowBusy, [issueId]: false };
    }
  }

  async function release(issueId) {
    rowBusy = { ...rowBusy, [issueId]: true };
    rowError = { ...rowError, [issueId]: '' };

    try {
      await apiPost('/api/v1/operations/cfh/release', { issueIds: [issueId] });
      await refresh();
    } catch (err) {
      rowError = {
        ...rowError,
        [issueId]: isPermissionDeniedError(err) ? 'Droits insuffisants.' : err.code || err.message,
      };
    } finally {
      rowBusy = { ...rowBusy, [issueId]: false };
    }
  }

  async function close(issueId, reason, sanctioned) {
    rowBusy = { ...rowBusy, [issueId]: true };
    rowError = { ...rowError, [issueId]: '' };

    try {
      await apiPost('/api/v1/operations/cfh/close', {
        issueIds: [issueId],
        reason,
        sanctioned,
      });
      await refresh();
    } catch (err) {
      rowError = {
        ...rowError,
        [issueId]: isPermissionDeniedError(err) ? 'Droits insuffisants.' : err.code || err.message,
      };
    } finally {
      rowBusy = { ...rowBusy, [issueId]: false };
    }
  }

  function openBanDraft(entry) {
    banDraft = {
      playerId: entry.reportedUserId,
      playerName: entry.reportedUserName,
      permanent: false,
      durationSeconds: '',
      reason: '',
    };
    banResult = null;
    banError = '';
  }

  function cancelBanDraft() {
    banDraft = null;
  }

  async function confirmBanDraft() {
    if (!banDraft) {
      return;
    }

    const validDuration = banDraft.permanent || positive(banDraft.durationSeconds);

    if (!positive(banDraft.playerId) || !validDuration || !reasonOk(banDraft.reason)) {
      banError =
        'Fill the fields: a duration is required unless permanent, and the reason needs at least 3 characters.';
      return;
    }

    banBusy = true;
    banError = '';

    try {
      banResult = await apiPost('/api/v1/operations/players/ban', {
        playerId: Number(banDraft.playerId),
        permanent: banDraft.permanent,
        durationSeconds: banDraft.permanent ? null : Number(banDraft.durationSeconds),
        reason: banDraft.reason.trim(),
      });
    } catch (err) {
      banError = isPermissionDeniedError(err) ? 'Droits insuffisants pour bannir un compte.' : err.code || err.message;
    } finally {
      banBusy = false;
    }
  }

  onMount(() => {
    void refresh();
  });
</script>

<section class="panel">
  <div class="panel-head">
    <h2>CFH ticket queue</h2>
    <button type="button" class="ghost-button" on:click={refresh} disabled={loading}>Refresh</button>
  </div>
  <p class="muted">Open and picked tickets, most recent first. Closing a ticket does not itself sanction the reported player — use "Ban reported player" separately if warranted.</p>

  {#if loading}
    <p class="muted">Loading queue...</p>
  {:else if forbidden}
    <AccessDeniedNotice message="Vous n'avez pas l'autorisation de gérer les tickets CFH." />
  {:else if error}
    <p class="empty-state danger">{error}</p>
  {/if}

  <table>
    <thead>
      <tr>
        <th>#</th>
        <th>State</th>
        <th>Age</th>
        <th>Reporter</th>
        <th>Reported</th>
        <th>Picked by</th>
        <th>Message</th>
        <th>Actions</th>
      </tr>
    </thead>
    <tbody>
      {#each queue as entry (entry.issueId)}
        <tr>
          <td>#{entry.issueId}</td>
          <td>{entry.state}</td>
          <td>{formatDuration(entry.issueAgeMs / 1000)}</td>
          <td><EntityLink id={entry.reporterUserId} label={entry.reporterUserName} {openPlayer} {openItem} /></td>
          <td><EntityLink id={entry.reportedUserId} label={entry.reportedUserName} {openPlayer} {openItem} /></td>
          <td>{entry.pickerUserName || '-'}</td>
          <td class="truncate">{entry.message || '-'}</td>
          <td>
            {#if canManage}
              <div class="op-actions">
                <button type="button" class="ghost-button" on:click={() => pick(entry.issueId)} disabled={rowBusy[entry.issueId]}>Pick</button>
                <button type="button" class="ghost-button" on:click={() => close(entry.issueId, 3, false)} disabled={rowBusy[entry.issueId]}>Resolve</button>
                <button type="button" class="ghost-button" on:click={() => close(entry.issueId, 1, false)} disabled={rowBusy[entry.issueId]}>Useless</button>
                <button type="button" class="ghost-button" on:click={() => release(entry.issueId)} disabled={rowBusy[entry.issueId]}>Release</button>
                {#if canBan}
                  <button type="button" on:click={() => openBanDraft(entry)}>Ban reported player</button>
                {/if}
              </div>
              {#if rowError[entry.issueId]}<p class="empty-state danger">{rowError[entry.issueId]}</p>{/if}
            {:else}
              <span class="muted">read-only</span>
            {/if}
          </td>
        </tr>
      {:else}
        <tr><td colspan="8" class="muted">No open or picked tickets.</td></tr>
      {/each}
    </tbody>
  </table>
</section>

{#if banDraft}
  <div class="modal-layer">
    <button class="modal-backdrop" type="button" aria-label="Cancel" on:click={cancelBanDraft}></button>
    <section class="modal-panel" role="dialog" aria-modal="true" style="width: min(460px, 100%)">
      <header class="modal-header">
        <div>
          <p class="eyebrow">CFH sanction</p>
          <h2>Ban reported player</h2>
        </div>
      </header>
      <p class="muted">{banDraft.playerName || 'player'} (#{banDraft.playerId})</p>
      <div class="op-field">
        <label>
          <input type="checkbox" bind:checked={banDraft.permanent} /> Permanent
        </label>
      </div>
      {#if !banDraft.permanent}
        <div class="op-field">
          <label for="cfh-ban-duration">Duration (seconds)</label>
          <input id="cfh-ban-duration" type="number" min="1" bind:value={banDraft.durationSeconds} placeholder="86400" />
        </div>
      {/if}
      <div class="op-field">
        <label for="cfh-ban-reason">Reason *</label>
        <input id="cfh-ban-reason" bind:value={banDraft.reason} placeholder="why this action?" />
      </div>
      {#if banError}<p class="empty-state danger">{banError}</p>{/if}
      {#if banResult}
        <p class="op-result" class:danger={!banResult.ok}>
          {banResult.ok ? '✅' : '❌'} {banResult.message} - cid
          <code>{compactCorrelation(banResult.correlationId)}</code>
        </p>
      {/if}
      <div class="op-actions">
        <button type="button" on:click={confirmBanDraft} disabled={banBusy}>Confirm ban</button>
        <button class="ghost-button" type="button" on:click={cancelBanDraft}>Close</button>
      </div>
    </section>
  </div>
{/if}
