<script>
  // Habbicon content and ownership.
  //
  // Two jobs that are read against completely different questions -- "is this set right?" and "why
  // does this player have that?" -- so they are tabs rather than two screens stacked vertically.
  import { apiGet } from '../lib/api.js';
  import { createResource } from '../lib/resource.js';
  import { createWriteOps } from '../lib/writeOps.js';
  import { hasDashboardCapability } from '../lib/permissions.js';
  import { CAPABILITIES } from '../lib/dashboardPermissions.js';
  import { identity } from '../lib/session.js';
  import { formatDate, formatNumber } from '../lib/format.js';
  import { t, translate } from '../lib/i18n.js';
  import { Smile, Users } from '@lucide/svelte';

  import AccessDeniedNotice from '../components/AccessDeniedNotice.svelte';
  import ConfirmReasonModal from '../components/ConfirmReasonModal.svelte';
  import Drawer from '../components/Drawer.svelte';
  import EmptyState from '../components/EmptyState.svelte';
  import HabbiconSprite from '../components/HabbiconSprite.svelte';
  import OpResult from '../components/OpResult.svelte';
  import PageHeader from '../components/PageHeader.svelte';
  import PickerModal from '../components/PickerModal.svelte';
  import StatCard from '../components/StatCard.svelte';
  import Tabs from '../components/Tabs.svelte';

  let tab = $state('collections');
  let search = $state('');
  let expanded = $state(null);

  let player = $state(null);
  let pickingPlayer = $state(false);
  let grantHabbiconId = $state('');

  let collectionDraft = $state(null);
  let habbiconDraft = $state(null);

  const ops = createWriteOps();

  let canManage = $derived(hasDashboardCapability($identity, CAPABILITIES.opsHabbiconsManage));

  const collections = createResource(
    () => ['habbicons', search.trim()],
    () => {
      const params = search.trim() ? `?search=${encodeURIComponent(search.trim())}` : '';
      return apiGet(`/api/v1/habbicons${params}`);
    }
  );

  const ownership = createResource(
    () => ['habbicons-player', player?.id ?? null],
    () => apiGet(`/api/v1/habbicons/players/${player.id}`),
    { enabled: () => player !== null }
  );

  let items = $derived(collections.data?.items ?? []);
  // Null until an asset pack is installed; every sprite then falls back to a placeholder tile.
  let artwork = $derived(collections.data?.artwork ?? null);
  // The ownership tab reads ids out of a different endpoint, so it borrows the collection list's
  // frames rather than shipping the pack twice.
  let spriteById = $derived(
    new Map(items.flatMap((c) => c.habbicons.map((h) => [h.id, h.sprite ?? null])))
  );
  let totalHabbicons = $derived(items.reduce((sum, c) => sum + c.entryCount + (c.rewardHabbiconId ? 1 : 0), 0));
  let setsWithReward = $derived(items.filter((c) => c.rewardHabbiconId > 0).length);

  // The ids are the client asset pack's numbering, not ours -- a mismatch draws the wrong picture
  // with nothing to error on. Worth saying on the page rather than only in the seed's header.
  let idWarning = $derived(items.length > 0);

  function emptyCollection() {
    return {
      code: '',
      sortOrder: 0,
      enabled: true,
      hidden: false,
      availableFrom: '',
      availableUntil: '',
      priceCredits: 0,
      priceActivityPoints: 0,
      activityPointType: 0,
      campaignCode: '',
    };
  }

  function emptyHabbicon(collectionId) {
    return {
      code: '',
      collectionId,
      sortOrder: 0,
      isCollectionReward: false,
      priceCredits: 0,
      priceActivityPoints: 0,
      activityPointType: 0,
      enabled: true,
      availableFrom: '',
      availableUntil: '',
    };
  }

  function toLocal(iso) {
    if (!iso) return '';
    const date = new Date(iso);
    if (Number.isNaN(date.getTime())) return '';
    const pad = (n) => String(n).padStart(2, '0');
    return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}T${pad(date.getHours())}:${pad(date.getMinutes())}`;
  }

  function fromLocal(value) {
    if (!value) return null;
    const date = new Date(value);
    return Number.isNaN(date.getTime()) ? null : date.toISOString();
  }

  function collectionBody(form, id) {
    const body = {
      code: form.code.trim(),
      sortOrder: Number(form.sortOrder) || 0,
      enabled: form.enabled,
      hidden: form.hidden,
      availableFrom: fromLocal(form.availableFrom),
      availableUntil: fromLocal(form.availableUntil),
      priceCredits: Number(form.priceCredits) || 0,
      priceActivityPoints: Number(form.priceActivityPoints) || 0,
      activityPointType: Number(form.activityPointType) || 0,
      campaignCode: form.campaignCode.trim(),
    };

    return id === null ? body : { collectionId: id, ...body };
  }

  function habbiconBody(form, id) {
    const body = {
      code: form.code.trim(),
      collectionId: Number(form.collectionId) || 0,
      sortOrder: Number(form.sortOrder) || 0,
      isCollectionReward: form.isCollectionReward,
      priceCredits: Number(form.priceCredits) || 0,
      priceActivityPoints: Number(form.priceActivityPoints) || 0,
      activityPointType: Number(form.activityPointType) || 0,
      enabled: form.enabled,
      availableFrom: fromLocal(form.availableFrom),
      availableUntil: fromLocal(form.availableUntil),
    };

    return id === null ? body : { habbiconId: id, ...body };
  }

  function saveCollection() {
    if (!canManage || !collectionDraft) return;

    const { id, form } = collectionDraft;

    ops.ask(
      id === null
        ? '/api/v1/operations/habbicons/collections'
        : '/api/v1/operations/habbicons/collections/update',
      collectionBody(form, id),
      id === null ? translate('habbicons.newCollection') : translate('habbicons.editCollection'),
      form.code,
      {
        key: 'collectionForm',
        valid: Boolean(form.code.trim()),
        invalidMessage: translate('habbicons.codeRequired'),
        onSuccess: async () => {
          collectionDraft = null;
          await collections.refresh();
        },
      }
    );
  }

  function saveHabbicon() {
    if (!canManage || !habbiconDraft) return;

    const { id, form } = habbiconDraft;

    ops.ask(
      id === null ? '/api/v1/operations/habbicons' : '/api/v1/operations/habbicons/update',
      habbiconBody(form, id),
      id === null ? translate('habbicons.newHabbicon') : translate('habbicons.editHabbicon'),
      form.code,
      {
        key: 'habbiconForm',
        valid: Boolean(form.code.trim()) && Number(form.collectionId) > 0,
        invalidMessage: translate('habbicons.codeRequired'),
        onSuccess: async () => {
          habbiconDraft = null;
          await collections.refresh();
        },
      }
    );
  }

  function deleteCollection(collection) {
    ops.ask(
      '/api/v1/operations/habbicons/collections/delete',
      { collectionId: collection.id },
      translate('habbicons.deleteCollection'),
      collection.code,
      { onSuccess: () => collections.refresh() }
    );
  }

  function deleteHabbicon(habbicon) {
    ops.ask(
      '/api/v1/operations/habbicons/delete',
      { habbiconId: habbicon.id },
      translate('habbicons.deleteHabbicon'),
      habbicon.code,
      { onSuccess: () => collections.refresh() }
    );
  }

  function grant() {
    if (!player || !Number(grantHabbiconId)) return;

    ops.ask(
      '/api/v1/operations/habbicons/grant',
      { playerId: player.id, habbiconId: Number(grantHabbiconId) },
      translate('habbicons.grant'),
      `${player.name} · #${grantHabbiconId}`,
      {
        onSuccess: async () => {
          grantHabbiconId = '';
          await ownership.refresh();
        },
      }
    );
  }

  function revoke(row) {
    ops.ask(
      '/api/v1/operations/habbicons/revoke',
      { playerId: player.id, habbiconId: row.habbiconId },
      translate('habbicons.revoke'),
      `${player.name} · ${row.code}`,
      { onSuccess: () => ownership.refresh() }
    );
  }
</script>

<section class="panel">
  <PageHeader title={$t('habbicons.title')} description={$t('habbicons.subtitle')}>
    {#snippet actions()}
      <button type="button" class="warning" onclick={collections.refresh}>
        {$t('common.refresh')}
      </button>
    {/snippet}
  </PageHeader>
</section>

{#if collections.forbidden}
  <AccessDeniedNotice />
{:else}
  <Tabs
    bind:active={tab}
    storageKey="habbicons"
    tabs={[
      {
        id: 'collections',
        label: $t('habbicons.tabCollections'),
        icon: Smile,
        count: items.length,
      },
      { id: 'players', label: $t('habbicons.tabPlayers'), icon: Users },
    ]}
  />

  {#if tab === 'collections'}
    <div class="metric-grid">
      <StatCard label={$t('habbicons.statCollections')} value={formatNumber(items.length)} />
      <StatCard label={$t('habbicons.statHabbicons')} value={formatNumber(totalHabbicons)} />
      <StatCard label={$t('habbicons.statWithReward')} value={formatNumber(setsWithReward)} />
    </div>

    <div class="panel">
      <div class="panel-head">
        <h2>{$t('habbicons.collections')}</h2>
        {#if canManage}
          <button
            type="button"
            class="success"
            onclick={() => (collectionDraft = { id: null, form: emptyCollection() })}
          >
            {$t('habbicons.newCollection')}
          </button>
        {/if}
      </div>

      {#if idWarning}
        <p class="muted">{$t('habbicons.idWarning')}</p>
      {/if}

      <div class="filters">
        <input
          autocomplete="off"
          spellcheck="false"
          type="search"
          bind:value={search}
          placeholder={$t('habbicons.searchPlaceholder')}
        />
      </div>

      {#if collections.loading}
        <p class="muted">{$t('common.loading')}</p>
      {:else if items.length === 0}
        <EmptyState message={$t('habbicons.noCollections')} />
      {:else}
        <div class="table-wrap">
          <table>
            <thead>
              <tr>
                <th>{$t('habbicons.code')}</th>
                <th>{$t('habbicons.entries')}</th>
                <th>{$t('habbicons.reward')}</th>
                <th>{$t('habbicons.setPrice')}</th>
                <th>{$t('habbicons.completedBy')}</th>
                <th>{$t('habbicons.state')}</th>
                <th></th>
              </tr>
            </thead>
            <tbody>
              {#each items as collection (collection.id)}
                <tr>
                  <td>
                    <div class="named">
                      <HabbiconSprite
                        sheet={artwork?.collectionSpritesheetUrl}
                        sprite={collection.sprite}
                        size={artwork?.collectionIconSize ?? 18}
                        alt={collection.code}
                      />
                      <div>
                        <strong>{collection.code}</strong>
                        <div class="muted small">{collection.localizationKey}</div>
                      </div>
                    </div>
                  </td>
                  <td>{formatNumber(collection.entryCount)}</td>
                  <td>
                    {#if collection.rewardHabbiconId}
                      {collection.rewardCode}
                    {:else}
                      <span class="muted">{$t('habbicons.noReward')}</span>
                    {/if}
                  </td>
                  <td>
                    {#if collection.priceCredits}
                      {formatNumber(collection.priceCredits)} c
                    {/if}
                    {#if collection.priceActivityPoints}
                      {formatNumber(collection.priceActivityPoints)} p{collection.activityPointType}
                    {/if}
                    {#if !collection.priceCredits && !collection.priceActivityPoints}
                      <span class="muted">{$t('habbicons.notSoldAsSet')}</span>
                    {/if}
                  </td>
                  <td>{formatNumber(collection.completedBy)}</td>
                  <td>
                    <div class="chips">
                      {#if !collection.enabled}
                        <span class="status-badge status-badge--bad">{$t('habbicons.disabled')}</span>
                      {/if}
                      {#if collection.hidden}
                        <span class="op-chip">{$t('habbicons.hidden')}</span>
                      {/if}
                    </div>
                  </td>
                  <td class="row-actions">
                    <button
                      type="button"
                      class="ghost-button"
                      onclick={() => (expanded = expanded === collection.id ? null : collection.id)}
                    >
                      {expanded === collection.id ? $t('habbicons.hide') : $t('habbicons.members')}
                    </button>
                    {#if canManage}
                      <button
                        type="button"
                        class="ghost-button"
                        onclick={() =>
                          (collectionDraft = {
                            id: collection.id,
                            form: {
                              ...collection,
                              availableFrom: toLocal(collection.availableFrom),
                              availableUntil: toLocal(collection.availableUntil),
                            },
                          })}
                      >
                        {$t('common.edit')}
                      </button>
                      <button type="button" class="danger" onclick={() => deleteCollection(collection)}>
                        {$t('common.delete')}
                      </button>
                    {/if}
                  </td>
                </tr>
                {#if expanded === collection.id}
                  <tr>
                    <td colspan="7">
                      <div class="panel-head">
                        <h3>{$t('habbicons.membersOf', { code: collection.code })}</h3>
                        {#if canManage}
                          <button
                            type="button"
                            class="success"
                            onclick={() =>
                              (habbiconDraft = { id: null, form: emptyHabbicon(collection.id) })}
                          >
                            {$t('habbicons.newHabbicon')}
                          </button>
                        {/if}
                      </div>
                      <table>
                        <thead>
                          <tr>
                            <th>{$t('habbicons.id')}</th>
                            <th>{$t('habbicons.code')}</th>
                            <th>{$t('habbicons.price')}</th>
                            <th>{$t('habbicons.owners')}</th>
                            <th>{$t('habbicons.state')}</th>
                            <th></th>
                          </tr>
                        </thead>
                        <tbody>
                          {#each collection.habbicons as habbicon (habbicon.id)}
                            <tr>
                              <td>{habbicon.id}</td>
                              <td>
                                <div class="named">
                                  <HabbiconSprite
                                    sheet={artwork?.spritesheetUrl}
                                    sprite={habbicon.sprite}
                                    size={artwork?.frameSize ?? 40}
                                    alt={habbicon.code}
                                  />
                                  <div>
                                    {habbicon.code}
                                    {#if habbicon.isCollectionReward}
                                      <span class="op-chip">{$t('habbicons.setReward')}</span>
                                    {/if}
                                    <div class="muted small">{habbicon.localizationKey}</div>
                                  </div>
                                </div>
                              </td>
                              <td>
                                {#if habbicon.priceCredits}{formatNumber(habbicon.priceCredits)} c{/if}
                                {#if habbicon.priceActivityPoints}
                                  {formatNumber(habbicon.priceActivityPoints)} p{habbicon.activityPointType}
                                {/if}
                              </td>
                              <td>{formatNumber(habbicon.owners)}</td>
                              <td>
                                {#if !habbicon.enabled}
                                  <span class="op-chip">{$t('habbicons.disabled')}</span>
                                {/if}
                              </td>
                              <td class="row-actions">
                                {#if canManage}
                                  <button
                                    type="button"
                                    class="ghost-button"
                                    onclick={() =>
                                      (habbiconDraft = {
                                        id: habbicon.id,
                                        form: {
                                          ...habbicon,
                                          availableFrom: toLocal(habbicon.availableFrom),
                                          availableUntil: toLocal(habbicon.availableUntil),
                                        },
                                      })}
                                  >
                                    {$t('common.edit')}
                                  </button>
                                  <button
                                    type="button"
                                    class="danger"
                                    onclick={() => deleteHabbicon(habbicon)}
                                  >
                                    {$t('common.delete')}
                                  </button>
                                {/if}
                              </td>
                            </tr>
                          {/each}
                        </tbody>
                      </table>
                    </td>
                  </tr>
                {/if}
              {/each}
            </tbody>
          </table>
        </div>
      {/if}
    </div>
  {/if}

  {#if tab === 'players'}
    <div class="panel">
      <div class="panel-head">
        <h2>{$t('habbicons.playerLookup')}</h2>
        <button type="button" class="ghost-button" onclick={() => (pickingPlayer = true)}>
          {player ? player.name : $t('habbicons.pickPlayer')}
        </button>
      </div>

      {#if !player}
        <EmptyState message={$t('habbicons.pickPlayerHint')} />
      {:else if ownership.loading}
        <p class="muted">{$t('common.loading')}</p>
      {:else}
        {#if canManage}
          <div class="filters">
            <input
              type="number"
              bind:value={grantHabbiconId}
              placeholder={$t('habbicons.grantPlaceholder')}
            />
            <button type="button" class="success" onclick={grant}>{$t('habbicons.grant')}</button>
          </div>
        {/if}

        {#if (ownership.data?.items ?? []).length === 0}
          <EmptyState message={$t('habbicons.ownsNothing')} />
        {:else}
          <div class="table-wrap">
            <table>
              <thead>
                <tr>
                  <th>{$t('habbicons.id')}</th>
                  <th>{$t('habbicons.code')}</th>
                  <th>{$t('habbicons.state')}</th>
                  <th>{$t('habbicons.source')}</th>
                  <th>{$t('habbicons.acquired')}</th>
                  <th>{$t('habbicons.lastUsed')}</th>
                  <th></th>
                </tr>
              </thead>
              <tbody>
                {#each ownership.data.items as row (row.habbiconId)}
                  <tr>
                    <td>{row.habbiconId}</td>
                    <td>
                      <div class="named">
                        <HabbiconSprite
                          sheet={artwork?.spritesheetUrl}
                          sprite={spriteById.get(row.habbiconId) ?? null}
                          size={artwork?.frameSize ?? 40}
                          alt={row.code}
                        />
                        <span>{row.code}</span>
                      </div>
                    </td>
                    <td>{row.state}</td>
                    <td>{row.source}</td>
                    <td>{formatDate(row.acquiredAt)}</td>
                    <td>{row.lastUsedAt ? formatDate(row.lastUsedAt) : '—'}</td>
                    <td class="row-actions">
                      {#if canManage}
                        <button type="button" class="danger" onclick={() => revoke(row)}>
                          {$t('habbicons.revoke')}
                        </button>
                      {/if}
                    </td>
                  </tr>
                {/each}
              </tbody>
            </table>
          </div>
        {/if}
      {/if}
    </div>
  {/if}
{/if}

{#if collectionDraft}
  <Drawer
    title={collectionDraft.id === null
      ? $t('habbicons.newCollection')
      : $t('habbicons.editCollection')}
    eyebrow={$t('habbicons.title')}
    onclose={() => (collectionDraft = null)}
  >
    <label>
      {$t('habbicons.code')}
      <input type="text" bind:value={collectionDraft.form.code} />
    </label>
    <p class="muted small">{$t('habbicons.collectionCodeHint')}</p>
    <label>
      {$t('habbicons.sortOrder')}
      <input type="number" bind:value={collectionDraft.form.sortOrder} />
    </label>
    <label>
      {$t('habbicons.campaign')}
      <input type="text" bind:value={collectionDraft.form.campaignCode} />
    </label>
    <label>
      {$t('habbicons.priceCredits')}
      <input type="number" bind:value={collectionDraft.form.priceCredits} />
    </label>
    <label>
      {$t('habbicons.pricePoints')}
      <input type="number" bind:value={collectionDraft.form.priceActivityPoints} />
    </label>
    <label>
      {$t('habbicons.activityPointType')}
      <input type="number" bind:value={collectionDraft.form.activityPointType} />
    </label>
    <label>
      {$t('habbicons.availableFrom')}
      <input type="datetime-local" bind:value={collectionDraft.form.availableFrom} />
    </label>
    <label>
      {$t('habbicons.availableUntil')}
      <input type="datetime-local" bind:value={collectionDraft.form.availableUntil} />
    </label>
    <label class="checkbox">
      <input type="checkbox" bind:checked={collectionDraft.form.enabled} />
      {$t('habbicons.enabled')}
    </label>
    <label class="checkbox">
      <input type="checkbox" bind:checked={collectionDraft.form.hidden} />
      {$t('habbicons.hiddenLabel')}
    </label>
    <p class="muted small">{$t('habbicons.hiddenHint')}</p>

    {#snippet actions()}
      <button type="button" onclick={saveCollection}>{$t('common.save')}</button>
      <button type="button" class="ghost-button" onclick={() => (collectionDraft = null)}>
        {$t('common.cancel')}
      </button>
    {/snippet}
  </Drawer>
{/if}

{#if habbiconDraft}
  <Drawer
    title={habbiconDraft.id === null ? $t('habbicons.newHabbicon') : $t('habbicons.editHabbicon')}
    eyebrow={$t('habbicons.title')}
    onclose={() => (habbiconDraft = null)}
  >
    <label>
      {$t('habbicons.code')}
      <input type="text" bind:value={habbiconDraft.form.code} />
    </label>
    <p class="muted small">{$t('habbicons.habbiconCodeHint')}</p>
    <label>
      {$t('habbicons.collectionId')}
      <input type="number" bind:value={habbiconDraft.form.collectionId} />
    </label>
    <label>
      {$t('habbicons.sortOrder')}
      <input type="number" bind:value={habbiconDraft.form.sortOrder} />
    </label>
    <label>
      {$t('habbicons.priceCredits')}
      <input type="number" bind:value={habbiconDraft.form.priceCredits} />
    </label>
    <label>
      {$t('habbicons.pricePoints')}
      <input type="number" bind:value={habbiconDraft.form.priceActivityPoints} />
    </label>
    <label>
      {$t('habbicons.activityPointType')}
      <input type="number" bind:value={habbiconDraft.form.activityPointType} />
    </label>
    <label>
      {$t('habbicons.availableFrom')}
      <input type="datetime-local" bind:value={habbiconDraft.form.availableFrom} />
    </label>
    <label>
      {$t('habbicons.availableUntil')}
      <input type="datetime-local" bind:value={habbiconDraft.form.availableUntil} />
    </label>
    <label class="checkbox">
      <input type="checkbox" bind:checked={habbiconDraft.form.isCollectionReward} />
      {$t('habbicons.setRewardLabel')}
    </label>
    <p class="muted small">{$t('habbicons.setRewardHint')}</p>
    <label class="checkbox">
      <input type="checkbox" bind:checked={habbiconDraft.form.enabled} />
      {$t('habbicons.enabled')}
    </label>

    {#snippet actions()}
      <button type="button" onclick={saveHabbicon}>{$t('common.save')}</button>
      <button type="button" class="ghost-button" onclick={() => (habbiconDraft = null)}>
        {$t('common.cancel')}
      </button>
    {/snippet}
  </Drawer>
{/if}

{#if pickingPlayer}
  <PickerModal
    kind="user"
    title={$t('habbicons.pickPlayer')}
    onSelect={(picked) => {
      player = picked;
      pickingPlayer = false;
    }}
    onClose={() => (pickingPlayer = false)}
  />
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

<OpResult result={$ops.result} />

<style>
  /* `.row-actions`, `.table-wrap`, `.filters` spacing and the chips all come from styles.css --
     only the two things the sheet has no opinion about are here. */
  .small {
    font-size: 0.8em;
  }

  .filters {
    display: flex;
    gap: 0.5rem;
  }

  /* A row of pills in a table cell; without it they butt together. */
  .chips {
    display: flex;
    gap: 4px;
    flex-wrap: wrap;
  }

  /* Picture beside its name, the way `.bot-cell` and `.badge-cell` do it elsewhere. */
  .named {
    display: inline-flex;
    align-items: center;
    gap: 8px;
  }
</style>
