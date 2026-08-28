// The one thing about the article editor that cannot be seen by looking at it: a body that comes
// back from the editor different from the way it went in. Marks are lost quietly — the paragraph
// still reads correctly, only the bold is gone — so the round trip is asserted rather than eyeballed.
//
//   node tools/articleBlocks.check.mjs

import assert from 'node:assert/strict';

import { blocksToDoc, docToBlocks, isAllowedHref, plainText } from '../src/lib/articleBlocks.js';

function roundTrip(blocks) {
  return docToBlocks(blocksToDoc(blocks));
}

function check(name, blocks, expected = blocks) {
  assert.deepEqual(roundTrip(blocks), expected, name);
}

// --- the five original block types survive untouched ------------------------------------------

check('plain paragraph', [{ type: 'p', text: 'Abobbados débarque en ville' }]);
check('heading', [{ type: 'h', text: 'Le programme' }]);
check('rule', [{ type: 'hr' }]);
check('image', [{ type: 'img', src: '/web_promo/x.png', caption: 'Une légende' }]);
check('button', [{ type: 'btn', label: 'Voir', href: '#/hotel' }]);

// --- the marks the toolbar adds -----------------------------------------------------------------

check('bold run', [{ type: 'p', text: [{ t: 'avant ' }, { t: 'gras', b: true }, { t: ' après' }] }]);

check('every mark at once', [
  { type: 'p', text: [{ t: 'x', b: true, i: true, u: true, s: true }] },
]);

check('link run', [{ type: 'p', text: [{ t: 'ici', href: 'https://habbo.fr' }] }]);

check('list', [{ type: 'list', ordered: true, items: ['un', [{ t: 'deux', b: true }]] }]);

// --- the normalisations, which are the point ---------------------------------------------------

// An unformatted body must come back as a plain string, not as a one-run array: that is the shape
// every article written before the editor existed already has.
assert.equal(typeof roundTrip([{ type: 'p', text: 'nu' }])[0].text, 'string', 'stays a string');

// The trailing empty paragraph ProseMirror always keeps is not a block, and saving it would fail
// the server's own rule that a paragraph carries text.
assert.deepEqual(docToBlocks(blocksToDoc([])), [], 'an empty body saves as an empty body');

assert.deepEqual(
  docToBlocks({ type: 'doc', content: [{ type: 'paragraph' }, { type: 'paragraph', content: [] }] }),
  [],
  'blank paragraphs are dropped'
);

// Adjacent runs of identical formatting are one run. ProseMirror splits text nodes on its own, and
// without the merge a body looks different after every open-and-save.
assert.deepEqual(
  docToBlocks({
    type: 'doc',
    content: [
      {
        type: 'paragraph',
        content: [
          { type: 'text', text: 'a', marks: [{ type: 'bold' }] },
          { type: 'text', text: 'b', marks: [{ type: 'bold' }] },
        ],
      },
    ],
  }),
  [{ type: 'p', text: [{ t: 'ab', b: true }] }],
  'adjacent identical runs merge'
);

// A javascript: href must not survive the editor either. The server refuses it, but a body that
// only fails at save time tells the writer nothing about which word did it.
assert.deepEqual(
  docToBlocks({
    type: 'doc',
    content: [
      {
        type: 'paragraph',
        // eslint-disable-next-line no-script-url
        content: [{ type: 'text', text: 'x', marks: [{ type: 'link', attrs: { href: 'javascript:alert(1)' } }] }],
      },
    ],
  }),
  [{ type: 'p', text: 'x' }],
  'a refused href is dropped, the text is kept'
);

// A list with no usable item is not a list. ProseMirror throws on `listItem+` with zero children,
// which would lose the whole editor over one malformed row.
assert.deepEqual(
  blocksToDoc([{ type: 'list', ordered: false, items: [] }]).content,
  [{ type: 'paragraph' }],
  'an empty list is dropped, not thrown on'
);

// An unknown block type is dropped rather than rendered: the column may hold one written by a
// later version of the dashboard.
assert.deepEqual(roundTrip([{ type: 'video', src: 'x' }]), [], 'unknown types are dropped');

// --- helpers used by the drawer -----------------------------------------------------------------

assert.equal(plainText([{ t: 'a', b: true }, { t: 'b' }]), 'ab');
assert.equal(plainText('nu'), 'nu');

assert.ok(isAllowedHref('#/hotel') && isAllowedHref('/news') && isAllowedHref('https://habbo.fr'));
// eslint-disable-next-line no-script-url
assert.ok(!isAllowedHref('javascript:alert(1)') && !isAllowedHref('//evil.tld') && !isAllowedHref(''));

console.log('articleBlocks: ok');
