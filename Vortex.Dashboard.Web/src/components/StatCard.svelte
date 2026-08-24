<script>
  // Metric tile — icon + label + big counter numeral + optional caption/delta. The upgraded metric
  // card (the legacy .metric-grid still works; this is the richer StatCard). Set `accent` for "money"
  // stats to tint the icon gold. `sub` is the small caption under the number (the old metric-grid
  // `<small>`); `delta` is a coloured up/down/flat change indicator.
  //
  // `value` and `sub` each take EITHER text or markup, and in Svelte 5 that has to be one prop
  // rather than the prop-plus-same-named-slot pair this used to be: a snippet is passed as a prop,
  // so `{#snippet value()}` and `value={...}` are the same name arriving by two routes and cannot
  // coexist. Callers that want a currency sprite in front of the number pass a snippet; callers
  // with a plain number keep passing a string, unchanged.
  let {
    label = '',
    value = '',
    sub = '',
    accent = false,
    delta = null, // { dir: 'up' | 'down' | 'flat', text: string }
    color = '', // optional left-accent stripe, e.g. to match a chart series colour
    icon = null, // snippet: the lucide glyph shown before the label
  } = $props();

  // A snippet is a function and text is not, which is the only distinction the markup below needs.
  const isSnippet = (slot) => typeof slot === 'function';

  // The tile is sized for a counter. A word -- "Development", a hostname, a status -- runs past the
  // card at the numeral size, so a long text value drops a size rather than overflowing.
  let longValue = $derived(!isSnippet(value) && String(value ?? '').length > 9);
</script>

<article class="stat" class:accent style={color ? `border-left: 3px solid ${color};` : undefined}>
  <span class="stat-label">
    {#if icon}<span class="stat-ico">{@render icon()}</span>{/if}
    {label}
  </span>
  <span class="stat-value" class:stat-value--long={longValue}>
    {#if isSnippet(value)}{@render value()}{:else}{value}{/if}
  </span>
  {#if sub}
    <small class="stat-sub">
      {#if isSnippet(sub)}{@render sub()}{:else}{sub}{/if}
    </small>
  {/if}
  {#if delta}
    <span class="stat-delta {delta.dir}">{delta.text}</span>
  {/if}
</article>
