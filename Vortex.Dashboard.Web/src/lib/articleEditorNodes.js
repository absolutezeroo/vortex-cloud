/**
 * The two blocks that are not text: a picture and a button.
 *
 * They sit inside the writing surface as atoms with their own small form, rather than as a separate
 * list under it, so the order a writer sees is the order the reader gets. The forms are plain DOM
 * rather than mounted Svelte components: a node view is created and destroyed by ProseMirror, on
 * its schedule, and handing it a component whose lifecycle Svelte also owns buys two owners for one
 * element and nothing else.
 */
import { Node } from '@tiptap/core';

import { BUTTON_NODE, IMAGE_NODE, isAllowedHref } from './articleBlocks.js';

function el(tag, className, text) {
  const node = document.createElement(tag);

  if (className) node.className = className;
  if (text) node.textContent = text;

  return node;
}

function field(labelText, value, placeholder, onInput) {
  const wrap = el('label', 'ae-field');

  wrap.append(el('span', null, labelText));

  const input = el('input');
  input.type = 'text';
  input.value = value ?? '';
  input.autocomplete = 'off';
  input.spellcheck = false;

  if (placeholder) input.placeholder = placeholder;

  input.addEventListener('input', () => onInput(input.value));
  wrap.append(input);

  return { wrap, input };
}

// An input the writer is typing in must never be overwritten by a redraw. ProseMirror calls
// `update` on transactions the node had nothing to do with — every keystroke elsewhere in the
// article — and assigning `.value` there would move the caret to the end of whatever is being typed.
function syncInput(input, value) {
  if (document.activeElement !== input && input.value !== (value ?? '')) {
    input.value = value ?? '';
  }
}

function head(labelText, deleteLabel, onDelete, extra) {
  const bar = el('div', 'ae-card-head');

  bar.append(el('span', 'chip', labelText));

  const actions = el('div', 'ae-card-actions');

  if (extra) actions.append(extra);

  // Ghost, not danger. Removing a block is ordinary editing, and the repo's danger fill would make
  // the loudest thing on a picture card the button that throws it away.
  const remove = el('button', 'ghost-button ae-remove', '×');
  remove.type = 'button';
  remove.title = deleteLabel;
  remove.setAttribute('aria-label', deleteLabel);
  remove.addEventListener('click', onDelete);
  actions.append(remove);

  bar.append(actions);

  return bar;
}

/**
 * Shared plumbing: an atom block whose node view is a small form. `build` gets the current
 * attributes and a setter, and returns the DOM plus the function that re-reads it.
 */
function formNode(name, tag, attributes, build) {
  return Node.create({
    name,
    group: 'block',
    atom: true,
    selectable: true,
    draggable: false,

    addOptions() {
      return { resolveUrl: () => '', onBrowse: null, labels: {} };
    },

    addAttributes() {
      return attributes;
    },

    parseHTML() {
      return [{ tag: `${tag}[data-${name}]` }];
    },

    renderHTML({ HTMLAttributes }) {
      return [tag, { [`data-${name}`]: '', ...HTMLAttributes }];
    },

    addNodeView() {
      return ({ node, editor, getPos }) => {
        const dom = el(tag, 'ae-card');
        dom.contentEditable = 'false';

        const setAttrs = (patch) => {
          const pos = typeof getPos === 'function' ? getPos() : null;
          if (pos == null) return;

          const { state, dispatch } = editor.view;
          dispatch(state.tr.setNodeMarkup(pos, undefined, { ...node.attrs, ...patch }));
        };

        const remove = () => {
          const pos = typeof getPos === 'function' ? getPos() : null;
          if (pos == null) return;

          editor.chain().focus().deleteRange({ from: pos, to: pos + node.nodeSize }).run();
        };

        const refresh = build(dom, this.options, { attrs: () => node.attrs, setAttrs, remove });

        // Once immediately: `build` only wires the DOM up, and without this the previews stay empty
        // until something else in the article causes a transaction.
        refresh(node.attrs);

        return {
          dom,
          // The form owns everything that happens inside it. Without this ProseMirror treats a
          // click in the href box as a click on an atom and moves the selection off the field.
          stopEvent: () => true,
          ignoreMutation: () => true,
          update(updated) {
            if (updated.type.name !== name) return false;

            node = updated;
            refresh(updated.attrs);

            return true;
          },
        };
      };
    },
  });
}

/** A full-width picture. `src` is a path under the asset host's `c_images`, never a URL. */
export const ArticleImage = formNode(
  IMAGE_NODE,
  'figure',
  { src: { default: '' }, caption: { default: '' } },
  (dom, options, api) => {
    const labels = options.labels;

    const browse = el('button', 'ghost-button', labels.browse ?? 'Browse');
    browse.type = 'button';
    browse.addEventListener('click', () => options.onBrowse?.((path) => api.setAttrs({ src: path })));

    dom.append(head(labels.image ?? 'Image', labels.remove ?? 'Remove', api.remove, browse));

    const preview = el('div', 'ae-preview');
    dom.append(preview);

    const src = field(labels.imageSrc ?? 'Path', api.attrs().src, '/web_promo/…', (value) =>
      api.setAttrs({ src: value })
    );
    const caption = field(labels.caption ?? 'Caption', api.attrs().caption, '', (value) =>
      api.setAttrs({ caption: value })
    );

    dom.append(src.wrap, caption.wrap);

    return (attrs) => {
      syncInput(src.input, attrs.src);
      syncInput(caption.input, attrs.caption);

      const url = attrs.src ? options.resolveUrl(attrs.src) : '';

      preview.replaceChildren();

      if (url) {
        const img = el('img');
        img.src = url;
        img.alt = '';
        preview.append(img);
      } else {
        preview.append(el('span', 'muted', labels.noImage ?? 'No image chosen'));
      }
    };
  }
);

/** A call to action. Its href is the one field a writer controls that the browser will follow. */
export const ArticleButton = formNode(
  BUTTON_NODE,
  'div',
  { label: { default: '' }, href: { default: '#/hotel' } },
  (dom, options, api) => {
    const labels = options.labels;

    dom.append(head(labels.button ?? 'Button', labels.remove ?? 'Remove', api.remove));

    const preview = el('div', 'ae-preview');
    const sample = el('span', 'ae-button-sample');
    preview.append(sample);
    dom.append(preview);

    const label = field(labels.buttonLabel ?? 'Label', api.attrs().label, '', (value) =>
      api.setAttrs({ label: value })
    );
    const href = field(labels.buttonHref ?? 'Link', api.attrs().href, '#/hotel', (value) =>
      api.setAttrs({ href: value })
    );

    const warning = el('small', 'ae-warning', labels.badHref ?? '');
    dom.append(label.wrap, href.wrap, warning);

    return (attrs) => {
      syncInput(label.input, attrs.label);
      syncInput(href.input, attrs.href);

      sample.textContent = attrs.label || (labels.buttonLabel ?? 'Button');

      // Said here rather than at save time. The server refuses the body as a whole, which tells a
      // writer that something in a 30-block article is wrong and not which line.
      warning.hidden = isAllowedHref(attrs.href);
    };
  }
);
