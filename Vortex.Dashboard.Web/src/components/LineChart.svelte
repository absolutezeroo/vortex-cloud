<script>
  // Minimal dependency-free SVG line chart. No charting library is installed in this project —
  // this hand-rolled component is the shared primitive for every trend chart on the dashboard
  // (currently: Economy spend/earn/marketplace trends, and every new stats page). Series share
  // one x-axis (by point index) and one y-scale (min/max across all series) so multiple series
  // stay comparable.
  //
  // The viewBox is measured from the actual rendered container width so viewBox units below can
  // be treated as CSS pixels for tooltip positioning. `preserveAspectRatio="none"` forces the SVG
  // to fill its box exactly on every dimension even when a resize/mount race leaves `containerWidth`
  // briefly stale (see the ResizeObserver note below) — without it, a stale viewBox falls back to
  // the SVG default `xMidYMid meet`, which letterboxes/centers instead of stretching, so the
  // rendered dots silently drift out of sync with the mouse-hover math (which always reads the
  // real box width fresh) until something forces a re-render. `preserveAspectRatio="none"` makes
  // a stale width harmless: the box still stretches to fit, so points and hover stay aligned.
  import { onMount, onDestroy } from 'svelte';
  import { t } from '../lib/i18n.js';

  export let series = []; // [{ name, color, points: [{ label, value }] }]
  export let height = 220;
  export let valueFormatter = (v) => String(v);
  export let emptyMessage = '';

  // Nonzero default so the chart renders at a sane size immediately, before the ResizeObserver
  // below reports the real measured width.
  let containerWidth = 300;
  let containerEl;
  let hoverIndex = null;
  let resizeObserver;

  // bind:clientWidth alone does not reliably report the real width the moment this component's
  // container first becomes visible (e.g. a panel that mounts once async data arrives) — Svelte's
  // own resize tracking can land a tick after first paint, and if nothing resizes afterward the
  // stale value sticks until the user forces a reflow (window resize, tab switch). A ResizeObserver
  // handles every *subsequent* resize reliably, but its own first callback is still asynchronous
  // (queued for the end of the current layout pass) — relying on it alone can still show one frame
  // of the 300px placeholder width before snapping to the real size, a visible flash/snap on every
  // mount. Reading getBoundingClientRect() synchronously here, before the observer's first callback
  // has a chance to fire, means the very first reactive render already has the correct width.
  onMount(() => {
    if (!containerEl) {
      return;
    }

    const initialWidth = containerEl.getBoundingClientRect().width;
    if (initialWidth > 0) {
      containerWidth = initialWidth;
    }

    if (typeof ResizeObserver === 'undefined') {
      return;
    }

    resizeObserver = new ResizeObserver((entries) => {
      const entry = entries[0];
      if (entry) {
        containerWidth = entry.contentRect.width;
      }
    });
    resizeObserver.observe(containerEl);
  });

  onDestroy(() => {
    resizeObserver?.disconnect();
  });

  const margin = { top: 12, right: 12, bottom: 26, left: 12 };

  $: viewWidth = Math.max(60, containerWidth);
  $: viewHeight = height;
  $: innerWidth = Math.max(1, viewWidth - margin.left - margin.right);
  $: innerHeight = Math.max(1, viewHeight - margin.top - margin.bottom);
  $: allPoints = series.flatMap((s) => s.points || []);
  $: hasData = allPoints.length > 0;
  $: pointCount = Math.max(1, ...series.map((s) => (s.points || []).length), 1);
  $: rawMax = Math.max(0, ...allPoints.map((p) => Number(p.value) || 0));
  $: rawMin = Math.min(0, ...allPoints.map((p) => Number(p.value) || 0));
  $: maxValue = rawMax === rawMin ? rawMax + 1 : rawMax;
  $: minValue = rawMin;

  // xFor/yFor/pathFor all take their reactive inputs (innerW/count/innerH/minV/maxV) as explicit
  // parameters instead of reading innerWidth/pointCount/innerHeight/minValue/maxValue from closure.
  // This looks redundant (the closure values ARE correct at call time) but it is not: per-item
  // bindings inside a nested {#each s.points as p, i} block, and $: statements, are only re-run by
  // Svelte when a variable *referenced directly in that expression's own text* changes -- a value
  // read only inside a called function's body is invisible to that dependency scan. Without this,
  // resizing the chart (or reloading with a different container width) correctly moves the <path>
  // (computed once per series in the outer {#each series as s} block, which re-runs unconditionally)
  // but silently leaves every per-point <circle> frozen at its old position, since the inner
  // each-block never gets told innerWidth/pointCount changed. Same class of bug as the
  // ApiExplorerPage `filtered` reactivity note elsewhere in this codebase.
  function xFor(index, innerW, count) {
    if (count <= 1) {
      return margin.left + innerW / 2;
    }
    return margin.left + (index / (count - 1)) * innerW;
  }

  function yFor(value, innerH, minV, maxV) {
    const range = maxV - minV || 1;
    return margin.top + innerH - ((Number(value) - minV) / range) * innerH;
  }

  function pathFor(points, innerW, count, innerH, minV, maxV) {
    if (!points || points.length === 0) {
      return '';
    }
    return points
      .map(
        (p, i) =>
          `${i === 0 ? 'M' : 'L'} ${xFor(i, innerW, count).toFixed(1)} ${yFor(p.value, innerH, minV, maxV).toFixed(1)}`,
      )
      .join(' ');
  }

  function indexFromClientX(clientX) {
    if (!containerEl || pointCount <= 1) {
      return 0;
    }

    const rect = containerEl.getBoundingClientRect();
    if (rect.width === 0) {
      return null;
    }

    // rect.width is the real CSS pixel width; viewWidth is the same number by construction
    // (see note above), so this scale factor is normally ~1 — kept explicit in case the browser
    // ever reports a fractional mismatch (zoom levels, subpixel layout).
    const scaleX = viewWidth / rect.width;
    const localX = (clientX - rect.left) * scaleX;
    const ratio = (localX - margin.left) / innerWidth;
    const idx = Math.round(ratio * (pointCount - 1));

    return Math.min(pointCount - 1, Math.max(0, idx));
  }

  function handleMove(event) {
    hoverIndex = indexFromClientX(event.clientX);
  }

  function handleLeave() {
    hoverIndex = null;
  }

  $: hoverRows =
    hoverIndex === null
      ? []
      : series
          .map((s) => ({
            name: s.name,
            color: s.color,
            point: (s.points || [])[hoverIndex],
          }))
          .filter((r) => r.point !== undefined);

  $: hoverLabel = hoverRows[0]?.point?.label ?? '';
  $: hoverX = hoverIndex === null ? 0 : xFor(hoverIndex, innerWidth, pointCount);
  $: tooltipAlignRight = hoverX > viewWidth * 0.6;

  $: gridLines = [0, 0.25, 0.5, 0.75, 1].map((t) => margin.top + t * innerHeight);
  $: labelPoints = (series[0]?.points || []).map((p, i) => ({ index: i, label: p.label }));
  // Thin the x-axis labels so they don't overlap when there are many points.
  $: labelStride = Math.max(1, Math.ceil(labelPoints.length / 8));
</script>

<div
  class="linechart"
  style={`height: ${height}px`}
  bind:this={containerEl}
  on:mousemove={handleMove}
  on:mouseleave={handleLeave}
  role="img"
  aria-label="Trend chart"
>
  {#if !hasData}
    <p class="muted">{emptyMessage || $t('lineChart.noData')}</p>
  {:else}
    <svg
      viewBox={`0 0 ${viewWidth} ${viewHeight}`}
      preserveAspectRatio="none"
      class="linechart-svg"
      aria-hidden="true"
    >
      {#each gridLines as y}
        <line x1={margin.left} x2={viewWidth - margin.right} y1={y} y2={y} class="linechart-grid" />
      {/each}
      {#each series as s}
        <path d={pathFor(s.points, innerWidth, pointCount, innerHeight, minValue, maxValue)} fill="none" stroke={s.color} stroke-width="2" stroke-linejoin="round" stroke-linecap="round" />
        {#each s.points || [] as p, i}
          <circle cx={xFor(i, innerWidth, pointCount)} cy={yFor(p.value, innerHeight, minValue, maxValue)} r="2.4" fill={s.color} />
        {/each}
      {/each}
      {#if hoverIndex !== null && hoverRows.length > 0}
        <line x1={hoverX} x2={hoverX} y1={margin.top} y2={viewHeight - margin.bottom} class="linechart-hover-line" />
        {#each hoverRows as row}
          <circle cx={hoverX} cy={yFor(row.point.value, innerHeight, minValue, maxValue)} r="4" fill={row.color} class="linechart-hover-dot" />
        {/each}
      {/if}
      {#each labelPoints as p (p.index)}
        {#if p.index % labelStride === 0}
          <text x={xFor(p.index, innerWidth, pointCount)} y={viewHeight - 6} class="linechart-axis-label" text-anchor="middle">{p.label}</text>
        {/if}
      {/each}
    </svg>

    {#if hoverIndex !== null && hoverRows.length > 0}
      <div
        class="linechart-tooltip"
        class:align-right={tooltipAlignRight}
        style={`left: ${hoverX}px;`}
      >
        <div class="linechart-tooltip-label">{hoverLabel}</div>
        {#each hoverRows as row}
          <div class="linechart-tooltip-row">
            <span class="legend-dot" style={`background: ${row.color};`}></span>
            <span class="linechart-tooltip-name">{row.name}</span>
            <strong>{valueFormatter(row.point.value)}</strong>
          </div>
        {/each}
      </div>
    {/if}
  {/if}
</div>

{#if series.length > 1 || (series.length === 1 && series[0].name)}
  <div class="linechart-legend">
    {#each series as s}
      <span class="linechart-legend-item">
        <span class="legend-dot" style={`background: ${s.color};`}></span>
        {s.name}
      </span>
    {/each}
  </div>
{/if}

<style>
  .linechart {
    position: relative;
    width: 100%;
  }

  .linechart-svg {
    width: 100%;
    height: 100%;
    display: block;
  }

  .linechart-grid {
    stroke: var(--line);
    stroke-width: 1;
  }

  .linechart-hover-line {
    stroke: var(--line-strong);
    stroke-width: 1;
    stroke-dasharray: 3 2;
  }

  .linechart-hover-dot {
    stroke: var(--surface);
    stroke-width: 1.5;
  }

  .linechart-axis-label {
    font-size: 9px;
    fill: var(--muted);
  }

  .linechart-legend {
    display: flex;
    flex-wrap: wrap;
    gap: 12px;
    margin-top: 8px;
  }

  .linechart-legend-item {
    display: inline-flex;
    align-items: center;
    gap: 6px;
    font-size: 0.8rem;
    color: var(--muted);
  }

  .linechart-tooltip {
    position: absolute;
    top: 6px;
    transform: translateX(-50%);
    min-width: 140px;
    max-width: 220px;
    pointer-events: none;
    background: var(--surface-raised, var(--surface));
    border: 1px solid var(--line-strong);
    border-radius: 8px;
    box-shadow: var(--shadow);
    padding: 8px 10px;
    z-index: 5;
  }

  .linechart-tooltip.align-right {
    transform: translateX(-90%);
  }

  .linechart-tooltip-label {
    font-size: 0.72rem;
    text-transform: uppercase;
    font-weight: 700;
    color: var(--muted);
    margin-bottom: 4px;
  }

  .linechart-tooltip-row {
    display: flex;
    align-items: center;
    gap: 6px;
    font-size: 0.82rem;
    padding: 1px 0;
  }

  .linechart-tooltip-name {
    flex: 1;
    min-width: 0;
    overflow: hidden;
    text-overflow: ellipsis;
    white-space: nowrap;
    color: var(--muted);
  }
</style>
