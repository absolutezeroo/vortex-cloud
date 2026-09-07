<script lang="ts">
  // The sequence editor, full screen.
  //
  // It lived inside the task drawer, which is a narrow column: three selects and a value field do
  // not fit on one line there, so every condition wrapped and a three-step sequence read as a wall.
  // Blocks you arrange need room to be arranged in -- so this takes the whole viewport, and the
  // drawer keeps only a summary and the button that opens it.
  import { X } from '@lucide/svelte';
  import SequenceGraph from '../graph/SequenceGraph.svelte';
  import { t } from '../../lib/i18n';

  // Rendered on the body, not where it was written: a drawer sets its own stacking context, so a
  // full-screen layer inside one would be trapped in that column.
  function portal(node: HTMLElement) {
    document.body.appendChild(node);

    return { destroy: () => node.remove() };
  }

  type Props = {
    open?: boolean;
    title: string;
    onclose: () => void;
    /** Everything else is forwarded to the graph, which owns those props. */
    [key: string]: unknown;
  };

  let { open = false, title, onclose, ...editor }: Props = $props();

  function onkeydown(event: KeyboardEvent) {
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

    <!-- The graph's own props are forwarded whole; this shell adds only the chrome. -->
    <SequenceGraph {...(editor as any)} />
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

  /* The graph fills what is left. It pans and zooms itself, so nothing here scrolls: a canvas
     inside a scroller is two ways to move the same thing and they fight. */
</style>

