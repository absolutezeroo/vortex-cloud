/**
 * The article body's two shapes, and the only translation between them.
 *
 * On disk a body is the closed block array `WebArticleBody` validates — typed JSON, never HTML, so
 * the public site never has a sanitiser to get wrong. In the drawer it is a ProseMirror document,
 * because a stack of textareas is not a writing surface.
 *
 * This module is that seam, and deliberately the only copy of it. A second mapping written inline
 * next to the editor is how a body starts losing one mark every time it is opened and saved.
 */

// The marks the toolbar may apply, and the single-letter key each is stored under. Closed on
// purpose: a mark some future extension introduces is dropped on the way out rather than quietly
// widening what the column accepts — the server would refuse the row anyway, and refusing it at
// save time tells the writer nothing about which word did it.
const MARK_KEYS = { bold: 'b', italic: 'i', underline: 'u', strike: 's' };

export const IMAGE_NODE = 'articleImage';
export const BUTTON_NODE = 'articleButton';

/** The hrefs a link or a button may carry. Mirrors `WebArticleBody.IsAllowedHref` exactly. */
export function isAllowedHref(href) {
  if (!href || !href.trim() || href.length > 2048) return false;
  if (href.startsWith('#/')) return true;
  if (href.startsWith('//')) return false;

  return href[0] === '/' || /^https?:\/\//i.test(href);
}

/** The text of a block as a reader would see it, marks removed. For counts and previews. */
export function plainText(text) {
  if (typeof text === 'string') return text;

  return Array.isArray(text) ? text.map((run) => run?.t ?? '').join('') : '';
}

function isBlank(text) {
  return !plainText(text).trim();
}

// Two adjacent runs carrying the same formatting are one run. ProseMirror splits text nodes for
// reasons of its own (a cursor sat there once), and without this a paragraph slowly accumulates
// fragments that make every saved body look different from the last.
function sameFormat(a, b) {
  const keys = (run) =>
    Object.keys(run)
      .filter((key) => key !== 't')
      .sort()
      .join(',');

  return keys(a) === keys(b) && a.href === b.href;
}

function appendRun(runs, run) {
  const last = runs[runs.length - 1];

  if (last && sameFormat(last, run)) last.t += run.t;
  else runs.push(run);
}

/**
 * A ProseMirror inline sequence, as the `text` field is stored: a plain string when nothing is
 * formatted, an array of runs when something is.
 */
function runsFromInline(content) {
  const runs = [];

  for (const node of content ?? []) {
    // Shift+Enter. Kept as a newline inside a run rather than becoming a block of its own: the
    // reader renders article text with `pre-wrap`, and a line break is not worth a new block type.
    if (node?.type === 'hardBreak') {
      appendRun(runs, { t: '\n' });
      continue;
    }

    if (node?.type !== 'text' || !node.text) continue;

    const run = { t: node.text };

    for (const mark of node.marks ?? []) {
      const key = MARK_KEYS[mark.type];

      if (key) run[key] = true;
      else if (mark.type === 'link' && isAllowedHref(mark.attrs?.href)) run.href = mark.attrs.href;
    }

    appendRun(runs, run);
  }

  // An unformatted body stays a plain string. Every article written before this editor existed is
  // that shape, and rewriting them all into single-run arrays would be a migration bought for
  // nothing.
  if (runs.every((run) => Object.keys(run).length === 1)) {
    return runs.map((run) => run.t).join('');
  }

  return runs;
}

/** The inverse: the `text` field as ProseMirror inline content. */
function inlineFromRuns(text) {
  const runs = typeof text === 'string' ? [{ t: text }] : Array.isArray(text) ? text : [];
  const content = [];

  for (const run of runs) {
    if (!run?.t) continue;

    const marks = [];

    for (const [type, key] of Object.entries(MARK_KEYS)) {
      if (run[key]) marks.push({ type });
    }

    if (isAllowedHref(run.href)) marks.push({ type: 'link', attrs: { href: run.href } });

    // A newline is a break in the schema, not a character a text node may hold.
    run.t.split('\n').forEach((part, index) => {
      if (index) content.push({ type: 'hardBreak' });
      if (part) content.push({ type: 'text', text: part, ...(marks.length ? { marks } : {}) });
    });
  }

  return content;
}

// A list item may hold several paragraphs. They are flattened into one run sequence separated by
// breaks rather than kept as separate paragraphs: the stored item is one text, and a nested
// paragraph array would be a second shape for the public reader to learn.
function inlineOfListItem(item) {
  const content = [];

  for (const child of item?.content ?? []) {
    if (content.length) content.push({ type: 'hardBreak' });
    content.push(...(child?.content ?? []));
  }

  return content;
}

/** The stored body, as a document the editor can load. */
export function blocksToDoc(blocks) {
  const content = [];

  for (const block of Array.isArray(blocks) ? blocks : []) {
    switch (block?.type) {
      case 'p':
        content.push({ type: 'paragraph', content: inlineFromRuns(block.text) });
        break;

      case 'h':
        content.push({ type: 'heading', attrs: { level: 2 }, content: inlineFromRuns(block.text) });
        break;

      case 'hr':
        content.push({ type: 'horizontalRule' });
        break;

      case 'img':
        content.push({ type: IMAGE_NODE, attrs: { src: block.src ?? '', caption: block.caption ?? '' } });
        break;

      case 'btn':
        content.push({ type: BUTTON_NODE, attrs: { label: block.label ?? '', href: block.href ?? '' } });
        break;

      case 'list': {
        // A list with no items does not satisfy the schema (`listItem+`), and ProseMirror throws
        // rather than dropping it — which would lose the editor over one malformed row.
        const items = (block.items ?? []).map((item) => ({
          type: 'listItem',
          content: [{ type: 'paragraph', content: inlineFromRuns(item) }],
        }));

        if (items.length) {
          content.push({ type: block.ordered ? 'orderedList' : 'bulletList', content: items });
        }

        break;
      }

      // An unknown type is dropped rather than shown. The column may hold a block written by a
      // later version, and showing it as nothing beats showing it as raw JSON.
      default:
        break;
    }
  }

  // ProseMirror will not load a document with no content, and an empty article has to open.
  return { type: 'doc', content: content.length ? content : [{ type: 'paragraph' }] };
}

/** The document, as the body to store. */
export function docToBlocks(doc) {
  const blocks = [];

  for (const node of doc?.content ?? []) {
    switch (node?.type) {
      case 'paragraph':
      case 'heading': {
        const text = runsFromInline(node.content);

        // The trailing empty paragraph ProseMirror always keeps is not a block. Saving it would
        // fail the server's own rule that a `p` carries text, on an article the writer had every
        // reason to think was finished.
        if (isBlank(text)) break;

        blocks.push({ type: node.type === 'heading' ? 'h' : 'p', text });
        break;
      }

      case 'horizontalRule':
        blocks.push({ type: 'hr' });
        break;

      case IMAGE_NODE:
        blocks.push({ type: 'img', src: node.attrs?.src ?? '', caption: node.attrs?.caption ?? '' });
        break;

      case BUTTON_NODE:
        blocks.push({ type: 'btn', label: node.attrs?.label ?? '', href: node.attrs?.href ?? '' });
        break;

      case 'bulletList':
      case 'orderedList': {
        const items = (node.content ?? [])
          .map((item) => runsFromInline(inlineOfListItem(item)))
          .filter((item) => !isBlank(item));

        if (items.length) blocks.push({ type: 'list', ordered: node.type === 'orderedList', items });

        break;
      }

      default:
        break;
    }
  }

  return blocks;
}
