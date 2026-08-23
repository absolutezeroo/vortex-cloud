#!/usr/bin/env node
// The boundaries CONTEXT.md states in prose, made executable.
//
// Four rules that no compiler, test or grep enforces today, all of them currently respected --
// which is exactly why encoding them is cheap: there is nothing to clean up first, only a state to
// hold. A rule that lives in a document is a rule that erodes on the first busy afternoon; the
// stale project references this repository carried for months are the proof.
//
//   node scripts/hooks/check-architecture-walls.mjs
//   node scripts/hooks/check-architecture-walls.mjs --update   # re-baseline leaks and the
//                                                              # protocol-free project list
//
// Exit 2 = a new violation. Exit 0 = the walls still stand.
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..', '..');
// The override exists so the hook test can point the check at an empty baseline and see it block.
const baselineFile =
  process.env.VORTEX_WALLS_BASELINE ??
  path.join(root, 'scripts', 'hooks', 'architecture-walls-baseline.json');
const update = process.argv.includes('--update');

/** Source files of a project, minus build output. */
function sources(project) {
  const dir = path.join(root, project);
  if (!fs.existsSync(dir)) return [];
  const out = [];
  const walk = (d) => {
    for (const e of fs.readdirSync(d, { withFileTypes: true })) {
      if (e.name === 'obj' || e.name === 'bin') continue;
      const p = path.join(d, e.name);
      if (e.isDirectory()) walk(p);
      else if (e.name.endsWith('.cs')) out.push(p);
    }
  };
  walk(dir);
  return out;
}

const rel = (p) => path.relative(root, p).split(path.sep).join('/');

// A mention inside a comment is prose about the rule, not a breach of it -- the emulator guard hook
// learned this the hard way when a commit message describing it blocked that very commit.
const isComment = (line) => /^\s*(\/\/|\/\*|\*)/.test(line);

/** Lines of `project` matching `pattern`, ignoring comments. */
function hits(project, pattern, skip = () => false) {
  const found = [];
  for (const file of sources(project)) {
    if (skip(rel(file))) continue;
    const lines = fs.readFileSync(file, 'utf8').split(/\r?\n/);
    lines.forEach((line, i) => {
      if (!isComment(line) && pattern.test(line)) {
        found.push(`${rel(file)}:${i + 1}  ${line.trim().slice(0, 100)}`);
      }
    });
  }
  return found;
}

const failures = [];

// ---------------------------------------------------------------------------------------------
// Wall 1 -- packet handlers stay orchestration-only.
//
// CONTEXT.md: handlers do not query database contexts or repositories. They route to a grain and
// nothing else. Currently zero violations across 536 handler files; this holds that at zero.
// ---------------------------------------------------------------------------------------------
const handlerDb = hits(
  'Vortex.PacketHandlers',
  /\b(IDbContextFactory|VortexDbContext|SaveChangesAsync)\b/
);
if (handlerDb.length > 0) {
  failures.push([
    'A packet handler reaches for the database. Handlers orchestrate; the grain owns the state.',
    handlerDb,
  ]);
}

// ---------------------------------------------------------------------------------------------
// Wall 2 -- the admin surface writes through grains, never behind their back.
//
// The dashboard reads the database directly (one read service, deliberately) but every mutation
// goes through the owning grain, so the single-writer-per-aggregate rule survives contact with the
// admin pages. A SaveChanges here would be a second writer that the grain never learns about.
// ---------------------------------------------------------------------------------------------
const dashboardWrites = hits('Vortex.Dashboard.API', /\bSaveChangesAsync\b/);
if (dashboardWrites.length > 0) {
  failures.push([
    'The dashboard writes to the database directly. Route the mutation through the owning grain.',
    dashboardWrites,
  ]);
}

// ---------------------------------------------------------------------------------------------
// Wall 3 -- protocol does not leak into contracts.
//
// Vortex.Primitives is referenced by every project in the solution; Vortex.Protocol holds the 1,092
// message records and is the highest-churn layer there is. While the two were one project, editing a
// composer rebuilt everything, and a grain interface typed in a wire record made the client's byte
// layout the domain model. Ten files were in that state and the baseline ratcheted them to zero, so
// this is now a hard zero rather than a ratchet: the hub does not reference the protocol at all.
//
// The pattern matches Vortex.Protocol, not the old Vortex.Primitives.Messages. That rename is
// exactly how this check could have died without anyone noticing -- it would have matched nothing
// and reported the walls holding, which is why the hook test drives it with a real violation.
// ---------------------------------------------------------------------------------------------
const leak = hits('Vortex.Primitives', /^\s*using\s+Vortex\.Protocol\b/).map((h) =>
  h.split(':')[0]
);
const leakFiles = [...new Set(leak)].sort();

// ---------------------------------------------------------------------------------------------
// Wall 4 -- the projects that hold no protocol keep holding none.
//
// Splitting Vortex.Protocol out of the hub is worth a measured 74s -> 17s on a composer edit,
// because the projects below stop rebuilding entirely: they contain no wire types at all. That win
// is one ProjectReference away from being handed back, and nothing about giving it back is visible
// -- the build simply gets slower again. So the list is held rather than trusted.
//
// A project earns its place here by not needing the protocol, not by deserving to. When one
// genuinely starts speaking the wire, take it off the list on purpose and know what it costs.
// ---------------------------------------------------------------------------------------------
const projectsWithProtocol = fs
  .readdirSync(root, { withFileTypes: true })
  .filter((e) => e.isDirectory() && e.name.startsWith('Vortex.'))
  .filter((e) => fs.existsSync(path.join(root, e.name, `${e.name}.csproj`)))
  .filter((e) =>
    fs
      .readFileSync(path.join(root, e.name, `${e.name}.csproj`), 'utf8')
      .includes('Vortex.Protocol.csproj')
  )
  .map((e) => e.name);

const protocolFree = fs.existsSync(baselineFile)
  ? (JSON.parse(fs.readFileSync(baselineFile, 'utf8')).protocolFree ?? [])
  : [];
const regressed = protocolFree.filter((p) => projectsWithProtocol.includes(p));
if (regressed.length > 0) {
  failures.push([
    'A protocol-free project now references Vortex.Protocol. Every composer edit rebuilds it again.',
    regressed,
  ]);
}

if (update) {
  const free = fs
    .readdirSync(root, { withFileTypes: true })
    .filter((e) => e.isDirectory() && e.name.startsWith('Vortex.'))
    .filter((e) => fs.existsSync(path.join(root, e.name, `${e.name}.csproj`)))
    .map((e) => e.name)
    .filter((n) => !projectsWithProtocol.includes(n))
    .sort();
  fs.writeFileSync(
    baselineFile,
    JSON.stringify({ protocolLeak: leakFiles, protocolFree: free }, null, 2) + '\n'
  );
  console.log(
    `Baseline updated: ${leakFiles.length} leaking file(s), ${free.length} protocol-free project(s).`
  );
  process.exit(0);
}

const baseline = fs.existsSync(baselineFile)
  ? JSON.parse(fs.readFileSync(baselineFile, 'utf8')).protocolLeak
  : [];
const known = new Set(baseline);
const newLeak = leakFiles.filter((f) => !known.has(f));
if (newLeak.length > 0) {
  failures.push([
    'A contracts file imports the wire protocol. Pass a snapshot, not a message record.',
    newLeak,
  ]);
}

if (failures.length === 0) {
  const healed = baseline.filter((f) => !leakFiles.includes(f));
  const note = healed.length > 0 ? ` (${healed.length} baselined leak(s) now gone -- --update)` : '';
  console.log(`Architecture walls hold: 4 checked, ${leakFiles.length} baselined leak(s)${note}.`);
  process.exit(0);
}

for (const [message, lines] of failures) {
  console.error(`\n${message}`);
  for (const line of lines) console.error(`  ${line}`);
}
console.error('');
process.exit(2);
