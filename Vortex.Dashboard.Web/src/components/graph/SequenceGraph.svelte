<script>
  // The canvas: pan, zoom, nodes you place, and wires you draw between them.
  //
  // What a wire means lives in lib/graph/model.js and is checked there. This file is only the
  // surface: where things are, what the cursor is doing, and which port it is over. It decides
  // nothing about validity -- it asks canWire and refuses to draw what the engine would refuse.
  import ActionNode from './ActionNode.svelte';
  import GraphWires from './GraphWires.svelte';
  import NodePalette from './NodePalette.svelte';
  import { canWire, connect, disconnect, toGraph } from '../../lib/graph/model.js';
  import { moveStep } from '../../lib/sequence/steps.js';
  import { t } from '../../lib/i18n.js';

  /**
   * @type {{
   *   steps: any[], actions: any[], canManage: boolean,
   *   factsFor: (a: string) => any[], operatorsFor: (a: string, k: string) => any[],
   *   defaultFilterValue: (a: string, k: string) => string,
   *   pickerFor: (meta: any) => string | null, pickedLabels: Record<string, any>,
   *   onchange: (steps: any[]) => void,
   *   onpick: (stepIndex: number, filterIndex: number, kind: string) => void,
   * }}
   */
  let {
    steps,
    actions,
    canManage,
    factsFor,
    operatorsFor,
    defaultFilterValue,
    pickerFor,
    pickedLabels,
    onchange,
    onpick,
  } = $props();

  // Where each node sits. Kept here rather than on the task: a position is how one operator likes
  // to look at a sequence, not something the hotel should store or another operator inherit.
  let layout = $state({});
  let pan = $state({ x: 0, y: 0 });
  let zoom = $state(1);
  let selected = $state(null);
  let pulling = $state(null);
  let ports = $state(0); // bumped to re-measure after anything that moves a port

  let canvasEl;
  let graph = $derived(toGraph(steps, factsFor, layout));

  /** Where a port ended up, in canvas coordinates. Measured, because node height varies. */
  function portAt(id) {
    void ports;

    if (!canvasEl) return null;

    const el = canvasEl.querySelector(`[data-port="${id}"]`);

    if (!el) return null;

    const box = el.getBoundingClientRect();
    const frame = canvasEl.getBoundingClientRect();

    return {
      x: (box.left + box.width / 2 - frame.left - pan.x) / zoom,
      y: (box.top + box.height / 2 - frame.top - pan.y) / zoom,
    };
  }

  function geometry(wire) {
    const from =
      wire.kind === 'flow' ? portAt(`${wire.from}:flow-out`) : portAt(`${wire.from}:out:${wire.fact}`);
    const to =
      wire.kind === 'flow' ? portAt(`${wire.to}:flow-in`) : portAt(`${wire.to}:in:${wire.filterIndex}`);

    if (!from || !to) return null;

    const kind = graph.nodes[wire.from]?.outputs.find((p) => p.key === wire.fact)?.kind;

    return { from, to, kind };
  }

  function toCanvas(event) {
    const frame = canvasEl.getBoundingClientRect();

    return {
      x: (event.clientX - frame.left - pan.x) / zoom,
      y: (event.clientY - frame.top - pan.y) / zoom,
    };
  }

  // --- moving a node ---------------------------------------------------------------------------

  function startMove(id, event) {
    event.preventDefault();
    selected = id;

    const origin = toCanvas(event);
    const start = { x: graph.nodes[id].x, y: graph.nodes[id].y };

    function move(e) {
      const at = toCanvas(e);

      layout = {
        ...layout,
        [id]: { x: start.x + (at.x - origin.x), y: start.y + (at.y - origin.y) },
      };
      ports += 1;
    }

    function up() {
      window.removeEventListener('pointermove', move);
      window.removeEventListener('pointerup', up);
    }

    window.addEventListener('pointermove', move);
    window.addEventListener('pointerup', up);
  }

  // --- drawing a wire --------------------------------------------------------------------------

  function startWire(nodeId, factKey, event) {
    event.preventDefault();
    event.stopPropagation();

    const from = portAt(`${nodeId}:out:${factKey}`);

    if (!from) return;

    pulling = { node: nodeId, fact: factKey, from, to: from };

    function move(e) {
      pulling = { ...pulling, to: toCanvas(e) };
    }

    function up() {
      window.removeEventListener('pointermove', move);
      window.removeEventListener('pointerup', up);
      // Dropped on nothing: the wire simply is not made. No error, no dangling state.
      pulling = null;
    }

    window.addEventListener('pointermove', move);
    window.addEventListener('pointerup', up);
  }

  /** A wire landed on a condition's input. The engine's two rules decide whether it holds. */
  function finishWire(toNode, filterIndex) {
    if (!pulling) return;

    const filter = steps[toNode]?.filters?.[filterIndex];

    if (filter && canWire(pulling.node, toNode, pulling.fact, filter)) {
      onchange(connect(steps, pulling.node, toNode, filterIndex));
      ports += 1;
    }

    pulling = null;
  }

  // --- the canvas itself -----------------------------------------------------------------------

  function startPan(event) {
    // Middle button, or the background with the left one: the two ways every node editor pans.
    if (event.button !== 1 && event.target !== canvasEl && !event.target.classList.contains('grid'))
      return;

    event.preventDefault();
    selected = null;

    const origin = { x: event.clientX - pan.x, y: event.clientY - pan.y };

    function move(e) {
      pan = { x: e.clientX - origin.x, y: e.clientY - origin.y };
    }

    function up() {
      window.removeEventListener('pointermove', move);
      window.removeEventListener('pointerup', up);
    }

    window.addEventListener('pointermove', move);
    window.addEventListener('pointerup', up);
  }

  function onwheel(event) {
    event.preventDefault();

    const next = Math.min(2, Math.max(0.4, zoom * (event.deltaY < 0 ? 1.1 : 1 / 1.1)));
    const frame = canvasEl.getBoundingClientRect();
    const cx = event.clientX - frame.left;
    const cy = event.clientY - frame.top;

    // Zoom towards the cursor rather than the origin, so the thing being looked at stays put.
    pan = {
      x: cx - ((cx - pan.x) / zoom) * next,
      y: cy - ((cy - pan.y) / zoom) * next,
    };
    zoom = next;
    ports += 1;
  }

  function addAction(name) {
    onchange([...steps, { actionCode: name, filters: [] }]);
    ports += 1;
  }

  function removeNode(id) {
    // Removing a step renumbers everything after it, and a `$N` pointing past the gap would then
    // name the wrong action. moveStep already knows how to keep references honest, so the removal
    // is done by moving the node to the end and dropping it.
    const { steps: shuffled } = moveStep(steps, id, steps.length - 1);

    onchange(shuffled.slice(0, -1));
    selected = null;
    ports += 1;
  }
</script>

<div class="graph">
  <NodePalette {actions} {canManage} onadd={addAction} />

  <div
    class="canvas"
    bind:this={canvasEl}
    onpointerdown={startPan}
    onwheel={onwheel}
    role="presentation"
  >
    <div class="grid" style:background-position="{pan.x}px {pan.y}px" style:--cell="{24 * zoom}px"></div>

    <div class="viewport" style:transform="translate({pan.x}px, {pan.y}px) scale({zoom})">
      <GraphWires wires={graph.wires} {geometry} dragging={pulling} />

      {#each graph.nodes as node (node.id)}
        <ActionNode
          {node}
          {actions}
          {canManage}
          {factsFor}
          {operatorsFor}
          {defaultFilterValue}
          {pickerFor}
          {pickedLabels}
          selected={selected === node.id}
          onmovestart={(e) => startMove(node.id, e)}
          onchange={() => {
            onchange(steps);
            ports += 1;
          }}
          onremove={() => removeNode(node.id)}
          onaddfilter={() => {
            const first = factsFor(node.action)[0];

            node.step.filters = [
              ...(node.step.filters ?? []),
              {
                factKey: first.key,
                op: operatorsFor(node.action, first.key)[0]?.value ?? 0,
                value: defaultFilterValue(node.action, first.key),
              },
            ];
            onchange(steps);
            ports += 1;
          }}
          onremovefilter={(i) => {
            node.step.filters.splice(i, 1);
            onchange(steps);
            ports += 1;
          }}
          onpick={(i, kind) => onpick(node.id, i, kind)}
          onportdown={(factKey, e) => startWire(node.id, factKey, e)}
          oninputup={(i) => finishWire(node.id, i)}
          oncut={(i) => {
            onchange(disconnect(steps, node.id, i));
            ports += 1;
          }}
        />
      {/each}
    </div>

    <div class="hints">
      <span>{$t('rewardTracks.graphHintPan')}</span>
      <span>{$t('rewardTracks.graphHintWire')}</span>
      <span class="zoom">{Math.round(zoom * 100)}%</span>
    </div>
  </div>
</div>

<style>
  .graph {
    display: flex;
    height: 100%;
    min-height: 0;
  }

  .canvas {
    position: relative;
    flex: 1 1 auto;
    overflow: hidden;
    background: var(--page);
    cursor: grab;
  }

  .canvas:active {
    cursor: grabbing;
  }

  /* The grid is the thing that tells you the canvas moved. It scrolls with the pan and scales with
     the zoom, which is the whole reason it is a background rather than an element. */
  .grid {
    position: absolute;
    inset: 0;
    background-image:
      linear-gradient(to right, rgba(255, 255, 255, 0.045) 1px, transparent 1px),
      linear-gradient(to bottom, rgba(255, 255, 255, 0.045) 1px, transparent 1px);
    background-size: var(--cell) var(--cell);
  }

  .viewport {
    position: absolute;
    inset: 0;
    transform-origin: 0 0;
  }

  .hints {
    position: absolute;
    right: 12px;
    bottom: 10px;
    display: flex;
    gap: 14px;
    align-items: center;
    color: var(--muted);
    font-size: 0.7rem;
    pointer-events: none;
  }

  .zoom {
    padding: 2px 7px;
    border: 1px solid var(--line-strong);
    border-radius: 4px;
    background: var(--surface);
  }
</style>
