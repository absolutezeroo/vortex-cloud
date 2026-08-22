#!/usr/bin/env node
// Refuses a NEW header constant above the id ceiling of the client build the revision targets.
//
// A header id the client's message registry does not contain registers without error on our side
// and can never fire: no exception, no log, no failing test -- the feature is simply dead. That is
// how four of the constants below got shipped. This script does not (cannot) tell a deliberate
// extension from an invented id; what it does is make every new one a decision that has to be
// written down here, next to the reason.
//
//   node scripts/hooks/check-header-ceiling.mjs
//   node scripts/hooks/check-header-ceiling.mjs --file <Headers.cs> --ceiling <id>   # one-off
//
// Exit 2 = an above-ceiling constant that is not in ALLOWED. Exit 0 = clean (stale or unverified
// allowlist entries are reported as warnings, they do not block).
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..', '..');

// Highest id present in the message registry of the client build each revision talks to. Re-derive
// after a client update: the registry lives in the AS3 sources named by AGENTS.md, and
// `dotnet run --project Vortex.Specs.Cli -- headers` prints what the specs scan extracted from it.
const CEILINGS = { Revision20260701: 4101 };

// name -> why it is allowed above the ceiling. `verified: false` means nobody has confirmed the
// official client accepts this id; it is carried as known debt, not as a decision.
const ALLOWED = {
  // Server-side extensions, outside the official range on purpose: only a modified client sends or
  // reads these, and the official client never will.
  VortexGetFurniEditorDataMessageEvent: { id: 8001, verified: true, why: 'vortex furni editor extension' },
  VortexFurniEditorDataMessageComposer: { id: 8002, verified: true, why: 'vortex furni editor extension' },
  VortexApplyFurniEditMessageEvent: { id: 8003, verified: true, why: 'vortex furni editor extension' },
  VortexFurniEditorRightsMessageComposer: { id: 8004, verified: true, why: 'vortex furni editor extension' },
  VortexGetFurniDefinitionMessageEvent: { id: 8005, verified: true, why: 'vortex furni editor extension' },
  VortexFurniDefinitionMessageComposer: { id: 8006, verified: true, why: 'vortex furni editor extension' },
  VortexApplyFurniDefinitionMessageEvent: { id: 8007, verified: true, why: 'vortex furni editor extension' },
  RentableSpaceGetConfigMessageEvent: { id: 4600, verified: true, why: 'vortex-client registry _composers[4600]' },
  RentableSpaceConfigMessageComposer: { id: 4600, verified: true, why: 'vortex-client registry _events[4600]' },
  RentableSpaceConfigureMessageEvent: { id: 4601, verified: true, why: 'vortex-client registry _composers[4601]' },

  // Placeholders, not extensions. Each collided with a real WIN63 id after the client header remap
  // and was parked out of range to stop it firing on someone else's packet -- which also means the
  // feature behind it cannot fire at all. Close one by finding its class in the client registry, or
  // by deleting the constant and what hangs off it. Their Headers.cs comments carry the full trace.
  GetGiftWrappingConfigurationVortexEvent: { id: 9008, verified: false, why: 'placeholder; 940 is the real GetGiftWrappingConfiguration and is mapped' },
  CustomizeAvatarWithFurniMessageEvent: { id: 9012, verified: false, why: 'placeholder; no customizeAvatarWithFurni anywhere in the WIN63 client' },
  CustomStackingHeightUpdateMessageComposer: { id: 9201, verified: false, why: 'placeholder; the 701 client reads stacking data only from the packed HeightMapUpdate' },
};

const errors = [];
const warnings = [];
const seen = new Set();

// A one-off pair for a revision not in CEILINGS yet, and how the hook test exercises the failure.
const arg = (name) => {
  const i = process.argv.indexOf(`--${name}`);
  return i >= 0 ? process.argv[i + 1] : undefined;
};
const targets = arg('file')
  ? [[arg('file'), Number(arg('ceiling') ?? 4101)]]
  : Object.entries(CEILINGS).map(([revision, ceiling]) => [
      path.join(root, 'Vortex.Revisions', revision, 'Headers.cs'),
      ceiling,
    ]);

for (const [file, ceiling] of targets) {
  const label = path.relative(root, file).split(path.sep).join('/') || file;
  if (!fs.existsSync(file)) {
    warnings.push(`${label} not found -- ceiling not checked`);
    continue;
  }

  const source = fs.readFileSync(file, 'utf8');
  const lines = source.split(/\r?\n/);

  lines.forEach((line, i) => {
    const match = /const\s+int\s+([A-Za-z0-9_]+)\s*=\s*(\d+)\s*;/.exec(line);
    if (!match) return;

    const [, name, raw] = match;
    const id = Number(raw);
    if (id <= ceiling) return;

    seen.add(name);
    const allowed = ALLOWED[name];
    if (!allowed) {
      errors.push(
        `${label}:${i + 1}  ${name} = ${id} is above the client ceiling (${ceiling}).\n` +
          '      An id the client does not know registers fine and never fires. Verify it against the AS3\n' +
          '      message registry; if it is a deliberate extension, add it to ALLOWED in this script.'
      );
    } else if (allowed.id !== id) {
      warnings.push(`${name} is allowed at ${allowed.id} but declared ${id} -- update the allowlist`);
    }
  });
}

if (!arg('file')) {
  for (const [name, entry] of Object.entries(ALLOWED)) {
    if (!seen.has(name)) warnings.push(`allowlist entry ${name} (${entry.id}) no longer exists above any ceiling -- remove it`);
  }
}

const unverified = Object.entries(ALLOWED).filter(([name, e]) => !e.verified && seen.has(name));

for (const w of warnings) console.error(`warning: ${w}`);
if (errors.length) {
  console.error(`\nHeader ids above the client ceiling (${errors.length}):`);
  for (const e of errors) console.error(`  - ${e}`);
  process.exit(2);
}
console.error(
  `check-header-ceiling: OK (${seen.size} allowed above ceiling, ${unverified.length} still unverified: ${unverified
    .map(([name]) => name)
    .join(', ') || 'none'}).`
);
