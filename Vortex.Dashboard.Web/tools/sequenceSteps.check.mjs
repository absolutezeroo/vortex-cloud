// Dragging a block must not quietly break the references inside it.
//
// A filter value of `$N` names an earlier step, and the server refuses one that points at itself or
// forwards. So reordering by array splice produces content that fails validation on save at best,
// and silently matches the wrong step at worst -- which is invisible in any screenshot of the
// editor. This is the part of the drag-and-drop that has to be right.

import assert from 'node:assert/strict';
import { moveStep, moveFilter, referencedStep } from '../src/lib/sequence/steps.ts';

const step = (action, ...values) => ({
  actionCode: action,
  filters: values.map((value) => ({ factKey: 'item', op: 0, value })),
});

// --- reading a reference -------------------------------------------------------------------
assert.equal(referencedStep('$0'), 0);
assert.equal(referencedStep('$12'), 12);
assert.equal(referencedStep('4312'), -1, 'a literal is not a reference');
assert.equal(referencedStep('$'), -1);
assert.equal(referencedStep('$-1'), -1);
assert.equal(referencedStep(''), -1);
assert.equal(referencedStep(undefined), -1);

// --- a reference follows the step it names ---------------------------------------------------
{
  // place, walk-on-$0, pick-up-$0  ->  move the walk to the end
  const steps = [step('place_item'), step('walk_on_furni', '$0'), step('pick_up_item', '$0')];
  const { steps: moved, clearedReferences } = moveStep(steps, 1, 2);

  assert.equal(clearedReferences, 0, 'both references still point backwards');
  assert.equal(moved[0].actionCode, 'place_item');
  assert.equal(moved[1].actionCode, 'pick_up_item');
  assert.equal(moved[2].actionCode, 'walk_on_furni');
  assert.equal(moved[1].filters[0].value, '$0', 'pick-up still names the placement');
  assert.equal(moved[2].filters[0].value, '$0', 'so does the walk');
}

// --- a reference that would point forwards is cleared, not retargeted ------------------------
{
  // Moving the placement to the end leaves both dependants pointing at a step after them.
  const steps = [step('place_item'), step('walk_on_furni', '$0'), step('pick_up_item', '$0')];
  const { steps: moved, clearedReferences } = moveStep(steps, 0, 2);

  assert.equal(clearedReferences, 2, 'both dependants lost their target');
  assert.equal(moved[0].filters[0].value, '', 'cleared rather than silently repointed');
  assert.equal(moved[1].filters[0].value, '');
  assert.equal(moved[2].actionCode, 'place_item');
}

// --- the reference is renumbered when an unrelated step moves in front -----------------------
{
  const steps = [step('place_item'), step('chat_with_someone'), step('walk_on_furni', '$0')];
  const { steps: moved, clearedReferences } = moveStep(steps, 1, 0);

  assert.equal(clearedReferences, 0);
  assert.equal(moved[2].filters[0].value, '$1', 'the placement is now step 1, so the reference is $1');
}

// --- moving a step onto itself changes nothing ----------------------------------------------
{
  const steps = [step('place_item'), step('walk_on_furni', '$0')];

  assert.equal(moveStep(steps, 1, 1).steps, steps, 'no copy, no churn');
  assert.equal(moveStep(steps, 5, 0).steps, steps, 'an impossible move is a no-op');
}

// --- a filter dragged to another step --------------------------------------------------------
{
  const steps = [step('place_item'), step('walk_on_furni', '4312')];
  const { steps: moved, clearedReferences } = moveFilter(steps, 1, 0, 0);

  assert.equal(clearedReferences, 0);
  assert.equal(moved[0].filters.length, 1);
  assert.equal(moved[1].filters.length, 0);
  assert.equal(moved[0].filters[0].value, '4312');
}

// --- a filter dragged in front of the step it references loses the reference -----------------
{
  const steps = [step('place_item'), step('walk_on_furni', '$0')];
  const { steps: moved, clearedReferences } = moveFilter(steps, 1, 0, 0);

  assert.equal(clearedReferences, 1, '$0 cannot be read by step 0 itself');
  assert.equal(moved[0].filters[0].value, '');
}

// --- the original is never mutated -----------------------------------------------------------
{
  const steps = [step('place_item'), step('walk_on_furni', '$0')];
  const before = JSON.stringify(steps);

  moveStep(steps, 0, 1);
  moveFilter(steps, 1, 0, 0);

  assert.equal(JSON.stringify(steps), before, 'drag preview must not edit the draft');
}

console.log('sequence steps: ok');
