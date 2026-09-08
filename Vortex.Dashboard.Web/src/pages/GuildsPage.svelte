<script lang="ts">
  // Acting on a guild: its members, the people waiting at the door, the people barred from it, and
  // what its forum says. Until now every one of those needed SQL, and a reported forum post could
  // only be answered by whoever ran the guild -- which is a problem when the post is theirs.
  //
  // One guild, one dialog, four tabs. Not four pages: the reason somebody opens a guild is a report,
  // and a report names a post whose author is a member whose request somebody approved.
  import { apiGet } from '../lib/api';
  import { createResource } from '../lib/resource';
  import { createWriteOps } from '../lib/writeOps';
  import { formatDate, formatNumber } from '../lib/format';
  import AccessDeniedNotice from '../components/AccessDeniedNotice.svelte';
  import AssetImage from '../components/AssetImage.svelte';
  import ConfirmReasonModal from '../components/ConfirmReasonModal.svelte';
  import DropdownMenu from '../components/DropdownMenu.svelte';
  import EmptyState from '../components/EmptyState.svelte';
  import EntityLink from '../components/EntityLink.svelte';
  import LoadingOverlay from '../components/LoadingOverlay.svelte';
  import Modal from '../components/Modal.svelte';
  import OpResult from '../components/OpResult.svelte';
  import PageHeader from '../components/PageHeader.svelte';
  import Pagination from '../components/Pagination.svelte';
  import SortTh from '../components/SortTh.svelte';
  import TableFilter from '../components/TableFilter.svelte';
  import Tabs from '../components/Tabs.svelte';
  import { filterRows, sortRows } from '../lib/tableView';
  import { openPlayer, openItem } from '../lib/session';
  import { t } from '../lib/i18n';
  import type { GuildDirectoryPage, GuildModeration, GuildThreadDetail } from '../lib/apiTypes';
  import type { Sort } from '../lib/tableView';

  /**
   * The server's ForumStaffAction / GroupStaffAction, by name.
   *
   * Sent as strings rather than as the numbers behind them: the wire is JSON and the enum is bound
   * by name, so `"Hide"` survives someone reordering the C# enum and `0` does not.
   */
  const FORUM = { hide: 'Hide', restore: 'Restore', delete: 'Delete' } as const;
  const MEMBER = {
    approve: 'ApproveRequest',
    reject: 'RejectRequest',
    kick: 'Kick',
    kickBan: 'KickAndBlock',
    liftBan: 'LiftBan',
  } as const;

  let page = $state(1);
  let term = $state('');
  let pendingOnly = $state(false);

  // Filters live in the key, so changing one re-reads without a refresh() call.
  const guilds = createResource(
    () => ['guilds', page, term, pendingOnly],
    () => {
      const params = new URLSearchParams({ page: String(page), limit: '40' });
      if (term.trim()) params.set('q', term.trim());
      if (pendingOnly) params.set('pending', 'true');

      return apiGet<GuildDirectoryPage>(`/api/v1/guilds?${params}`);
    },
  );

  let selectedId = $state<number | null>(null);
  let openThreadId = $state<number | null>(null);

  const guild = createResource(
    () => ['guild', selectedId],
    () => apiGet<GuildModeration>(`/api/v1/guilds/${selectedId}`),
    { enabled: () => selectedId !== null },
  );

  const thread = createResource(
    () => ['guild-thread', selectedId, openThreadId],
    () => apiGet<GuildThreadDetail>(`/api/v1/guilds/${selectedId}/threads/${openThreadId}`),
    { enabled: () => selectedId !== null && openThreadId !== null },
  );

  // A write changes both lists -- a kick changes the member count in the roster behind the dialog --
  // so both are re-read rather than the one that happens to be on screen.
  const ops = createWriteOps(() => {
    guilds.refresh();
    guild.refresh();
    thread.refresh();
  });

  let rows = $derived(guilds.data?.items ?? []);
  let listQuery = $state('');
  let listSort = $state<Sort>({ key: '', dir: 'desc' });
  let visible = $derived(sortRows(filterRows(rows, listQuery), listSort));
  let totalPages = $derived(
    Math.max(1, Math.ceil((guilds.data?.total ?? 0) / (guilds.data?.limit || 40))),
  );

  let detail = $derived(guild.data);
  let tab = $state('members');

  function open(id: number) {
    selectedId = id;
    openThreadId = null;
    tab = 'members';
  }

  function close() {
    selectedId = null;
    openThreadId = null;
    ops.clear();
  }

  function search() {
    page = 1;
  }

  /** What a member row offers. The owner is listed but cannot be removed -- see KickCoreAsync. */
  const memberMenu = () => [
    { id: MEMBER.kick, label: $t('guilds.kick') },
    { id: MEMBER.kickBan, label: $t('guilds.kickAndBan'), danger: true },
  ];

  const threadMenu = (state: string) => [
    state === 'Hidden'
      ? { id: FORUM.restore, label: $t('guilds.restore') }
      : { id: FORUM.hide, label: $t('guilds.hide') },
    { id: FORUM.delete, label: $t('guilds.delete'), danger: true },
  ];

  const postMenu = (state: string, deleted: boolean) => [
    state === 'Visible'
      ? { id: FORUM.hide, label: $t('guilds.hide') }
      : { id: FORUM.restore, label: $t('guilds.restore') },
    { id: FORUM.delete, label: $t('guilds.delete'), danger: true, disabled: deleted },
  ];

  function memberAction(action: string, playerId: number, name: string | null) {
    ops.ask(
      '/api/v1/operations/guilds/member',
      { guildId: selectedId, playerId, action },
      $t(`guilds.confirm_${action}`),
      `${name || `#${playerId}`} — ${detail?.guild.name ?? ''}`,
      { danger: action === MEMBER.kick || action === MEMBER.kickBan },
    );
  }

  function threadAction(action: string, threadId: number, subject: string) {
    ops.ask(
      '/api/v1/operations/guilds/forum/thread',
      { guildId: selectedId, threadId, action },
      $t(`guilds.confirmThread_${action}`),
      subject,
      { danger: action === FORUM.delete },
    );
  }

  function postAction(action: string, postId: number, author: string | null) {
    ops.ask(
      '/api/v1/operations/guilds/forum/post',
      { guildId: selectedId, postId, action },
      $t(`guilds.confirmPost_${action}`),
      `#${postId} — ${author || ''}`,
      { danger: action === FORUM.delete },
    );
  }

  function deleteGuild() {
    ops.ask(
      '/api/v1/operations/guilds/delete',
      { guildId: selectedId },
      $t('guilds.confirmDelete'),
      detail?.guild.name ?? '',
      { danger: true, onSuccess: () => close() },
    );
  }
</script>

<section class="panel">
  <PageHeader title={$t('guilds.title')} description={$t('guilds.description')}>
    {#snippet actions()}
      <button type="button" class="warning" onclick={guilds.refresh} disabled={guilds.loading}>
        {$t('common.refresh')}
      </button>
    {/snippet}
  </PageHeader>
</section>

<section class="panel">
  <form class="toolbar-grid" onsubmit={(event) => { event.preventDefault(); search(); }}>
    <label>
      {$t('guilds.search')}
      <input
        autocomplete="off"
        spellcheck="false"
        type="search"
        bind:value={term}
        placeholder={$t('guilds.searchPlaceholder')}
      />
    </label>
    <label>
      {$t('guilds.pendingOnly')}
      <select bind:value={pendingOnly} onchange={search}>
        <option value={false}>{$t('guilds.allGuilds')}</option>
        <option value={true}>{$t('guilds.withRequests')}</option>
      </select>
    </label>
  </form>

  {#if guilds.forbidden}
    <AccessDeniedNotice message={$t('guilds.accessDenied')} />
  {:else if guilds.error}
    <p class="empty-state danger" role="alert">{guilds.error}</p>
  {/if}
</section>

{#if !guilds.forbidden}
  <section class="panel" style="margin-top: 12px;">
    <TableFilter bind:query={listQuery} shown={visible.length} total={rows.length} />

    {#if rows.length === 0 && !guilds.loading}
      <EmptyState message={$t('guilds.noGuilds')} />
    {:else}
      <div class="table-wrap">
        <table>
          <thead>
            <tr>
              <SortTh label={$t('guilds.colGuild')} key="name" bind:sort={listSort} initialDir="asc" />
              <SortTh label={$t('guilds.colOwner')} key="ownerName" bind:sort={listSort} initialDir="asc" />
              <SortTh label={$t('guilds.colMembers')} key="members" bind:sort={listSort} />
              <SortTh label={$t('guilds.colRequests')} key="pendingRequests" bind:sort={listSort} />
              <SortTh label={$t('guilds.colBans')} key="bans" bind:sort={listSort} />
              <SortTh label={$t('guilds.colThreads')} key="threads" bind:sort={listSort} />
              <SortTh label={$t('guilds.colCreated')} key="createdAt" bind:sort={listSort} />
              <th>{$t('common.actions')}</th>
            </tr>
          </thead>
          <tbody>
            {#each visible as row (row.id)}
              <tr>
                <td>
                  <div class="guild-cell">
                    <AssetImage src={row.badgeUrl} alt="" size={28} />
                    <div>
                      <strong>{row.name}</strong>
                      <small class="muted">#{row.id} · {$t(`guilds.type_${row.type}`)}</small>
                    </div>
                  </div>
                </td>
                <td>
                  <EntityLink id={row.ownerId} label={row.ownerName} {openPlayer} {openItem} />
                </td>
                <td>{formatNumber(row.members)}</td>
                <td>
                  {#if row.pendingRequests}
                    <span class="status-badge status-badge--warn">{row.pendingRequests}</span>
                  {:else}
                    <span class="muted">0</span>
                  {/if}
                </td>
                <td>{formatNumber(row.bans)}</td>
                <td>{formatNumber(row.threads)}</td>
                <td>{formatDate(row.createdAt)}</td>
                <td>
                  <button type="button" class="ghost-button" onclick={() => open(row.id)}>
                    {$t('guilds.open')}
                  </button>
                </td>
              </tr>
            {/each}
          </tbody>
        </table>
      </div>

      <Pagination
        {page}
        pageCount={totalPages}
        total={guilds.data?.total ?? 0}
        pageSize={guilds.data?.limit ?? 40}
        label={$t('guilds.paginationLabel')}
        pageWord={$t('common.page')}
        prevLabel={$t('common.prev')}
        nextLabel={$t('common.next')}
        disabled={guilds.loading}
        onchange={(next) => (page = next)}
      />
    {/if}
  </section>
{/if}

{#if selectedId !== null}
  <Modal
    title={detail?.guild.name ?? $t('guilds.loadingGuild')}
    eyebrow={$t('guilds.eyebrow')}
    labelledBy="guild-modal-title"
    onclose={close}
  >
    {#if guild.error}
      <p class="empty-state danger" role="alert">{guild.error}</p>
    {:else if detail}
      <div class="guild-head">
        <AssetImage src={detail.guild.badgeUrl} alt="" size={44} />
        <div class="guild-facts">
          <p class="muted">{detail.guild.description || $t('guilds.noDescription')}</p>
          <p class="muted">
            {$t('guilds.owner')}
            <EntityLink
              id={detail.guild.ownerId}
              label={detail.guild.ownerName}
              {openPlayer}
              {openItem}
            />
            · {$t('guilds.room')} {detail.guild.roomName || `#${detail.guild.roomId}`}
            · {$t(`guilds.type_${detail.guild.type}`)}
            · {$t('guilds.memberCount', { count: formatNumber(detail.guild.memberCount) })}
            {#if !detail.guild.forumEnabled}
              · <span class="status-badge status-badge--unknown">{$t('guilds.forumOff')}</span>
            {/if}
          </p>
        </div>
      </div>

      {#if openThreadId !== null}
        <!-- The thread replaces the tabs rather than opening a second dialog over them: a modal on
             a modal is two things to dismiss and one of them is always the wrong one. -->
        <div class="thread-head">
          <button type="button" class="ghost-button" onclick={() => (openThreadId = null)}>
            {$t('guilds.backToForum')}
          </button>
          {#if thread.data}
            <strong>{thread.data.thread.subject}</strong>
            <span class="status-badge">{$t(`guilds.threadState_${thread.data.thread.state}`)}</span>
          {/if}
        </div>

        {#if thread.error}
          <p class="empty-state danger" role="alert">{thread.error}</p>
        {:else if thread.data}
          <div class="table-wrap">
            <table>
              <thead>
                <tr>
                  <th>{$t('guilds.colAuthor')}</th>
                  <th>{$t('guilds.colMessage')}</th>
                  <th>{$t('guilds.colState')}</th>
                  <th>{$t('guilds.colPosted')}</th>
                  <th>{$t('common.actions')}</th>
                </tr>
              </thead>
              <tbody>
                {#each thread.data.posts as post (post.id)}
                  <tr class:removed={post.deleted}>
                    <td>
                      <EntityLink
                        id={post.authorId}
                        label={post.authorName}
                        {openPlayer}
                        {openItem}
                      />
                    </td>
                    <td class="message">{post.message}</td>
                    <td>
                      <span class="status-badge">{$t(`guilds.postState_${post.state}`)}</span>
                      {#if post.deleted}
                        <span class="status-badge status-badge--bad">{$t('guilds.deleted')}</span>
                      {/if}
                      {#if post.adminId}
                        <small class="muted">
                          {$t('guilds.byAdmin', { name: post.adminName || `#${post.adminId}` })}
                        </small>
                      {/if}
                    </td>
                    <td>{formatDate(post.createdAt)}</td>
                    <td>
                      <DropdownMenu
                        label={$t('common.actions')}
                        align="end"
                        items={postMenu(post.state, post.deleted)}
                        onpick={(action) => postAction(action, post.id, post.authorName)}
                      />
                    </td>
                  </tr>
                {:else}
                  <tr><td colspan="5" class="muted">{$t('guilds.noPosts')}</td></tr>
                {/each}
              </tbody>
            </table>
          </div>
        {/if}
      {:else}
        <Tabs
          bind:active={tab}
          tabs={[
            { id: 'members', label: $t('guilds.tabMembers'), count: detail.members.length },
            { id: 'requests', label: $t('guilds.tabRequests'), count: detail.pendingRequests.length },
            { id: 'bans', label: $t('guilds.tabBans'), count: detail.bans.length },
            { id: 'forum', label: $t('guilds.tabForum'), count: detail.threads.length },
          ]}
        />

        {#if tab === 'members'}
          <div class="table-wrap">
            <table>
              <thead>
                <tr>
                  <th>{$t('guilds.colPlayer')}</th>
                  <th>{$t('guilds.colRank')}</th>
                  <th>{$t('guilds.colJoined')}</th>
                  <th>{$t('common.actions')}</th>
                </tr>
              </thead>
              <tbody>
                {#each detail.members as member (member.playerId)}
                  <tr>
                    <td>
                      <EntityLink
                        id={member.playerId}
                        label={member.playerName}
                        {openPlayer}
                        {openItem}
                      />
                    </td>
                    <td>
                      <span class="status-badge">{$t(`guilds.rank_${member.rank}`)}</span>
                    </td>
                    <td>{formatDate(member.joinedAt)}</td>
                    <td>
                      {#if member.isOwner}
                        <!-- Not a disabled menu: the owner cannot be removed at all, and offering
                             the action greyed out would suggest a permission is missing. -->
                        <span class="muted">{$t('guilds.ownerNotRemovable')}</span>
                      {:else}
                        <DropdownMenu
                          label={$t('common.actions')}
                          align="end"
                          items={memberMenu()}
                          onpick={(action) =>
                            memberAction(action, member.playerId, member.playerName)}
                        />
                      {/if}
                    </td>
                  </tr>
                {:else}
                  <tr><td colspan="4" class="muted">{$t('guilds.noMembers')}</td></tr>
                {/each}
              </tbody>
            </table>
          </div>
        {:else if tab === 'requests'}
          <div class="table-wrap">
            <table>
              <thead>
                <tr>
                  <th>{$t('guilds.colPlayer')}</th>
                  <th>{$t('guilds.colRequested')}</th>
                  <th>{$t('common.actions')}</th>
                </tr>
              </thead>
              <tbody>
                {#each detail.pendingRequests as request (request.playerId)}
                  <tr>
                    <td>
                      <EntityLink
                        id={request.playerId}
                        label={request.playerName}
                        {openPlayer}
                        {openItem}
                      />
                    </td>
                    <td>{formatDate(request.requestedAt)}</td>
                    <td>
                      <div class="op-actions">
                        <button
                          type="button"
                          onclick={() =>
                            memberAction(MEMBER.approve, request.playerId, request.playerName)}
                        >
                          {$t('guilds.approve')}
                        </button>
                        <button
                          type="button"
                          class="ghost-button"
                          onclick={() =>
                            memberAction(MEMBER.reject, request.playerId, request.playerName)}
                        >
                          {$t('guilds.reject')}
                        </button>
                      </div>
                    </td>
                  </tr>
                {:else}
                  <tr><td colspan="3" class="muted">{$t('guilds.noRequests')}</td></tr>
                {/each}
              </tbody>
            </table>
          </div>
        {:else if tab === 'bans'}
          <div class="table-wrap">
            <table>
              <thead>
                <tr>
                  <th>{$t('guilds.colPlayer')}</th>
                  <th>{$t('guilds.colBannedBy')}</th>
                  <th>{$t('guilds.colBannedAt')}</th>
                  <th>{$t('common.actions')}</th>
                </tr>
              </thead>
              <tbody>
                {#each detail.bans as ban (ban.playerId)}
                  <tr>
                    <td>
                      <EntityLink id={ban.playerId} label={ban.playerName} {openPlayer} {openItem} />
                    </td>
                    <td>
                      <EntityLink
                        id={ban.blockedById}
                        label={ban.blockedByName}
                        {openPlayer}
                        {openItem}
                      />
                    </td>
                    <td>{formatDate(ban.blockedAt)}</td>
                    <td>
                      <button
                        type="button"
                        class="ghost-button"
                        onclick={() => memberAction(MEMBER.liftBan, ban.playerId, ban.playerName)}
                      >
                        {$t('guilds.liftBan')}
                      </button>
                    </td>
                  </tr>
                {:else}
                  <tr><td colspan="4" class="muted">{$t('guilds.noBans')}</td></tr>
                {/each}
              </tbody>
            </table>
          </div>
        {:else}
          <div class="table-wrap">
            <table>
              <thead>
                <tr>
                  <th>{$t('guilds.colSubject')}</th>
                  <th>{$t('guilds.colAuthor')}</th>
                  <th>{$t('guilds.colState')}</th>
                  <th>{$t('guilds.colPosts')}</th>
                  <th>{$t('guilds.colLastPost')}</th>
                  <th>{$t('common.actions')}</th>
                </tr>
              </thead>
              <tbody>
                {#each detail.threads as row (row.id)}
                  <tr>
                    <td>
                      <button type="button" class="linklike" onclick={() => (openThreadId = row.id)}>
                        {row.subject}
                      </button>
                      {#if row.isPinned}
                        <span class="status-badge">{$t('guilds.pinned')}</span>
                      {/if}
                    </td>
                    <td>
                      <EntityLink id={row.authorId} label={row.authorName} {openPlayer} {openItem} />
                    </td>
                    <td>
                      <span class="status-badge">{$t(`guilds.threadState_${row.state}`)}</span>
                    </td>
                    <td>{formatNumber(row.postCount)}</td>
                    <td>{row.lastPostAt ? formatDate(row.lastPostAt) : '-'}</td>
                    <td>
                      <DropdownMenu
                        label={$t('common.actions')}
                        align="end"
                        items={threadMenu(row.state)}
                        onpick={(action) => threadAction(action, row.id, row.subject)}
                      />
                    </td>
                  </tr>
                {:else}
                  <tr><td colspan="6" class="muted">{$t('guilds.noThreads')}</td></tr>
                {/each}
              </tbody>
            </table>
          </div>
        {/if}
      {/if}

      {#if $ops.result}
        <OpResult result={$ops.result} />
      {/if}
    {/if}

    {#snippet actions()}
      <button type="button" class="danger" onclick={deleteGuild} disabled={!detail}>
        {$t('guilds.deleteGuild')}
      </button>
      <button type="button" class="ghost-button" onclick={close}>{$t('common.close')}</button>
    {/snippet}
  </Modal>
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

<LoadingOverlay show={guilds.loading || guild.loading || thread.loading} />

<style>
  .guild-cell {
    display: flex;
    align-items: center;
    gap: 10px;
  }

  .guild-cell small {
    display: block;
  }

  .guild-head {
    display: flex;
    align-items: flex-start;
    gap: 12px;
    margin-bottom: 10px;
  }

  .guild-facts {
    min-width: 0;
  }

  .thread-head {
    display: flex;
    align-items: center;
    gap: 10px;
    margin: 10px 0;
  }

  /* A long post is the thing being judged, so it wraps rather than being cut off with an ellipsis
     that hides the half somebody was reported for. */
  .message {
    max-width: 420px;
    overflow-wrap: anywhere;
    white-space: pre-wrap;
  }

  .removed {
    opacity: 0.6;
  }
</style>
