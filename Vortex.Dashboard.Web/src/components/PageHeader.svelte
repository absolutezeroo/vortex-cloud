<script>
  // Title, one line of description, and the page's own actions on the right of the title.
  //
  // Written because "Refresh" kept landing in the wrong place: on a page with filters it fell into
  // the filter row and read as if it belonged to them, and on a page without filters it sat alone
  // under the description, attached to nothing. Both come from the same cause -- the button was
  // wherever the toolbar happened to be, instead of being part of the header.
  //
  // So: identity on the left, what you can do to the page on the right, and filters on their own
  // row underneath where they belong. Same shape on every page, so the button is always in the
  // place you already looked.
  //
  //   <PageHeader title={$t('bots.title')} description={$t('bots.description')}>
  //     {#snippet actions()}
  //       <button type="button" onclick={bots.refresh} class="warning">{$t('common.refresh')}</button>
  //     {/snippet}
  //   </PageHeader>

  /**
   * @typedef {Object} Props
   * @property {string} [title]
   * @property {string} [description]
   * @property {import('svelte').Snippet} [icon] - optional glyph before the title
   * @property {import('svelte').Snippet} [actions] - page-level actions, aligned with the title
   */

  /** @type {Props} */
  let { title = '', description = '', icon, actions } = $props();
</script>

<header class="page-header">
  <div class="page-header-row">
    <h2>
      {#if icon}{@render icon()}{/if}
      {title}
    </h2>
    {#if actions}
      <div class="page-header-actions">{@render actions()}</div>
    {/if}
  </div>
  {#if description}
    <p class="muted">{description}</p>
  {/if}
</header>

<style>
  .page-header {
    margin-bottom: 14px;
  }

  /* The actions sit on the TITLE's line, not the description's: the description can wrap to two
     lines on a narrow window without dragging the buttons down with it. */
  .page-header-row {
    display: flex;
    align-items: center;
    justify-content: space-between;
    gap: 12px;
  }

  /* With no title the actions are the row's only child, and space-between puts them on the
     left. The pages that pass title="" do it because the shell's <h1> already says it. */
  .page-header-row:not(:has(h2)) {
    justify-content: flex-end;
  }

  .page-header-row h2 {
    display: flex;
    align-items: center;
    gap: 8px;
    margin: 0;
  }

  .page-header-actions {
    display: flex;
    align-items: center;
    gap: 8px;
    flex: none;
  }

  .page-header p {
    margin: 6px 0 0;
  }
</style>
