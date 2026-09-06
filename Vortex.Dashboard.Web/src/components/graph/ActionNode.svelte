<script>
  // One action on the canvas: a header, its conditions, and the ports a later condition wires to.
  //
  // The header is the drag handle and the only draggable part, because the body is full of selects
  // and text fields an operator has to be able to click into.
  import { Search, X } from '@lucide/svelte';
  import AssetImage from '../AssetImage.svelte';
  import { portColour } from '../../lib/graph/model.js';
  import { t } from '../../lib/i18n.js';

  /**
   * @type {{
   *   node: any, actions: any[], canManage: boolean, selected: boolean,
   *   factsFor: (a: string) => any[], operatorsFor: (a: string, k: string) => any[],
   *   defaultFilterValue: (a: string, k: string) => string,
   *   pickerFor: (meta: any) => string | null, pickedLabels: Record<string, any>,
   *   onmovestart: (event: PointerEvent) => void, onchange: () => void,
   *   onremove: () => void, onaddfilter: () => void, onremovefilter: (i: number) => void,
   *   onpick: (i: number, kind: string) => void,
   *   onportdown: (factKey: string, event: PointerEvent) => void,
   *   oninputup: (filterIndex: number) => void,
   *   oncut: (filterIndex: number) => void,
   * }}
   */
  let {
    node,
    actions,
    canManage,
    selected,
    factsFor,
    operatorsFor,
    defaultFilterValue,
    pickerFor,
    pickedLabels,
    onmovestart,
    onchange,
    onremove,
    onaddfilter,
    onremovefilter,
    onpick,
    onportdown,
    oninputup,
    oncut,
  } = $props();

  let facts = $derived(factsFor(node.action));

  function label(fact) {
    const translated = $t(fact.labelKey);

    return translated === fact.labelKey ? fact.fallbackLabel : translated;
  }
</script>

<div
  class="node"
  class:selected
  style:left="{node.x}px"
  style:top="{node.y}px"
  data-node={node.id}
>
  <header class="node-head" onpointerdown={canManage ? onmovestart : undefined}>
    <span class="node-index">{node.id + 1}</span>
    <span class="node-title">{node.action}</span>
    {#if canManage}
      <button type="button" class="node-close" title={$t('common.remove')} onclick={onremove}>
        <X size={13} />
      </button>
    {/if}
  </header>

  <!-- Flow in and out: the order, as something you can see and follow. The data-port attribute is
       how the wire layer finds this point again after the node has been dragged -- measured, not
       computed, because a node's height depends on how many conditions it carries. -->
  <span class="flow-port in" data-port="{node.id}:flow-in" aria-hidden="true"></span>
  <span class="flow-port out" data-port="{node.id}:flow-out" aria-hidden="true"></span>

  <div class="node-body">
    <label class="node-field">
      <span>{$t('rewardTracks.action')}</span>
      <select bind:value={node.step.actionCode} onchange={onchange} disabled={!canManage}>
        {#each actions as action (action.name)}
          <option value={action.name}>{action.name}</option>
        {/each}
      </select>
    </label>

    {#each node.filters as entry (entry.index)}
      {@const meta = facts.find((f) => f.key === entry.filter.factKey) ?? null}
      {@const picked = pickedLabels[`${node.id}:${entry.index}`]}
      <div class="condition">
        <!-- The input side of a condition: a wire lands here, and clicking a live one cuts it. -->
        <span
          class="port in"
          class:wired={entry.wiredTo >= 0}
          style:--port={portColour(meta?.kind)}
          onpointerup={() => oninputup(entry.index)}
          onclick={() => entry.wiredTo >= 0 && oncut(entry.index)}
          data-port="{node.id}:in:{entry.index}"
          role="presentation"
          title={entry.wiredTo >= 0
            ? $t('rewardTracks.cutWire', { n: entry.wiredTo + 1 })
            : $t('rewardTracks.wireHint')}
        ></span>

        <select
          bind:value={entry.filter.factKey}
          disabled={!canManage}
          onchange={() => {
            entry.filter.value = defaultFilterValue(node.action, entry.filter.factKey);
            entry.filter.op = operatorsFor(node.action, entry.filter.factKey)[0]?.value ?? 0;
            onchange();
          }}
        >
          {#each facts as fact (fact.key)}
            <option value={fact.key}>{label(fact)}</option>
          {/each}
        </select>

        <select bind:value={entry.filter.op} disabled={!canManage} onchange={onchange}>
          {#each operatorsFor(node.action, entry.filter.factKey) as op (op.value)}
            <option value={op.value}>{$t(op.key)}</option>
          {/each}
        </select>

        {#if entry.wiredTo >= 0}
          <!-- Wired: the value is the earlier action's, so there is nothing to type. -->
          <span class="wired-value">{$t('rewardTracks.filterSameAsStep', { n: entry.wiredTo + 1 })}</span>
        {:else if meta?.values?.length}
          <select bind:value={entry.filter.value} disabled={!canManage} onchange={onchange}>
            {#each meta.values as allowed (allowed.value)}
              <option value={allowed.value}>{label(allowed)}</option>
            {/each}
          </select>
        {:else}
          <input
            type="text"
            bind:value={entry.filter.value}
            disabled={!canManage}
            onchange={onchange}
            placeholder={$t('rewardTracks.conditionValuePlaceholder')}
          />
          {#if pickerFor(meta)}
            <button
              type="button"
              class="node-icon-button"
              title={$t('rewardTracks.pickValue')}
              disabled={!canManage}
              onclick={() => onpick(entry.index, pickerFor(meta))}
            >
              <Search size={13} />
            </button>
          {/if}
        {/if}

        {#if canManage}
          <button
            type="button"
            class="node-icon-button"
            title={$t('common.remove')}
            onclick={() => onremovefilter(entry.index)}
          >
            <X size={13} />
          </button>
        {/if}

        {#if picked?.name}
          <span class="picked">
            {#if picked.iconUrl}<AssetImage src={picked.iconUrl} alt="" size={18} />{/if}
            {picked.name}
          </span>
        {/if}
      </div>
    {/each}

    {#if facts.length > 0 && canManage}
      <button type="button" class="node-add" onclick={onaddfilter}>
        {$t('rewardTracks.addFilter')}
      </button>
    {:else if facts.length === 0}
      <p class="node-empty">{$t('rewardTracks.actionHasNoFacts')}</p>
    {/if}
  </div>

  <!-- What this action records, and therefore what a later condition can read. One port per fact,
       coloured by what it carries. -->
  {#if node.outputs.length}
    <div class="node-ports">
      {#each node.outputs as port (port.key)}
        <div class="port-row">
          <span class="port-label">{port.label}</span>
          <span
            class="port out"
            style:--port={portColour(port.kind)}
            onpointerdown={(e) => canManage && onportdown(port.key, e)}
            data-port="{node.id}:out:{port.key}"
            role="presentation"
            title={$t('rewardTracks.dragWire')}
          ></span>
        </div>
      {/each}
    </div>
  {/if}
</div>

<style>
  .node {
    position: absolute;
    /* Wide enough for a condition on one line -- fact, operator, value and its picker. At 300 the
       third control wrapped, which is the exact failure that got the editor out of the drawer. */
    width: 360px;
    border: 2px solid var(--line-strong);
    border-radius: 6px;
    background: var(--surface);
    box-shadow: var(--shadow);
    user-select: none;
  }

  .node.selected {
    border-color: var(--accent);
  }

  /* The header is the handle. Coloured like the trigger nodes it stands for, and the only part that
     starts a drag -- the body has fields an operator must be able to click into. */
  .node-head {
    display: flex;
    align-items: center;
    gap: 8px;
    padding: 7px 10px;
    background: rgba(var(--accent-rgb), 0.75);
    border-radius: 3px 3px 0 0;
    cursor: grab;
    font-weight: 700;
    font-size: 0.78rem;
    letter-spacing: 0.04em;
    text-transform: uppercase;
  }

  .node-head:active {
    cursor: grabbing;
  }

  .node-index {
    flex: 0 0 auto;
    min-width: 18px;
    text-align: center;
    border-radius: 3px;
    background: rgba(0, 0, 0, 0.28);
    font-size: 0.7rem;
  }

  .node-title {
    flex: 1 1 auto;
    overflow: hidden;
    text-overflow: ellipsis;
    white-space: nowrap;
    text-transform: none;
    letter-spacing: 0;
  }

  .node-close {
    flex: 0 0 auto;
    display: inline-flex;
    padding: 2px;
    border: 0;
    border-radius: 3px;
    background: transparent;
    color: inherit;
    cursor: pointer;
  }

  .node-body {
    display: flex;
    flex-direction: column;
    gap: 7px;
    padding: 9px 10px;
  }

  .node-field {
    display: flex;
    flex-direction: column;
    gap: 3px;
    font-size: 0.72rem;
    color: var(--muted);
  }

  .node-field select {
    width: 100%;
  }

  .condition {
    position: relative;
    display: flex;
    flex-wrap: wrap;
    align-items: center;
    gap: 4px;
    padding: 6px 6px 6px 10px;
    border: 1px solid rgba(var(--gold-rgb), 0.35);
    border-radius: 5px;
    background: rgba(var(--gold-rgb), 0.08);
    font-size: 0.76rem;
  }

  .condition select,
  .condition input {
    flex: 1 1 4rem;
    min-width: 0;
    width: auto;
    font-size: 0.74rem;
    padding: 3px 6px;
  }

  /* The operator is two words at most, so it takes what it needs and leaves the rest to the value. */
  .condition select:nth-of-type(2) {
    flex: 0 1 auto;
  }

  .wired-value {
    flex: 1 1 auto;
    color: var(--accent-strong);
    font-weight: 600;
  }

  .picked {
    flex: 1 0 100%;
    display: flex;
    align-items: center;
    gap: 5px;
    color: var(--muted);
    font-size: 0.7rem;
  }

  .node-icon-button {
    flex: 0 0 auto;
    display: inline-flex;
    padding: 3px;
    border: 1px solid var(--line);
    border-radius: 4px;
    background: transparent;
    color: var(--muted);
    cursor: pointer;
  }

  .node-add {
    align-self: flex-start;
    padding: 4px 10px;
    border: 1px dashed var(--line-strong);
    border-radius: 5px;
    background: transparent;
    color: var(--muted);
    font-size: 0.72rem;
    cursor: pointer;
  }

  .node-empty {
    margin: 0;
    color: var(--muted);
    font-size: 0.72rem;
  }

  .node-ports {
    display: flex;
    flex-direction: column;
    gap: 5px;
    padding: 7px 10px 10px;
    border-top: 1px solid var(--line);
  }

  .port-row {
    display: flex;
    align-items: center;
    justify-content: flex-end;
    gap: 7px;
    font-size: 0.72rem;
    color: var(--muted);
  }

  /* A port is a target, so it is bigger than it looks: the dot is 10px and the hit area is 20. */
  .port {
    width: 10px;
    height: 10px;
    border-radius: 50%;
    border: 2px solid var(--port, var(--muted));
    background: var(--surface);
    cursor: crosshair;
  }

  .port::after {
    content: '';
    position: absolute;
    width: 20px;
    height: 20px;
    margin: -7px 0 0 -7px;
  }

  .port.out {
    position: relative;
    margin-right: -16px;
  }

  .port.in {
    position: absolute;
    left: -6px;
    top: 50%;
    transform: translateY(-50%);
  }

  .port.wired {
    background: var(--port, var(--accent));
  }

  /* Flow in and out, as diamonds, so the order never looks like data. */
  .flow-port {
    position: absolute;
    width: 11px;
    height: 11px;
    top: 13px;
    background: var(--ink);
    transform: rotate(45deg);
  }

  .flow-port.in {
    left: -6px;
  }

  .flow-port.out {
    right: -6px;
  }
</style>
