<script>
  // Bots are authored from inside the client, so this page reads. What it adds over the raw table is
  // the decoded skill blob: a bot whose menu shows no buttons, or one configured to chat but with
  // zero phrases, looks identical in `bots` and completely different here.
  import { onMount } from 'svelte';
  import { apiGet } from '../lib/api.js';
  import { createWriteOps } from '../lib/writeOps.js';
  import { hasDashboardCapability } from '../lib/permissions.js';
  import { CAPABILITIES } from '../lib/dashboardPermissions.js';
  import { identity } from '../lib/session.js';
  import ConfirmReasonModal from '../components/ConfirmReasonModal.svelte';
  import OpResult from '../components/OpResult.svelte';

  import { formatNumber } from '../lib/format.js';
  import { isPermissionDeniedError } from '../lib/permissions.js';
  import { openPlayer } from '../lib/session.js';
  import AccessDeniedNotice from '../components/AccessDeniedNotice.svelte';
  import AssetImage from '../components/AssetImage.svelte';
  import EmptyState from '../components/EmptyState.svelte';
  import EntityLink from '../components/EntityLink.svelte';
  import LineChart from '../components/LineChart.svelte';
  import Pagination from '../components/Pagination.svelte';
  import PickerModal from '../components/PickerModal.svelte';
  import StatCard from '../components/StatCard.svelte';
  import { Bot, MessageSquare, MapPin, Users, Hand } from '@lucide/svelte';
  import { t } from '../lib/i18n.js';

  const PAGE_SIZE = 40;

  let term = '';
  let owner = null;
  let placedFilter = '';
  let page = 1;
  let loading = false;
  let forbidden = false;
  let error = '';
  let list = null;
  let stats = null;
  let handItems = null;
  let selected = null;
  let detail = null;
  let detailLoading = false;
  let pickingOwner = false;

  const ops = createWriteOps(refresh);

  $: canManage = hasDashboardCapability($identity, CAPABILITIES.opsContentManage);

  const emptyHandItem = () => ({ handItemId: 0, name: '', nutrition: 0, thirst: 0 });
  let handItemForm = emptyHandItem();

  // The only picture of a hand item is an avatar holding it, and the id decides which. Built from
  // the template so a brand-new id previews before its row exists.
  $: handItemPreviewUrl =
    handItemForm.handItemId && handItems?.imageTemplate
      ? handItems.imageTemplate.replace('{item}', String(Number(handItemForm.handItemId)))
      : null;
  let botDraft = null;

  $: totalPages = list ? Math.max(1, Math.ceil((list.total || 0) / (list.limit || PAGE_SIZE))) : 1;

  async function refresh() {
    loading = true;
    error = '';
    forbidden = false;

    const params = new URLSearchParams({ page: String(page), limit: String(PAGE_SIZE) });
    if (term.trim()) params.set('q', term.trim());
    if (owner) params.set('ownerId', String(owner.id));
    if (placedFilter) params.set('placed', placedFilter);

    try {
      const [listResult, statsResult, handItemsResult] = await Promise.all([
        apiGet(`/api/v1/bots?${params}`),
        apiGet('/api/v1/bots/stats'),
        apiGet('/api/v1/hand-items'),
      ]);
      list = listResult;
      stats = statsResult;
      handItems = handItemsResult;
    } catch (err) {
      if (isPermissionDeniedError(err)) {
        forbidden = true;
        list = null;
        stats = null;
        handItems = null;
        return;
      }

      error = err.message;
      list = null;
    } finally {
      loading = false;
    }
  }

  function goToPage(next) {
    page = next;
    void refresh();
  }

  function search() {
    page = 1;
    void refresh();
  }

  // The server returns the client's own skill identifiers; map them to copy an operator can read,
  // and show anything unrecognised as-is rather than swallowing it.
  const SKILL_LABELS = {
    dressUp: 'bots.skillDressUp',
    chatter: 'bots.skillChatter',
    randomWalk: 'bots.skillRandomWalk',
    dance: 'bots.skillDance',
    changeName: 'bots.skillChangeName',
    noPickUp: 'bots.skillNoPickUp',
  };

  function skillLabel(skill) {
    const key = SKILL_LABELS[skill];
    return key ? $t(key) : skill;
  }

  function clearOwner() {
    owner = null;
    search();
  }

  async function select(row) {
    if (selected === row.id) {
      selected = null;
      detail = null;
      return;
    }

    selected = row.id;
    detail = null;
    detailLoading = true;

    try {
      detail = await apiGet(`/api/v1/bots/${row.id}`);
    } catch (err) {
      error = err.message;
    } finally {
      detailLoading = false;
    }
  }

  $: growthSeries = stats
    ? [
        {
          name: $t('bots.totalBots'),
          color: 'var(--accent)',
          points: (stats.growth || []).map((p) => ({ label: p.label, value: p.botsCreated })),
        },
      ]
    : [];

  onMount(() => {
    void refresh();
  });
</script>

<section class="panel">
  <div class="panel-head"><h2>{$t('bots.title')}</h2></div>
  <p class="muted">{$t('bots.description')}</p>

  <form class="toolbar-grid" on:submit|preventDefault={search}>
    <label>
      {$t('bots.search')}
      <input type="search" bind:value={term} placeholder={$t('bots.searchPlaceholder')} />
    </label>
    <label>
      {$t('bots.owner')}
      <button type="button" class="ghost-button" on:click={() => (pickingOwner = true)}>
        {owner ? owner.name : $t('bots.anyOwner')}
      </button>
    </label>
    <label>
      {$t('bots.placement')}
      <select bind:value={placedFilter}>
        <option value="">{$t('bots.placementAny')}</option>
        <option value="true">{$t('bots.placementInRoom')}</option>
        <option value="false">{$t('bots.placementInventory')}</option>
      </select>
    </label>
    <button type="submit" disabled={loading}>{$t('common.refresh')}</button>
    {#if owner}
      <button type="button" class="ghost-button" on:click={clearOwner}>{$t('bots.clearOwner')}</button>
    {/if}
  </form>

  {#if loading}
    <p class="muted">{$t('common.loading')}</p>
  {:else if forbidden}
    <AccessDeniedNotice message={$t('bots.accessDenied')} />
  {:else if error}
    <p class="empty-state danger">{error}</p>
  {/if}
</section>

{#if stats}
  <div class="metric-grid" style="margin-top: 12px;">
    <StatCard label={$t('bots.totalBots')} value={formatNumber(stats.totals.totalBots)}>
      <Bot slot="icon" size={15} strokeWidth={2} aria-hidden="true" />
    </StatCard>
    <StatCard label={$t('bots.placedBots')} value={formatNumber(stats.totals.placedBots)} sub={$t('bots.inventoryBots', { count: formatNumber(stats.totals.inventoryBots) })}>
      <MapPin slot="icon" size={15} strokeWidth={2} aria-hidden="true" />
    </StatCard>
    <StatCard label={$t('bots.chattyBots')} value={formatNumber(stats.totals.chattyBots)} sub={$t('bots.autoChatBots', { count: formatNumber(stats.totals.autoChatBots) })}>
      <MessageSquare slot="icon" size={15} strokeWidth={2} aria-hidden="true" />
    </StatCard>
    <StatCard label={$t('bots.wanderingBots')} value={formatNumber(stats.totals.wanderingBots)}>
      <Bot slot="icon" size={15} strokeWidth={2} aria-hidden="true" />
    </StatCard>
    <StatCard label={$t('bots.distinctOwners')} value={formatNumber(stats.totals.distinctOwners)}>
      <Users slot="icon" size={15} strokeWidth={2} aria-hidden="true" />
    </StatCard>
    <StatCard label={$t('bots.roomsWithBots')} value={formatNumber(stats.totals.roomsWithBots)}>
      <MapPin slot="icon" size={15} strokeWidth={2} aria-hidden="true" />
    </StatCard>
  </div>

  <div class="panel" style="margin-top: 12px;">
    <div class="panel-head"><h2>{$t('bots.growthTitle')}</h2></div>
    <LineChart series={growthSeries} valueFormatter={(v) => formatNumber(v)} />
  </div>
{/if}

{#if list}
  <section class="panel" style="margin-top: 12px;">
    <div class="panel-head"><h2>{$t('bots.rosterTitle')}</h2></div>
    <div class="table-wrap">
      <table>
        <thead>
          <tr>
            <th>{$t('bots.colBot')}</th>
            <th>{$t('bots.colOwner')}</th>
            <th>{$t('bots.colRoom')}</th>
            <th>{$t('bots.colPosition')}</th>
            <th>{$t('bots.colSkills')}</th>
            <th>{$t('bots.colPhrases')}</th>
            <th>{$t('bots.colAutoChat')}</th>
            {#if canManage}<th></th>{/if}
          </tr>
        </thead>
        <tbody>
          {#each list.items || [] as row}
            <tr class:selected={selected === row.id} on:click={() => select(row)} style="cursor: pointer;">
              <td>
                <span class="bot-cell">
                  <AssetImage src={row.avatarUrl} alt={row.name} size={36} fallbackIcon={Bot} />
                  <span>
                    <strong>{row.name}</strong>
                    {#if row.motto}<small class="muted">{row.motto}</small>{/if}
                  </span>
                </span>
              </td>
              <td><EntityLink type="player" id={row.ownerId} label={row.ownerName} {openPlayer} /></td>
              <td>
                {#if row.roomId}
                  {row.roomName || `#${row.roomId}`}
                {:else}
                  <span class="muted">{$t('bots.inHand')}</span>
                {/if}
              </td>
              <td>{row.placed ? `${row.x}, ${row.y}` : '—'}</td>
              <td>
                {#each row.skillNames || [] as skill}
                  <span class="status-badge status-badge--ok skill-chip">{skillLabel(skill)}</span>
                {:else}
                  <span class="status-badge status-badge--warn">{$t('bots.noSkills')}</span>
                {/each}
              </td>
              <td>{formatNumber(row.phraseCount)}</td>
              <td>{row.autoChat ? $t('common.yes') : $t('common.no')}</td>
              {#if canManage}
                <td class="row-actions">
                  <button
                    type="button"
                    class="ghost-button"
                    disabled={row.placed}
                    title={row.placed ? $t('bots.placedLocked') : ''}
                    on:click|stopPropagation={() =>
                      (botDraft = { id: row.id, name: row.name, motto: row.motto, figure: row.figure })}
                  >
                    {$t('bots.edit')}
                  </button>
                  <button
                    type="button"
                    class="ghost-button danger"
                    disabled={row.placed}
                    title={row.placed ? $t('bots.placedLocked') : ''}
                    on:click|stopPropagation={() =>
                      ops.ask(
                        '/api/v1/operations/content/bots/delete',
                        { botId: row.id },
                        $t('bots.deleteBot'),
                        $t('bots.deleteBotSummary', { name: row.name })
                      )}
                  >
                    {$t('bots.delete')}
                  </button>
                </td>
              {/if}
            </tr>
            {#if selected === row.id}
              <tr>
                <td colspan={canManage ? 8 : 7}>
                  {#if detailLoading}
                    <p class="muted">{$t('common.loading')}</p>
                  {:else if detail}
                    <dl class="detail-grid">
                      <div><dt>{$t('bots.detailFigure')}</dt><dd><code>{detail.figure}</code></dd></div>
                      <div><dt>{$t('bots.detailGender')}</dt><dd>{detail.gender}</dd></div>
                      <div><dt>{$t('bots.detailDelay')}</dt><dd>{detail.chatDelaySeconds}s</dd></div>
                      <div><dt>{$t('bots.detailMix')}</dt><dd>{detail.mixSentences ? $t('common.yes') : $t('common.no')}</dd></div>
                      <div><dt>{$t('bots.detailCreated')}</dt><dd>{new Date(detail.createdAt).toLocaleString()}</dd></div>
                    </dl>
                    {#if (detail.phrases || []).length > 0}
                      <ul class="phrase-list">
                        {#each detail.phrases as phrase}
                          <li>{phrase}</li>
                        {/each}
                      </ul>
                    {:else}
                      <EmptyState message={$t('bots.noPhrases')} />
                    {/if}
                  {/if}
                </td>
              </tr>
            {/if}
          {:else}
            <tr><td colspan={canManage ? 8 : 7} class="muted">{$t('bots.noBots')}</td></tr>
          {/each}
        </tbody>
      </table>
    </div>

    {#if canManage && botDraft}
      <form
        class="inline-form"
        on:submit|preventDefault={() =>
          ops.ask(
            '/api/v1/operations/content/bots',
            {
              botId: botDraft.id,
              name: botDraft.name,
              motto: botDraft.motto || '',
              figure: botDraft.figure || '',
            },
            $t('bots.updateBot'),
            $t('bots.updateBotSummary', { name: botDraft.name })
          )}
      >
        <label>
          {$t('bots.colBot')}
          <input bind:value={botDraft.name} required />
        </label>
        <label>
          {$t('bots.motto')}
          <input bind:value={botDraft.motto} />
        </label>
        <label>
          {$t('bots.detailFigure')}
          <input bind:value={botDraft.figure} />
        </label>
        <button type="submit">{$t('bots.save')}</button>
        <button type="button" class="ghost-button" on:click={() => (botDraft = null)}>
          {$t('bots.cancel')}
        </button>
      </form>
      <p class="muted">{$t('bots.placedHint')}</p>
    {/if}

    {#if $ops.result}
      <OpResult result={$ops.result} />
    {/if}

    {#if totalPages > 1}
      <Pagination
        {page}
        pageCount={totalPages}
        total={list.total}
        pageSize={list.limit}
        label={$t('bots.paginationLabel')}
        pageWord={$t('common.page')}
        prevLabel={$t('common.prev')}
        nextLabel={$t('common.next')}
        disabled={loading}
        on:change={(e) => goToPage(e.detail)}
      />
    {/if}
  </section>
{/if}

{#if handItems}
  <section class="panel" style="margin-top: 12px;">
    <div class="panel-head">
      <h2>{$t('bots.handItemsTitle')}</h2>
      <span class="muted">{$t('bots.handItemsCount', { total: handItems.count, consumable: handItems.consumableCount })}</span>
    </div>
    <p class="muted">{$t('bots.handItemsDescription')}</p>
    {#if (handItems.items || []).length === 0}
      <EmptyState message={$t('bots.noHandItems')} />
    {:else}
      <div class="table-wrap">
        <table>
          <thead>
            <tr>
              <th>{$t('bots.colHandItemId')}</th>
              <th>{$t('bots.colHandItemName')}</th>
              <th>{$t('bots.colNutrition')}</th>
              <th>{$t('bots.colThirst')}</th>
              {#if canManage}<th></th>{/if}
            </tr>
          </thead>
          <tbody>
            {#each handItems.items as item}
              <tr>
                <td>
                  <span class="bot-cell">
                    <AssetImage src={item.imageUrl} alt={item.name} size={40} fallbackIcon={Hand} />
                    {item.handItemId}
                  </span>
                </td>
                <td>{item.name}</td>
                <td>{item.nutrition || '—'}</td>
                <td>{item.thirst || '—'}</td>
                {#if canManage}
                  <td class="row-actions">
                    <button type="button" class="ghost-button" on:click={() => (handItemForm = { ...item })}>
                      {$t('bots.edit')}
                    </button>
                    <button
                      type="button"
                      class="ghost-button danger"
                      on:click={() =>
                        ops.ask(
                          '/api/v1/operations/content/hand-items/delete',
                          { id: item.id },
                          $t('bots.deleteHandItem'),
                          $t('bots.deleteHandItemSummary', { name: item.name })
                        )}
                    >
                      {$t('bots.delete')}
                    </button>
                  </td>
                {/if}
              </tr>
            {/each}
          </tbody>
        </table>
      </div>
    {/if}
  </section>
{/if}

{#if canManage && handItems}
  <section class="panel" style="margin-top: 12px;">
    <div class="panel-head"><h2>{$t('bots.handItemEditorTitle')}</h2></div>
    <p class="muted">{$t('bots.handItemEditorHint')}</p>
    <form
      class="inline-form"
      on:submit|preventDefault={() =>
        ops.ask(
          '/api/v1/operations/content/hand-items',
          {
            handItemId: Number(handItemForm.handItemId),
            name: handItemForm.name,
            nutrition: Number(handItemForm.nutrition) || 0,
            thirst: Number(handItemForm.thirst) || 0,
          },
          $t('bots.saveHandItem'),
          $t('bots.saveHandItemSummary', { id: handItemForm.handItemId, name: handItemForm.name })
        )}
    >
      <label>
        {$t('bots.colHandItemId')}
        <span class="bot-cell">
          <input type="number" bind:value={handItemForm.handItemId} min="1" />
          <AssetImage src={handItemPreviewUrl} alt="" size={40} fallbackIcon={Hand} />
        </span>
      </label>
      <label>
        {$t('bots.colHandItemName')}
        <input bind:value={handItemForm.name} />
      </label>
      <label>
        {$t('bots.colNutrition')}
        <input type="number" bind:value={handItemForm.nutrition} min="0" />
      </label>
      <label>
        {$t('bots.colThirst')}
        <input type="number" bind:value={handItemForm.thirst} min="0" />
      </label>
      <button type="submit" disabled={!handItemForm.name.trim() || !handItemForm.handItemId}>
        {$t('bots.saveHandItem')}
      </button>
      <button type="button" class="ghost-button" on:click={() => (handItemForm = emptyHandItem())}>
        {$t('bots.clear')}
      </button>
    </form>
  </section>
{/if}

{#if pickingOwner}
  <PickerModal
    kind="user"
    title={$t('bots.pickOwner')}
    onSelect={(picked) => {
      owner = picked;
      pickingOwner = false;
      search();
    }}
    onClose={() => (pickingOwner = false)}
  />
{/if}

<ConfirmReasonModal
  open={Boolean($ops.pending)}
  title={$ops.pending?.title ?? ''}
  summary={$ops.pending?.summary ?? ''}
  confirmLabel={$ops.pending?.title ?? $t('common.confirm')}
  busy={$ops.busy}
  error={$ops.error}
  on:confirm={(e) => ops.confirm(e.detail)}
  on:cancel={() => ops.cancel()}
/>

<style>
  tr.selected {
    background: var(--surface-raised, rgba(255, 255, 255, 0.04));
  }

  .bot-cell {
    display: inline-flex;
    align-items: center;
    gap: 8px;
  }

  .bot-cell small {
    display: block;
  }

  .skill-chip {
    margin-right: 4px;
  }

  .detail-grid {
    display: grid;
    grid-template-columns: repeat(auto-fit, minmax(160px, 1fr));
    gap: 10px;
    margin: 0 0 12px;
  }

  .detail-grid dt {
    font-size: 0.75rem;
    text-transform: uppercase;
    letter-spacing: 0.04em;
    opacity: 0.7;
  }

  .detail-grid dd {
    margin: 2px 0 0;
  }

  .phrase-list {
    margin: 0;
    padding-left: 18px;
  }
</style>
