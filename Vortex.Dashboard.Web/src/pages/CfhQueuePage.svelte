<script>
  import Modal from '../components/Modal.svelte';
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
  import TableFilter from '../components/TableFilter.svelte';
  import SortTh from '../components/SortTh.svelte';
  import { filterRows, sortRows } from '../lib/tableView.js';
  import { t, translate } from '../lib/i18n.js';

  const closeReasons = [
    { value: 1, label: 'Useless' },
    { value: 2, label: 'Sanctioned' },
    { value: 3, label: 'Resolved' },
  ];

  let loading = $state(false);
  let forbidden = $state(false);
  let error = $state('');
  let queue = $state([]);

  // The queue is one request, no paging: on a busy hotel it is the table you scroll looking for one
  // reporter's name.
  let queueQuery = $state('');
  let queueSort = $state({ key: '', dir: 'desc' });
  let queueView = $derived(sortRows(filterRows(queue, queueQuery), queueSort));

  // Row-scoped action state, keyed by issueId.
  let rowBusy = $state({});
  let rowError = $state({});

  // Inline "ban reported player" panel — opened per row, closed after confirm/cancel.
  // The ban IS an audited sanction, unlike the queue moves above: it goes through the shared write
  // store so its reason reaches the audit log and the reason-history datalist, and so a 403 mid-
  // session reads the same here as everywhere else. The draft holds the form; the store holds the
  // write.
  let banDraft = $state(null);
  const banOps = createWriteOps();

  let canManage = $derived(hasDashboardCapability($identity, CAPABILITIES.opsCfhManage));
  let canBan = $derived(hasDashboardCapability($identity, CAPABILITIES.opsBanAccount));

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
    <button type="button" class="ghost-button" onclick={refresh} disabled={loading}>{$t('common.refresh')}</button>
  </div>
  <p class="muted">{$t('cfh.description')}</p>

  {#if loading}
    <p class="muted">{$t('cfh.loadingQueue')}</p>
  {:else if forbidden}
    <AccessDeniedNotice message={$t('cfh.accessDenied')} />
  {:else if error}
    <p class="empty-state danger" role="alert">{error}</p>
  {/if}

  <TableFilter bind:query={queueQuery} shown={queueView.length} total={queue.length} />

  <table>
    <thead>
      <tr>
        <SortTh label="#" key="issueId" bind:sort={queueSort} initialDir="asc" />
        <SortTh label={$t('cfh.colState')} key="state" bind:sort={queueSort} initialDir="asc" />
        <SortTh label={$t('cfh.colAge')} key="issueAgeMs" bind:sort={queueSort} />
        <SortTh label={$t('cfh.colReporter')} key="reporterUserName" bind:sort={queueSort} initialDir="asc" />
        <SortTh label={$t('cfh.colReported')} key="reportedUserName" bind:sort={queueSort} initialDir="asc" />
        <SortTh label={$t('cfh.colPickedBy')} key="pickerUserName" bind:sort={queueSort} initialDir="asc" />
        <th>{$t('cfh.colMessage')}</th>
        <th>{$t('cfh.colActions')}</th>
      </tr>
    </thead>
    <tbody>
      {#each queueView as entry (entry.issueId)}
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
                <button type="button" class="ghost-button" onclick={() => pick(entry.issueId)} disabled={rowBusy[entry.issueId]}>{$t('cfh.pick')}</button>
                <button type="button" class="ghost-button" onclick={() => close(entry.issueId, 3, false)} disabled={rowBusy[entry.issueId]}>{$t('cfh.resolve')}</button>
                <button type="button" class="ghost-button" onclick={() => close(entry.issueId, 1, false)} disabled={rowBusy[entry.issueId]}>{$t('cfh.useless')}</button>
                <button type="button" class="ghost-button" onclick={() => release(entry.issueId)} disabled={rowBusy[entry.issueId]}>{$t('cfh.release')}</button>
                {#if canBan}
                  <button type="button" onclick={() => openBanDraft(entry)}>{$t('cfh.banReportedPlayer')}</button>
                {/if}
              </div>
              {#if rowError[entry.issueId]}<p class="empty-state danger" role="alert">{rowError[entry.issueId]}</p>{/if}
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
  <Modal
    title={$t('cfh.banReportedPlayer')}
    eyebrow={$t('cfh.sanctionEyebrow')}
    width={460}
    labelledBy="cfh-ban-title"
    onclose={cancelBanDraft}
  >
    <p class="muted">{banDraft.playerName || $t('cfh.player')} (#{banDraft.playerId})</p>
    <div class="op-checkbox-field">
      <input autocomplete="off" spellcheck="false" id="cfh-ban-permanent" type="checkbox" bind:checked={banDraft.permanent} />
      <label for="cfh-ban-permanent">{$t('common.permanent')}</label>
    </div>
    {#if !banDraft.permanent}
      <div class="op-field">
        <label for="cfh-ban-duration">{$t('cfh.durationSeconds')}</label>
        <input autocomplete="off" spellcheck="false" id="cfh-ban-duration" type="number" min="1" bind:value={banDraft.durationSeconds} placeholder="86400" />
      </div>
    {/if}
    <div class="op-field">
      <label for="cfh-ban-reason">{$t('common.reasonRequired')}</label>
      <input autocomplete="off" spellcheck="false" id="cfh-ban-reason" bind:value={banDraft.reason} placeholder={$t('common.reasonPlaceholder')} list="reason-history" />
    </div>
    {#if $banOps.error}<p class="empty-state danger" role="alert">{$banOps.error}</p>{/if}
    {#if $banOps.result}
      <OpResult result={$banOps.result} />
    {/if}

    {#snippet actions()}

      <button type="button" onclick={confirmBanDraft} disabled={$banOps.busy}>{$t('cfh.confirmBan')}</button>
      <button class="ghost-button" type="button" onclick={cancelBanDraft}>{$t('cfh.close')}</button>

    {/snippet}
  </Modal>
{/if}
