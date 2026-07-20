<script>
  // Metric tile — icon + label + big counter numeral + optional caption/delta. The upgraded metric
  // card (the legacy .metric-grid still works; this is the richer StatCard). Set `accent` for "money"
  // stats to tint the icon gold. Provide an `icon` slot for the lucide glyph, and either the `value`
  // prop or a `value` slot (e.g. to prepend a currency sprite). `sub` is the small caption under the
  // number (the old metric-grid `<small>`); `delta` is a coloured up/down/flat change indicator.
  export let label = '';
  export let value = '';
  export let sub = '';
  export let accent = false;
  export let delta = null; // { dir: 'up' | 'down' | 'flat', text: string }
  export let color = ''; // optional left-accent stripe, e.g. to match a chart series colour
</script>

<article class="stat" class:accent style={color ? `border-left: 3px solid ${color};` : undefined}>
  <span class="stat-label">
    {#if $$slots.icon}<span class="stat-ico"><slot name="icon" /></span>{/if}
    {label}
  </span>
  <span class="stat-value"><slot name="value">{value}</slot></span>
  {#if $$slots.sub || sub}
    <small class="stat-sub"><slot name="sub">{sub}</slot></small>
  {/if}
  {#if delta}
    <span class="stat-delta {delta.dir}">{delta.text}</span>
  {/if}
</article>
