<script>
  // The load test. Two halves that only mean something together: what the run cost the server, and
  // what a real client sitting in the room made of it. Synthetic players supply the first and can
  // never supply the second -- they have nothing to draw -- so the frame rate here comes from
  // whatever real clients are connected, and the page says so rather than pretending otherwise.
  import { onMount, onDestroy } from 'svelte';
  import { apiGet } from '../lib/api.js';
  import { createWriteOps } from '../lib/writeOps.js';
  import { hasDashboardCapability, isPermissionDeniedError } from '../lib/permissions.js';
  import { CAPABILITIES } from '../lib/dashboardPermissions.js';
  import { identity } from '../lib/session.js';
  import { formatNumber, formatDate } from '../lib/format.js';
  import AccessDeniedNotice from '../components/AccessDeniedNotice.svelte';
  import ConfirmReasonModal from '../components/ConfirmReasonModal.svelte';
  import EmptyState from '../components/EmptyState.svelte';
  import OpResult from '../components/OpResult.svelte';
  import StatCard from '../components/StatCard.svelte';
  import PickerModal from '../components/PickerModal.svelte';
  import AssetImage from '../components/AssetImage.svelte';
  import Tabs from '../components/Tabs.svelte';
  import { Gauge, Users, Boxes, TriangleAlert, Activity, Play, History, Monitor } from '@lucide/svelte';
  import { t } from '../lib/i18n.js';

  let loading = false;
  let forbidden = false;
  let error = '';
  let data = null;
  let timer = null;

  const ops = createWriteOps(refresh);

  $: canRun = hasDashboardCapability($identity, CAPABILITIES.opsBenchmarkRun);
  $: canConfigure = hasDashboardCapability($identity, CAPABILITIES.opsConfigManage);
  $: running = data?.running ?? false;
  // The switch that decides whether any of this is allowed. Shown at the top rather than left for
  // the operator to discover through a refusal, which is how the first version read.
  $: enabled = data?.enabled ?? false;

  let picking = null;

  // A past run, loaded whole from its file. The history list carries a headline; the graph needs
  // every sample, and those only exist on disk.
  let openRun = null;
  let openRunName = '';
  let openRunError = '';

  async function openHistoryRun(run) {
    openRunName = run.fileName;
    openRunError = '';
    openRun = null;

    try {
      openRun = await apiGet(`/api/v1/benchmark/runs/${run.fileName}`);
    } catch (err) {
      openRunError = err.message;
    }
  }

  // The same verdict arrives in two shapes and it is not worth fighting: the live one comes through
  // the API, which camel-cases everything, and a past one is the report file served as it sits on
  // disk, which does not. Normalised once here rather than guessed at four call sites.
  const GRADES = ['Good', 'Watch', 'Bad'];

  const verdictOf = (raw) => {
    if (!raw) return null;

    const grade = raw.Grade ?? raw.grade;

    return {
      grade: typeof grade === 'number' ? (GRADES[grade] ?? '') : (grade ?? ''),
      headline: raw.Headline ?? raw.headline ?? '',
      findings: raw.Findings ?? raw.findings ?? [],
    };
  };

  $: liveVerdict = verdictOf(data?.verdict);
  $: openVerdict = verdictOf(openRun?.verdict);

  const gradeTone = (grade) => {
    if (grade === 'Bad') return 'status-badge--bad';
    if (grade === 'Watch') return 'status-badge--warn';
    if (grade === 'Good') return 'status-badge--ok';
    return 'status-badge--unknown';
  };

  const gradeLabel = (grade) => $t(`benchmark.grade${grade || 'Unknown'}`);

  // Four jobs, four tabs. Stacked down one page they made a column nobody could scan: the switch,
  // the live numbers, the past runs and the form all want to be looked at separately.
  let tab = 'run';

  let form = {
    players: 50,
    furniture: 200,
    durationSeconds: 60,
    rampSeconds: 10,
    walkIntervalMs: 2000,
    chatIntervalMs: 8000,
    label: '',
    roomId: 0,
    roomName: '',
    // Empty means "pick a plain floor item for me". Several are interleaved across the floor rather
    // than laid in blocks, which is what a real room looks like to the client's renderer.
    furni: [],
  };

  const dropFurni = (id) => {
    form.furni = form.furni.filter((item) => item.id !== id);
  };

  async function refresh() {
    loading = data === null;
    error = '';

    try {
      data = await apiGet('/api/v1/benchmark');
    } catch (err) {
      if (isPermissionDeniedError(err)) {
        forbidden = true;
        data = null;
        return;
      }

      error = err.message;
    } finally {
      loading = false;
    }
  }

  // Polled rather than pushed: a run is seconds-long and the samples arrive one a second, so a timer
  // is the whole mechanism. Faster while it runs, because that is the only time anything moves.
  function schedule() {
    clearInterval(timer);
    timer = setInterval(refresh, running ? 1000 : 10000);
  }

  $: if (data) schedule();

  onMount(() => {
    void refresh();
  });

  onDestroy(() => clearInterval(timer));

  const phaseTone = (phase) => {
    if (phase === 'Failed') return 'status-badge--bad';
    if (phase === 'Finished') return 'status-badge--ok';
    if (phase === 'Idle') return 'status-badge--unknown';
    return 'status-badge--warn';
  };

  // The chart is drawn by hand rather than pulled in: one series over time, a fixed height, and no
  // interaction. A charting library would be more code than the twelve lines below.
  const points = (samples, pick) => {
    if (!samples || samples.length < 2) return '';

    const values = samples.map(pick);
    const max = Math.max(...values, 1);

    return values
      .map((value, index) => {
        const x = (index / (values.length - 1)) * 100;
        const y = 100 - (value / max) * 100;

        return `${x.toFixed(2)},${y.toFixed(2)}`;
      })
      .join(' ');
  };

  $: samples = data?.samples ?? [];
  $: peakRtt = samples.length ? Math.max(...samples.map((s) => s.rttP95Ms)) : 0;
</script>

<section class="panel">
  <div class="panel-head">
      <h2>{$t('benchmark.title')}</h2>
      <button type="button" on:click={refresh} disabled={loading} class="warning">{$t('common.refresh')}</button>
  </div>
  <p class="muted">{$t('benchmark.description')}</p>

  {#if loading}
    <p class="muted">{$t('common.loading')}</p>
  {:else if forbidden}
    <AccessDeniedNotice message={$t('benchmark.accessDenied')} />
  {:else if error}
    <p class="empty-state danger" role="alert">{error}</p>
  {/if}
</section>

{#if data}
  <section class="panel gate" style="margin-top: 12px;">
    <div class="panel-head">
      <h2>{$t('benchmark.gateTitle')}</h2>
      <span class="status-badge {enabled ? 'status-badge--ok' : 'status-badge--bad'}">
        {enabled ? $t('benchmark.gateOn') : $t('benchmark.gateOff')}
      </span>
    </div>
    <p class="muted">{$t('benchmark.gateHelp')}</p>
    {#if canConfigure}
      <div class="form-actions">
        <button
          type="button"
          class="ghost-button"
          on:click={() =>
            ops.ask(
              '/api/v1/operations/config',
              { key: 'benchmark.enabled', value: enabled ? 'false' : 'true' },
              enabled ? $t('benchmark.gateDisable') : $t('benchmark.gateEnable'),
              enabled ? $t('benchmark.gateDisableSummary') : $t('benchmark.gateEnableSummary')
            )}
        >
          {enabled ? $t('benchmark.gateDisable') : $t('benchmark.gateEnable')}
        </button>
      </div>
    {:else}
      <p class="muted">{$t('benchmark.gateNoPermission')}</p>
    {/if}
  </section>

  <Tabs
    bind:active={tab}
    storageKey="benchmark"
    tabs={[
      { id: 'run', label: $t('benchmark.tabRun'), icon: Gauge },
      { id: 'history', label: $t('benchmark.tabHistory'), icon: History, count: (data.runs || []).length },
      { id: 'client', label: $t('benchmark.tabClient'), icon: Monitor },
      { id: 'start', label: $t('benchmark.tabStart'), icon: Play },
    ]}
  />

  {#if tab === 'run'}
  <section class="panel" style="margin-top: 12px;">
    <div class="panel-head">
      <h2>{$t('benchmark.stateTitle')}</h2>
      <span class="status-badge {phaseTone(data.phase)}">{$t(`benchmark.phase${data.phase}`)}</span>
    </div>

    {#if data.error}
      <p class="empty-state danger" role="alert">{data.error}</p>
    {/if}

    {#if data.residue}
      <!-- The one outcome that outlives the run: rows the teardown could not remove. -->
      <p class="empty-state danger" role="alert">{$t('benchmark.residue', { detail: data.residue })}</p>
    {/if}

    {#if liveVerdict}
      <!-- The answer, before the measurements. Somebody running their first load test does not know
           what 1.6 ms means; they know what "a player froze" means. -->
      <div class="verdict">
        <span class="status-badge {gradeTone(liveVerdict.grade)}">
          {gradeLabel(liveVerdict.grade)}
        </span>
        <strong>{liveVerdict.headline}</strong>
      </div>
      {#if liveVerdict.findings.length > 0}
        <ul class="findings">
          {#each liveVerdict.findings as finding}
            <li>{finding}</li>
          {/each}
        </ul>
      {/if}
    {/if}

    <div class="metric-grid">
      <StatCard label={$t('benchmark.connected')} value={formatNumber(data.connectedClients)}>
        <Users slot="icon" size={15} strokeWidth={2} aria-hidden="true" />
      </StatCard>
      <StatCard label={$t('benchmark.furniturePlaced')} value={formatNumber(data.placedFurniture)}>
        <Boxes slot="icon" size={15} strokeWidth={2} aria-hidden="true" />
      </StatCard>
      <StatCard label={$t('benchmark.worstRtt')} value={`${peakRtt.toFixed(1)} ms`}>
        <Gauge slot="icon" size={15} strokeWidth={2} aria-hidden="true" />
      </StatCard>
      <StatCard label={$t('benchmark.failures')} value={formatNumber(data.summary?.failures ?? 0)}>
        <TriangleAlert slot="icon" size={15} strokeWidth={2} aria-hidden="true" />
      </StatCard>
    </div>

    {#if data.reportPath}
      <p class="report">
        {$t('benchmark.reportWritten')}
        <code>{data.reportPath}</code>
        <button type="button" class="ghost-button" on:click={() => navigator.clipboard?.writeText(data.reportPath)}>
          {$t('benchmark.copyPath')}
        </button>
      </p>
      <p class="muted">{$t('benchmark.reportHelp')}</p>
    {/if}

    {#if data.startedAt}
      <p class="muted">
        {$t('benchmark.startedAt', { at: formatDate(data.startedAt) })}
        {#if data.endedAt}— {$t('benchmark.endedAt', { at: formatDate(data.endedAt) })}{/if}
        {#if data.roomId}— {$t('benchmark.roomId', { id: data.roomId })}{/if}
      </p>
    {/if}
  </section>

  <section class="panel" style="margin-top: 12px;">
    <div class="panel-head"><h2>{$t('benchmark.chartTitle')}</h2></div>
    <p class="muted">{$t('benchmark.chartHelp')}</p>

    {#if samples.length < 2}
      <EmptyState message={$t('benchmark.noSamples')} />
    {:else}
      <div class="chart-wrap">
        <svg viewBox="0 0 100 100" preserveAspectRatio="none" role="img" aria-label={$t('benchmark.chartTitle')}>
          <polyline class="chart-line chart-line--p95" points={points(samples, (s) => s.rttP95Ms)} />
          <polyline class="chart-line chart-line--median" points={points(samples, (s) => s.rttMedianMs)} />
        </svg>
        <div class="chart-legend">
          <span class="chart-key chart-key--median"></span> {$t('benchmark.legendMedian')}
          <span class="chart-key chart-key--p95"></span> {$t('benchmark.legendP95')}
        </div>
      </div>

      <div class="table-wrap">
        <table>
          <thead>
            <tr>
              <th>{$t('benchmark.colAt')}</th>
              <th>{$t('benchmark.colClients')}</th>
              <th>{$t('benchmark.colRttMedian')}</th>
              <th>{$t('benchmark.colRttP95')}</th>
              <th>{$t('benchmark.colPackets')}</th>
              <th>{$t('benchmark.colFailures')}</th>
            </tr>
          </thead>
          <tbody>
            {#each samples.slice(-15).reverse() as sample}
              <tr>
                <td>{formatDate(sample.at)}</td>
                <td>{formatNumber(sample.connectedClients)}</td>
                <td>{sample.rttMedianMs.toFixed(1)} ms</td>
                <td>{sample.rttP95Ms.toFixed(1)} ms</td>
                <td>{formatNumber(sample.packetsReceived)}</td>
                <td>{formatNumber(sample.failures)}</td>
              </tr>
            {/each}
          </tbody>
        </table>
      </div>
    {/if}
  </section>

  {/if}

  {#if tab === 'client'}
  <section class="panel client-note" style="margin-top: 12px;">
    <div class="panel-head">
      <h2>{$t('benchmark.clientTitle')}</h2>
      <Activity size={15} strokeWidth={2} aria-hidden="true" />
    </div>
    <p class="muted">{$t('benchmark.clientHelp')}</p>
  </section>

  {/if}

  {#if tab === 'history'}
  <section class="panel" style="margin-top: 12px;">
    <div class="panel-head"><h2>{$t('benchmark.historyTitle')}</h2></div>
    <p class="muted">{$t('benchmark.historyHelp')}</p>

    {#if (data.runs || []).length === 0}
      <EmptyState message={$t('benchmark.noRuns')} />
    {:else}
      <div class="table-wrap">
        <table>
          <thead>
            <tr>
              <th>{$t('benchmark.colVerdict')}</th>
              <th>{$t('benchmark.colWhen')}</th>
              <th>{$t('benchmark.colLoad')}</th>
              <th>{$t('benchmark.colRoom')}</th>
              <th>{$t('benchmark.colPeak')}</th>
              <th>{$t('benchmark.colWorst')}</th>
              <th>{$t('benchmark.colFailures')}</th>
              <th>{$t('benchmark.colFile')}</th>
            </tr>
          </thead>
          <tbody>
            {#each data.runs as run}
              <tr class:selected={openRunName === run.fileName}>
                <td>
                  <span class="status-badge {gradeTone(run.grade)}">{gradeLabel(run.grade)}</span>
                </td>
                <td>
                  {formatDate(run.writtenAtUtc)}
                  {#if run.label}<br /><small class="muted">{run.label}</small>{/if}
                </td>
                <td>
                  {formatNumber(run.players)} × {formatNumber(run.furniture)}
                  <small class="muted">{run.durationSeconds}s</small>
                </td>
                <td>
                  #{run.roomId}
                  <small class="muted">
                    {run.borrowedRoom ? $t('benchmark.borrowedRoom') : $t('benchmark.throwawayShort')}
                  </small>
                </td>
                <td>{formatNumber(run.peakClients)}</td>
                <td>{run.worstRttMs.toFixed(1)} ms</td>
                <td>
                  {#if run.failures > 0}
                    <span class="status-badge status-badge--bad">{formatNumber(run.failures)}</span>
                  {:else}
                    0
                  {/if}
                </td>
                <td>
                  <span class="cell">
                    <button type="button" class="ghost-button" on:click={() => openHistoryRun(run)}>
                      {$t('benchmark.openRun')}
                    </button>
                    <button
                      type="button"
                      class="ghost-button"
                      on:click={() => navigator.clipboard?.writeText(run.path)}
                    >
                      {$t('benchmark.copyPath')}
                    </button>
                  </span>
                </td>
              </tr>
            {/each}
          </tbody>
        </table>
      </div>
    {/if}

    {#if openRunError}
      <p class="empty-state danger" role="alert">{openRunError}</p>
    {:else if openRun}
      <h3 class="subhead">{openRunName}</h3>

      {#if openVerdict}
        <div class="verdict">
          <span class="status-badge {gradeTone(openVerdict.grade)}">
            {gradeLabel(openVerdict.grade)}
          </span>
          <strong>{openVerdict.headline}</strong>
        </div>
        {#if openVerdict.findings.length > 0}
          <ul class="findings">
            {#each openVerdict.findings as finding}
              <li>{finding}</li>
            {/each}
          </ul>
        {/if}
      {/if}

      {#if (openRun.client?.samples || []).length > 1}
        <p class="muted">{$t('benchmark.chartHelp')}</p>
        <div class="chart-wrap">
          <svg viewBox="0 0 100 100" preserveAspectRatio="none" role="img" aria-label={$t('benchmark.chartTitle')}>
            <polyline class="chart-line chart-line--p95" points={points(openRun.client.samples, (x) => x.RttP95Ms)} />
            <polyline class="chart-line chart-line--median" points={points(openRun.client.samples, (x) => x.RttMedianMs)} />
          </svg>
          <div class="chart-legend">
            <span class="chart-key chart-key--median"></span> {$t('benchmark.legendMedian')}
            <span class="chart-key chart-key--p95"></span> {$t('benchmark.legendP95')}
          </div>
        </div>
      {/if}

      {#if (openRun.rooms?.steps || []).length > 0}
        <h3 class="subhead">{$t('benchmark.stepsTitle')}</h3>
        <p class="muted">{$t('benchmark.stepsHelp')}</p>
        <div class="steps">
          {#each openRun.rooms.steps.slice(0, 8) as step}
            <div class="step">
              <span class="step-name">{step.Name}</span>
              <span class="step-bar">
                <span class="step-fill" style="width: {Math.min(100, step.ShareOfTickPercent)}%"></span>
              </span>
              <span class="step-value">
                {step.ShareOfTickPercent.toFixed(1)}%
                <small class="muted">p99 {step.P99Ms.toFixed(1)} ms</small>
              </span>
            </div>
          {/each}
        </div>
      {/if}
    {/if}
  </section>
  {/if}

  {#if tab === 'start'}
  {#if canRun}
    <section class="panel" style="margin-top: 12px;">
      <div class="panel-head"><h2>{$t('benchmark.runTitle')}</h2></div>
      <p class="muted">{$t('benchmark.runHelp')}</p>

      <form
        class="inline-form editor-form"
        on:submit|preventDefault={() =>
          ops.ask(
            '/api/v1/operations/benchmark/start',
            {
              players: Number(form.players) || 0,
              furniture: Number(form.furniture) || 0,
              furnitureIds: form.furni.map((item) => item.id),
              roomId: Number(form.roomId) || 0,
              durationSeconds: Number(form.durationSeconds) || 0,
              rampSeconds: Number(form.rampSeconds) || 0,
              walkIntervalMs: Number(form.walkIntervalMs) || 0,
              chatIntervalMs: Number(form.chatIntervalMs) || 0,
              label: form.label || '',
            },
            $t('benchmark.start'),
            $t('benchmark.startSummary', {
              players: form.players,
              furniture: form.furniture,
              seconds: form.durationSeconds,
            })
          )}
      >
        <label>
          {$t('benchmark.fieldRoom')}
          <span class="cell">
            <button type="button" class="ghost-button" on:click={() => (picking = 'room')}>
              {$t('benchmark.pickRoom')}
            </button>
            {#if form.roomId}
              <span class="op-chip">{form.roomName || form.roomId} <small>#{form.roomId}</small></span>
              <button
                type="button"
                class="ghost-button"
                on:click={() => {
                  form.roomId = 0;
                  form.roomName = '';
                }}
              >
                {$t('benchmark.useThrowaway')}
              </button>
            {:else}
              <span class="muted">{$t('benchmark.throwawayRoom')}</span>
            {/if}
          </span>
          <small class="muted">{$t('benchmark.fieldRoomHelp')}</small>
        </label>
        <label>
          {$t('benchmark.fieldPlayers')}
          <input autocomplete="off" spellcheck="false" type="number" min="1" max="2000" bind:value={form.players} />
          <small class="muted">{$t('benchmark.fieldPlayersHelp')}</small>
        </label>
        <label>
          {$t('benchmark.fieldFurniture')}
          <input autocomplete="off" spellcheck="false" type="number" min="0" max="20000" bind:value={form.furniture} />
          <small class="muted">{$t('benchmark.fieldFurnitureHelp')}</small>
        </label>
        <label>
          {$t('benchmark.fieldFurniKinds')}
          <span class="cell">
            <button type="button" class="ghost-button" on:click={() => (picking = 'furni')}>
              {$t('benchmark.pickFurni')}
            </button>
            {#if form.furni.length === 0}
              <span class="muted">{$t('benchmark.furniAuto')}</span>
            {/if}
          </span>
          {#if form.furni.length > 0}
            <span class="furni-chips">
              {#each form.furni as item}
                <span class="op-chip">
                  <AssetImage src={item.iconUrl} alt={item.name} size={24} />
                  {item.name}
                  <button type="button" class="chip-remove" on:click={() => dropFurni(item.id)}>×</button>
                </span>
              {/each}
            </span>
          {/if}
          <small class="muted">{$t('benchmark.fieldFurniKindsHelp')}</small>
        </label>
        <label>
          {$t('benchmark.fieldDuration')}
          <input autocomplete="off" spellcheck="false" type="number" min="1" max="3600" bind:value={form.durationSeconds} />
        </label>
        <label>
          {$t('benchmark.fieldRamp')}
          <input autocomplete="off" spellcheck="false" type="number" min="0" bind:value={form.rampSeconds} />
          <small class="muted">{$t('benchmark.fieldRampHelp')}</small>
        </label>
        <label>
          {$t('benchmark.fieldWalk')}
          <input autocomplete="off" spellcheck="false" type="number" min="0" step="500" bind:value={form.walkIntervalMs} />
          <small class="muted">{$t('benchmark.fieldWalkHelp')}</small>
        </label>
        <label>
          {$t('benchmark.fieldChat')}
          <input autocomplete="off" spellcheck="false" type="number" min="0" step="500" bind:value={form.chatIntervalMs} />
        </label>
        <label>
          {$t('benchmark.fieldLabel')}
          <input autocomplete="off" spellcheck="false" bind:value={form.label} placeholder={$t('benchmark.fieldLabelPlaceholder')} />
        </label>
        <div class="form-actions">
          <button type="submit" disabled={running || !enabled}>{$t('benchmark.start')}</button>
          <button
            type="button"
            class="ghost-button danger"
            disabled={!running}
            on:click={() =>
              ops.ask(
                '/api/v1/operations/benchmark/stop',
                {},
                $t('benchmark.stop'),
                $t('benchmark.stopSummary')
              )}
          >
            {$t('benchmark.stop')}
          </button>
        </div>
      </form>
    </section>
  {:else}
    <section class="panel" style="margin-top: 12px;">
      <p class="muted">{$t('benchmark.noRunPermission')}</p>
    </section>
  {/if}
  {/if}
{/if}

{#if picking === 'room'}
  <PickerModal
    kind="room"
    title={$t('benchmark.pickRoom')}
    onSelect={(picked) => {
      form.roomId = picked.id;
      form.roomName = picked.name;
      picking = null;
    }}
    onClose={() => (picking = null)}
  />
{:else if picking === 'furni'}
  <PickerModal
    kind="furniture"
    title={$t('benchmark.pickFurni')}
    onSelect={(picked) => {
      // Kept open would be nicer for picking five in a row, but the modal closes on select and
      // reopening is one click -- not worth forking the shared component over.
      if (!form.furni.some((item) => item.id === picked.id)) {
        form.furni = [...form.furni, { id: picked.id, name: picked.name, iconUrl: picked.iconUrl }];
      }

      picking = null;
    }}
    onClose={() => (picking = null)}
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
  danger={$ops.pending?.danger ?? false}
  onconfirm={ops.confirm}
  oncancel={() => ops.cancel()}
/>

<OpResult result={$ops.result} />

<style>
  .verdict {
    display: flex;
    align-items: center;
    gap: 10px;
    flex-wrap: wrap;
    margin: 4px 0 8px;
  }

  .findings {
    margin: 0 0 10px;
    padding-left: 20px;
    line-height: 1.6;
  }

  /* A bar per tick step, longest first. One glance says where the time goes, which a column of
     milliseconds never does -- the whole reason this exists rather than a table. */
  .steps {
    display: grid;
    gap: 6px;
    margin-top: 8px;
  }

  .step {
    display: grid;
    grid-template-columns: 140px 1fr 140px;
    align-items: center;
    gap: 10px;
  }

  .step-bar {
    height: 12px;
    border-radius: 8px;
    background: var(--surface-raised, rgba(255, 255, 255, 0.08));
    overflow: hidden;
  }

  .step-fill {
    display: block;
    height: 100%;
    background: var(--accent, #4f8cff);
  }

  .step-value {
    text-align: right;
    font-variant-numeric: tabular-nums;
  }

  .furni-chips {
    display: flex;
    flex-wrap: wrap;
    gap: 6px;
  }

  .chip-remove {
    background: none;
    border: 0;
    padding: 0 0 0 4px;
    cursor: pointer;
    font-size: 1.1em;
    line-height: 1;
    color: inherit;
  }

  .report {
    display: flex;
    align-items: center;
    flex-wrap: wrap;
    gap: 8px;
    margin: 10px 0 2px;
  }

  .report code {
    word-break: break-all;
  }

  /* The picked room and its buttons on one line. .editor-form gives the grid; this is the row
     inside one of its cells. */
  .cell {
    display: inline-flex;
    align-items: center;
    gap: 8px;
    flex-wrap: wrap;
  }

  /* A fixed box the line is stretched into, so a run of six samples and a run of six hundred are
     read the same way. The viewBox is unitless and the stroke is un-scaled, which is what keeps the
     line one pixel wide at any width. */
  .chart-wrap svg {
    width: 100%;
    height: 160px;
    display: block;
  }

  .chart-line {
    fill: none;
    stroke-width: 1;
    vector-effect: non-scaling-stroke;
  }

  .chart-line--median {
    stroke: var(--accent, #4f8cff);
  }

  .chart-line--p95 {
    stroke: var(--danger, #e2504b);
    opacity: 0.75;
  }

  .chart-legend {
    display: flex;
    align-items: center;
    gap: 8px;
    font-size: 0.85em;
    margin: 8px 0 4px;
  }

  .chart-key {
    display: inline-block;
    width: 14px;
    height: 3px;
    border-radius: 8px;
  }

  .chart-key--median {
    background: var(--accent, #4f8cff);
  }

  .chart-key--p95 {
    background: var(--danger, #e2504b);
  }

  .client-note .panel-head {
    display: flex;
    align-items: center;
    gap: 8px;
  }
</style>
