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

/** One fact an action records, as the catalogue describes it. */
export type Fact = {
  key: string;
  /** Translation key; the fallback is shown when the dictionary has no entry for it. */
  labelKey?: string;
  fallbackLabel: string;
  kind: string;
};

/** One test on a step. `value` holding `$N` is a wire rather than a literal. */
export type Filter = { factKey: string; value: string };

/** One step of a task's sequence. */
export type Step = { actionCode: string; filters?: Filter[] };

/** Where the operator dragged each node, keyed by node id. */
export type Layout = Record<string, { x: number; y: number } | undefined>;

export type ActionNode = {
  type: 'action';
  id: string;
  step: Step;
  index: number;
  action: string;
  x: number;
  y: number;
  outputs: { key: string; label: string; kind: string }[];
  filterCount: number;
};

export type ConditionNode = {
  type: 'condition';
  id: string;
  step: Step;
  index: number;
  filterIndex: number;
  filter: Filter;
  action: string;
  fact: string;
  /** The step a `$N` reads from, or -1 for a literal. */
  wiredTo: number;
  x: number;
  y: number;
};

export type GraphNode = ActionNode | ConditionNode;

/** flow = the order, applies = what a condition constrains, data = a `$N` reference. */
export type Wire = {
  kind: 'flow' | 'applies' | 'data';
  from: string;
  to: string;
  fact?: string;
};

/** A wire's colour follows the fact it carries, so the same kind reads the same across the canvas. */
export const PORT_COLOURS: Record<string, string> = {
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

export function portColour(kind: string): string {
  return PORT_COLOURS[kind] ?? PORT_COLOURS.OpaqueId;
}

/**
 * Reads `$N`, the only reference syntax the engine has.
 *
 * @returns the step index, or -1 for a literal.
 */
export function referencedStep(value: unknown): number {
  if (typeof value !== 'string' || value.length < 2 || value[0] !== '$') return -1;

  const n = Number(value.slice(1));

  return Number.isInteger(n) && n >= 0 ? n : -1;
}

/**
 * Turns the steps into nodes and wires.
 *
 * Two kinds of node, because a task has two kinds of thing in it:
 *
 *   - an **action** node per step, whose output ports are the facts it records;
 *   - a **condition** node per filter, which is a module of its own rather than a row buried in an
 *     action. That is what lets one be dragged from the palette, moved between actions, and read
 *     at a glance -- a filter is a test somebody wrote, not a property of the action.
 *
 * And three kinds of wire: the order (flow), which action a condition constrains (applies), and a
 * `$N` reference reading an earlier action's recorded value (data).
 *
 * Positions are laid out left to right when nothing is stored, so an existing task opens as a
 * readable chain rather than a pile at the origin.
 */
export function toGraph(
  steps: Step[] | null | undefined,
  factsFor: (actionCode: string) => Fact[],
  layout: Layout = {},
): { nodes: GraphNode[]; wires: Wire[] } {
  const nodes: GraphNode[] = [];
  const wires: Wire[] = [];

  (steps ?? []).forEach((step, index) => {
    nodes.push({
      type: 'action',
      id: `a:${index}`,
      step,
      index,
      action: step.actionCode,
      x: layout[`a:${index}`]?.x ?? 80 + index * 420,
      y: layout[`a:${index}`]?.y ?? 80,
      outputs: factsFor(step.actionCode).map((fact) => ({
        key: fact.key,
        label: fact.fallbackLabel,
        kind: fact.kind,
      })),
      filterCount: (step.filters ?? []).length,
    });

    // The order, drawn.
    if (index > 0) {
      wires.push({ kind: 'flow', from: `a:${index - 1}`, to: `a:${index}` });
    }

    (step.filters ?? []).forEach((filter, filterIndex) => {
      const id = `c:${index}:${filterIndex}`;

      nodes.push({
        type: 'condition',
        id,
        step,
        index,
        filterIndex,
        filter,
        action: step.actionCode,
        fact: filter.factKey,
        wiredTo: referencedStep(filter.value),
        x: layout[id]?.x ?? 80 + index * 420 + 60,
        y: layout[id]?.y ?? 300 + filterIndex * 150,
      });

      // Which action this condition constrains. Not decoration: it is the only thing that says
      // whose signal the test is applied to, and dragging this wire is how a condition moves.
      wires.push({ kind: 'applies', from: id, to: `a:${index}`, fact: filter.factKey });

      // And the $N reference, if the value is read from an earlier action rather than typed.
      if (referencedStep(filter.value) >= 0) {
        wires.push({
          kind: 'data',
          from: `a:${referencedStep(filter.value)}`,
          to: id,
          fact: filter.factKey,
        });
      }
    });
  });

  return { nodes, wires };
}

/** The action a node belongs to, whichever kind it is. */
export function stepOf(nodeId: string): number {
  const [, index] = nodeId.split(':');

  return Number(index);
}

/**
 * The first step after `fromIndex` that records `factKey`.
 *
 * A fact port is only ever good for a `$N`, and `$N` is read by a condition on a LATER step that
 * tests the SAME fact. So this is the whole answer to "what is this port for": that step, or
 * nothing at all -- a port with no reader can do nothing, and the canvas has to show that rather
 * than let it be dragged for no result.
 *
 * @returns the step index, or -1.
 */
export function readerOf(
  steps: Step[] | null | undefined,
  fromIndex: number,
  factKey: string,
  factsFor: (actionCode: string) => Fact[],
): number {
  const found = (steps ?? []).findIndex(
    (step, i) => i > fromIndex && factsFor(step.actionCode).some((fact) => fact.key === factKey)
  );

  return found;
}

/**
 * Whether a `$N` wire may be drawn from one action's fact port to a condition.
 *
 * Two rules, both the engine's: a reference resolves to what an **earlier** step recorded, and it
 * resolves for the **same fact key** -- `$0` on a condition about furniture reads step 0's
 * furniture, not its room. A canvas that let an operator draw either would be drawing a filter the
 * server refuses.
 */
export function canWire(
  fromNode: number,
  toNode: number,
  factKey: string,
  filter: Filter,
): boolean {
  if (fromNode >= toNode) return false;

  return filter.factKey === factKey;
}

/** Draws the wire: the filter now reads that step's value for its own fact. */
export function connect(
  steps: Step[],
  fromNode: number,
  toNode: number,
  filterIndex: number,
): Step[] {
  return steps.map((step, index) =>
    index === toNode
      ? {
          ...step,
          filters: (step.filters ?? []).map((filter, i) =>
            i === filterIndex ? { ...filter, value: `$${fromNode}` } : filter,
          ),
        }
      : step
  );
}

/** Cuts it: the filter goes back to comparing a literal, which is an empty field to fill. */
export function disconnect(steps: Step[], toNode: number, filterIndex: number): Step[] {
  return connect(steps, -1, toNode, filterIndex).map((step, index) =>
    index === toNode
      ? {
          ...step,
          filters: (step.filters ?? []).map((filter, i) =>
            i === filterIndex ? { ...filter, value: '' } : filter,
          ),
        }
      : step
  );
}
