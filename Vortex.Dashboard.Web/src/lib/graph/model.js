/**
 * The sequence, seen as a graph.
 *
 * This is not decoration over a list. A task's sequence already has two kinds of edge in it, and
 * both are invisible in a stack of rows:
 *
 *   - the **order**: step 0 must be satisfied before step 1, which is a flow edge;
 *   - a filter value of `$N`, which names an earlier step and reads the value that step recorded
 *     for the same fact. That is literally a wire from one action's output to one condition's
 *     input, and "the same furniture you just placed" is the wire being there.
 *
 * So a node graph is the honest shape, and this module is the conversion both ways. Everything
 * here is pure: the canvas can be wrong about pixels, but it cannot be wrong about what a wire
 * means, because it never decides that.
 */

/** A wire's colour follows the fact it carries, so the same kind reads the same across the canvas. */
export const PORT_COLOURS = {
  RoomId: '#6cb2d1',
  FurnitureId: '#ffc21c',
  PlayerId: '#63c39d',
  OfferId: '#be96f5',
  BadgeCode: '#e37c88',
  CategoryId: '#68d8ec',
  Text: '#b8c6d6',
  Number: '#dbb15e',
  Enum: '#5ad89a',
  OpaqueId: '#8d9096',
};

export function portColour(kind) {
  return PORT_COLOURS[kind] ?? PORT_COLOURS.OpaqueId;
}

/**
 * Reads `$N`, the only reference syntax the engine has.
 *
 * @returns the step index, or -1 for a literal.
 */
export function referencedStep(value) {
  if (typeof value !== 'string' || value.length < 2 || value[0] !== '$') return -1;

  const n = Number(value.slice(1));

  return Number.isInteger(n) && n >= 0 ? n : -1;
}

/**
 * Turns the steps into nodes and wires.
 *
 * One node per step, in order. A node's output ports are the facts its action emits — that is what
 * a later condition can be wired to. Its input ports are its own filters, because a filter is the
 * thing that consumes a value.
 *
 * Positions are laid out left to right when a step has none stored, so an existing task opens as a
 * readable chain rather than a pile at the origin.
 */
export function toGraph(steps, factsFor, layout = {}) {
  const nodes = (steps ?? []).map((step, index) => ({
    id: index,
    step,
    action: step.actionCode,
    x: layout[index]?.x ?? 80 + index * 320,
    y: layout[index]?.y ?? 80 + (index % 2) * 60,
    outputs: factsFor(step.actionCode).map((fact) => ({
      key: fact.key,
      label: fact.fallbackLabel,
      kind: fact.kind,
    })),
    filters: (step.filters ?? []).map((filter, filterIndex) => ({
      index: filterIndex,
      filter,
      wiredTo: referencedStep(filter.value),
    })),
  }));

  const wires = [];

  for (const node of nodes) {
    // The flow edge: this action follows the previous one. It is the order, drawn.
    if (node.id > 0) {
      wires.push({ kind: 'flow', from: node.id - 1, to: node.id });
    }

    for (const entry of node.filters) {
      if (entry.wiredTo >= 0) {
        wires.push({
          kind: 'data',
          from: entry.wiredTo,
          to: node.id,
          fact: entry.filter.factKey,
          filterIndex: entry.index,
        });
      }
    }
  }

  return { nodes, wires };
}

/**
 * Whether a wire may be drawn from one node's fact port to a filter on another node.
 *
 * Two rules, both the engine's: a reference resolves to what an **earlier** step recorded, and it
 * resolves for the **same fact key** — `$0` on a filter about furniture reads step 0's furniture,
 * not its room. A canvas that let an operator draw either of those would be drawing a filter the
 * server refuses.
 */
export function canWire(fromNode, toNode, factKey, filter) {
  if (fromNode >= toNode) return false;

  return filter.factKey === factKey;
}

/** Draws the wire: the filter now reads that step's value for its own fact. */
export function connect(steps, fromNode, toNode, filterIndex) {
  return steps.map((step, index) =>
    index === toNode
      ? {
          ...step,
          filters: step.filters.map((filter, i) =>
            i === filterIndex ? { ...filter, value: `$${fromNode}` } : filter
          ),
        }
      : step
  );
}

/** Cuts it: the filter goes back to comparing a literal, which is an empty field to fill. */
export function disconnect(steps, toNode, filterIndex) {
  return connect(steps, -1, toNode, filterIndex).map((step, index) =>
    index === toNode
      ? {
          ...step,
          filters: step.filters.map((filter, i) =>
            i === filterIndex ? { ...filter, value: '' } : filter
          ),
        }
      : step
  );
}
