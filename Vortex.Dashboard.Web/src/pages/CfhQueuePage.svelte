<script>
  import OpResult from '../components/OpResult.svelte';
  import { onMount } from 'svelte';
  import { apiGet, apiPost } from '../lib/api.js';
  import { createWriteOps } from '../lib/writeOps.js';
  import { isPermissionDeniedError, hasDashboardCapability } from '../lib/permissions.js';
  import { formatDuration, compactCorrelation } from '../lib/format.js';
  import { CAPABILITIES } from '../lib/dashboardPermissions.js';
  import { reasonOk, positive } from '../lib/validation.js';
  import AccessDeniedNotice from '../components/AccessDeniedNotice.svelte';
  import EntityLink from '../components/EntityLink.svelte';
  import { identity, openPlayer, openItem } from '../lib/session.js';
  import { t, translate } from '../lib/i18n.js';

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
  // The ban IS an audited sanction, unlike the queue moves above: it goes through the shared write
  // store so its reason reaches the audit log and the reason-history datalist, and so a 403 mid-
  // session reads the same here as everywhere else. The draft holds the form; the store holds the
  // write.
  let banDraft = null;
  const banOps = createWriteOps();

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

  // pick / release / close are queue moves, not audited writes: the moderator's identity is already
  // on the ticket and there is nothing to justify, which is why they do not go through the shared
  // reason modal like the sanctions below do. They did each carry their own copy of this
  // busy/refresh/report-the-403 dance -- it lives here once instead.
  async function rowAction(issueId, endpoint, body) {
    rowBusy = { ...rowBusy, [issueId]: true };
    rowError = { ...rowError, [issueId]: '' };

    try {
      await apiPost(endpoint, { issueIds: [issueId], ...body });
      await refresh();
    } catch (err) {
      rowError = {
        ...rowError,
        [issueId]: isPermissionDeniedError(err)
          ? translate('common.insufficientRights')
          : err.code || err.message,
      };
    } finally {
      rowBusy = { ...rowBusy, [issueId]: false };
    }
  }

  const pick = (issueId) => rowAction(issueId, '/api/v1/operations/cfh/pick');
  const release = (issueId) => rowAction(issueId, '/api/v1/operations/cfh/release');
  const close = (issueId, reason, sanctioned) =>
    rowAction(issueId, '/api/v1/operations/cfh/close', { reason, sanctioned });

  function openBanDraft(entry) {
    banDraft = {
      playerId: entry.reportedUserId,
      playerName: entry.reportedUserName,
      permanent: false,
      durationSeconds: '',
      reason: '',
    };
    banOps.clear();
  }

  function cancelBanDraft() {
    banDraft = null;
    banOps.clear();
  }

  async function confirmBanDraft() {
    if (!banDraft) {
      return;
    }

    const validDuration = banDraft.permanent || positive(banDraft.durationSeconds);
    const valid = positive(banDraft.playerId) && validDuration && reasonOk(banDraft.reason);

    const staged = banOps.ask(
      '/api/v1/operations/players/ban',
      {
        playerId: Number(banDraft.playerId),
        permanent: banDraft.permanent,
        durationSeconds: banDraft.permanent ? null : Number(banDraft.durationSeconds),
      },
      translate('cfh.confirmBan'),
      banDraft.playerName || '',
      { valid, invalidMessage: translate('cfh.banValidation'), reason: banDraft.reason.trim() },
    );

    // The draft form is its own confirmation step, so the write commits straight away rather than
    // opening a second dialog on top of it.
    if (staged) {
      await banOps.confirm();
    }
  }

  onMount(() => {
    void refresh();
  });
</script>

<section class="panel">
  <div class="panel-head">
    <h2>{$t('cfh.title')}</h2>
    <button type="button" class="ghost-button" on:click={refresh} disabled={loading}>{$t('common.refresh')}</button>
  </div>
  <p class="muted">{$t('cfh.description')}</p>

  {#if loading}
    <p class="muted">{$t('cfh.loadingQueue')}</p>
  {:else if forbidden}
    <AccessDeniedNotice message={$t('cfh.accessDenied')} />
  {:else if error}
    <p class="empty-state danger">{error}</p>
  {/if}

  <table>
    <thead>
      <tr>
        <th>#</th>
        <th>{$t('cfh.colState')}</th>
        <th>{$t('cfh.colAge')}</th>
        <th>{$t('cfh.colReporter')}</th>
        <th>{$t('cfh.colReported')}</th>
        <th>{$t('cfh.colPickedBy')}</th>
        <th>{$t('cfh.colMessage')}</th>
        <th>{$t('cfh.colActions')}</th>
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
                <button type="button" class="ghost-button" on:click={() => pick(entry.issueId)} disabled={rowBusy[entry.issueId]}>{$t('cfh.pick')}</button>
                <button type="button" class="ghost-button" on:click={() => close(entry.issueId, 3, false)} disabled={rowBusy[entry.issueId]}>{$t('cfh.resolve')}</button>
                <button type="button" class="ghost-button" on:click={() => close(entry.issueId, 1, false)} disabled={rowBusy[entry.issueId]}>{$t('cfh.useless')}</button>
                <button type="button" class="ghost-button" on:click={() => release(entry.issueId)} disabled={rowBusy[entry.issueId]}>{$t('cfh.release')}</button>
                {#if canBan}
                  <button type="button" on:click={() => openBanDraft(entry)}>{$t('cfh.banReportedPlayer')}</button>
                {/if}
              </div>
              {#if rowError[entry.issueId]}<p class="empty-state danger">{rowError[entry.issueId]}</p>{/if}
            {:else}
              <span class="muted">{$t('cfh.readOnly')}</span>
            {/if}
          </td>
        </tr>
      {:else}
        <tr><td colspan="8" class="muted">{$t('cfh.noTickets')}</td></tr>
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
          <p class="eyebrow">{$t('cfh.sanctionEyebrow')}</p>
          <h2>{$t('cfh.banReportedPlayer')}</h2>
        </div>
      </header>
      <p class="muted">{banDraft.playerName || $t('cfh.player')} (#{banDraft.playerId})</p>
      <div class="op-checkbox-field">
        <input id="cfh-ban-permanent" type="checkbox" bind:checked={banDraft.permanent} />
        <label for="cfh-ban-permanent">{$t('common.permanent')}</label>
      </div>
      {#if !banDraft.permanent}
        <div class="op-field">
          <label for="cfh-ban-duration">{$t('cfh.durationSeconds')}</label>
          <input id="cfh-ban-duration" type="number" min="1" bind:value={banDraft.durationSeconds} placeholder="86400" />
        </div>
      {/if}
      <div class="op-field">
        <label for="cfh-ban-reason">{$t('common.reasonRequired')}</label>
        <input id="cfh-ban-reason" bind:value={banDraft.reason} placeholder={$t('common.reasonPlaceholder')} list="reason-history" />
      </div>
      {#if $banOps.error}<p class="empty-state danger">{$banOps.error}</p>{/if}
      {#if $banOps.result}
        <OpResult result={$banOps.result} />
      {/if}
      <div class="op-actions">
        <button type="button" on:click={confirmBanDraft} disabled={$banOps.busy}>{$t('cfh.confirmBan')}</button>
        <button class="ghost-button" type="button" on:click={cancelBanDraft}>{$t('cfh.close')}</button>
      </div>
    </section>
  </div>
{/if}
