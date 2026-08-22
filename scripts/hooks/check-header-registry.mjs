#!/usr/bin/env node
// Every header id we map must exist in the client's own message registry.
//
// This is the check that the old ceiling heuristic was approximating. An id above the client's
// maximum is dead, yes -- but so is an id in the middle of the range that the client simply never
// registered, and there are far more of those. Both register cleanly on our side, both can never
// fire, and neither shows up in the build, the tests or a grep. 14 of our 955 mapped headers are in
// that state right now: 14 handlers and composers that look implemented and are not reachable.
//
//   node scripts/hooks/check-header-registry.mjs
//   node scripts/hooks/check-header-registry.mjs --update   # accept the current list as baseline
//
// The client checkout lives outside this repository (same rule as the specs: a sibling holding a
// `sources` directory). Without it there is no registry to compare against, so the script falls
// back to the ceiling check -- weaker, but it needs nothing but this repository.
//
// Exit 2 = a mapped id the client's registry does not contain, and the baseline does not know about.
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..', '..');
// The override exists so the hook test can point at an empty baseline and see the block happen.
const baselineFile =
  process.env.VORTEX_HEADER_BASELINE ?? path.join(root, 'scripts', 'hooks', 'header-registry-baseline.json');
const revision = 'Revision20260701';
const headersFile = path.join(root, 'Vortex.Revisions', revision, 'Headers.cs');
const mapsDir = path.join(root, 'Vortex.Revisions', revision, 'Maps');
const update = process.argv.includes('--update');

// Highest id in the registry of the client build this revision targets. Only used when the client
// sources are absent; when they are present the registry itself is the answer.
const CEILING = 4101;
// Ids deliberately outside the official range: only a modified client speaks these.
const EXTENSIONS = new Set([
  'VortexGetFurniEditorDataMessageEvent',
  'VortexFurniEditorDataMessageComposer',
  'VortexApplyFurniEditMessageEvent',
  'VortexFurniEditorRightsMessageComposer',
  'VortexGetFurniDefinitionMessageEvent',
  'VortexFurniDefinitionMessageComposer',
  'VortexApplyFurniDefinitionMessageEvent',
  'RentableSpaceGetConfigMessageEvent',
  'RentableSpaceConfigMessageComposer',
  'RentableSpaceConfigureMessageEvent',
]);

// ---- our side ------------------------------------------------------------------------------------
const headers = {};
let section = '';
for (const line of fs.readFileSync(headersFile, 'utf8').split(/\r?\n/)) {
  const enclosing = /class\s+(MessageEvent|MessageComposer)\b/.exec(line);
  if (enclosing) {
    section = enclosing[1];
    continue;
  }
  const constant = /const\s+int\s+([A-Za-z0-9_]+)\s*=\s*(\d+)\s*;/.exec(line);
  if (constant) headers[constant[1]] = { id: Number(constant[2]), section };
}

// A constant nobody maps is inert. Only what the revision actually wires up can be dead.
const mapped = new Set();
for (const file of fs.readdirSync(mapsDir)) {
  const text = fs.readFileSync(path.join(mapsDir, file), 'utf8');
  for (const use of text.matchAll(/Message(?:Event|Composer)\.([A-Za-z0-9_]+)/g)) mapped.add(use[1]);
}
const ours = [...mapped].filter((name) => headers[name]);

// ---- the client's registry -----------------------------------------------------------------------
function findClientTrees() {
  const parent = path.dirname(root);
  const roots = [];
  for (const entry of fs.readdirSync(parent, { withFileTypes: true })) {
    if (!entry.isDirectory()) continue;
    const sources = path.join(parent, entry.name, 'sources');
    if (!fs.existsSync(sources)) continue;
    for (const build of fs.readdirSync(sources, { withFileTypes: true })) {
      if (!build.isDirectory()) continue;
      const src = path.join(sources, build.name, 'src');
      if (fs.existsSync(path.join(src, 'com', 'sulake', 'habbo', 'communication')))
        roots.push({ src, build: build.name });
    }
  }
  return roots;
}

function indexClasses(src) {
  const files = new Map();
  (function walk(dir) {
    for (const entry of fs.readdirSync(dir, { withFileTypes: true })) {
      const full = path.join(dir, entry.name);
      if (entry.isDirectory()) walk(full);
      else if (entry.name.endsWith('.as')) files.set(entry.name.slice(0, -3), full);
    }
  })(src);
  return files;
}

// The registry is one class holding two id tables. Its file name is obfuscated and changes with the
// build, so find it by content: the file in communication/ with the most `table[id] = Class` lines.
function readRegistry(src) {
  const dir = path.join(src, 'com', 'sulake', 'habbo', 'communication');
  let best = { count: 0, entries: [] };
  for (const name of fs.readdirSync(dir).filter((f) => f.endsWith('.as'))) {
    const entries = [
      ...fs.readFileSync(path.join(dir, name), 'utf8').matchAll(/(\w+)\[(\d+)\]\s*=\s*([A-Za-z_]\w*)\s*;/g),
    ].map((m) => ({ table: m[1], id: Number(m[2]), cls: m[3] }));
    if (entries.length > best.count) best = { count: entries.length, entries };
  }
  return best.entries;
}

// Which table is which direction, decided from the classes themselves rather than from a variable
// name the next client build will rename. A class the client COMPOSES is one it sends: that is an
// incoming message for us. One that `extends MessageEvent` is one it receives: our composer.
function directionOf(table, entries, classFiles) {
  let sent = 0;
  let received = 0;
  for (const entry of entries.filter((e) => e.table === table).slice(0, 24)) {
    const file = classFiles.get(entry.cls);
    if (!file) continue;
    const text = fs.readFileSync(file, 'utf8');
    if (/implements\s+IMessageComposer/.test(text)) sent++;
    else if (/extends\s+MessageEvent\b|implements\s+IMessageParser/.test(text)) received++;
  }
  if (sent === 0 && received === 0) return null; // no signal: refuse to guess a direction
  return sent > received ? 'MessageEvent' : 'MessageComposer';
}

// More than one client can sit beside the repository -- this checkout has the 2016 Flash client and
// two WIN63 builds. Only the one this revision targets can arbitrate its ids, so match on the
// revision's own date before falling back to whichever registry is largest.
const stamp = /(\d{6})/.exec(revision)?.[1] ?? '';
const trees = findClientTrees().sort(
  (a, b) => Number(b.build.includes(stamp)) - Number(a.build.includes(stamp))
);
let known = null;

if (trees.length > 0) {
  const src = trees[0].src;
  const entries = readRegistry(src);
  if (entries.length > 0) {
    const classFiles = indexClasses(src);
    const found = { MessageEvent: new Set(), MessageComposer: new Set() };
    let directions = 0;
    for (const table of new Set(entries.map((e) => e.table))) {
      const direction = directionOf(table, entries, classFiles);
      if (!direction) continue;
      directions++;
      for (const entry of entries.filter((e) => e.table === table)) found[direction].add(entry.id);
    }
    // Both directions or nothing: a half-read registry would report every composer as unreachable.
    if (directions >= 2 && found.MessageEvent.size > 0 && found.MessageComposer.size > 0) known = found;
  }
}

// ---- fallback: the ceiling, when there is no client to ask ----------------------------------------
if (!known) {
  const above = ours.filter((name) => headers[name].id > CEILING && !EXTENSIONS.has(name));
  console.error(
    'check-header-registry: no client sources beside this repository -- falling back to the id ceiling.'
  );
  if (above.length) {
    console.error(`\nMapped header ids above the ${revision} client ceiling (${CEILING}):`);
    for (const name of above) console.error(`  - ${headers[name].section}.${name} = ${headers[name].id}`);
    console.error('\nAn id the client does not know registers fine and never fires.');
    process.exit(2);
  }
  console.error(`check-header-registry: ceiling OK (${ours.length} mapped headers).`);
  process.exit(0);
}

// ---- compare -------------------------------------------------------------------------------------
const unreachable = ours
  .filter((name) => !EXTENSIONS.has(name) && !known[headers[name].section].has(headers[name].id))
  .map((name) => `${headers[name].section}/${name}=${headers[name].id}`)
  .sort();

if (update) {
  // Keep the file's `notes`: they say why an entry is tolerated, and regenerating the list is not a
  // reason to lose that.
  const notes = fs.existsSync(baselineFile)
    ? (JSON.parse(fs.readFileSync(baselineFile, 'utf8')).notes ?? {})
    : {};
  fs.writeFileSync(baselineFile, `${JSON.stringify({ unreachable, notes }, null, 2)}\n`);
  console.error(`check-header-registry: baseline written (${unreachable.length} unreachable mappings).`);
  process.exit(0);
}

if (!fs.existsSync(baselineFile)) {
  console.error(`check-header-registry: no baseline at ${path.relative(root, baselineFile)}. Run with --update.`);
  process.exit(2);
}

const baseline = new Set(JSON.parse(fs.readFileSync(baselineFile, 'utf8')).unreachable);
const added = unreachable.filter((entry) => !baseline.has(entry));
const fixed = [...baseline].filter((entry) => !unreachable.includes(entry));

for (const entry of fixed) console.error(`warning: ${entry} is reachable now -- run --update to lock it in`);

if (added.length) {
  console.error(`\nMapped to a header id the client's registry does not contain (${added.length}):`);
  for (const entry of added) console.error(`  - ${entry}`);
  console.error(
    '\nThe handler or composer registers cleanly and can never fire. Find the id the client actually\n' +
      "binds for this message in its registry, or drop the mapping. Don't baseline it to move on."
  );
  process.exit(2);
}

console.error(
  `check-header-registry: OK (${ours.length} mapped headers, ${unreachable.length} known unreachable, registry from ${trees[0].build}).`
);
