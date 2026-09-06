<script>
  // The sequence as blocks you pick up, line up and clip together.
  //
  // Reordering is not cosmetic here: a filter value of `$N` names an earlier step, and the server
  // refuses one that points at itself or forwards. So every drop runs through lib/sequence/steps.js,
  // which rewrites the references for the new positions and clears the ones the move invalidates --
  // and says how many it cleared, so the operator is told rather than finding out on save.
  //
  // Native HTML5 drag rather than a library: it is four handlers, it gives keyboard-free
  // drag out of the box, and the repository ships no drag dependency to reuse.
  import ActionBlock from './ActionBlock.svelte';
  import FilterBlock from './FilterBlock.svelte';
  import { moveFilter, moveStep, stepAccepts } from '../../lib/sequence/steps.js';
  import { t } from '../../lib/i18n.js';

  /**
   * @type {{
   *   steps: any[],
   *   actions: any[],
   *   canManage: boolean,
   *   factsFor: (action: string) => any[],
   *   operatorsFor: (action: string, factKey: string) => any[],
   *   referencesFor: (steps: any[], index: number, factKey: string) => any[],
   *   defaultFilterValue: (action: string, factKey: string) => string,
   *   pickerFor: (meta: any) => string | null,
   *   pickedLabels: Record<string, any>,
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
    referencesFor,
    defaultFilterValue,
    pickerFor,
    pickedLabels,
    onchange,
    onpick,
  } = $props();

  // What is in the hand, and where the gap is currently open.
  let carrying = $state(null);
  let dropTarget = $state(null);
  let notice = $state('');

  function beginStep(index) {
    carrying = { type: 'step', index };
    notice = '';
  }

  function beginFilter(stepIndex, filterIndex) {
    carrying = { type: 'filter', stepIndex, filterIndex };
    notice = '';
  }

  function end() {
    carrying = null;
    dropTarget = null;
  }

  /** A filter can only land under an action that emits the fact it tests. */
  function accepts(target) {
    if (!carrying) return false;

    if (carrying.type === 'step') return target.type === 'step';

    if (target.type !== 'filter-slot') return false;

    const filter = steps[carrying.stepIndex]?.filters?.[carrying.filterIndex];

    return Boolean(filter) && stepAccepts(steps[target.stepIndex], filter.factKey, factsFor);
  }

  function over(event, target) {
    if (!accepts(target)) return;

    event.preventDefault();
    dropTarget = target;
  }

  function drop(event, target) {
    if (!accepts(target)) return;

    event.preventDefault();

    const result =
      carrying.type === 'step'
        ? moveStep(steps, carrying.index, target.index)
        : moveFilter(steps, carrying.stepIndex, carrying.filterIndex, target.stepIndex);

    if (result.steps !== steps) onchange(result.steps);

    notice =
      result.clearedReferences > 0
        ? $t('rewardTracks.referencesCleared', { count: result.clearedReferences })
        : '';

    end();
  }

  function isOpen(target) {
    return (
      dropTarget !== null &&
      dropTarget.type === target.type &&
      dropTarget.index === target.index &&
      dropTarget.stepIndex === target.stepIndex
    );
  }
</script>

<div class="sequence" role="list">
  {#each steps as step, stepIndex (stepIndex)}
    <!-- The gap that opens above each block: the drop point, and the only thing that moves while
         the cursor travels. -->
    <div
      class="slot"
      class:open={isOpen({ type: 'step', index: stepIndex, stepIndex: undefined })}
      ondragover={(e) => over(e, { type: 'step', index: stepIndex })}
      ondrop={(e) => drop(e, { type: 'step', index: stepIndex })}
      role="presentation"
    ></div>

    <ActionBlock
      {step}
      index={stepIndex}
      {actions}
      {canManage}
      removable={steps.length > 1}
      dragging={carrying?.type === 'step' && carrying.index === stepIndex}
      ondragstart={() => beginStep(stepIndex)}
      ondragend={end}
      onactionchange={() => {
        step.filters = [];
        onchange(steps);
      }}
      onremove={() => {
        onchange(steps.filter((_, i) => i !== stepIndex));
      }}
    >
      <div
        class="filter-slot"
        class:open={isOpen({ type: 'filter-slot', stepIndex, index: undefined })}
        ondragover={(e) => over(e, { type: 'filter-slot', stepIndex })}
        ondrop={(e) => drop(e, { type: 'filter-slot', stepIndex })}
        role="list"
      >
        {#each step.filters ?? [] as filter, filterIndex (filterIndex)}
          <FilterBlock
            {filter}
            {canManage}
            facts={factsFor(step.actionCode)}
            operators={operatorsFor(step.actionCode, filter.factKey)}
            references={referencesFor(steps, stepIndex, filter.factKey)}
            picked={pickedLabels[`${stepIndex}:${filterIndex}`]}
            picker={pickerFor(
              factsFor(step.actionCode).find((f) => f.key === filter.factKey) ?? null
            )}
            dragging={carrying?.type === 'filter' &&
              carrying.stepIndex === stepIndex &&
              carrying.filterIndex === filterIndex}
            onfactchange={(key) => {
              filter.value = defaultFilterValue(step.actionCode, key);
              filter.op = operatorsFor(step.actionCode, key)[0]?.value ?? 0;
              onchange(steps);
            }}
            onpick={() =>
              onpick(
                stepIndex,
                filterIndex,
                pickerFor(factsFor(step.actionCode).find((f) => f.key === filter.factKey))
              )}
            onremove={() => {
              step.filters.splice(filterIndex, 1);
              onchange(steps);
            }}
            ondragstart={() => beginFilter(stepIndex, filterIndex)}
            ondragend={end}
          />
        {/each}

        {#if factsFor(step.actionCode).length > 0}
          <button
            type="button"
            class="ghost-button block-add block-add--filter"
            disabled={!canManage}
            onclick={() => {
              const first = factsFor(step.actionCode)[0];

              step.filters = [
                ...(step.filters ?? []),
                {
                  factKey: first.key,
                  op: operatorsFor(step.actionCode, first.key)[0]?.value ?? 0,
                  value: defaultFilterValue(step.actionCode, first.key),
                },
              ];
              onchange(steps);
            }}
          >
            {$t('rewardTracks.addFilter')}
          </button>
        {:else}
          <p class="muted small no-facts">{$t('rewardTracks.actionHasNoFacts')}</p>
        {/if}
      </div>
    </ActionBlock>
  {/each}

  <!-- The slot after the last block, so a step can be dropped at the end. -->
  <div
    class="slot"
    class:open={isOpen({ type: 'step', index: steps.length - 1, stepIndex: undefined })}
    ondragover={(e) => over(e, { type: 'step', index: steps.length - 1 })}
    ondrop={(e) => drop(e, { type: 'step', index: steps.length - 1 })}
    role="presentation"
  ></div>

  <button
    type="button"
    class="ghost-button block-add block-add--action"
    disabled={!canManage}
    onclick={() => {
      onchange([
        ...steps,
        { actionCode: actions[0]?.name ?? '', filters: [] },
      ]);
    }}
  >
    {$t('rewardTracks.addStep')}
  </button>

  {#if notice}
    <p class="muted small notice" role="status">{notice}</p>
  {/if}
</div>

<style>
  .sequence {
    display: flex;
    flex-direction: column;
  }

  /* Closed, a slot is invisible. Open, it is the gap the block will fall into -- which is what
     makes the arrangement feel like blocks clipping together rather than a list reordering. */
  .slot {
    height: 0;
    border-radius: 6px;
    transition: height 90ms ease, background 90ms ease;
  }

  .slot.open {
    height: 28px;
    margin: 4px 0;
    background: rgba(var(--accent-rgb), 0.18);
    border: 1px dashed rgba(var(--accent-rgb), 0.6);
  }

  /* The area under an action that accepts its conditions. It only shows an edge while something is
     hovering it, so a resting sequence stays quiet. */
  .filter-slot.open {
    border-radius: 6px;
    outline: 1px dashed rgba(var(--gold-rgb), 0.6);
    outline-offset: 3px;
  }

  .notice {
    margin-top: 8px;
  }

  .no-facts {
    margin: 7px 0 0 14px;
  }
</style>
