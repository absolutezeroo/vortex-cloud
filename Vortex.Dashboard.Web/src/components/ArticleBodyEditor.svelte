<script>
  /**
   * The article body, written the way an article is written: one surface, a toolbar, and the
   * pictures and buttons sitting where they will sit on the site.
   *
   * What it is NOT is a rich-text field. Nothing here produces HTML — the toolbar's marks and the
   * two form blocks are mapped back into the same closed block vocabulary the server has always
   * validated (`articleBlocks.js`), so the public site still has no markup to sanitise. A writer
   * gets the editor they asked for; the security property that made the body typed JSON is
   * unchanged.
   *
   * The component is deliberately UNCONTROLLED: it loads `value` once and then only emits. The
   * parent's draft is rebuilt on every keystroke, and feeding that back in would reset the
   * document under the caret. Switching language or article re-mounts it through `{#key}`.
   */
  import { onDestroy, onMount } from 'svelte';

  import { Editor } from '@tiptap/core';
  import StarterKit from '@tiptap/starter-kit';

  import {
    Bold,
    Italic,
    Underline,
    Strikethrough,
    Link,
    Unlink,
    Heading2,
    List,
    ListOrdered,
    Minus,
    Image,
    SquareMousePointer,
    Undo2,
    Redo2,
  } from '@lucide/svelte';

  import {
    BUTTON_NODE,
    IMAGE_NODE,
    blocksToDoc,
    docToBlocks,
    isAllowedHref,
  } from '../lib/articleBlocks.js';
  import { ArticleButton, ArticleImage } from '../lib/articleEditorNodes.js';
  import { t } from '../lib/i18n.js';

  let { value = [], onchange, resolveUrl = () => '', onpickimage = null, readonly = false } = $props();

  let host;
  let editor = $state(null);
  // Bumped on every transaction so the toolbar's pressed states follow the caret. ProseMirror is
  // not reactive and Svelte cannot see inside it.
  let tick = $state(0);

  let linkOpen = $state(false);
  let linkValue = $state('');

  const labels = $derived({
    image: $t('articles.blockimg'),
    button: $t('articles.blockbtn'),
    remove: $t('common.delete'),
    browse: $t('articles.browse'),
    imageSrc: $t('articles.blockImageSrc'),
    caption: $t('articles.blockCaption'),
    noImage: $t('articles.noImageChosen'),
    buttonLabel: $t('articles.blockLabel'),
    buttonHref: $t('articles.blockHref'),
    badHref: $t('articles.blockHrefHelp'),
  });

  onMount(() => {
    editor = new Editor({
      element: host,
      editable: !readonly,
      content: blocksToDoc(value),
      extensions: [
        StarterKit.configure({
          // Only what the block vocabulary can hold. A mark the toolbar cannot map is a mark the
          // writer would lose on save without being told.
          code: false,
          codeBlock: false,
          blockquote: false,
          heading: { levels: [2] },
          link: { openOnClick: false, autolink: false, protocols: ['http', 'https'] },
        }),
        ArticleImage.configure({
          resolveUrl,
          labels,
          onBrowse: (apply) => onpickimage?.(apply),
        }),
        ArticleButton.configure({ labels }),
      ],
      onUpdate: ({ editor: instance }) => onchange?.(docToBlocks(instance.getJSON())),
      onTransaction: () => (tick += 1),
    });

    tick += 1;
  });

  onDestroy(() => editor?.destroy());

  const active = $derived.by(() => {
    void tick;
    if (!editor) return {};

    return {
      bold: editor.isActive('bold'),
      italic: editor.isActive('italic'),
      underline: editor.isActive('underline'),
      strike: editor.isActive('strike'),
      link: editor.isActive('link'),
      heading: editor.isActive('heading'),
      bulletList: editor.isActive('bulletList'),
      orderedList: editor.isActive('orderedList'),
      undo: editor.can().undo(),
      redo: editor.can().redo(),
    };
  });

  const chain = () => editor?.chain().focus();

  function openLink() {
    if (!editor) return;

    linkValue = editor.getAttributes('link').href ?? '';
    linkOpen = true;
  }

  function applyLink() {
    if (!isAllowedHref(linkValue)) return;

    chain()?.extendMarkRange('link').setLink({ href: linkValue }).run();
    linkOpen = false;
  }

  function clearLink() {
    chain()?.extendMarkRange('link').unsetLink().run();
    linkOpen = false;
  }

  function insertImage() {
    // Straight to the picker: an empty picture card the writer then has to click "browse" on is a
    // step that exists only because it was easier to build.
    onpickimage?.((path) => chain()?.insertContent({ type: IMAGE_NODE, attrs: { src: path, caption: '' } }).run());
  }
</script>

<div class="ae" class:readonly>
  {#if !readonly}
    <div class="ae-toolbar">
      <button type="button" class="ae-tool" class:on={active.bold} disabled={!editor} onclick={() => chain()?.toggleBold().run()} title={$t('articles.bold')} aria-label={$t('articles.bold')} aria-pressed={!!active.bold}>
        <Bold size={15} strokeWidth={2.5} aria-hidden="true" />
      </button>
      <button type="button" class="ae-tool" class:on={active.italic} disabled={!editor} onclick={() => chain()?.toggleItalic().run()} title={$t('articles.italic')} aria-label={$t('articles.italic')} aria-pressed={!!active.italic}>
        <Italic size={15} strokeWidth={2.5} aria-hidden="true" />
      </button>
      <button type="button" class="ae-tool" class:on={active.underline} disabled={!editor} onclick={() => chain()?.toggleUnderline().run()} title={$t('articles.underline')} aria-label={$t('articles.underline')} aria-pressed={!!active.underline}>
        <Underline size={15} strokeWidth={2.5} aria-hidden="true" />
      </button>
      <button type="button" class="ae-tool" class:on={active.strike} disabled={!editor} onclick={() => chain()?.toggleStrike().run()} title={$t('articles.strike')} aria-label={$t('articles.strike')} aria-pressed={!!active.strike}>
        <Strikethrough size={15} strokeWidth={2.5} aria-hidden="true" />
      </button>

      <span class="ae-sep"></span>

      <button type="button" class="ae-tool" class:on={active.link} disabled={!editor} onclick={openLink} title={$t('articles.link')} aria-label={$t('articles.link')} aria-pressed={!!active.link}>
        <Link size={15} strokeWidth={2.5} aria-hidden="true" />
      </button>
      <button type="button" class="ae-tool" class:on={active.heading} disabled={!editor} onclick={() => chain()?.toggleHeading({ level: 2 }).run()} title={$t('articles.blockh')} aria-label={$t('articles.blockh')} aria-pressed={!!active.heading}>
        <Heading2 size={15} strokeWidth={2.5} aria-hidden="true" />
      </button>
      <button type="button" class="ae-tool" class:on={active.bulletList} disabled={!editor} onclick={() => chain()?.toggleBulletList().run()} title={$t('articles.bulletList')} aria-label={$t('articles.bulletList')} aria-pressed={!!active.bulletList}>
        <List size={15} strokeWidth={2.5} aria-hidden="true" />
      </button>
      <button type="button" class="ae-tool" class:on={active.orderedList} disabled={!editor} onclick={() => chain()?.toggleOrderedList().run()} title={$t('articles.orderedList')} aria-label={$t('articles.orderedList')} aria-pressed={!!active.orderedList}>
        <ListOrdered size={15} strokeWidth={2.5} aria-hidden="true" />
      </button>

      <span class="ae-sep"></span>

      <button type="button" class="ae-tool" disabled={!editor} onclick={insertImage} title={$t('articles.blockimg')} aria-label={$t('articles.blockimg')}>
        <Image size={15} strokeWidth={2.5} aria-hidden="true" />
      </button>
      <button type="button" class="ae-tool" disabled={!editor} onclick={() => chain()?.insertContent({ type: BUTTON_NODE, attrs: { label: '', href: '#/hotel' } }).run()} title={$t('articles.blockbtn')} aria-label={$t('articles.blockbtn')}>
        <SquareMousePointer size={15} strokeWidth={2.5} aria-hidden="true" />
      </button>
      <button type="button" class="ae-tool" disabled={!editor} onclick={() => chain()?.setHorizontalRule().run()} title={$t('articles.blockhr')} aria-label={$t('articles.blockhr')}>
        <Minus size={15} strokeWidth={2.5} aria-hidden="true" />
      </button>

      <span class="ae-spacer"></span>

      <button type="button" class="ae-tool" disabled={!active.undo} onclick={() => chain()?.undo().run()} title={$t('common.undo')} aria-label={$t('common.undo')}>
        <Undo2 size={15} strokeWidth={2.5} aria-hidden="true" />
      </button>
      <button type="button" class="ae-tool" disabled={!active.redo} onclick={() => chain()?.redo().run()} title={$t('common.redo')} aria-label={$t('common.redo')}>
        <Redo2 size={15} strokeWidth={2.5} aria-hidden="true" />
      </button>
    </div>

    {#if linkOpen}
      <!-- svelte-ignore a11y_autofocus -- the bar exists only to take this one value -->
      <div class="ae-linkbar">
        <input
          autocomplete="off"
          spellcheck="false"
          autofocus
          bind:value={linkValue}
          placeholder="#/hotel"
          onkeydown={(e) => {
            if (e.key === 'Enter') applyLink();
            if (e.key === 'Escape') (linkOpen = false);
          }}
        />
        <button type="button" onclick={applyLink} disabled={!isAllowedHref(linkValue)}>{$t('common.apply')}</button>
        <button type="button" class="ghost-button" onclick={clearLink} aria-label={$t('articles.removeLink')}>
          <Unlink size={14} strokeWidth={2} aria-hidden="true" />
        </button>
        <small class="muted">{$t('articles.blockHrefHelp')}</small>
      </div>
    {/if}
  {/if}

  <div class="ae-surface" bind:this={host}></div>
</div>

<style>
  .ae {
    border: 1px solid var(--line-strong);
    border-radius: 10px;
    background: var(--surface-strong);
    overflow: hidden;
  }

  .ae-toolbar {
    display: flex;
    align-items: center;
    gap: 2px;
    flex-wrap: wrap;
    padding: 6px 8px;
    background: var(--table-header-bg);
    border-bottom: 1px solid var(--line);
    position: sticky;
    top: 0;
    z-index: 2;
  }

  .ae-tool {
    display: inline-flex;
    align-items: center;
    justify-content: center;
    width: 30px;
    height: 28px;
    padding: 0;
    border: 1px solid transparent;
    border-radius: 6px;
    background: transparent;
    color: var(--muted-strong);
    cursor: pointer;
  }

  .ae-tool:hover:not(:disabled) {
    background: var(--surface-hover);
    color: var(--ink);
  }

  .ae-tool:disabled {
    opacity: 0.35;
    cursor: default;
  }

  /* Pressed, not merely hovered: with only a background change a writer cannot tell whether bold is
     on without typing a letter to find out. */
  .ae-tool.on {
    background: var(--accent-soft);
    border-color: var(--accent);
    color: var(--ink);
  }

  .ae-sep {
    width: 1px;
    height: 18px;
    margin: 0 5px;
    background: var(--line-strong);
  }

  .ae-spacer {
    flex: 1;
  }

  .ae-linkbar {
    display: flex;
    align-items: center;
    gap: 6px;
    flex-wrap: wrap;
    padding: 6px 8px;
    background: var(--surface-raised);
    border-bottom: 1px solid var(--line);
  }

  .ae-linkbar input {
    flex: 1 1 220px;
    min-width: 0;
  }

  .ae-surface {
    max-height: 60vh;
    overflow-y: auto;
  }

  /*
    Everything below is created by ProseMirror, outside Svelte's compiler, so it carries no scoping
    attribute and a plain selector would match nothing at all.
  */
  .ae-surface :global(.ProseMirror) {
    min-height: 220px;
    padding: 14px 16px;
    outline: none;
    color: var(--ink);
    line-height: 1.6;
    /* A hard break is stored as a newline in the run; without this it renders as a space. */
    white-space: pre-wrap;
  }

  .ae-surface :global(.ProseMirror > * + *) {
    margin-top: 0.75em;
  }

  /* At 1.15rem a sub-heading read as a slightly bold paragraph, which is exactly the distinction a
     writer needs to see to know the block took. */
  .ae-surface :global(.ProseMirror h2) {
    font-size: 1.35rem;
    font-weight: 600;
    line-height: 1.3;
    margin-top: 1.1em;
  }

  .ae-surface :global(.ProseMirror a) {
    color: var(--accent-strong);
    text-decoration: underline;
  }

  /* styles.css strips list markers globally (nav menus, chip rows). Inside the article they are the
     only thing that tells a bulleted list from three short paragraphs. */
  .ae-surface :global(.ProseMirror ul) {
    padding-left: 1.4em;
    list-style: disc;
  }

  .ae-surface :global(.ProseMirror ol) {
    padding-left: 1.4em;
    list-style: decimal;
  }

  .ae-surface :global(.ProseMirror li) {
    margin: 0.2em 0;
  }

  .ae-surface :global(.ProseMirror hr) {
    border: 0;
    border-top: 2px solid var(--line-strong);
    margin: 1.2em 0;
  }

  /* The picture and button cards. */
  .ae-surface :global(.ae-card) {
    margin: 12px 0;
    padding: 10px 12px;
    border: 1px solid var(--line-strong);
    border-radius: 8px;
    background: var(--surface);
  }

  .ae-surface :global(.ae-card.ProseMirror-selectednode) {
    border-color: var(--accent);
    box-shadow: 0 0 0 2px var(--accent-soft);
  }

  .ae-surface :global(.ae-card-head) {
    display: flex;
    align-items: center;
    justify-content: space-between;
    gap: 8px;
    margin-bottom: 8px;
  }

  .ae-surface :global(.ae-card-actions) {
    display: flex;
    align-items: center;
    gap: 6px;
  }

  .ae-surface :global(.ae-remove) {
    padding: 0;
    width: 30px;
    line-height: 1;
    font-size: 1.15rem;
  }

  .ae-surface :global(.ae-remove:hover) {
    color: var(--button-danger-border-top);
  }

  .ae-surface :global(.ae-field) {
    display: flex;
    align-items: center;
    gap: 8px;
    margin-top: 6px;
  }

  .ae-surface :global(.ae-field > span) {
    flex: 0 0 84px;
    font-size: 0.78rem;
    color: var(--muted);
  }

  .ae-surface :global(.ae-field input) {
    flex: 1;
    min-width: 0;
  }

  .ae-surface :global(.ae-preview) {
    display: flex;
    align-items: center;
    justify-content: center;
    min-height: 64px;
    padding: 8px;
    border: 1px dashed var(--line-strong);
    border-radius: 6px;
    background: var(--page);
  }

  .ae-surface :global(.ae-preview img) {
    max-height: 180px;
    max-width: 100%;
    border-radius: 4px;
  }

  .ae-surface :global(.ae-button-sample) {
    padding: 6px 18px;
    border: 1px solid var(--button-border);
    border-top-color: var(--button-border-top);
    border-radius: 8px;
    background: var(--button-bg);
    color: var(--button-ink);
    font-size: 0.85rem;
  }

  .ae-surface :global(.ae-warning) {
    display: block;
    margin-top: 6px;
    color: var(--button-danger-border-top);
  }
</style>
