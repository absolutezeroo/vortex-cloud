<script lang="ts">
  // One action on the canvas: what the player did, and what it records.
  //
  // Its conditions are no longer inside it -- they are their own nodes, wired in. What is left here
  // is the action itself, the order ports, and one output port per fact it records, which is what a
  // later condition can read.
  import { ChevronLeft, ChevronRight, X } from '@lucide/svelte';
  import { portColour } from '../../lib/graph/model';
  import { t } from '../../lib/i18n';

  /**
   * @type {{
   *   node: any, actions: any[], canManage: boolean, selected: boolean, pulling: any,
   *   appliesLit: boolean, last: boolean, onreorder: (delta: number) => void,
   *   candidateFor: (factKey: string) => boolean, usableFor: (factKey: string) => boolean,
   *   onmovestart: (e: PointerEvent) => void, onchange: () => void, onremove: () => void,
   *   onportdown: (factKey: string, e: PointerEvent) => void,
   *   onportup: (factKey: string) => void,
   *   onappliesup: () => void,
   * }}
   */
  let {
    node,
    actions,
    canManage,
    selected,
    pulling,
    appliesLit,
    last,
    candidateFor,
    usableFor,
    onmovestart,
    onchange,
    onremove,
    onreorder,
    onportdown,
    onportup,
    onappliesup,
  } = $props();
</script>

<div class="node" class:selected style:left="{node.x}px" style:top="{node.y}px">
  <header class="node-head" onpointerdown={canManage ? onmovestart : undefined}>
    <!-- The number IS the sequence: where the node sits on the canvas is decoration, and this is
         the only thing the engine reads. So the two ways to change it live on it. -->
    <span class="node-index">{node.index + 1}</span>
    {#if canManage}
      <span class="node-order" onpointerdown={(e) => e.stopPropagation()} role="presentation">
        <button
          type="button"
          disabled={node.index === 0}
          title={$t('rewardTracks.moveEarlier')}
          onclick={() => onreorder(-1)}
        >
          <ChevronLeft size={12} />
        </button>
        <button
          type="button"
          disabled={last}
          title={$t('rewardTracks.moveLater')}
          onclick={() => onreorder(1)}
        >
          <ChevronRight size={12} />
        </button>
      </span>
    {/if}
    <span class="node-title">{node.action}</span>
    {#if canManage}
      <button type="button" class="node-close" title={$t('common.remove')} onclick={onremove}>
        <X size={13} />
      </button>
    {/if}
  </header>

  <!-- The order. Diamonds, so it never looks like data. -->
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

    <!-- Where conditions plug in. It carries a count, so an action with tests on it says so even
         when they have been dragged off to one side of the canvas. -->
    <div class="applies-row">
      <span
        class="port applies"
        class:lit={appliesLit}
        data-port="{node.id}:applies"
        onpointerup={onappliesup}
        role="presentation"
        title={$t('rewardTracks.appliesHint')}
      ></span>
      <span>{$t('rewardTracks.conditionCount', { count: node.filterCount })}</span>
    </div>
  </div>

  {#if node.outputs.length}
    <div class="node-ports">
      {#each node.outputs as port (port.key)}
        {@const usable = usableFor(port.key)}
        <!-- A port is only good for a `$N`, and a `$N` needs a LATER action that reports the same
             fact. Where there is none the port can do nothing, so it says so instead of looking
             like every other one and being dragged for nothing. -->
        <div class="port-row" class:inert={!pulling && !usable}>
          <span>{port.label}</span>
          <span
            class="port out"
            class:candidate={pulling?.from === 'value' && candidateFor(port.key)}
            class:blocked={pulling?.from === 'value' && !candidateFor(port.key)}
            style:--port={portColour(port.kind)}
            data-port="{node.id}:out:{port.key}"
            onpointerdown={(e) => canManage && onportdown(port.key, e)}
            onpointerup={() => onportup(port.key)}
            role="presentation"
            title={usable ? $t('rewardTracks.dragWire') : $t('rewardTracks.portHasNoReader')}
          ></span>
        </div>
      {/each}
    </div>
  {/if}
</div>

<style>
  /* A node is a small panel, and the theme already says what a panel is: a 2px edge, a 5px corner
     and an inner bevel. Inventing a second card style beside it is what made this look like a
     different application bolted onto the dashboard. */
  .node {
    position: absolute;
    width: 300px;
    border: 2px solid var(--line-strong);
    border-radius: 5px;
    background: var(--surface);
    box-shadow: var(--panel-shadow);
    user-select: none;
  }

  .node.selected {
    border-color: var(--gold);
    box-shadow: var(--panel-shadow), 0 0 0 1px rgba(var(--gold-rgb), 0.35);
  }

  .node-head {
    display: flex;
    align-items: center;
    gap: 8px;
    padding: 7px 10px;
    background: var(--surface-strong);
    border-bottom: 2px solid var(--line-strong);
    border-left: 3px solid var(--accent);
    border-radius: 3px 3px 0 0;
    cursor: grab;
    font-weight: 700;
    font-size: 0.78rem;
  }

  .node-head:active {
    cursor: grabbing;
  }

  /* The step number, as the same gold tag the tables use for a premium mark. */
  .node-index {
    flex: 0 0 auto;
    min-width: 18px;
    padding: 0 5px;
    text-align: center;
    border: 1px solid rgba(var(--gold-rgb), 0.45);
    border-radius: 4px;
    background: var(--gold-soft);
    color: var(--gold);
    font-size: 0.68rem;
  }

  .node-order {
    flex: 0 0 auto;
    display: inline-flex;
    gap: 1px;
  }

  .node-order button {
    display: inline-flex;
    padding: 1px;
    border: 0;
    border-radius: 3px;
    background: transparent;
    color: var(--muted);
    cursor: pointer;
  }

  .node-order button:hover:not(:disabled) {
    background: var(--surface-hover);
    color: var(--gold);
  }

  .node-order button:disabled {
    opacity: 0.25;
    cursor: default;
  }

  .node-title {
    flex: 1 1 auto;
    overflow: hidden;
    text-overflow: ellipsis;
    white-space: nowrap;
    color: var(--ink);
  }

  .node-close {
    flex: 0 0 auto;
    display: inline-flex;
    padding: 2px;
    border: 0;
    border-radius: 3px;
    background: transparent;
    color: var(--muted);
    cursor: pointer;
  }

  .node-body {
    display: flex;
    flex-direction: column;
    gap: 8px;
    padding: 9px 10px;
  }

  .node-field {
    display: flex;
    flex-direction: column;
    gap: 3px;
    font-size: 0.7rem;
    color: var(--muted);
  }

  .node-field select {
    width: 100%;
  }

  .applies-row {
    display: flex;
    align-items: center;
    gap: 7px;
    color: var(--muted);
    font-size: 0.7rem;
  }

  .node-ports {
    display: flex;
    flex-direction: column;
    gap: 5px;
    padding: 7px 10px 10px;
    border-top: 2px solid var(--line);
    background: var(--surface-strong);
    border-radius: 0 0 3px 3px;
  }

  .port-row {
    display: flex;
    align-items: center;
    justify-content: flex-end;
    gap: 7px;
    font-size: 0.72rem;
    color: var(--muted);
  }

  .port-row.inert {
    opacity: 0.4;
  }

  .port {
    width: 10px;
    height: 10px;
    border-radius: 50%;
    border: 2px solid var(--port, var(--muted));
    background: var(--surface);
    cursor: crosshair;
  }

  .port.out {
    position: relative;
    margin-right: -16px;
  }

  .port.applies {
    margin-left: -16px;
    border-color: var(--accent);
  }

  .port.applies.lit {
    background: var(--accent-soft);
    box-shadow: 0 0 0 4px rgba(var(--accent-rgb), 0.22);
  }

  /* While a wire is out, every port says whether it can take it. This is the single thing that
     turns "drag onto the right dot" from a guess into something you can see. */
  .port.candidate {
    border-color: var(--gold);
    background: var(--gold-soft);
    box-shadow: 0 0 0 4px rgba(var(--gold-rgb), 0.22);
  }

  .port.blocked {
    opacity: 0.25;
  }

  .flow-port {
    position: absolute;
    width: 11px;
    height: 11px;
    top: 13px;
    border: 2px solid var(--line-strong);
    background: var(--gold);
    transform: rotate(45deg);
  }

  .flow-port.in {
    left: -6px;
  }

  .flow-port.out {
    right: -6px;
  }
</style>
