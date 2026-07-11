<script>
  // Minimal dependency-free SVG line chart. No charting library is installed in this project —
  // this hand-rolled component is the shared primitive for every trend chart on the dashboard
  // (currently: Economy spend/earn/marketplace trends). Series share one x-axis (by point index)
  // and one y-scale (min/max across all series) so multiple currencies stay comparable.
  //
  // The viewBox is measured from the actual rendered container width (bind:clientWidth) rather
  // than a fixed constant: a fixed viewBox combined with `preserveAspectRatio="none"` stretches
  // X and Y by different factors whenever the container's aspect ratio doesn't match the fixed
  // viewBox's, which visibly distorts stroke widths/dots/text (looks oversized/blobby). Matching
  // the viewBox to the real pixel box 1:1 means there is nothing to stretch, and it also means
  // viewBox units below can be treated as CSS pixels for tooltip positioning.
  export let series = []; // [{ name, color, points: [{ label, value }] }]
  export let height = 220;
  export let valueFormatter = (v) => String(v);
  export let emptyMessage = 'No data for this window.';

  // Nonzero default so the chart renders at a sane size immediately, before the resize binding
  // below reports the real measured width (which lands a tick after first paint).
  let containerWidth = 300;
  let containerEl;
  let hoverIndex = null;

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

  function xFor(index) {
    if (pointCount <= 1) {
      return margin.left + innerWidth / 2;
    }
    return margin.left + (index / (pointCount - 1)) * innerWidth;
  }

  function yFor(value) {
    const range = maxValue - minValue || 1;
    return margin.top + innerHeight - ((Number(value) - minValue) / range) * innerHeight;
  }

  function pathFor(points) {
    if (!points || points.length === 0) {
      return '';
    }
    return points
      .map((p, i) => `${i === 0 ? 'M' : 'L'} ${xFor(i).toFixed(1)} ${yFor(p.value).toFixed(1)}`)
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
  $: hoverX = hoverIndex === null ? 0 : xFor(hoverIndex);
  $: tooltipAlignRight = hoverX > viewWidth * 0.6;

  $: gridLines = [0, 0.25, 0.5, 0.75, 1].map((t) => margin.top + t * innerHeight);
  $: labelPoints = (series[0]?.points || []).map((p, i) => ({ index: i, label: p.label }));
  // Thin the x-axis labels so they don't overlap when there are many points.
  $: labelStride = Math.max(1, Math.ceil(labelPoints.length / 8));
</script>

<div
  class="linechart"
  style={`height: ${height}px`}
  bind:clientWidth={containerWidth}
  bind:this={containerEl}
  on:mousemove={handleMove}
  on:mouseleave={handleLeave}
  role="img"
  aria-label="Trend chart"
>
  {#if !hasData}
    <p class="muted">{emptyMessage}</p>
  {:else}
    <svg viewBox={`0 0 ${viewWidth} ${viewHeight}`} class="linechart-svg" aria-hidden="true">
      {#each gridLines as y}
        <line x1={margin.left} x2={viewWidth - margin.right} y1={y} y2={y} class="linechart-grid" />
      {/each}
      {#each series as s}
        <path d={pathFor(s.points)} fill="none" stroke={s.color} stroke-width="2" stroke-linejoin="round" stroke-linecap="round" />
        {#each s.points || [] as p, i}
          <circle cx={xFor(i)} cy={yFor(p.value)} r="2.4" fill={s.color} />
        {/each}
      {/each}
      {#if hoverIndex !== null && hoverRows.length > 0}
        <line x1={hoverX} x2={hoverX} y1={margin.top} y2={viewHeight - margin.bottom} class="linechart-hover-line" />
        {#each hoverRows as row}
          <circle cx={hoverX} cy={yFor(row.point.value)} r="4" fill={row.color} class="linechart-hover-dot" />
        {/each}
      {/if}
      {#each labelPoints as p (p.index)}
        {#if p.index % labelStride === 0}
          <text x={xFor(p.index)} y={viewHeight - 6} class="linechart-axis-label" text-anchor="middle">{p.label}</text>
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
