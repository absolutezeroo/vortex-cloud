// A wire has to mean what the engine will read.
//
// The canvas can be wrong about pixels and somebody will see it. It cannot be wrong about what a
// wire means: `$N` resolves to what an EARLIER step recorded FOR THE SAME FACT, and a wire that
// breaks either rule produces content the server refuses -- or worse, one it accepts and that never
// matches. So the meaning lives in a pure module and is checked here.

import assert from 'node:assert/strict';
import {
  canWire,
  connect,
  disconnect,
  readerOf,
  referencedStep,
  toGraph,
} from '../src/lib/graph/model.js';

const FACTS = {
  place_item: [
    { key: 'item', kind: 'OpaqueId', fallbackLabel: 'The furniture' },
    { key: 'room', kind: 'RoomId', fallbackLabel: 'Room' },
  ],
  walk_on_furni: [
    { key: 'item', kind: 'OpaqueId', fallbackLabel: 'The furniture' },
    { key: 'room', kind: 'RoomId', fallbackLabel: 'Room' },
  ],
  chat_with_someone: [{ key: 'room', kind: 'RoomId', fallbackLabel: 'Room' }],
};

const factsFor = (action) => FACTS[action] ?? [];
const step = (action, ...values) => ({
  actionCode: action,
  filters: values.map((value) => ({ factKey: 'item', op: 0, value })),
});

// --- the order is an edge --------------------------------------------------------------------
{
  const { nodes, wires } = toGraph([step('place_item'), step('walk_on_furni')], factsFor);

  assert.equal(nodes.filter((n) => n.type === 'action').length, 2);
  assert.deepEqual(
    wires.filter((w) => w.kind === 'flow'),
    [{ kind: 'flow', from: 'a:0', to: 'a:1' }],
    'the second action follows the first, and that is a wire'
  );
}

// --- a condition is a node of its own, attached to the action it constrains -------------------
{
  const { nodes, wires } = toGraph([step('place_item', '4312')], factsFor);
  const condition = nodes.find((n) => n.type === 'condition');

  assert.ok(condition, 'a filter is a module on the canvas, not a row inside the action');
  assert.equal(condition.id, 'c:0:0');
  assert.deepEqual(
    wires.filter((w) => w.kind === 'applies'),
    [{ kind: 'applies', from: 'c:0:0', to: 'a:0', fact: 'item' }],
    'and the wire is what says which action it tests'
  );
}

// --- a $N reference is a data wire -----------------------------------------------------------
{
  const { wires } = toGraph([step('place_item'), step('walk_on_furni', '$0')], factsFor);
  const data = wires.filter((w) => w.kind === 'data');

  assert.equal(data.length, 1);
  assert.deepEqual(data[0], { kind: 'data', from: 'a:0', to: 'c:1:0', fact: 'item' });
}

// --- a literal is not a wire -----------------------------------------------------------------
{
  const { wires } = toGraph([step('place_item'), step('walk_on_furni', '4312')], factsFor);

  assert.equal(wires.filter((w) => w.kind === 'data').length, 0);
  assert.equal(referencedStep('4312'), -1);
}

// --- ports are the facts the action emits ----------------------------------------------------
{
  const { nodes } = toGraph([step('chat_with_someone')], factsFor);

  assert.deepEqual(
    nodes.find((n) => n.type === 'action').outputs.map((p) => p.key),
    ['room'],
    'chat emits a room and nothing else, so it offers one port'
  );
}

// --- the two rules a wire must obey ----------------------------------------------------------
{
  const filter = { factKey: 'item', op: 0, value: '' };

  assert.equal(canWire(0, 1, 'item', filter), true);
  assert.equal(canWire(1, 0, 'item', filter), false, 'a reference cannot point forwards');
  assert.equal(canWire(1, 1, 'item', filter), false, 'nor at its own step');
  assert.equal(
    canWire(0, 1, 'room', filter),
    false,
    '$N reads the same fact key: a furniture filter cannot read a room port'
  );
}

// --- a port with no reader can do nothing ------------------------------------------------------
//
// This is what a fact port is FOR: a later step that tests the same fact. Get it wrong and the
// canvas either offers a drag that produces nothing, or refuses one that would have worked.
{
  const steps = [step('place_item'), step('chat_with_someone'), step('walk_on_furni')];

  assert.equal(readerOf(steps, 0, 'item', factsFor), 2, 'the first LATER step that reports it');
  assert.equal(readerOf(steps, 0, 'room', factsFor), 1, 'chat reports a room, so it is the reader');
  assert.equal(readerOf(steps, 2, 'item', factsFor), -1, 'nothing follows the last step');
  assert.equal(
    readerOf([step('place_item'), step('chat_with_someone')], 0, 'item', factsFor),
    -1,
    'a step that never reports the fact is not a reader'
  );
}

// --- drawing and cutting ---------------------------------------------------------------------
{
  const steps = [step('place_item'), step('walk_on_furni', '')];
  const wired = connect(steps, 0, 1, 0);

  assert.equal(wired[1].filters[0].value, '$0');
  assert.equal(steps[1].filters[0].value, '', 'the original is untouched');

  const cut = disconnect(wired, 1, 0);

  assert.equal(cut[1].filters[0].value, '', 'cutting leaves a literal to fill, not a dangling ref');
}

// --- layout is a hint, never a meaning -------------------------------------------------------
{
  const steps = [step('place_item'), step('walk_on_furni', '$0')];
  const a = toGraph(steps, factsFor);
  const b = toGraph(steps, factsFor, { 'a:0': { x: 999, y: 999 } });

  assert.equal(b.nodes[0].x, 999, 'a stored position is used');
  assert.deepEqual(a.wires, b.wires, 'moving a node changes no wire');
}

console.log('graph model: ok');
