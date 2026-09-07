<script>
  // One action and the conditions clipped under it. The unit an operator drags.
  import { GripVertical, X } from '@lucide/svelte';
  import { t, translate } from '../../lib/i18n';

  /**
   * @type {{
   *   step: any,
   *   index: number,
   *   actions: any[],
   *   canManage: boolean,
   *   removable: boolean,
   *   dragging: boolean,
   *   ondragstart: (event: DragEvent) => void,
   *   ondragend: () => void,
   *   onactionchange: () => void,
   *   onremove: () => void,
   *   children?: any,
   * }}
   */
  let {
    step,
    index,
    actions,
    canManage,
    removable,
    dragging,
    ondragstart,
    ondragend,
    onactionchange,
    onremove,
    children,
  } = $props();
</script>

<div class="step-card" class:dragging>
  <div class="step-head block block--action">
    <span
      class="block-grip"
      draggable={canManage}
      ondragstart={ondragstart}
      ondragend={ondragend}
      title={$t('rewardTracks.dragAction')}
      aria-hidden="true"
    >
      <GripVertical size={14} />
    </span>
    <span class="block-label">{$t('rewardTracks.stepN', { n: index + 1 })}</span>
    <select bind:value={step.actionCode} onchange={onactionchange} disabled={!canManage}>
      {#each actions as action (action.name)}
        <option value={action.name}>
          {action.name}{action.wired ? '' : ` — ${translate('rewardTracks.notWired')}`}
        </option>
      {/each}
    </select>
    {#if removable}
      <button
        type="button"
        class="ghost-button block-remove"
        title={$t('common.remove')}
        disabled={!canManage}
        onclick={onremove}
      >
        <X size={14} />
      </button>
    {/if}
  </div>

  {@render children?.()}
</div>

<style>
  .step-card {
    margin-bottom: 14px;
  }

  .block-grip {
    flex: 0 0 auto;
    display: inline-flex;
    align-items: center;
    cursor: grab;
    color: var(--accent-strong);
  }

  .block-grip:active {
    cursor: grabbing;
  }

  .dragging {
    opacity: 0.4;
  }
</style>
