#!/usr/bin/env node
// Ratchet on the wire disagreements the specs already compute and nothing reads.
//
// `Vortex.Specs.Cli -- conflicts --kind field_count` knows, for every packet, how many fields each
// source says it carries: the official client's own code, the reference emulators, and us. When our
// count differs from the CLIENT's count, either the client parses fields we never wrote or we write
// fields it never reads, and both desynchronize everything after that field. Nothing in the build,
// the tests or grep sees it; the symptom is a dialog that never opens.
//
// The list is a triage queue, not a bug list. An array is ONE field on the client side and two in a
// parser that reads its length and then its elements -- `incoming/AcceptFriend` is exactly that and
// is correct. Read the packet before you change it.
//
// This does not demand the existing disagreements be fixed. It fixes their LIST, so a new one
// fails the gate on the commit that introduces it instead of surfacing in a running client.
//
//   node scripts/hooks/check-wire-conflicts.mjs            # compare against the baseline
//   node scripts/hooks/check-wire-conflicts.mjs --update   # accept the current list as the baseline
//
// Exit 2 = a disagreement with the client that the baseline does not contain.
import { spawnSync } from 'node:child_process';
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..', '..');
const baselineFile = path.join(root, 'scripts', 'hooks', 'wire-conflicts-baseline.json');
const update = process.argv.includes('--update');

const cli = spawnSync(
  'dotnet',
  ['run', '--project', 'Vortex.Specs.Cli', '--', 'conflicts', '--kind', 'field_count', '--limit', '100000'],
  { cwd: root, encoding: 'utf8', timeout: 300_000 }
);

if (cli.status !== 0) {
  console.error(`check-wire-conflicts: could not run Vortex.Specs.Cli (exit ${cli.status}).\n${cli.stderr || cli.stdout}`);
  process.exit(2);
}

// Output shape:
//     cf_e941b1366b  incoming/AcceptFriend: field count
//         as3:WIN63-... (client_code): 1 fields: unknown
//         vortex (vortex_emulator): 2 fields: int32, array
const entries = [];
let current = null;
for (const line of cli.stdout.split(/\r?\n/)) {
  const header = /^ {4}(cf_[0-9a-f]+) {2}(.+?): field count$/.exec(line);
  if (header) {
    current = { id: header[1], subject: header[2], positions: [] };
    entries.push(current);
    continue;
  }
  const position = /^ {8}(\S+) \(([a-z_]+)\): (\d+) fields/.exec(line);
  if (position && current) {
    current.positions.push({ origin: position[1], authority: position[2], fields: Number(position[3]) });
  }
}

if (entries.length === 0) {
  console.error('check-wire-conflicts: parsed no conflicts -- the CLI output format changed, this check is blind.');
  process.exit(2);
}

// The client and reference checkouts live outside this repository on purpose (SpecWorkspace looks
// for a sibling holding a `sources` directory), so the specs scan degrades instead of failing when
// they are absent -- on CI, they always are. Without the client's own field counts there is nothing
// to compare against, and reporting "OK" for that would be worse than saying nothing.
if (!entries.some((e) => e.positions.some((p) => p.authority === 'client_code'))) {
  console.error(
    'check-wire-conflicts: skipped -- no client_code source in the scan. The official client sources\n' +
      'are not checked out beside this repository, so the specs have no field counts to compare ours to.'
  );
  process.exit(0);
}

// Only disagreements against the client itself. A disagreement with a reference emulator is
// evidence, not authority (AGENTS.md), and there are ~100 of those that mean nothing on their own.
const againstClient = entries
  .filter((e) => {
    const ours = e.positions.find((p) => p.origin === 'vortex');
    const client = e.positions.filter((p) => p.authority === 'client_code');
    return ours && client.length > 0 && client.some((c) => c.fields !== ours.fields);
  })
  .map((e) => e.subject)
  .sort();

if (update) {
  fs.writeFileSync(baselineFile, `${JSON.stringify({ subjects: againstClient }, null, 2)}\n`);
  console.error(`check-wire-conflicts: baseline written (${againstClient.length} disagreements with the client).`);
  process.exit(0);
}

if (!fs.existsSync(baselineFile)) {
  console.error(`check-wire-conflicts: no baseline at ${path.relative(root, baselineFile)}. Run with --update.`);
  process.exit(2);
}

const baseline = new Set(JSON.parse(fs.readFileSync(baselineFile, 'utf8')).subjects);
const added = againstClient.filter((s) => !baseline.has(s));
const fixed = [...baseline].filter((s) => !againstClient.includes(s));

for (const s of fixed) console.error(`warning: ${s} no longer disagrees with the client -- run --update to lock it in`);

if (added.length) {
  console.error(`\nNew wire disagreement with the official client (${added.length}):`);
  for (const s of added) {
    const entry = entries.find((e) => e.subject === s);
    console.error(`  - ${s}`);
    for (const p of entry.positions) console.error(`      ${p.origin} (${p.authority}): ${p.fields} fields`);
  }
  console.error(
    '\nOur field count must match the client class that parses these bytes. Audit it against the AS3\n' +
      'source (.claude/agents/wire-truth-auditor.md), then fix the serializer -- or, if the client is\n' +
      'the one that is wrong, record why and run this script with --update.'
  );
  process.exit(2);
}

console.error(
  `check-wire-conflicts: OK (${againstClient.length} known disagreements with the client, ${entries.length} field-count conflicts total).`
);
