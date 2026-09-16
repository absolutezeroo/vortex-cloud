#!/usr/bin/env node
// One bug class, five seams.
//
// Every one of these is a place where a declaration on one side has to match an implementation on
// the other, where the compiler sees neither side, and where a mismatch produces SILENCE rather
// than an error: the box saves nothing, the packet never arrives, the handler never runs, the knob
// does nothing. They are the "we forgot something obvious again" bugs, and they have all shipped:
//
//   seam 1  wired int params    a box reads IntParams[N] it never declared a rule for
//                               -> the value is always the default, nothing says so
//   seam 2  wired variable flags a reading declares CanWriteValue with no write behind it
//                               -> the client offers it in the picker, selecting it does nothing
//                               (shipped: @type and @position.x, fixed in 482acb1)
//   seam 3  inbound chain       a handler whose message has no parser mapped to a header id
//                               -> the client sends, PackageHandler logs "Incoming Unknown", done
//   seam 4  outbound composers  a composer built and sent with no serializer mapped
//                               -> PackageEncoder returns 0, SuperSocket writes nothing
//   seam 5  runtime config      a key read from IServerConfigGrain but absent from ConfigKeyCatalog
//                               -> the setting is real and no operator can find it
//
//   node scripts/hooks/check-inert-declarations.mjs           compare, exit 2 on a NEW gap
//   node scripts/hooks/check-inert-declarations.mjs --update  accept the current gaps as baseline
//   node scripts/hooks/check-inert-declarations.mjs --all     print every gap, baselined or not
//
// Ratchet, not cleanup: the gaps that exist today are listed in inert-declarations-baseline.json
// with a reason each. A new one fails.
import { readFileSync, writeFileSync, existsSync, readdirSync, statSync } from 'node:fs';
import { fileURLToPath } from 'node:url';
import { dirname, join, resolve, sep } from 'node:path';

const here = dirname(fileURLToPath(import.meta.url));
const root = resolve(here, '../..');
const baselineFile =
  process.env.VORTEX_INERT_BASELINE ?? join(here, 'inert-declarations-baseline.json');
const update = process.argv.includes('--update');
const showAll = process.argv.includes('--all');

// ---- shared helpers ------------------------------------------------------------------------

function walk(rel, out = []) {
  const abs = join(root, rel);
  if (!existsSync(abs)) return out;
  for (const entry of readdirSync(abs, { withFileTypes: true })) {
    if (entry.name === 'obj' || entry.name === 'bin' || entry.name === 'node_modules') continue;
    const child = join(rel, entry.name);
    if (entry.isDirectory()) walk(child, out);
    else if (entry.name.endsWith('.cs')) out.push(child);
  }
  return out;
}

const read = (rel) => readFileSync(join(root, rel), 'utf8');
// Comments explain the bugs these checks look for, so they have to go before matching or a
// paragraph about a removed flag reads as the flag still being declared.
const stripComments = (src) => src.replace(/\/\/[^\n]*/g, '').replace(/\/\*[\s\S]*?\*\//g, '');

const projects = readdirSync(root).filter(
  (d) => d.startsWith('Vortex.') && statSync(join(root, d)).isDirectory()
);
const productionFiles = projects
  .filter((p) => !p.endsWith('Tests') && p !== 'Vortex.Tests.Support')
  .flatMap((p) => walk(p));

// The body of a member, whether it is `=> expr;` or `{ ... }`.
function memberBody(src, signature) {
  const m = signature.exec(src);
  if (!m) return null;
  const start = m.index + m[0].length;
  const arrow = /^\s*=>/.exec(src.slice(start));
  if (arrow) {
    let depth = 0;
    for (let i = start + arrow[0].length; i < src.length; i++) {
      const c = src[i];
      if ('([{'.includes(c)) depth++;
      else if (')]}'.includes(c)) depth--;
      else if (c === ';' && depth <= 0) return src.slice(start, i);
    }
    return src.slice(start);
  }
  const brace = src.indexOf('{', start);
  if (brace < 0) return null;
  let depth = 0;
  for (let i = brace; i < src.length; i++) {
    if (src[i] === '{') depth++;
    else if (src[i] === '}' && --depth === 0) return src.slice(brace, i + 1);
  }
  return null;
}

// Elements of the first list literal in a body, counted at depth 1 so a nested call does not add.
function countListElements(body) {
  let text = body.trim();
  if (text.startsWith('{')) text = text.slice(1, -1);
  const open = text.search(/\[/) >= 0 ? text.search(/\[/) : text.search(/\{/);
  if (open < 0) return null;
  let depth = 0;
  let elements = 0;
  let sawContent = false;
  for (let i = open; i < text.length; i++) {
    const c = text[i];
    if ('([{'.includes(c)) depth++;
    else if (')]}'.includes(c)) {
      if (--depth === 0) break;
    } else if (depth === 1) {
      if (c === ',') elements++;
      else if (!/\s/.test(c)) sawContent = true;
    }
  }
  return sawContent ? elements + 1 : 0;
}

const gaps = [];
const gap = (seam, id, detail, where) => gaps.push({ seam, id, detail, where });

// ---- seam 1: a wired box reads an int param it declared no rule for -------------------------
// TryNormalizeIntParams keeps exactly GetIntParamRules().Count values (or more, with a tail rule).
// Reading past that is reading a default the client can never set.

const WIRED_LOGIC_DIR = 'Vortex.Rooms/Object/Logic/Furniture/Floor/Wired';
const wiredClasses = new Map();
for (const rel of walk(WIRED_LOGIC_DIR)) {
  const src = stripComments(read(rel));
  const decl =
    /(?:^|\n)\s*(?:public |internal |protected )*(abstract |sealed )?class\s+([A-Za-z0-9_]+)(?:<[^>]*>)?\s*(?:\([^)]*\))?\s*:\s*([A-Za-z0-9_]+)/.exec(
      src
    );
  if (!decl) continue;
  const [, modifier, name, base] = decl;
  const rulesBody = memberBody(src, /List<IWiredParamRule>\s+GetIntParamRules\s*\(\s*\)/);
  let own = null;
  let usesBase = false;
  if (rulesBody !== null) {
    usesBase = /base\.GetIntParamRules\s*\(/.test(rulesBody);
    const added = (rulesBody.match(/rules\.Add\s*\(/g) || []).length;
    own = added > 0 ? added : countListElements(rulesBody);
    // `[.. base.GetIntParamRules(), x]` counts the spread as an element; it is not a rule.
    if (usesBase && added === 0 && own !== null) own = Math.max(0, own - 1);
  }
  const tailBody = memberBody(src, /IWiredParamRule\??\s+GetIntParamTailRule\s*\(\s*\)/);
  const indices = [
    ...[...src.matchAll(/GetIntParam<[^>]*>\(\s*(\d+)\s*\)/g)].map((m) => +m[1]),
    ...[...src.matchAll(/IntParams\[\s*(\d+)\s*\]/g)].map((m) => +m[1]),
    ...[...src.matchAll(/IntParams\.ElementAtOrDefault\(\s*(\d+)\s*\)/g)].map((m) => +m[1]),
  ];
  wiredClasses.set(name, {
    rel,
    name,
    base,
    own,
    usesBase,
    hasTail: tailBody !== null && /new\s+Wired/.test(tailBody),
    maxRead: indices.length ? Math.max(...indices) : -1,
    logicKey: (/\[RoomObjectLogic\("([^"]+)"\)\]/.exec(src) || [])[1] ?? null,
    isAbstract: (modifier ?? '').trim() === 'abstract',
  });
}
const wiredChain = (name, acc = []) => {
  const c = wiredClasses.get(name);
  if (!c || acc.includes(c)) return acc;
  acc.push(c);
  return wiredChain(c.base, acc);
};
for (const box of wiredClasses.values()) {
  if (box.isAbstract || !box.logicKey) continue;
  const chain = wiredChain(box.name);
  let declared = 0;
  for (const link of chain) {
    if (link.own === null) continue;
    declared += link.own;
    if (!link.usesBase) break;
  }
  if (chain.some((l) => l.hasTail)) continue;
  const maxRead = Math.max(...chain.map((l) => l.maxRead));
  if (maxRead >= declared) {
    gap(
      'wired-int-params',
      box.logicKey,
      `reads IntParams[${maxRead}] but declares ${declared} rule(s): that value can never be saved`,
      box.rel
    );
  }
}

// ---- seam 2: a wired variable declares a capability it does not implement --------------------
// The client filters its pickers on these flags. A flag with no code is a selectable line that
// does nothing; code with no flag is a capability that is never offered.

const variableClasses = new Map();
for (const rel of [...walk('Vortex.Rooms/Wired/Variables'), ...walk('Vortex.Rooms/Wired/LevelUp')]) {
  const src = stripComments(read(rel));
  for (const m of src.matchAll(
    /(?:^|\n)\s*(?:public |internal |protected )*(abstract |sealed )?class\s+([A-Za-z0-9_]+)(?:<[^>]*>)?\s*(?:\([^)]*\))?\s*:\s*([A-Za-z0-9_]+)/g
  )) {
    const [, modifier, name, base] = m;
    const tail = src.slice(m.index);
    const flagsDecl = /Flags\s*=>\s*([^;]+);/.exec(tail);
    variableClasses.set(name, {
      rel,
      name,
      base,
      isAbstract: (modifier ?? '').trim() === 'abstract',
      flags: flagsDecl
        ? [...flagsDecl[1].matchAll(/WiredVariableFlags\.([A-Za-z]+)/g)].map((f) => f[1])
        : null,
      varName: (/VariableName\s*=>\s*"([^"]+)"/.exec(tail) || [])[1] ?? null,
      write: /override\s+(?:async\s+)?Task<bool>\s+Set(?:ValueAsync|ValueFor[A-Za-z]*Async)/.test(src),
      timestamps: /override\s+bool\s+TryGetTimestamps/.test(src),
      connectors: /override\s+Dictionary<WiredVariableValue,\s*string>\s+GetTextConnectors/.test(src),
      readValue:
        /override\s+bool\s+TryGetValue|override\s+WiredVariableValue\s+GetValueFor/.test(src),
    });
  }
}
const variableChain = (name, acc = []) => {
  const c = variableClasses.get(name);
  if (!c || acc.includes(c)) return acc;
  acc.push(c);
  return variableChain(c.base, acc);
};
for (const variable of variableClasses.values()) {
  if (variable.isAbstract || !variable.flags) continue;
  const chain = variableChain(variable.name);
  const has = (key) => chain.some((l) => l[key]);
  const flags = new Set(variable.flags);
  const label = variable.varName ?? variable.name;
  const complain = (detail) => gap('wired-variable-flags', label, detail, variable.rel);

  if (flags.has('CanWriteValue') && !has('write'))
    complain('declares CanWriteValue with no write behind it: the picker offers it, selecting it does nothing');
  if (!flags.has('CanWriteValue') && variable.write)
    complain('implements a write without CanWriteValue: the client never offers it');
  if ((flags.has('CanReadCreationTime') || flags.has('CanReadLastUpdateTime')) && !has('timestamps'))
    complain('declares a time reading with no TryGetTimestamps');
  if (flags.has('HasTextConnector') && !has('connectors'))
    complain('declares HasTextConnector with no GetTextConnectors');
  if (flags.has('HasValue') && !has('readValue'))
    complain('declares HasValue with no read');
}

// ---- seam 6: a wired box whose behaviour never reads its own configuration -------------------
// The box is in the catalogue, its dialog is complete, it saves. Its one method returns a constant
// or sits behind a commented-out block, so a builder configures it and the chain does nothing.

const PRINCIPAL_METHODS = [
  'ExecuteAsync',
  'SelectAsync',
  'CanTriggerAsync',
  'EvaluateAsync',
  'IsSatisfiedAsync',
  'MutatePolicyAsync',
  'ApplyToTextAsync',
];
for (const rel of walk(WIRED_LOGIC_DIR)) {
  const raw = read(rel);
  const key = (/\[RoomObjectLogic\("([^"]+)"\)\]/.exec(raw) || [])[1];
  if (!key) continue;
  for (const method of PRINCIPAL_METHODS) {
    const body = memberBody(
      raw,
      new RegExp(`(?:public|protected)[^\\n;{]*\\b${method}\\s*\\([^)]*\\)`)
    );
    if (body === null) continue;
    const commentedLines = Math.max(
      0,
      ...(body.match(/\/\*[\s\S]*?\*\//g) ?? []).map((b) => b.split('\n').length - 1)
    );
    const clean = stripComments(body);
    const readsConfig = /_wiredData|_ctx\.|ctx\.|Get(?:IntParam|StuffIds|SnapshotFurni)/.test(clean);
    const statements = clean.split('\n').filter((l) => l.trim() && !'{}'.includes(l.trim())).length;
    if (commentedLines >= 4)
      gap(
        'wired-inert-behaviour',
        key,
        `${method} is a ${commentedLines}-line commented-out block: the box configures, saves and does nothing`,
        rel
      );
    else if (!readsConfig && statements <= 3)
      gap(
        'wired-inert-behaviour',
        key,
        `${method} returns a constant without reading the box's own configuration`,
        rel
      );
  }
}

// ---- seam 3: a handler whose message never reaches the dispatcher ----------------------------
// contract -> parser -> header id -> handler. Miss the middle and the handler is unreachable code
// that compiles, tests and ships.

const incomingContracts = new Set();
for (const rel of walk('Vortex.Protocol')) {
  if (!rel.includes(`Messages${sep}Incoming`)) continue;
  for (const m of read(rel).matchAll(
    /(?:^|\n)\s*(?:public |internal )(?:sealed |abstract )?(?:class|record)\s+([A-Za-z0-9_]+)/g
  ))
    incomingContracts.add(m[1]);
}
const mappedParsers = new Set();
for (const rel of walk('Vortex.Revisions'))
  for (const m of read(rel).matchAll(/MapParser\(\s*MessageEvent\.[A-Za-z0-9_]+\s*,\s*new\s+([A-Za-z0-9_]+)/g))
    mappedParsers.add(m[1]);
const parsedTypes = new Set();
for (const rel of walk('Vortex.Revisions')) {
  if (!rel.includes('Parsers')) continue;
  const src = read(rel);
  const classNames = [
    ...src.matchAll(/(?:^|\n)\s*(?:public |internal )?(?:sealed )?class\s+([A-Za-z0-9_]+)/g),
  ].map((m) => m[1]);
  if (!classNames.some((c) => mappedParsers.has(c))) continue;
  for (const m of [
    ...src.matchAll(/(?:return|=>)\s*new\s+([A-Za-z0-9_]+)/g),
    ...src.matchAll(/typeof\(\s*([A-Za-z0-9_]+Message)\s*\)/g),
  ])
    if (incomingContracts.has(m[1])) parsedTypes.add(m[1]);
}
const handlers = new Map();
for (const rel of walk('Vortex.PacketHandlers'))
  for (const m of read(rel).matchAll(/IMessageHandler<\s*([A-Za-z0-9_]+)\s*>/g))
    if (!handlers.has(m[1])) handlers.set(m[1], rel);
for (const [message, rel] of handlers)
  if (!parsedTypes.has(message))
    gap('inbound-chain', message, 'a handler exists but no mapped parser produces this message: it can never run', rel);
for (const message of parsedTypes)
  if (!handlers.has(message))
    gap('inbound-chain', message, 'parsed off the wire and handled by nobody', 'Vortex.Revisions');

// ---- seam 4: a composer sent with no serializer ----------------------------------------------
// PackageEncoder.Encode returns 0 and SuperSocket writes nothing. One warning, one counter, and a
// client that waits forever for a reply that was never bytes.

const outgoingContracts = new Set();
for (const rel of walk('Vortex.Protocol')) {
  if (!rel.includes(`Messages${sep}Outgoing`)) continue;
  // Only the types that are actually composers: Messages/Outgoing also holds the payload
  // sub-records nested inside them, which have no serializer of their own and need none.
  for (const m of read(rel).matchAll(
    /(?:^|\n)\s*(?:public |internal )(?:sealed |abstract )?(?:class|record)\s+([A-Za-z0-9_]+)[^{;]*:\s*[^{;]*\bIComposer\b/g
  ))
    outgoingContracts.add(m[1]);
}
const mappedSerializers = new Set();
for (const rel of walk('Vortex.Revisions'))
  for (const m of read(rel).matchAll(/MapSerializer\(\s*typeof\(\s*([A-Za-z0-9_]+)\s*\)/g))
    mappedSerializers.add(m[1]);
const builtComposers = new Map();
for (const rel of productionFiles) {
  if (rel.startsWith('Vortex.Revisions') || rel.startsWith('Vortex.Protocol')) continue;
  for (const m of stripComments(read(rel)).matchAll(/new\s+([A-Za-z0-9_]+)\s*[({]/g))
    if (outgoingContracts.has(m[1]) && !builtComposers.has(m[1])) builtComposers.set(m[1], rel);
}
for (const [composer, rel] of builtComposers)
  if (!mappedSerializers.has(composer))
    gap('outbound-composers', composer, 'built and sent with no serializer mapped: the packet is dropped at the encoder', rel);

// ---- seam 5: a runtime setting no operator can see -------------------------------------------
// ConfigKeyCatalog is what the dashboard's config editor renders. A key read from
// IServerConfigGrain and absent from it is a setting that exists and cannot be reached.

const catalogFile = 'Vortex.Primitives/Server/ConfigKeyCatalog.cs';
const catalogKeys = new Set(
  [...read(catalogFile).matchAll(/new\(\s*\n?\s*"([a-z0-9_.]+)"/g)].map((m) => m[1])
);
for (const rel of productionFiles) {
  if (rel === catalogFile) continue;
  const src = read(rel);
  // Only files that actually talk to the config grain: a `const string ... Key` elsewhere is
  // something else's key (a spec block name, a role) and not an operator setting.
  if (!/IServerConfigGrain|ServerConfigValues|GetManyAsync|GetIntAsync|GetValueAsync|GetBoolAsync/.test(src))
    continue;
  for (const m of src.matchAll(/const\s+string\s+[A-Za-z0-9_]*Key\s*=\s*"([a-z0-9_]+(?:\.[a-z0-9_]+)+)"/g))
    if (!catalogKeys.has(m[1]))
      gap('config-catalog', m[1], 'read from IServerConfigGrain but absent from ConfigKeyCatalog: no operator can set it', rel);
}

// ---- report ----------------------------------------------------------------------------------

gaps.sort((a, b) => a.seam.localeCompare(b.seam) || a.id.localeCompare(b.id));
const fingerprint = (g) => `${g.seam}/${g.id}`;

if (update) {
  const notes = existsSync(baselineFile) ? (JSON.parse(readFileSync(baselineFile, 'utf8')).notes ?? {}) : {};
  writeFileSync(
    baselineFile,
    `${JSON.stringify({ known: gaps.map(fingerprint), notes }, null, 2)}\n`
  );
  console.error(`check-inert-declarations: baseline written (${gaps.length} known gap(s)).`);
  process.exit(0);
}

const baseline = existsSync(baselineFile)
  ? new Set(JSON.parse(readFileSync(baselineFile, 'utf8')).known)
  : new Set();
const added = gaps.filter((g) => !baseline.has(fingerprint(g)));
const fixed = [...baseline].filter((k) => !gaps.some((g) => fingerprint(g) === k));

const print = (list, title) => {
  if (!list.length) return;
  console.error(`\n${title}`);
  let seam = null;
  for (const g of list) {
    if (g.seam !== seam) {
      seam = g.seam;
      console.error(`\n  [${seam}]`);
    }
    console.error(`    ${g.id}`);
    console.error(`        ${g.detail}`);
    console.error(`        ${g.where}`);
  }
};

if (showAll) print(gaps, `All gaps (${gaps.length}), baselined or not:`);

if (fixed.length)
  console.error(
    `check-inert-declarations: ${fixed.length} baselined gap(s) are gone. Run --update to drop them: ${fixed.join(', ')}`
  );

if (!added.length) {
  console.error(
    `check-inert-declarations: OK (${wiredClasses.size} wired boxes, ${variableClasses.size} variable readings, ` +
      `${handlers.size} handlers, ${builtComposers.size} composers, ${catalogKeys.size} config keys; ` +
      `${gaps.length} known gap(s) baselined).`
  );
  process.exit(0);
}

print(added, `check-inert-declarations: ${added.length} NEW silent gap(s).`);
console.error(
  '\nEach of these compiles, passes its tests and does nothing at runtime. Close the seam rather\n' +
    "than baselining it; --update is for gaps you have decided to live with, and each one wants a\n" +
    'line in the baseline notes saying why.'
);
process.exit(2);
