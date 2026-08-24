// Draw a box around what is wrong, say what you want, send.
//
// Written because a screenshot says WHERE something is wrong and nothing else: which page, which
// component, which of the four buttons in that row, and what the element actually computes to all
// have to be worked out afterwards, one question at a time. A selection carries all of it.
//
// The part that makes this worth having is `__svelte_meta`: Svelte stamps every element it creates
// in dev with the source file and line it came from. So a report names the exact file:line rather
// than a class that appears on nine pages.
//
// Dev only -- main.js imports it behind import.meta.env.DEV, and the endpoint it posts to exists
// only in the dev server (tools/vite-plugin-ui-reports.js).

const ENDPOINT = '/__ui-report';
const HOTKEY = 'b'; // with ctrl+shift -- arm the selection
const HOTKEY_LIST = 'l'; // with ctrl+shift -- open the list
const HOTKEY_NOTE = 'n'; // with ctrl+shift -- a note about the page, with nothing to point at

/** Classes Svelte adds for style scoping carry no meaning for a human reading the report. */
const meaningful = (el) =>
  [...el.classList].filter((c) => !c.startsWith('svelte-'));

/**
 * Every app source an element sits inside, nearest first.
 *
 * One file:line is not enough, and the first real report showed why: an element inside a
 * StatCard resolves to StatCard.svelte:26, which is the component's own markup and identical
 * for all three cards on the page. The chain carries the call site too -- the LAST entry is
 * the page -- so "which of these three" answers itself.
 *
 * node_modules is dropped: an icon resolving to @lucide/svelte/dist/Icon.svelte says nothing
 * about this dashboard, and three of them per card drowned the list.
 */
function sources(el) {
  const chain = [];

  for (let node = el; node && node !== document.body; node = node.parentElement) {
    const loc = node.__svelte_meta?.loc;

    if (!loc?.file) continue;

    const file = loc.file.split(String.fromCharCode(92)).join('/');

    if (file.includes('node_modules')) continue;

    const at = file.split('/src/').pop() + ':' + loc.line;

    if (!chain.includes(at)) chain.push(at);
  }

  return chain;
}

const source = (el) => sources(el)[0] ?? null;

/** The container is the culprit more often than the element -- a row that is a column, a cell too narrow. */
function parentLayout(el) {
  const p = el.parentElement;

  if (!p || p === document.body) return null;

  const cs = getComputedStyle(p);
  const flex = cs.display.includes('flex');
  const grid = cs.display.includes('grid');

  return {
    of: [...p.classList].filter((c) => !c.startsWith('svelte-')).join('.') || p.tagName.toLowerCase(),
    display: cs.display,
    flexDirection: flex ? cs.flexDirection : undefined,
    flexWrap: flex ? cs.flexWrap : undefined,
    gridTemplateColumns: grid ? cs.gridTemplateColumns : undefined,
    alignItems: cs.alignItems,
    justifyContent: cs.justifyContent,
    gap: cs.gap,
  };
}

/** What an element IS, in the terms the source uses. */
function describe(el) {
  const text = (el.textContent || '').trim().replace(/\s+/g, ' ').slice(0, 60);
  const cs = getComputedStyle(el);
  const box = el.getBoundingClientRect();

  return {
    tag: el.tagName.toLowerCase() + (el.type ? `[${el.type}]` : ''),
    classes: meaningful(el),
    sources: sources(el),
    parent: parentLayout(el),
    text: text || null,
    label: el.getAttribute('aria-label') || el.getAttribute('title') || null,
    size: `${Math.round(box.width)}x${Math.round(box.height)}`,
    // The properties every layout complaint this dashboard has produced turned on.
    css: {
      display: cs.display,
      flexDirection: cs.flexDirection,
      gridTemplateColumns: cs.gridTemplateColumns === 'none' ? undefined : cs.gridTemplateColumns,
      padding: cs.padding,
      border: `${cs.borderTopWidth} ${cs.borderTopStyle} ${cs.borderTopColor}`,
      borderRadius: cs.borderRadius,
      background: cs.backgroundColor,
      color: cs.color,
      fontSize: cs.fontSize,
      textTransform: cs.textTransform === 'none' ? undefined : cs.textTransform,
      boxShadow: cs.boxShadow === 'none' ? undefined : cs.boxShadow.slice(0, 80),
    },
  };
}

/**
 * The elements the box is actually about.
 *
 * Everything intersecting a rectangle includes every ancestor that happens to be large enough,
 * so the list would open with <body> and the page shell every time. Kept: elements whose own box
 * is mostly inside the selection, which is what "I drew a box around this" means.
 */
function selected(rect) {
  const out = [];

  for (const el of document.querySelectorAll('body *')) {
    if (el.closest('#ui-reporter')) continue;

    const b = el.getBoundingClientRect();

    if (!b.width || !b.height) continue;

    const overlapX = Math.max(0, Math.min(b.right, rect.right) - Math.max(b.left, rect.left));
    const overlapY = Math.max(0, Math.min(b.bottom, rect.bottom) - Math.max(b.top, rect.top));
    const covered = (overlapX * overlapY) / (b.width * b.height);

    if (covered > 0.75) out.push(el);
  }

  // Drop an element whose only child is also selected: the child is the specific one.
  const strict = out.filter((el) => !out.some((other) => other !== el && el.contains(other) && el.children.length === 1));

  if(strict.length) return strict;

  // Nothing was 75% inside the box. That is not "nothing is there" -- it is what happens when
  // you draw a small box over a line of text inside a wide element: the <p> spans the panel, so
  // barely any of ITS box is in the selection. One report came back with an empty element list
  // for exactly that reason, and it had to be guessed at from the note alone.
  //
  // Fall back to whatever the box actually crosses, most-covered first, keeping the few smallest
  // -- the smallest element the box touches is nearly always the thing being pointed at.
  const touched = [];

  for(const el of document.querySelectorAll('body *'))
  {
    if(el.closest('#ui-reporter')) continue;

    const b = el.getBoundingClientRect();

    if(!b.width || !b.height) continue;

    const overlapX = Math.max(0, Math.min(b.right, rect.right) - Math.max(b.left, rect.left));
    const overlapY = Math.max(0, Math.min(b.bottom, rect.bottom) - Math.max(b.top, rect.top));

    if(overlapX <= 0 || overlapY <= 0) continue;

    touched.push({el, area: b.width * b.height, covered: (overlapX * overlapY) / (b.width * b.height)});
  }

  return touched
    .sort((a, b) => b.covered - a.covered || a.area - b.area)
    .slice(0, 6)
    .map((hit) => hit.el);
}

/**
 * The selection's markup, so the thing can be rebuilt in isolation.
 *
 * Every layout defect this dashboard produced was diagnosed by reconstructing the offending
 * row in a bare panel and measuring it. Shipping the markup with the report removes the step
 * where that has to be retyped from a screenshot.
 */
function excerpt(elements) {
  if (!elements.length) return null;

  // The outermost of the selected ones already contains the rest.
  const root = elements.find((el) => elements.every((other) => other === el || el.contains(other) || !el.contains(other)))
    ?? elements[0];
  const outer = elements.reduce((a, b) => (a.contains(b) ? a : b), root);

  return outer.outerHTML
    .replace(/ class="([^"]*)"/g, (_, c) => {
      const kept = c.split(/\s+/).filter((x) => x && !x.startsWith('svelte-')).join(' ');

      return kept ? ` class="${kept}"` : '';
    })
    .replace(/\s+/g, ' ')
    .slice(0, 4000);
}

export function mountDevReporter() {
  const host = document.createElement('div');

  host.id = 'ui-reporter';
  host.innerHTML = `
    <button class="r-arm" type="button" title="Signaler (Ctrl+Shift+B)">⌖</button>
    <div class="r-veil" hidden><div class="r-box"></div></div>
    <form class="r-form" hidden>
      <strong class="r-count"></strong>
      <textarea rows="3" placeholder="Ce qui ne va pas, et ce que tu veux à la place…"></textarea>
      <div class="r-actions">
        <button type="submit">Envoyer</button>
        <button type="button" class="r-cancel">Annuler</button>
      </div>
    </form>
    <section class="r-list" hidden>
      <header>
        <strong>Signalements</strong>
        <label><input type="checkbox" class="r-all" /> tout</label>
        <button type="button" class="r-close">×</button>
      </header>
      <div class="r-rows"></div>
    </section>
    <output class="r-toast" hidden></output>`;
  document.body.appendChild(host);

  const style = document.createElement('style');

  style.textContent = `
    #ui-reporter { position: fixed; inset: 0; pointer-events: none; z-index: 2147483647;
      font: 13px/1.4 system-ui, sans-serif; }
    #ui-reporter > * { pointer-events: auto; }
    #ui-reporter .r-arm { position: fixed; right: 14px; bottom: 14px; width: 34px; height: 34px;
      border-radius: 50%; border: 0; background: #0f7dbc; color: #fff; font-size: 17px;
      cursor: pointer; box-shadow: 0 2px 8px rgba(0,0,0,.45); }
    #ui-reporter .r-arm[data-armed] { background: #c0174e; }
    #ui-reporter .r-veil { position: fixed; inset: 0; cursor: crosshair;
      background: rgba(0,0,0,.28); }
    #ui-reporter .r-box { position: absolute; border: 2px solid #ffb900;
      background: rgba(255,185,0,.12); pointer-events: none; }
    #ui-reporter .r-form { position: fixed; right: 14px; bottom: 58px; width: 340px;
      display: grid; gap: 8px; padding: 12px; border-radius: 8px; background: #17181c;
      color: #f0f1f3; border: 1px solid rgba(255,255,255,.14); box-shadow: 0 8px 28px rgba(0,0,0,.5); }
    #ui-reporter textarea { font: inherit; padding: 7px 9px; border-radius: 5px;
      border: 2px solid #275d8e; background: #ccd8df; color: #444; resize: vertical; }
    #ui-reporter .r-actions { display: flex; gap: 8px; }
    #ui-reporter .r-actions button { font: inherit; font-weight: 700; padding: 6px 12px;
      border-radius: 5px; border: 2px solid #2a9cde; background: #0f7dbc; color: #fff; cursor: pointer; }
    #ui-reporter .r-cancel { background: #212228 !important; border-color: #3d3f47 !important; }
    #ui-reporter .r-count { color: #8d9096; font-weight: 400; }
    #ui-reporter .r-list { position: fixed; right: 14px; bottom: 58px; width: 420px;
      max-height: 70vh; display: flex; flex-direction: column; border-radius: 8px;
      background: #17181c; color: #f0f1f3; border: 1px solid rgba(255,255,255,.14);
      box-shadow: 0 8px 28px rgba(0,0,0,.5); overflow: hidden; }
    #ui-reporter .r-list header { display: flex; align-items: center; gap: 10px; padding: 10px 12px;
      border-bottom: 1px solid rgba(255,255,255,.1); }
    #ui-reporter .r-list header strong { margin-right: auto; }
    #ui-reporter .r-list label { display: flex; align-items: center; gap: 5px; color: #8d9096; }
    #ui-reporter .r-close { border: 0; background: none; color: #8d9096; font-size: 18px;
      cursor: pointer; line-height: 1; }
    #ui-reporter .r-rows { overflow-y: auto; padding: 6px; display: grid; gap: 6px; }
    #ui-reporter .r-item { display: grid; gap: 4px; padding: 8px 9px; border-radius: 6px;
      background: #212228; border: 1px solid #2f3138; }
    #ui-reporter .r-item[data-done] { opacity: .45; }
    #ui-reporter .r-item textarea { font: inherit; width: 100%; border: 0; background: none;
      color: #f0f1f3; resize: none; padding: 0; }
    #ui-reporter .r-meta { display: flex; align-items: center; gap: 8px; color: #8d9096;
      font-size: 11.5px; }
    #ui-reporter .r-meta button { margin: 0; border: 0; background: none; cursor: pointer;
      color: #8d9096; font: inherit; text-decoration: underline; }
    #ui-reporter .r-meta .r-del { color: #e37c88; margin-left: auto; }
    #ui-reporter .r-empty { padding: 14px; color: #8d9096; text-align: center; }
    #ui-reporter .r-toast { position: fixed; right: 14px; bottom: 58px; padding: 8px 12px;
      border-radius: 5px; background: #00813e; color: #fff; font-weight: 700; }`;
  document.head.appendChild(style);

  const arm = host.querySelector('.r-arm');
  const veil = host.querySelector('.r-veil');
  const box = host.querySelector('.r-box');
  const form = host.querySelector('.r-form');
  const note = host.querySelector('textarea');
  const count = host.querySelector('.r-count');
  const toast = host.querySelector('.r-toast');
  const list = host.querySelector('.r-list');
  const rows = host.querySelector('.r-rows');
  const showAll = host.querySelector('.r-all');

  let start = null;
  let picked = [];
  let rect = null;

  const setArmed = (on) => {
    veil.hidden = !on;
    if (on) arm.setAttribute('data-armed', ''); else arm.removeAttribute('data-armed');
    box.style.width = box.style.height = '0px';
  };

  const say = (text, ok = true) => {
    toast.textContent = text;
    toast.style.background = ok ? '#00813e' : '#c0174e';
    toast.hidden = false;
    setTimeout(() => { toast.hidden = true; }, 2600);
  };

  /**
   * The list, read from the same file the CLI reads.
   *
   * Editable in place: a report is a sentence written in a hurry, and the moment to correct or
   * withdraw it is while looking at the thing, not later in a JSONL file.
   */
  async function refreshList() {
    let data = [];

    try {
      data = await (await fetch(ENDPOINT)).json();
    } catch {
      rows.innerHTML = '<p class="r-empty">Point de collecte injoignable.</p>';

      return;
    }

    const visible = showAll.checked ? data : data.filter((r) => !r.done);

    if (!visible.length) {
      rows.innerHTML = `<p class="r-empty">${data.length ? "Rien d'ouvert." : 'Aucun signalement.'}</p>`;

      return;
    }

    rows.textContent = '';

    for (const r of visible.slice().reverse()) {
      const item = document.createElement('div');

      item.className = 'r-item';
      if (r.done) item.dataset.done = '';

      const text = document.createElement('textarea');

      text.rows = Math.min(4, Math.ceil((r.note || ' ').length / 52));
      text.value = r.note || '';
      text.addEventListener('change', () => patch(r.id, { note: text.value.trim() }));

      const meta = document.createElement('div');

      meta.className = 'r-meta';
      meta.innerHTML =
        `<span>${r.route}</span>` +
        (r.where ? `<span>${r.where}</span>` : '') +
        `<button type="button" class="r-toggle">${r.done ? 'rouvrir' : 'traité'}</button>` +
        '<button type="button" class="r-del">supprimer</button>';

      meta.querySelector('.r-toggle').addEventListener('click', () => patch(r.id, { done: !r.done }));
      meta.querySelector('.r-del').addEventListener('click', () => remove(r.id));

      item.append(text, meta);
      rows.append(item);
    }
  }

  async function patch(id, body) {
    await fetch(ENDPOINT, {
      method: 'PATCH',
      headers: { 'content-type': 'application/json' },
      body: JSON.stringify({ id, ...body }),
    });
    refreshList();
  }

  async function remove(id) {
    await fetch(`${ENDPOINT}?id=${encodeURIComponent(id)}`, { method: 'DELETE' });
    refreshList();
  }

  const openList = (on) => {
    list.hidden = !on;
    if (on) refreshList();
  };

  showAll.addEventListener('change', refreshList);
  host.querySelector('.r-close').addEventListener('click', () => openList(false));

  arm.addEventListener('click', () => setArmed(veil.hidden));
  arm.addEventListener('contextmenu', (e) => {
    e.preventDefault();
    setArmed(false);
    openList(list.hidden);
  });

  addEventListener('keydown', (e) => {
    if (e.ctrlKey && e.shiftKey && e.key.toLowerCase() === HOTKEY) {
      e.preventDefault();
      setArmed(veil.hidden);
    } else if (e.ctrlKey && e.shiftKey && e.key.toLowerCase() === HOTKEY_LIST) {
      e.preventDefault();
      setArmed(false);
      form.hidden = true;
      openList(list.hidden);
    } else if (e.ctrlKey && e.shiftKey && e.key.toLowerCase() === HOTKEY_NOTE) {
      // Not everything has a box round it -- a transition, a flow, a page taken as a whole.
      e.preventDefault();
      setArmed(false);
      picked = [];
      rect = null;
      count.textContent = 'note sur la page, sans sélection';
      form.hidden = false;
      note.focus();
    } else if (e.key === 'Escape') {
      setArmed(false);
      form.hidden = true;
      list.hidden = true;
    }
  });

  veil.addEventListener('pointerdown', (e) => {
    start = { x: e.clientX, y: e.clientY };
    veil.setPointerCapture(e.pointerId);
  });

  veil.addEventListener('pointermove', (e) => {
    if (!start) return;

    const x = Math.min(start.x, e.clientX);
    const y = Math.min(start.y, e.clientY);

    box.style.left = `${x}px`;
    box.style.top = `${y}px`;
    box.style.width = `${Math.abs(e.clientX - start.x)}px`;
    box.style.height = `${Math.abs(e.clientY - start.y)}px`;
  });

  veil.addEventListener('pointerup', (e) => {
    if (!start) return;

    rect = {
      left: Math.min(start.x, e.clientX),
      top: Math.min(start.y, e.clientY),
      right: Math.max(start.x, e.clientX),
      bottom: Math.max(start.y, e.clientY),
    };
    start = null;

    // A click rather than a drag: take whatever is under the pointer.
    if (rect.right - rect.left < 6 && rect.bottom - rect.top < 6) {
      veil.hidden = true;
      const el = document.elementFromPoint(e.clientX, e.clientY);

      veil.hidden = false;
      picked = el ? [el] : [];
    } else {
      picked = selected(rect);
    }

    count.textContent = picked.length
      ? `${picked.length} élément(s) — ${picked.map((el) => source(el)).filter(Boolean)[0] ?? 'source inconnue'}`
      : 'aucun élément — la note partira seule';
    form.hidden = false;
    note.focus();
  });

  host.querySelector('.r-cancel').addEventListener('click', () => {
    form.hidden = true;
    setArmed(false);
  });

  form.addEventListener('submit', async (e) => {
    e.preventDefault();

    const report = {
      note: note.value.trim(),
      route: location.hash || '#/',
      theme: document.documentElement.getAttribute('data-theme'),
      viewport: `${innerWidth}x${innerHeight}`,
      area: rect && {
        x: Math.round(rect.left),
        y: Math.round(rect.top),
        w: Math.round(rect.right - rect.left),
        h: Math.round(rect.bottom - rect.top),
      },
      elements: picked.slice(0, 25).map(describe),
      html: excerpt(picked),
    };

    try {
      const res = await fetch(ENDPOINT, {
        method: 'POST',
        headers: { 'content-type': 'application/json' },
        body: JSON.stringify(report),
      });

      if (!res.ok) throw new Error(String(res.status));

      note.value = '';
      form.hidden = true;
      setArmed(false);
      say('Signalé');
      if (!list.hidden) refreshList();
    } catch (err) {
      say(`Échec: ${err.message}`, false);
    }
  });
}
