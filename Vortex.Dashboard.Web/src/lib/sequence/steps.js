/**
 * Moving blocks in a task's sequence, references and all.
 *
 * A filter's value may be `$N`, naming an earlier step: that is what "the same furniture you just
 * placed" means, and the server refuses a reference that points at itself or forwards. So a
 * sequence cannot be reordered by moving an array element — every `$N` in it has to be rewritten
 * for the new positions, and any reference the move would invalidate has to be dealt with rather
 * than left to fail validation on save.
 *
 * That is the whole reason this file exists and is pure: it is the part of a drag-and-drop editor
 * that can be wrong in a way a screenshot never shows.
 */

/** A filter value of the form `$N`, or -1. */
export function referencedStep(value) {
  if (typeof value !== 'string' || value.length < 2 || value[0] !== '$') return -1;

  const n = Number(value.slice(1));

  return Number.isInteger(n) && n >= 0 ? n : -1;
}

/**
 * Moves one step, rewriting every reference so each still points at the same step it did.
 *
 * A reference that would end up pointing at its own step or at a later one is cleared to a literal
 * rather than silently retargeted: the operator moved a block past the thing it depended on, and
 * quietly repointing it at a different step would be worse than an empty field they can see.
 */
export function moveStep(steps, from, to) {
  if (from === to || from < 0 || to < 0 || from >= steps.length || to >= steps.length) {
    return { steps, clearedReferences: 0 };
  }

  const order = steps.map((_, i) => i);

  order.splice(to, 0, order.splice(from, 1)[0]);

  // oldIndex -> newIndex, so a reference can be rewritten to wherever its step ended up.
  const newIndexOf = new Map(order.map((oldIndex, newIndex) => [oldIndex, newIndex]));
  let clearedReferences = 0;

  const moved = order.map((oldIndex, newIndex) => {
    const step = steps[oldIndex];

    return {
      ...step,
      filters: (step.filters ?? []).map((filter) => {
        const target = referencedStep(filter.value);

        if (target < 0) return { ...filter };

        const retargeted = newIndexOf.get(target);

        // Still earlier than the step that reads it: the reference survives the move.
        if (retargeted !== undefined && retargeted < newIndex) {
          return { ...filter, value: `$${retargeted}` };
        }

        clearedReferences += 1;

        return { ...filter, value: '' };
      }),
    };
  });

  return { steps: moved, clearedReferences };
}

/**
 * Moves one filter from one step to another.
 *
 * The fact it tests belongs to the action it sits under, so a filter dropped on a step whose action
 * does not emit that fact would be refused on save. The caller decides whether the drop is legal;
 * this only performs it, and clears a reference the new position cannot satisfy.
 */
export function moveFilter(steps, fromStep, filterIndex, toStep, toIndex = -1) {
  if (fromStep === toStep && (toIndex === -1 || toIndex === filterIndex)) {
    return { steps, clearedReferences: 0 };
  }

  const filter = steps[fromStep]?.filters?.[filterIndex];

  if (!filter) return { steps, clearedReferences: 0 };

  const target = referencedStep(filter.value);
  const survives = target < 0 || target < toStep;
  const landed = survives ? { ...filter } : { ...filter, value: '' };

  const moved = steps.map((step, i) => {
    let filters = [...(step.filters ?? [])];

    if (i === fromStep) filters.splice(filterIndex, 1);

    if (i === toStep) {
      const at = toIndex < 0 || toIndex > filters.length ? filters.length : toIndex;

      filters.splice(at, 0, landed);
    }

    return { ...step, filters };
  });

  return { steps: moved, clearedReferences: survives ? 0 : 1 };
}

/** Whether a step may carry a filter on this fact, given what its action emits. */
export function stepAccepts(step, factKey, factsFor) {
  return factsFor(step.actionCode).some((fact) => fact.key === factKey);
}
