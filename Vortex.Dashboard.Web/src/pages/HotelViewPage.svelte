<script lang="ts">
  /**
   * The hotel view -- the first screen a player sees -- configured as a screen instead of as
   * seventy flat client keys.
   *
   * What makes this page worth existing is not that the keys become editable; they already were, on
   * the gamedata page. It is that nothing in `external_variables.json` says what they mean. The
   * client reads a closed list of widget types, a closed list of element types, nine background
   * layers named in its own source and four motions whose fields are positional and differ per
   * motion -- and it accepts a mistake in any of them in complete silence, rendering a blank slot or
   * abandoning a whole content column with nothing in a log. Those lists arrive with the read, from
   * the server that validates the save against them, so a dropdown here cannot drift from what the
   * client will take.
   *
   * One save for the whole configuration, because removing something has to be expressible: a
   * deleted campaign is three keys that must stop existing, and a patch of changed values cannot say
   * that. The server writes the keys this configuration implies and deletes every other
   * `landing.view.*` key, which is the same statement made once.
   */
  import { apiGet } from '../lib/api';
  import { createResource } from '../lib/resource';
  import { createWriteOps } from '../lib/writeOps';
  import { hasDashboardCapability } from '../lib/permissions';
  import { CAPABILITIES } from '../lib/dashboardPermissions';
  import { identity } from '../lib/session';
  import { t } from '../lib/i18n';
  import {
    blankObject,
    describeCampaign,
    describeSlot,
    freeObjectIndex,
    isScheduled,
    labeller,
    objectAssetUrl,
    refitObject,
    resolveAssetUrl,
    scheduledCodes,
  } from '../lib/hotelView';
  import type {
    HotelViewCampaign,
    HotelViewConfig,
    HotelViewObject,
    HotelViewScene,
    HotelViewSlot,
  } from '../lib/apiTypes';
  import type { FieldChange } from '../lib/changes';

  import AccessDeniedNotice from '../components/AccessDeniedNotice.svelte';
  import AssetImage from '../components/AssetImage.svelte';
  import Button from '../components/Button.svelte';
  import Chip from '../components/Chip.svelte';
  import ConfirmReasonModal from '../components/ConfirmReasonModal.svelte';
  import Drawer from '../components/Drawer.svelte';
  import EmptyState from '../components/EmptyState.svelte';
  import OpResult from '../components/OpResult.svelte';
  import PageHeader from '../components/PageHeader.svelte';
  import Switch from '../components/Switch.svelte';
  import Tabs from '../components/Tabs.svelte';
  import ElementRows from '../components/hotelView/ElementRows.svelte';
  import LayoutRows from '../components/hotelView/LayoutRows.svelte';
  import ScheduleRows from '../components/hotelView/ScheduleRows.svelte';

  import { Home, Image, LayoutGrid, Megaphone, Plus, Settings, Trash2 } from '@lucide/svelte';

  // Written as a constant because `${...}` in markup is Svelte's own interpolation: typed inline it
  // becomes a reference to an `image` that does not exist, which is a build the page fails, not a
  // placeholder.
  const URI_PLACEHOLDER = '${image.library.url}reception/…';

  // The common settings that produce one audit line each, listed rather than walked, so the label
  // key for every one of them is a real key and not whatever the contract happens to be called.
  const COMMON_KEYS = [
    'textColor',
    'etchingColor',
    'etchingPosition',
    'leftPaneWidth',
    'rightPaneWidth',
    'layoutXml',
    'roomCategory',
    'rightPaneDimmerHidden',
    'sceneSchedule',
  ] as const;

  let canManage = $derived(hasDashboardCapability($identity, CAPABILITIES.opsGamedataManage));
  let label = $derived(labeller($t));

  const config = createResource(
    () => ['hotelview'],
    () => apiGet<HotelViewConfig>('/api/v1/hotelview'),
  );

  // `baseline` is the file as it was read and is what the save is guarded against; `draft` is the
  // form. Keeping both is what lets the page show an operator what they changed, and lets a reload
  // after a save replace the form rather than merge into it.
  let baseline = $state<HotelViewConfig | null>(null);
  let draft = $state<HotelViewConfig | null>(null);

  $effect(() => {
    const data = config.data;
    if (!data) return;

    if (
      baseline &&
      baseline.modifiedUtc === data.modifiedUtc &&
      baseline.textsModifiedUtc === data.textsModifiedUtc
    ) {
      return;
    }

    baseline = structuredClone(data);
    draft = structuredClone(data);
  });

  let dirty = $derived(
    Boolean(draft) && JSON.stringify(draft) !== JSON.stringify(baseline),
  );

  let vocabulary = $derived(draft?.vocabulary ?? config.data?.vocabulary ?? null);
  let placeholders = $derived(draft?.placeholders ?? {});
  let codes = $derived(
    [...new Set([...(draft?.campaigns ?? []).map((c) => c.code), ...scheduledCodes(draft?.slots ?? [])])].sort(),
  );

  // The two column widths, falling back to the client's own defaults when the hotel has not set
  // them -- an empty key means "unset", and the client then uses 500 and 250.
  let paneWidths = $derived([
    Number(draft?.common.leftPaneWidth) || 500,
    Number(draft?.common.rightPaneWidth) || 250,
  ]);

  // Placement comes from the client's slot table rather than from the order of the boxes, so a slot
  // that moves column in a future client moves here without anyone editing a nth-child rule.
  const columnOf = (column: string | undefined): string =>
    column === 'left' ? '1' : column === 'right' ? '2' : '1 / -1';

  const TABS = [
    { id: 'layout', icon: LayoutGrid },
    { id: 'campaigns', icon: Megaphone },
    { id: 'scenery', icon: Image },
    { id: 'settings', icon: Settings },
  ];

  let active = $state('layout');

  const ops = createWriteOps(() => config.refresh());

  // --- editing -------------------------------------------------------------------------------

  /**
   * Which slots reach a campaign.
   *
   * Computed from the draft rather than read off the server's `usedBy`, which describes the file as
   * it was loaded and goes stale the moment a schedule row is edited -- the one moment an operator
   * is looking at it.
   */
  const scheduledSlots = (code: string): number[] =>
    (draft?.slots ?? [])
      .filter((row) => row.schedule.some((entry) => entry.code === code))
      .map((row) => row.number);

  // Every mutation goes through one of these rather than assigning to `draft` from the markup. The
  // form only renders once `draft` exists, but a handler written inline is a closure the checker
  // cannot see that through -- and writing the guard once here is shorter than proving it nine times.
  function patchCommon(patch: Partial<HotelViewConfig['common']>) {
    if (!draft) return;
    draft.common = { ...draft.common, ...patch };
  }

  function removeScene(code: string) {
    if (!draft) return;
    draft.scenes = draft.scenes.filter((scene) => scene.code !== code);
  }

  function patchExtra(key: string, value: string) {
    if (!draft) return;
    draft.extras = draft.extras.map((row) => (row.key === key ? { ...row, value } : row));
  }

  function removeExtra(key: string) {
    if (!draft) return;
    draft.extras = draft.extras.filter((row) => row.key !== key);
  }

  function removeCampaign(code: string) {
    if (!draft) return;
    draft.campaigns = draft.campaigns.filter((c) => c.code !== code);
    editingCampaign = null;
  }

  function patchSlot(number: number, patch: Partial<HotelViewSlot>) {
    if (!draft) return;
    draft.slots = draft.slots.map((slot) => (slot.number === number ? { ...slot, ...patch } : slot));
  }

  function patchCampaign(code: string, patch: Partial<HotelViewCampaign>) {
    if (!draft) return;
    draft.campaigns = draft.campaigns.map((campaign) =>
      campaign.code === code ? { ...campaign, ...patch } : campaign,
    );
  }

  function patchScene(code: string, patch: Partial<HotelViewScene>) {
    if (!draft) return;
    draft.scenes = draft.scenes.map((scene) => (scene.code === code ? { ...scene, ...patch } : scene));
  }

  /** A campaign's code is three key names; renaming it is a delete and a create. */
  function renameCampaign(from: string, to: string) {
    if (!draft) return;
    draft.campaigns = draft.campaigns.map((campaign) =>
      campaign.code === from ? { ...campaign, code: to } : campaign,
    );
    editingCampaign = to;
  }

  function addCampaign() {
    if (!draft) return;
    const code = newCode.trim();
    if (code === '' || draft.campaigns.some((campaign) => campaign.code === code)) return;

    draft.campaigns = [
      ...draft.campaigns,
      { code, widget: 'generic', elements: [], layout: [], usedBy: [] },
    ];
    newCode = '';
    editingCampaign = code;
  }

  function addScene() {
    if (!draft || !vocabulary) return;
    const code = newScene.trim();
    if (code === '' || draft.scenes.some((scene) => scene.code === code)) return;

    draft.scenes = [
      ...draft.scenes,
      {
        code,
        layers: vocabulary.backgroundLayers.map((layer) => ({ layer, uri: '', visible: true })),
        objects: [],
      },
    ];
    newScene = '';
  }

  function addObject(scene: HotelViewScene) {
    if (!vocabulary) return;
    const index = freeObjectIndex(scene.objects, vocabulary.maxObjects);
    if (index === 0) return;

    patchScene(scene.code, {
      objects: [...scene.objects, blankObject(index, vocabulary.motionTypes[0].motion, vocabulary)],
    });
  }

  function patchObject(scene: HotelViewScene, index: number, next: HotelViewObject) {
    patchScene(scene.code, {
      objects: scene.objects.map((item) => (item.index === index ? next : item)),
    });
  }

  function setObjectField(scene: HotelViewScene, item: HotelViewObject, position: number, value: string) {
    const fields = [...item.fields];
    while (fields.length <= position) fields.push('');
    fields[position] = value;
    patchObject(scene, item.index, { ...item, fields });
  }

  // --- saving --------------------------------------------------------------------------------

  /**
   * What changed, per slot, per campaign, per scene.
   *
   * Coarse on purpose: the audit line is read by a person, and "slot 4: widgetcontainer · 3 →
   * widgetcontainer · 4" says what happened where "conf: 2026-01-09 12:00,jan26r2;… → …" does not.
   */
  let changes = $derived.by<FieldChange[]>(() => {
    if (!draft || !baseline) return [];
    const out: FieldChange[] = [];
    const empty = $t('hotelView.emptySlot');

    for (const key of COMMON_KEYS) {
      const before = baseline.common[key];
      const after = draft.common[key];
      if (JSON.stringify(before) !== JSON.stringify(after)) {
        out.push({
          label: label(`hotelView.common.${key}`, key),
          from: String(before ?? ''),
          to: String(after ?? ''),
        });
      }
    }

    for (const slot of draft.slots) {
      const was = baseline.slots.find((s) => s.number === slot.number);
      if (JSON.stringify(was) === JSON.stringify(slot)) continue;
      out.push({
        label: $t('hotelView.slotN', { n: slot.number }),
        from: describeSlot(was, empty),
        to: describeSlot(slot, empty),
      });
    }

    for (const campaign of draft.campaigns) {
      const was = baseline.campaigns.find((c) => c.code === campaign.code);
      if (JSON.stringify(was) === JSON.stringify(campaign)) continue;
      out.push({
        label: `${$t('hotelView.campaign')} ${campaign.code}`,
        from: was ? describeCampaign(was) : $t('hotelView.created'),
        to: describeCampaign(campaign),
      });
    }

    for (const was of baseline.campaigns) {
      if (!draft.campaigns.some((c) => c.code === was.code)) {
        out.push({
          label: `${$t('hotelView.campaign')} ${was.code}`,
          from: describeCampaign(was),
          to: $t('hotelView.deleted'),
        });
      }
    }

    for (const scene of draft.scenes) {
      const was = baseline.scenes.find((s) => s.code === scene.code);
      if (JSON.stringify(was) === JSON.stringify(scene)) continue;
      out.push({
        label: `${$t('hotelView.scene')} ${scene.code || $t('hotelView.defaultScene')}`,
        from: was ? `${was.layers.filter((l) => l.uri).length} · ${was.objects.length}` : $t('hotelView.created'),
        to: `${scene.layers.filter((l) => l.uri).length} · ${scene.objects.length}`,
      });
    }

    if (JSON.stringify(draft.extras) !== JSON.stringify(baseline.extras)) {
      out.push({
        label: $t('hotelView.tab_settings'),
        from: String(baseline.extras.length),
        to: String(draft.extras.length),
      });
    }

    return out;
  });

  function save() {
    if (!draft || !baseline) return;

    ops.ask(
      '/api/v1/operations/hotelview',
      {
        expectedModifiedUtc: baseline.modifiedUtc,
        expectedTextsModifiedUtc: baseline.textsModifiedUtc,
        common: draft.common,
        slots: draft.slots,
        campaigns: draft.campaigns,
        scenes: draft.scenes,
        extras: draft.extras,
      },
      $t('hotelView.save'),
      $t('hotelView.saveSummary'),
      { changes, valid: dirty },
    );
  }

  // --- drawers -------------------------------------------------------------------------------

  let editingSlot = $state<number | null>(null);
  let editingCampaign = $state<string | null>(null);
  let newCode = $state('');
  let newScene = $state('');

  let slot = $derived(draft?.slots.find((s) => s.number === editingSlot) ?? null);
  let campaign = $derived(draft?.campaigns.find((c) => c.code === editingCampaign) ?? null);
  let shapeOfSlot = $derived(vocabulary?.slotShapes.find((s) => s.number === editingSlot) ?? null);
</script>

{#if !canManage}
  <AccessDeniedNotice />
{:else}
  <PageHeader title={$t('hotelView.title')} description={$t('hotelView.subtitle')}>
    {#snippet icon()}<Home size={20} />{/snippet}
    {#snippet actions()}
      {#if dirty}<Chip label={$t('hotelView.unsaved', { count: changes.length })} tone="warning" />{/if}
      <Button disabled={!dirty || $ops.busy} onclick={save}>{$t('hotelView.save')}</Button>
      <Button variant="ghost" disabled={!dirty} onclick={() => (draft = structuredClone(baseline))}>
        {$t('hotelView.discard')}
      </Button>
    {/snippet}
  </PageHeader>

  {#if $ops.results['']}<OpResult result={$ops.results['']} />{/if}

  {#if config.loading && !draft}
    <EmptyState kind="loading" message={$t('common.loading')} />
  {:else if !draft || !vocabulary}
    <EmptyState kind="error" message={$t('hotelView.unreadable')} />
  {:else if !draft.available}
    <EmptyState kind="error" message={$t('hotelView.noAssetRoot')} />
  {:else}
    <Tabs
      tabs={TABS.map((tab) => ({ ...tab, label: $t(`hotelView.tab_${tab.id}`) }))}
      bind:active
      storageKey="hotelview-tab"
    />

    {#if active === 'layout'}
      <p class="hint">{$t('hotelView.layoutHint')}</p>

      <div
        class="grid"
        style:grid-template-columns="{paneWidths[0]}fr {paneWidths[1]}fr"
      >
        {#each draft.slots as row (row.number)}
          {@const shape = vocabulary.slotShapes.find((s) => s.number === row.number)}
          <button
            type="button"
            class="cell"
            class:filled={row.widget !== ''}
            style:grid-column={columnOf(shape?.column)}
            onclick={() => (editingSlot = row.number)}
          >
            <span class="cell-number">{row.number}</span>
            <span class="cell-widget">{row.widget || $t('hotelView.emptySlot')}</span>
            <span class="cell-detail">
              {#if isScheduled(row.widget)}
                {$t('hotelView.scheduledCount', { count: row.schedule.length })}
              {:else if row.elements.length > 0}
                {$t('hotelView.elementCount', { count: row.elements.length })}
              {/if}
            </span>
            {#if row.separator}<span class="cell-flag">{$t('hotelView.separator')}</span>{/if}
          </button>
        {/each}
      </div>
    {:else if active === 'campaigns'}
      <p class="hint">{$t('hotelView.campaignsHint')}</p>

      <div class="new">
        <input
          type="text"
          bind:value={newCode}
          placeholder={$t('hotelView.newCodePlaceholder')}
          aria-label={$t('hotelView.campaignCode')}
        />
        <Button variant="ghost" size="sm" onclick={addCampaign}>
          <Plus size={14} />{$t('hotelView.addCampaign')}
        </Button>
      </div>

      {#if draft.campaigns.length === 0}
        <EmptyState message={$t('hotelView.noCampaigns')} />
      {:else}
        <div class="cards">
          {#each draft.campaigns as item (item.code)}
            {@const reachedBy = scheduledSlots(item.code)}
            <button type="button" class="card" onclick={() => (editingCampaign = item.code)}>
              <AssetImage
                src={resolveAssetUrl(
                  item.layout.find((value) => value.key === 'bitmap.uri')?.value ?? '',
                  placeholders,
                )}
                alt={item.code}
                size={44}
              />
              <span class="card-body">
                <strong>{item.code}</strong>
                <span class="card-meta">{describeCampaign(item)}</span>
              </span>
              {#if reachedBy.length === 0}
                <Chip label={$t('hotelView.unscheduled')} tone="warning" />
              {:else}
                <Chip label={$t('hotelView.usedBy', { slots: reachedBy.join(', ') })} tone="accent" />
              {/if}
            </button>
          {/each}
        </div>
      {/if}
    {:else if active === 'scenery'}
      <p class="hint">{$t('hotelView.sceneryHint')}</p>

      <section class="hv-section">
        <h3>{$t('hotelView.sceneSchedule')}</h3>
        <p class="help">{$t('hotelView.sceneScheduleHelp')}</p>
        <ScheduleRows
          schedule={draft.common.sceneSchedule}
          codes={draft.scenes.map((scene) => scene.code).filter(Boolean)}
          onchange={(schedule) => patchCommon({ sceneSchedule: schedule })}
        />
      </section>

      <div class="new">
        <input
          type="text"
          bind:value={newScene}
          placeholder={$t('hotelView.newScenePlaceholder')}
          aria-label={$t('hotelView.sceneCode')}
        />
        <Button variant="ghost" size="sm" onclick={addScene}>
          <Plus size={14} />{$t('hotelView.addScene')}
        </Button>
      </div>

      {#each draft.scenes as scene (scene.code)}
        <section class="hv-section">
          <h3>
            {scene.code || $t('hotelView.defaultScene')}
            {#if scene.code}
              <Button
                variant="danger"
                size="sm"
                ariaLabel={$t('common.delete')}
                onclick={() => removeScene(scene.code)}
                ><Trash2 size={14} /></Button
              >
            {/if}
          </h3>
          {#if scene.code}<p class="help">{$t('hotelView.seasonalSceneHelp')}</p>{/if}

          <h4>{$t('hotelView.layers')}</h4>
          <div class="layers">
            {#each scene.layers as layer (layer.layer)}
              <div class="layer">
                <AssetImage src={resolveAssetUrl(layer.uri, placeholders)} alt={layer.layer} size={48} />
                <div class="layer-body">
                  <strong>{layer.layer}</strong>
                  <p class="help">{label(`hotelView.layer.${layer.layer}`)}</p>
                  <input
                    type="text"
                    value={layer.uri}
                    placeholder={URI_PLACEHOLDER}
                    aria-label={layer.layer}
                    oninput={(e) =>
                      patchScene(scene.code, {
                        layers: scene.layers.map((l) =>
                          l.layer === layer.layer ? { ...l, uri: e.currentTarget.value } : l,
                        ),
                      })}
                  />
                </div>
                <Switch
                  checked={layer.visible}
                  ariaLabel={$t('hotelView.visible')}
                  onchange={(checked) =>
                    patchScene(scene.code, {
                      layers: scene.layers.map((l) =>
                        l.layer === layer.layer ? { ...l, visible: checked } : l,
                      ),
                    })}
                />
              </div>
            {/each}
          </div>

          <h4>{$t('hotelView.objects')}</h4>
          <p class="help">{$t('hotelView.objectsHelp')}</p>
          {#each scene.objects as item, position (item.index)}
            {@const motion = vocabulary.motionTypes.find((m) => m.motion === item.motion)}
            <!-- Twenty snowflakes are twenty identical rows; explaining the motion under each of
                 them is twenty copies of one sentence. It appears where the motion changes. -->
            {@const explains = scene.objects[position - 1]?.motion !== item.motion}
            <div class="object">
              <AssetImage
                src={objectAssetUrl(item, vocabulary, placeholders)}
                alt={item.asset}
                size={40}
              />
              <span class="object-index">#{item.index}</span>

              <label class="field">
                <span>{$t('hotelView.asset')}</span>
                <input
                  type="text"
                  value={item.asset}
                  oninput={(e) => patchObject(scene, item.index, { ...item, asset: e.currentTarget.value })}
                />
              </label>

              <label class="field">
                <span>{$t('hotelView.motion')}</span>
                <select
                  value={item.motion}
                  onchange={(e) =>
                    patchObject(scene, item.index, refitObject(item, e.currentTarget.value, vocabulary))}
                >
                  {#if !motion}<option value={item.motion}>{item.motion}</option>{/if}
                  {#each vocabulary.motionTypes as type (type.motion)}
                    <option value={type.motion}>{type.motion}</option>
                  {/each}
                </select>
              </label>

              {#each motion?.fields ?? [] as field, position (field.name)}
                <label class="field">
                  <span>{label(`hotelView.field.${field.name}`, field.name)}</span>
                  <input
                    type={field.kind === 'string' ? 'text' : 'number'}
                    value={item.fields[position] ?? ''}
                    oninput={(e) => setObjectField(scene, item, position, e.currentTarget.value)}
                  />
                </label>
              {/each}

              <Button
                variant="danger"
                size="sm"
                ariaLabel={$t('common.delete')}
                onclick={() =>
                  patchScene(scene.code, {
                    objects: scene.objects.filter((o) => o.index !== item.index),
                  })}><Trash2 size={14} /></Button
              >

              {#if explains}
                <p class="object-help">{label(`hotelView.motionHelp.${item.motion}`)}</p>
              {/if}
            </div>
          {/each}

          <Button
            variant="ghost"
            size="sm"
            disabled={scene.objects.length >= vocabulary.maxObjects}
            onclick={() => addObject(scene)}
          >
            <Plus size={14} />{$t('hotelView.addObject')}
          </Button>
        </section>
      {/each}
    {:else}
      <section class="hv-section">
        <h3>{$t('hotelView.commonSettings')}</h3>
        <div class="settings">
          <label class="field">
            <span>{$t('hotelView.common.textColor')}</span>
            <input type="text" bind:value={draft.common.textColor} placeholder="ffffff" />
            <p class="help">{$t('hotelView.common.colourHelp')}</p>
          </label>
          <label class="field">
            <span>{$t('hotelView.common.etchingColor')}</span>
            <input type="text" bind:value={draft.common.etchingColor} placeholder="000000" />
          </label>
          <label class="field">
            <span>{$t('hotelView.common.etchingPosition')}</span>
            <select bind:value={draft.common.etchingPosition}>
              <option value="">{$t('hotelView.unset')}</option>
              <option value="top">top</option>
              <option value="bottom">bottom</option>
            </select>
          </label>
          <label class="field">
            <span>{$t('hotelView.common.leftPaneWidth')}</span>
            <input type="number" bind:value={draft.common.leftPaneWidth} placeholder="500" />
          </label>
          <label class="field">
            <span>{$t('hotelView.common.rightPaneWidth')}</span>
            <input type="number" bind:value={draft.common.rightPaneWidth} placeholder="250" />
          </label>
          <label class="field">
            <span>{$t('hotelView.common.layoutXml')}</span>
            <input
              type="text"
              bind:value={draft.common.layoutXml}
              placeholder="landing_view_default_dynamic_layout"
            />
            <p class="help">{$t('hotelView.common.layoutXmlHelp')}</p>
          </label>
          <label class="field">
            <span>{$t('hotelView.common.roomCategory')}</span>
            <input type="text" bind:value={draft.common.roomCategory} />
          </label>
          <div class="field">
            <span>{$t('hotelView.common.rightPaneDimmerHidden')}</span>
            <Switch
              checked={draft.common.rightPaneDimmerHidden}
              ariaLabel={$t('hotelView.common.rightPaneDimmerHidden')}
              onchange={(checked) => patchCommon({ rightPaneDimmerHidden: checked })}
            />
          </div>
        </div>
      </section>

      <section class="hv-section">
        <h3>{$t('hotelView.otherKeys')}</h3>
        <p class="help">{$t('hotelView.otherKeysHelp')}</p>
        <table>
          <thead>
            <tr>
              <th>{$t('gamedata.key')}</th>
              <th>{$t('gamedata.value')}</th>
              <th></th>
            </tr>
          </thead>
          <tbody>
            {#each draft.extras as extra (extra.key)}
              <tr class:dead={!extra.read}>
                <td>
                  <code>{extra.key}</code>
                  {#if !extra.read}<Chip label={$t('hotelView.neverRead')} tone="danger" />{/if}
                </td>
                <td>
                  <input
                    type="text"
                    value={extra.value}
                    aria-label={extra.key}
                    oninput={(e) => patchExtra(extra.key, e.currentTarget.value)}
                  />
                </td>
                <td>
                  <Button
                    variant="danger"
                    size="sm"
                    ariaLabel={$t('common.delete')}
                    onclick={() => removeExtra(extra.key)}
                    ><Trash2 size={14} /></Button
                  >
                </td>
              </tr>
            {/each}
          </tbody>
        </table>
      </section>
    {/if}
  {/if}

  {#if slot && vocabulary}
    <Drawer
      title={$t('hotelView.slotN', { n: slot.number })}
      eyebrow={label(`hotelView.column.${shapeOfSlot?.column ?? 'top'}`)}
      width={640}
      onclose={() => (editingSlot = null)}
    >
      <label class="field">
        <span>{$t('hotelView.widget')}</span>
        <select
          value={slot.widget}
          onchange={(e) => patchSlot(slot.number, { widget: e.currentTarget.value })}
        >
          <option value="">{$t('hotelView.emptySlot')}</option>
          {#if slot.widget && !vocabulary.widgetTypes.includes(slot.widget)}
            <option value={slot.widget}>{slot.widget}</option>
          {/if}
          {#each vocabulary.widgetTypes as type (type)}
            <option value={type}>{type}</option>
          {/each}
        </select>
        <p class="help">{label(`hotelView.widgetHelp.${slot.widget}`, $t('hotelView.pickWidget'))}</p>
      </label>

      {#if isScheduled(slot.widget)}
        <h4>{$t('hotelView.schedule')}</h4>
        <p class="help">{$t('hotelView.scheduleHelp')}</p>
        <ScheduleRows
          schedule={slot.schedule}
          {codes}
          onchange={(schedule) => patchSlot(slot.number, { schedule })}
        />
      {:else if slot.widget === 'generic'}
        <h4>{$t('hotelView.elements')}</h4>
        <ElementRows
          elements={slot.elements}
          {vocabulary}
          onchange={(elements) => patchSlot(slot.number, { elements })}
        />
        <h4>{$t('hotelView.layout')}</h4>
        <LayoutRows
          layout={slot.layout}
          {vocabulary}
          {placeholders}
          onchange={(layout) => patchSlot(slot.number, { layout })}
        />
      {/if}

      {#if shapeOfSlot?.canSeparate}
        <h4>{$t('hotelView.separator')}</h4>
        <p class="help">{$t('hotelView.separatorHelp')}</p>
        <Switch
          checked={slot.separator}
          label={$t('hotelView.separator')}
          onchange={(checked) => patchSlot(slot.number, { separator: checked })}
        />
        {#if slot.separator}
          <label class="field">
            <span>{$t('hotelView.separatorTitle')}</span>
            <input
              type="text"
              value={slot.separatorTitle}
              oninput={(e) => patchSlot(slot.number, { separatorTitle: e.currentTarget.value })}
            />
          </label>
        {/if}
      {/if}

      {#if shapeOfSlot?.canIgnore}
        <h4>{$t('hotelView.ignore')}</h4>
        <p class="help">{$t('hotelView.ignoreHelp')}</p>
        <Switch
          checked={slot.ignore}
          label={$t('hotelView.ignore')}
          onchange={(checked) => patchSlot(slot.number, { ignore: checked })}
        />
      {/if}

      {#snippet actions()}
        <Button variant="ghost" onclick={() => (editingSlot = null)}>{$t('common.close')}</Button>
      {/snippet}
    </Drawer>
  {/if}

  {#if campaign && vocabulary}
    <Drawer
      title={campaign.code}
      eyebrow={$t('hotelView.campaign')}
      width={720}
      onclose={() => (editingCampaign = null)}
    >
      <label class="field">
        <span>{$t('hotelView.campaignCode')}</span>
        <input
          type="text"
          value={campaign.code}
          oninput={(e) => renameCampaign(campaign.code, e.currentTarget.value.trim())}
        />
        <p class="help">{$t('hotelView.campaignCodeHelp')}</p>
      </label>

      <label class="field">
        <span>{$t('hotelView.widget')}</span>
        <select
          value={campaign.widget}
          onchange={(e) => patchCampaign(campaign.code, { widget: e.currentTarget.value })}
        >
          {#each vocabulary.widgetTypes as type (type)}
            <option value={type}>{type}</option>
          {/each}
        </select>
        <p class="help">{label(`hotelView.widgetHelp.${campaign.widget}`)}</p>
      </label>

      <h4>{$t('hotelView.elements')}</h4>
      <ElementRows
        elements={campaign.elements}
        {vocabulary}
        onchange={(elements) => patchCampaign(campaign.code, { elements })}
      />

      <h4>{$t('hotelView.layout')}</h4>
      <LayoutRows
        layout={campaign.layout}
        {vocabulary}
        {placeholders}
        onchange={(layout) => patchCampaign(campaign.code, { layout })}
      />

      {#snippet actions()}
        <Button
          variant="danger"
          onclick={() => removeCampaign(campaign.code)}>{$t('common.delete')}</Button
        >
        <Button variant="ghost" onclick={() => (editingCampaign = null)}>{$t('common.close')}</Button>
      {/snippet}
    </Drawer>
  {/if}

  <ConfirmReasonModal
    open={Boolean($ops.pending)}
    title={$ops.pending?.title ?? ''}
    summary={$ops.pending?.summary ?? ''}
    changes={$ops.pending?.changes ?? []}
    danger={false}
    busy={$ops.busy}
    error={$ops.error}
    onconfirm={ops.confirm}
    oncancel={() => ops.cancel()}
  />
{/if}

<style>
  .hint,
  .help {
    margin: 0.25rem 0 0.75rem;
    font-size: 0.8rem;
    color: var(--muted);
  }

  /* The six slots at the proportions the client gives them, so the grid on screen is the grid a
     player sees rather than a legend to be decoded. */
  /* Columns and placement are both set inline from the client's own numbers -- see `paneWidths` and
     `columnOf`. Only the narrow-screen collapse lives here, and it has to beat those inline values,
     hence the `!important` that is otherwise never worth writing. */
  .grid {
    display: grid;
    gap: 0.6rem;
    max-width: 960px;
  }

  .cell {
    display: flex;
    flex-direction: column;
    gap: 0.15rem;
    align-items: flex-start;
    min-height: 78px;
    min-width: 0;
    /* A button does not stretch to its grid area the way a div does -- it sizes to its label. Without
       this the six slots render as six small boxes and the proportions the grid exists to show are
       not on screen at all. */
    width: 100%;
    padding: 0.6rem 0.7rem;
    /* An empty slot still has to read as a slot. A 1px dash in the border colour vanished against
       the navy, and an operator could not tell slot 6 existed. */
    border: 2px dashed var(--line-strong);
    border-radius: 10px;
    background: transparent;
    color: inherit;
    cursor: pointer;
    text-align: left;
  }

  .cell.filled {
    border: 2px solid var(--line);
    background: var(--surface-raised);
  }

  .cell:hover,
  .cell:focus-visible {
    border-color: var(--accent);
  }

  .cell-number {
    font-size: 0.7rem;
    opacity: 0.6;
  }

  .cell-widget {
    font-weight: 600;
  }

  .cell-detail,
  .cell-flag {
    font-size: 0.76rem;
    color: var(--muted);
  }

  /* Named `hv-section`, not `block`: `.block` is already a global class -- a bordered flex row with
     a notch, from the wired editor -- and Svelte's scoping does not stop a global rule from matching
     an element that also carries the scoped class. The whole tab laid itself out sideways. */
  .hv-section {
    margin: 1rem 0 1.5rem;
  }

  .hv-section h3 {
    display: flex;
    align-items: center;
    gap: 0.5rem;
    margin: 0 0 0.2rem;
  }

  /* Not scoped to a section: the drawers are written in this component too, and their headings sat
     flush against the control above them. */
  h4 {
    margin: 1.2rem 0 0.3rem;
  }

  .new {
    display: flex;
    gap: 0.5rem;
    align-items: center;
    margin-bottom: 0.75rem;
  }

  .cards {
    display: grid;
    grid-template-columns: repeat(auto-fill, minmax(260px, 1fr));
    gap: 0.6rem;
  }

  .card {
    display: flex;
    align-items: center;
    gap: 0.6rem;
    width: 100%;
    padding: 0.6rem;
    border: 1px solid var(--line);
    border-radius: 10px;
    background: transparent;
    color: inherit;
    cursor: pointer;
    text-align: left;
  }

  .card-body {
    display: flex;
    flex-direction: column;
    min-width: 0;
    flex: 1;
  }

  .card-meta {
    font-size: 0.76rem;
    color: var(--muted);
  }

  .layers {
    display: grid;
    grid-template-columns: repeat(auto-fill, minmax(380px, 1fr));
    gap: 0.6rem;
  }

  .layer {
    display: flex;
    gap: 0.6rem;
    align-items: flex-start;
    padding: 0.5rem;
    border: 1px solid var(--line);
    border-radius: 8px;
  }

  .layer-body {
    flex: 1;
    min-width: 0;
  }

  /* An input's default width is a character count, not its container's. Inside a flex or grid cell
     that leaves a box too narrow to read the URL it holds, which is the one thing it is for. */
  .layer-body input,
  .field input,
  .field select,
  .settings input,
  .settings select {
    width: 100%;
    min-width: 0;
  }

  .layer-body .help {
    margin: 0.1rem 0 0.3rem;
  }

  /* Flex-wrap rather than a grid: an object has four to seven fields depending on its motion, and
     `repeat(auto-fit, …)` cannot be mixed with the fixed tracks the preview and the delete button
     need -- the whole track list is then invalid and every field lands on its own row, which is
     twenty screens for twenty objects. */
  .object {
    display: flex;
    flex-wrap: wrap;
    gap: 0.4rem;
    align-items: flex-end;
    padding: 0.5rem;
    border: 1px solid var(--line);
    border-radius: 8px;
    margin-bottom: 0.5rem;
  }

  .object .field {
    flex: 1 1 90px;
  }

  .object .field:first-of-type {
    flex: 2 1 170px;
  }

  .object-index {
    font-size: 0.78rem;
    color: var(--muted);
    padding-bottom: 0.4rem;
  }

  .object-help {
    flex: 1 0 100%;
    margin: 0.2rem 0 0;
    font-size: 0.75rem;
    color: var(--muted);
  }

  .settings {
    display: grid;
    grid-template-columns: repeat(auto-fill, minmax(220px, 1fr));
    gap: 0.7rem;
  }

  .field {
    display: flex;
    flex-direction: column;
    gap: 0.2rem;
    min-width: 0;
  }

  .field > span {
    font-size: 0.74rem;
    color: var(--muted);
    text-transform: uppercase;
    letter-spacing: 0.03em;
  }

  table {
    width: 100%;
    border-collapse: collapse;
  }

  td,
  th {
    text-align: left;
    padding: 0.35rem 0.5rem;
    border-bottom: 1px solid var(--line);
    vertical-align: middle;
  }

  tr.dead code {
    opacity: 0.6;
    text-decoration: line-through;
  }

  @media (max-width: 720px) {
    .grid {
      grid-template-columns: 1fr !important;
    }

    .cell {
      grid-column: 1 !important;
    }
  }
</style>
