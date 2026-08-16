<script>
  // The navigator's own configuration. Unlike every other admin page, the interesting state here is
  // emptiness: an unseeded hotel renders an empty left pane in the client whatever the room list
  // looks like, and a tab with no blocks renders blank. Both are called out at the top, with the
  // one-click seed that fills in the client's own default tabs.
  //
  // Every mutation funnels through the same ConfirmReasonModal, so the audited reason cannot be
  // skipped by a stray Enter key on one of the many small forms.
  import { onMount } from 'svelte';
  import { apiGet } from '../lib/api.js';
  import { createWriteOps } from '../lib/writeOps.js';
  import { formatNumber } from '../lib/format.js';
  import { isPermissionDeniedError, hasDashboardCapability } from '../lib/permissions.js';
  import { CAPABILITIES } from '../lib/dashboardPermissions.js';
  import { identity } from '../lib/session.js';
  import AccessDeniedNotice from '../components/AccessDeniedNotice.svelte';
  import Drawer from '../components/Drawer.svelte';
  import PageHeader from '../components/PageHeader.svelte';
  import ConfirmReasonModal from '../components/ConfirmReasonModal.svelte';
  import EmptyState from '../components/EmptyState.svelte';
  import OpResult from '../components/OpResult.svelte';
  import StatCard from '../components/StatCard.svelte';
  import Tabs from '../components/Tabs.svelte';
  import { Compass, LayoutList, FolderTree, CalendarRange } from '@lucide/svelte';
  import { t } from '../lib/i18n.js';

  let loading = $state(false);
  let forbidden = $state(false);
  let error = $state('');
  let data = $state(null);
  // Every write goes through createWriteOps: the modal collects the reason, the store posts it,
  // remembers it and refreshes -- this callback is only the page's own "clear the drafts" step.
  // What the one drawer calls itself, per kind. Passed the translator so the title follows a
  // language change like every other label on the page.
  function editorTitle(kind, translator) {
    if (kind === 'context') return translator('navigatorConfig.tabsTitle');
    if (kind === 'quickLink') return translator('navigatorConfig.quickLinksTitle');
    if (kind === 'category') return translator('navigatorConfig.categoriesTitle');
    return translator('navigatorConfig.eventCategoriesTitle');
  }

  const ops = createWriteOps(async () => {
    editing = null;
    contextForm = newContext();
    categoryForm = newCategory();
    eventCategoryForm = newEventCategory();
    await refresh();
  });

  let canManage = $derived(hasDashboardCapability($identity, CAPABILITIES.opsNavigatorManage));
  let queryTypes = $derived(data?.queryTypes || []);
  let searchCodes = $derived(data?.searchCodes || []);

  const newContext = () => ({ searchCode: '', visible: true, queryType: 0, orderNum: 0 });
  const newQuickLink = (contextId) => ({
    contextId,
    searchCode: '',
    filter: '',
    localization: '',
    queryType: 0,
    orderNum: 0,
  });
  const newCategory = () => ({
    name: '',
    visible: true,
    automatic: false,
    automaticCategory: '',
    globalCategory: '',
    staffOnly: false,
    minRank: 1,
    orderNum: 0,
  });
  const newEventCategory = () => ({ name: '', visible: true });

  let contextForm = $state(newContext());
  let quickLinkForms = $state({});
  let categoryForm = $state(newCategory());
  let eventCategoryForm = $state(newEventCategory());
  let editing = $state(null); // { kind, id, draft }

  // These sections are independent jobs that were stacked vertically, so reaching the last one
  // meant scrolling past every other. Nothing here is read against anything else -- which is
  // both what makes tabs right and what would have made them wrong.
  let tab = $state('contexts');

  async function refresh() {
    loading = true;
    error = '';
    forbidden = false;

    try {
      data = await apiGet('/api/v1/navigator/config');
      quickLinkForms = Object.fromEntries(
        (data.contexts || []).map((c) => [c.id, newQuickLink(c.id)])
      );
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

  const ask = (endpoint, body, title, summary) => ops.ask(endpoint, body, title, summary);

  function startEdit(kind, row, parentId = null) {
    // parentId: a quick link belongs to a tab, and the drawer no longer sits inside that tab's
    // markup, so the parent it posts against has to travel with the edit.
    editing = { kind, id: row.id, parentId, draft: { ...row } };
  }

  function queryTypeLabel(value) {
    return queryTypes.find((q) => q.value === value)?.label ?? value;
  }

  // Picking a known code should preselect what that code means to the client; the operator can still
  // override it, which is the whole point of the table being configurable.
  function onCodePicked(draft, code) {
    draft.searchCode = code;
    const known = searchCodes.find((s) => s.code === code);
    if (known) draft.queryType = known.queryType;
  }

  onMount(() => {
    void refresh();
  });
</script>

<section class="panel">
  <PageHeader title={$t('navigatorConfig.title')} description={$t('navigatorConfig.description')} />

  <div class="toolbar">
    <button type="button" onclick={refresh} disabled={loading}>{$t('common.refresh')}</button>
    {#if canManage}
      <button
        type="button"
        class="ghost-button"
        onclick={() =>
          ask(
            '/api/v1/operations/navigator/seed-defaults',
            {},
            $t('navigatorConfig.seedTitle'),
            $t('navigatorConfig.seedSummary')
          )}
      >
        {$t('navigatorConfig.seedButton')}
      </button>
    {/if}
  </div>

  {#if loading}
    <p class="muted">{$t('common.loading')}</p>
  {:else if forbidden}
    <AccessDeniedNotice message={$t('navigatorConfig.accessDenied')} />
  {:else if error}
    <p class="empty-state danger">{error}</p>
  {/if}

  {#if $ops.result}
    <OpResult result={$ops.result} />
  {/if}
</section>

{#if data}
  <div class="metric-grid" style="margin-top: 12px;">
    <StatCard label={$t('navigatorConfig.tabs')} value={formatNumber(data.health.contextCount)}>
      {#snippet icon()}
        <Compass size={15} strokeWidth={2} aria-hidden="true" />
      {/snippet}
    </StatCard>
    <StatCard label={$t('navigatorConfig.blocks')} value={formatNumber(data.health.quickLinkCount)}>
      {#snippet icon()}
        <LayoutList size={15} strokeWidth={2} aria-hidden="true" />
      {/snippet}
    </StatCard>
    <StatCard label={$t('navigatorConfig.categories')} value={formatNumber(data.health.flatCategoryCount)}>
      {#snippet icon()}
        <FolderTree size={15} strokeWidth={2} aria-hidden="true" />
      {/snippet}
    </StatCard>
    <StatCard label={$t('navigatorConfig.eventCategories')} value={formatNumber(data.health.eventCategoryCount)}>
      {#snippet icon()}
        <CalendarRange size={15} strokeWidth={2} aria-hidden="true" />
      {/snippet}
    </StatCard>
  </div>

  {#if !data.health.seeded || data.health.missingTabs.length > 0 || data.health.emptyTabs.length > 0}
    <section class="panel warn-panel" style="margin-top: 12px;">
      <div class="panel-head"><h2>{$t('navigatorConfig.healthTitle')}</h2></div>
      {#if !data.health.seeded}
        <p>{$t('navigatorConfig.notSeeded')}</p>
      {/if}
      {#if data.health.missingTabs.length > 0}
        <p>
          {$t('navigatorConfig.missingTabs')}
          {#each data.health.missingTabs as code}<code class="chip">{code}</code>{/each}
        </p>
      {/if}
      {#if data.health.emptyTabs.length > 0}
        <p>
          {$t('navigatorConfig.emptyTabs')}
          {#each data.health.emptyTabs as tab}<code class="chip">{tab.searchCode}</code>{/each}
        </p>
      {/if}
    </section>
  {/if}

  <Tabs
    bind:active={tab}
    storageKey="navigatorConfig"
    tabs={[
      { id: 'contexts', label: $t('navigatorConfig.tabContexts'), icon: Compass, count: data?.contexts?.length },
      { id: 'categories', label: $t('navigatorConfig.tabCategories'), icon: FolderTree, count: data?.flatCategories?.length },
      { id: 'events', label: $t('navigatorConfig.tabEvents'), icon: CalendarRange, count: data?.eventCategories?.length },
    ]}
  />

  {#if tab === 'contexts'}
  <section class="panel" style="margin-top: 12px;">
    <div class="panel-head"><h2>{$t('navigatorConfig.tabsTitle')}</h2></div>

    {#if (data.contexts || []).length === 0}
      <EmptyState message={$t('navigatorConfig.noTabs')} />
    {/if}

    {#each data.contexts || [] as context}
      <article class="tab-card">
        <header>
          <div>
            <strong><code>{context.searchCode}</code></strong>
            {#if !context.knownCode}
              <span class="status-badge status-badge--warn">{$t('navigatorConfig.unknownCode')}</span>
            {/if}
            {#if !context.visible}
              <span class="status-badge status-badge--unknown">{$t('navigatorConfig.hidden')}</span>
            {/if}
            <small class="muted">
              {queryTypeLabel(context.queryType)} · {$t('navigatorConfig.order')} {context.orderNum}
            </small>
          </div>
          {#if canManage}
            <div class="row-actions">
              <button type="button" class="ghost-button" onclick={() => startEdit('context', context)}>
                {$t('navigatorConfig.edit')}
              </button>
              <button
                type="button"
                class="ghost-button danger"
                onclick={() =>
                  ask(
                    '/api/v1/operations/navigator/contexts/delete',
                    { contextId: context.id },
                    $t('navigatorConfig.deleteTab'),
                    $t('navigatorConfig.deleteTabSummary', { code: context.searchCode })
                  )}
              >
                {$t('navigatorConfig.delete')}
              </button>
            </div>
          {/if}
        </header>


        <div class="table-wrap">
          <table>
            <thead>
              <tr>
                <th>{$t('navigatorConfig.colBlockCode')}</th>
                <th>{$t('navigatorConfig.colQueryType')}</th>
                <th>{$t('navigatorConfig.colFilter')}</th>
                <th>{$t('navigatorConfig.colLocalization')}</th>
                <th>{$t('navigatorConfig.colOrder')}</th>
                {#if canManage}<th></th>{/if}
              </tr>
            </thead>
            <tbody>
              {#each context.quickLinks || [] as link}
                <tr>
                  <td>
                    <code>{link.searchCode}</code>
                    {#if !link.knownCode}
                      <span class="status-badge status-badge--warn">{$t('navigatorConfig.unknownCode')}</span>
                    {/if}
                  </td>
                  <td>{queryTypeLabel(link.queryType)}</td>
                  <td>{link.filter || '—'}</td>
                  <td>{link.localization || '—'}</td>
                  <td>{link.orderNum}</td>
                  {#if canManage}
                    <td class="row-actions">
                      <button type="button" class="ghost-button" onclick={() => startEdit('quickLink', link, context.id)}>
                        {$t('navigatorConfig.edit')}
                      </button>
                      <button
                        type="button"
                        class="ghost-button danger"
                        onclick={() =>
                          ask(
                            '/api/v1/operations/navigator/quick-links/delete',
                            { quickLinkId: link.id },
                            $t('navigatorConfig.deleteBlock'),
                            $t('navigatorConfig.deleteBlockSummary', { code: link.searchCode })
                          )}
                      >
                        {$t('navigatorConfig.delete')}
                      </button>
                    </td>
                  {/if}
                </tr>
              {:else}
                <tr>
                  <td colspan={canManage ? 6 : 5} class="muted">{$t('navigatorConfig.noBlocks')}</td>
                </tr>
              {/each}
            </tbody>
          </table>
        </div>

        {#if canManage && quickLinkForms[context.id]}
          <form
            class="edit-grid"
            onsubmit={(event) => {
              event.preventDefault();
              ask(
                '/api/v1/operations/navigator/quick-links',
                {
                  contextId: context.id,
                  searchCode: quickLinkForms[context.id].searchCode,
                  filter: quickLinkForms[context.id].filter || '',
                  localization: quickLinkForms[context.id].localization || '',
                  queryType: quickLinkForms[context.id].queryType,
                  orderNum: quickLinkForms[context.id].orderNum,
                },
                $t('navigatorConfig.addBlock'),
                $t('navigatorConfig.addBlockSummary', {
                  code: quickLinkForms[context.id].searchCode,
                  tab: context.searchCode,
                })
              );
            }}
          >
            <label>
              {$t('navigatorConfig.searchCode')}
              <select
                value={quickLinkForms[context.id].searchCode}
                onchange={(e) => onCodePicked(quickLinkForms[context.id], e.currentTarget.value)}
              >
                <option value="">{$t('navigatorConfig.pickCode')}</option>
                {#each searchCodes as code}
                  <option value={code.code}>{code.code} — {code.queryTypeLabel}</option>
                {/each}
              </select>
            </label>
            <label>
              {$t('navigatorConfig.queryType')}
              <select bind:value={quickLinkForms[context.id].queryType}>
                {#each queryTypes as q}
                  <option value={q.value}>{q.label}</option>
                {/each}
              </select>
            </label>
            <label>
              {$t('navigatorConfig.order')}
              <input type="number" bind:value={quickLinkForms[context.id].orderNum} />
            </label>
            <button type="submit" disabled={!quickLinkForms[context.id].searchCode}>
              {$t('navigatorConfig.addBlock')}
            </button>
          </form>
        {/if}
      </article>
    {/each}

    {#if canManage}
      <form
        class="edit-grid"
        onsubmit={(event) => {
          event.preventDefault();
          ask(
            '/api/v1/operations/navigator/contexts',
            contextForm,
            $t('navigatorConfig.addTab'),
            $t('navigatorConfig.addTabSummary', { code: contextForm.searchCode })
          );
        }}
      >
        <label>
          {$t('navigatorConfig.searchCode')}
          <select value={contextForm.searchCode} onchange={(e) => onCodePicked(contextForm, e.currentTarget.value)}>
            <option value="">{$t('navigatorConfig.pickCode')}</option>
            {#each searchCodes.filter((c) => c.topLevel) as code}
              <option value={code.code}>{code.code} — {code.queryTypeLabel}</option>
            {/each}
          </select>
        </label>
        <label>
          {$t('navigatorConfig.queryType')}
          <select bind:value={contextForm.queryType}>
            {#each queryTypes as q}
              <option value={q.value}>{q.label}</option>
            {/each}
          </select>
        </label>
        <label>
          {$t('navigatorConfig.order')}
          <input type="number" bind:value={contextForm.orderNum} />
        </label>
        <label class="check">
          <input type="checkbox" bind:checked={contextForm.visible} />
          {$t('navigatorConfig.visible')}
        </label>
        <button type="submit" disabled={!contextForm.searchCode}>{$t('navigatorConfig.addTab')}</button>
      </form>
    {/if}
  </section>
  {/if}

  {#if tab === 'categories'}
  <section class="panel" style="margin-top: 12px;">
    <div class="panel-head"><h2>{$t('navigatorConfig.categoriesTitle')}</h2></div>
    <p class="muted">{$t('navigatorConfig.categoriesDescription')}</p>
    <div class="table-wrap">
      <table>
        <thead>
          <tr>
            <th>{$t('navigatorConfig.colId')}</th>
            <th>{$t('navigatorConfig.colName')}</th>
            <th>{$t('navigatorConfig.colRooms')}</th>
            <th>{$t('navigatorConfig.colVisible')}</th>
            <th>{$t('navigatorConfig.colStaffOnly')}</th>
            <th>{$t('navigatorConfig.colMinRank')}</th>
            <th>{$t('navigatorConfig.colOrder')}</th>
            {#if canManage}<th></th>{/if}
          </tr>
        </thead>
        <tbody>
          {#each data.flatCategories || [] as category}
            <tr>
              <td>{category.id}</td>
              <td>{category.name}</td>
              <td>{formatNumber(category.roomCount)}</td>
              <td>{category.visible ? $t('common.yes') : $t('common.no')}</td>
              <td>{category.staffOnly ? $t('common.yes') : $t('common.no')}</td>
              <td>{category.minRank}</td>
              <td>{category.orderNum}</td>
              {#if canManage}
                <td class="row-actions">
                  <button type="button" class="ghost-button" onclick={() => startEdit('category', category)}>
                    {$t('navigatorConfig.edit')}
                  </button>
                  <button
                    type="button"
                    class="ghost-button danger"
                    onclick={() =>
                      ask(
                        '/api/v1/operations/navigator/categories/delete',
                        { categoryId: category.id },
                        $t('navigatorConfig.deleteCategory'),
                        $t('navigatorConfig.deleteCategorySummary', {
                          name: category.name,
                          rooms: category.roomCount,
                        })
                      )}
                  >
                    {$t('navigatorConfig.delete')}
                  </button>
                </td>
              {/if}
            </tr>
          {:else}
            <tr>
              <td colspan={canManage ? 8 : 7} class="muted">{$t('navigatorConfig.noCategories')}</td>
            </tr>
          {/each}
        </tbody>
      </table>
    </div>

    {#if canManage}
      <form
        class="edit-grid"
        onsubmit={(event) => {
          event.preventDefault();
          ask(
            '/api/v1/operations/navigator/categories',
            categoryForm,
            $t('navigatorConfig.addCategory'),
            $t('navigatorConfig.addCategorySummary', { name: categoryForm.name })
          );
        }}
      >
        <label>
          {$t('navigatorConfig.colName')}
          <input bind:value={categoryForm.name} placeholder={$t('navigatorConfig.categoryPlaceholder')} />
        </label>
        <label>
          {$t('navigatorConfig.colMinRank')}
          <input type="number" bind:value={categoryForm.minRank} />
        </label>
        <label>
          {$t('navigatorConfig.order')}
          <input type="number" bind:value={categoryForm.orderNum} />
        </label>
        <label class="check">
          <input type="checkbox" bind:checked={categoryForm.staffOnly} />
          {$t('navigatorConfig.colStaffOnly')}
        </label>
        <button type="submit" disabled={!categoryForm.name.trim()}>{$t('navigatorConfig.addCategory')}</button>
      </form>
    {/if}
  </section>
  {/if}

  {#if tab === 'events'}
  <section class="panel" style="margin-top: 12px;">
    <div class="panel-head"><h2>{$t('navigatorConfig.eventCategoriesTitle')}</h2></div>
    <p class="muted">{$t('navigatorConfig.eventCategoriesDescription')}</p>
    <div class="table-wrap">
      <table>
        <thead>
          <tr>
            <th>{$t('navigatorConfig.colId')}</th>
            <th>{$t('navigatorConfig.colName')}</th>
            <th>{$t('navigatorConfig.colActiveAds')}</th>
            <th>{$t('navigatorConfig.colVisible')}</th>
            {#if canManage}<th></th>{/if}
          </tr>
        </thead>
        <tbody>
          {#each data.eventCategories || [] as category}
            <tr>
              <td>{category.id}</td>
              <td>{category.name}</td>
              <td>{formatNumber(category.activeAdCount)}</td>
              <td>{category.visible ? $t('common.yes') : $t('common.no')}</td>
              {#if canManage}
                <td class="row-actions">
                  <button type="button" class="ghost-button" onclick={() => startEdit('eventCategory', category)}>
                    {$t('navigatorConfig.edit')}
                  </button>
                  <button
                    type="button"
                    class="ghost-button danger"
                    onclick={() =>
                      ask(
                        '/api/v1/operations/navigator/event-categories/delete',
                        { categoryId: category.id },
                        $t('navigatorConfig.deleteEventCategory'),
                        $t('navigatorConfig.deleteEventCategorySummary', { name: category.name })
                      )}
                  >
                    {$t('navigatorConfig.delete')}
                  </button>
                </td>
              {/if}
            </tr>
          {:else}
            <tr>
              <td colspan={canManage ? 5 : 4} class="muted">{$t('navigatorConfig.noEventCategories')}</td>
            </tr>
          {/each}
        </tbody>
      </table>
    </div>

    {#if canManage}
      <form
        class="edit-grid"
        onsubmit={(event) => {
          event.preventDefault();
          ask(
            '/api/v1/operations/navigator/event-categories',
            eventCategoryForm,
            $t('navigatorConfig.addEventCategory'),
            $t('navigatorConfig.addEventCategorySummary', { name: eventCategoryForm.name })
          );
        }}
      >
        <label>
          {$t('navigatorConfig.colName')}
          <input bind:value={eventCategoryForm.name} />
        </label>
        <label class="check">
          <input type="checkbox" bind:checked={eventCategoryForm.visible} />
          {$t('navigatorConfig.visible')}
        </label>
        <button type="submit" disabled={!eventCategoryForm.name.trim()}>
          {$t('navigatorConfig.addEventCategory')}
        </button>
      </form>
    {/if}
  </section>
  {/if}
{/if}

{#if editing}
  <!-- One drawer for all four editors: `editing` was already a single {kind, id, draft},
       so what used to unfold inside four different lists is one panel that switches on kind. -->
  <Drawer
    title={editorTitle(editing.kind, $t)}
    eyebrow={$t('navigatorConfig.title')}
    onclose={() => (editing = null)}
  >
    {#if editing.kind === 'context'}
            <form
              class="edit-grid"
              onsubmit={(event) => {
                event.preventDefault();
                ask(
                  '/api/v1/operations/navigator/contexts/update',
                  { contextId: editing.id, ...editing.draft },
                  $t('navigatorConfig.updateTab'),
                  $t('navigatorConfig.updateTabSummary', { code: editing.draft.searchCode })
                );
              }}
            >
              <label>
                {$t('navigatorConfig.searchCode')}
                <input bind:value={editing.draft.searchCode} required />
              </label>
              <label>
                {$t('navigatorConfig.queryType')}
                <select bind:value={editing.draft.queryType}>
                  {#each queryTypes as q}
                    <option value={q.value}>{q.label}</option>
                  {/each}
                </select>
              </label>
              <label>
                {$t('navigatorConfig.order')}
                <input type="number" bind:value={editing.draft.orderNum} />
              </label>
              <label class="check">
                <input type="checkbox" bind:checked={editing.draft.visible} />
                {$t('navigatorConfig.visible')}
              </label>
              <button type="submit">{$t('navigatorConfig.save')}</button>
              <button type="button" class="ghost-button" onclick={() => (editing = null)}>
                {$t('navigatorConfig.cancel')}
              </button>
            </form>
    {:else if editing.kind === 'quickLink'}
                    <tr>
                      <td colspan={canManage ? 6 : 5}>
                        <form
                          class="edit-grid"
                          onsubmit={(event) => {
                            event.preventDefault();
                            ask(
                              '/api/v1/operations/navigator/quick-links/update',
                              {
                                quickLinkId: editing.id,
                                contextId: editing.parentId,
                                searchCode: editing.draft.searchCode,
                                filter: editing.draft.filter || '',
                                localization: editing.draft.localization || '',
                                queryType: editing.draft.queryType,
                                orderNum: editing.draft.orderNum,
                              },
                              $t('navigatorConfig.updateBlock'),
                              $t('navigatorConfig.updateBlockSummary', { code: editing.draft.searchCode })
                            );
                          }}
                        >
                          <label>
                            {$t('navigatorConfig.searchCode')}
                            <input bind:value={editing.draft.searchCode} required />
                          </label>
                          <label>
                            {$t('navigatorConfig.queryType')}
                            <select bind:value={editing.draft.queryType}>
                              {#each queryTypes as q}
                                <option value={q.value}>{q.label}</option>
                              {/each}
                            </select>
                          </label>
                          <label>
                            {$t('navigatorConfig.colFilter')}
                            <input bind:value={editing.draft.filter} />
                          </label>
                          <label>
                            {$t('navigatorConfig.colLocalization')}
                            <input bind:value={editing.draft.localization} />
                          </label>
                          <label>
                            {$t('navigatorConfig.order')}
                            <input type="number" bind:value={editing.draft.orderNum} />
                          </label>
                          <button type="submit">{$t('navigatorConfig.save')}</button>
                          <button type="button" class="ghost-button" onclick={() => (editing = null)}>
                            {$t('navigatorConfig.cancel')}
                          </button>
                        </form>
                      </td>
                    </tr>
    {:else if editing.kind === 'category'}
                <tr>
                  <td colspan={canManage ? 8 : 7}>
                    <form
                      class="edit-grid"
                      onsubmit={(event) => {
                        event.preventDefault();
                        ask(
                          '/api/v1/operations/navigator/categories/update',
                          { categoryId: editing.id, ...editing.draft },
                          $t('navigatorConfig.updateCategory'),
                          $t('navigatorConfig.updateCategorySummary', { name: editing.draft.name })
                        );
                      }}
                    >
                      <label>
                        {$t('navigatorConfig.colName')}
                        <input bind:value={editing.draft.name} required />
                      </label>
                      <label>
                        {$t('navigatorConfig.colMinRank')}
                        <input type="number" bind:value={editing.draft.minRank} />
                      </label>
                      <label>
                        {$t('navigatorConfig.order')}
                        <input type="number" bind:value={editing.draft.orderNum} />
                      </label>
                      <label class="check">
                        <input type="checkbox" bind:checked={editing.draft.visible} />
                        {$t('navigatorConfig.visible')}
                      </label>
                      <label class="check">
                        <input type="checkbox" bind:checked={editing.draft.staffOnly} />
                        {$t('navigatorConfig.colStaffOnly')}
                      </label>
                      <button type="submit">{$t('navigatorConfig.save')}</button>
                      <button type="button" class="ghost-button" onclick={() => (editing = null)}>
                        {$t('navigatorConfig.cancel')}
                      </button>
                    </form>
                  </td>
                </tr>
    {:else if editing.kind === 'eventCategory'}
                <tr>
                  <td colspan={canManage ? 5 : 4}>
                    <form
                      class="edit-grid"
                      onsubmit={(event) => {
                        event.preventDefault();
                        ask(
                          '/api/v1/operations/navigator/event-categories/update',
                          {
                            categoryId: editing.id,
                            name: editing.draft.name,
                            visible: editing.draft.visible,
                          },
                          $t('navigatorConfig.updateEventCategory'),
                          $t('navigatorConfig.updateEventCategorySummary', { name: editing.draft.name })
                        );
                      }}
                    >
                      <label>
                        {$t('navigatorConfig.colName')}
                        <input bind:value={editing.draft.name} required />
                      </label>
                      <label class="check">
                        <input type="checkbox" bind:checked={editing.draft.visible} />
                        {$t('navigatorConfig.visible')}
                      </label>
                      <button type="submit">{$t('navigatorConfig.save')}</button>
                      <button type="button" class="ghost-button" onclick={() => (editing = null)}>
                        {$t('navigatorConfig.cancel')}
                      </button>
                    </form>
                  </td>
                </tr>
    {/if}

    {#snippet actions()}
      <button type="button" class="ghost-button" onclick={() => (editing = null)}>{$t('common.cancel')}</button>
    {/snippet}
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
  .tab-card {
    border: 1px solid var(--border, rgba(255, 255, 255, 0.08));
    border-radius: 10px;
    padding: 12px;
    margin-bottom: 12px;
  }

  .tab-card header {
    display: flex;
    align-items: center;
    justify-content: space-between;
    gap: 12px;
    flex-wrap: wrap;
    margin-bottom: 8px;
  }

  .tab-card header small {
    display: block;
  }

  .row-actions {
    display: inline-flex;
    gap: 6px;
    flex-wrap: wrap;
  }

  .edit-grid {
    display: flex;
    flex-wrap: wrap;
    align-items: flex-end;
    gap: 10px;
    margin-top: 10px;
  }

  .edit-grid label {
    display: flex;
    flex-direction: column;
    gap: 4px;
    font-size: 0.8rem;
  }

  .edit-grid label.check {
    flex-direction: row;
    align-items: center;
    gap: 6px;
  }

  .chip {
    margin-right: 6px;
  }

  .warn-panel {
    border-left: 3px solid var(--warn, #e0a33e);
  }
</style>
