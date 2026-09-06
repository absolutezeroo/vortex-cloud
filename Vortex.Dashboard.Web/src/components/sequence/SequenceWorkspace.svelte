<script>
  // The sequence editor, full screen.
  //
  // It lived inside the task drawer, which is a narrow column: three selects and a value field do
  // not fit on one line there, so every condition wrapped and a three-step sequence read as a wall.
  // Blocks you arrange need room to be arranged in -- so this takes the whole viewport, and the
  // drawer keeps only a summary and the button that opens it.
  import { X } from '@lucide/svelte';
  import SequenceEditor from './SequenceEditor.svelte';
  import { t } from '../../lib/i18n.js';

  // Rendered on the body, not where it was written: a drawer sets its own stacking context, so a
  // full-screen layer inside one would be trapped in that column.
  function portal(node) {
    document.body.appendChild(node);

    return { destroy: () => node.remove() };
  }

  /** @type {{ open: boolean, title: string, onclose: () => void, editor: any }} */
  let { open = false, title, onclose, ...editor } = $props();

  function onkeydown(event) {
    if (event.key === 'Escape') onclose();
  }
</script>

<svelte:window on:keydown={open ? onkeydown : undefined} />

{#if open}
  <div class="workspace-layer" use:portal>
    <header class="workspace-head">
      <div>
        <h2>{title}</h2>
        <p class="muted small">{$t('rewardTracks.sequenceHint')}</p>
      </div>
      <button type="button" class="ghost-button" onclick={onclose}>
        <X size={16} />
        {$t('common.close')}
      </button>
    </header>

    <div class="workspace-canvas">
      <div class="workspace-inner">
        <SequenceEditor {...editor} />
      </div>
    </div>
  </div>
{/if}

<style>
  /* Above the drawer it was opened from, and above the drawer's own backdrop. */
  .workspace-layer {
    position: fixed;
    inset: 0;
    z-index: 1200;
    display: flex;
    flex-direction: column;
    background: var(--page);
  }

  .workspace-head {
    display: flex;
    align-items: flex-start;
    justify-content: space-between;
    gap: 16px;
    padding: 18px 24px;
    border-bottom: 2px solid var(--line-strong);
    background: var(--surface);
  }

  .workspace-head h2 {
    margin: 0 0 4px;
    font-size: 1.05rem;
  }

  .workspace-head p {
    margin: 0;
    max-width: 70ch;
  }

  /* The canvas scrolls, the header does not: a long sequence must not push the way out off screen. */
  .workspace-canvas {
    flex: 1 1 auto;
    overflow: auto;
    padding: 24px;
  }

  /* Wide enough that a condition -- fact, operator, value, picker -- fits on one line, which is the
     whole reason this is not in the drawer. */
  .workspace-inner {
    max-width: 900px;
    margin: 0 auto;
  }
</style>
