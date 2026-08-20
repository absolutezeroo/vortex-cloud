#!/usr/bin/env node
// PostToolUse dispatcher: routes an edited file to the checks that the build does NOT run.
//
// Wired from .claude/settings.json. Reads the hook payload on stdin, matches tool_input.file_path,
// and runs only what applies -- non-matching edits exit 0 immediately. Exit 2 hands stderr back to
// Claude so the break is fixed in the same turn instead of at runtime.
import { spawnSync } from 'node:child_process';
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const here = path.dirname(fileURLToPath(import.meta.url));
const root = path.resolve(here, '..', '..');

let payload = {};
try {
  payload = JSON.parse(fs.readFileSync(0, 'utf8') || '{}');
} catch {
  process.exit(0); // Not a hook invocation we understand; never block on our own parsing.
}

const file = payload.tool_input?.file_path ?? payload.tool_input?.notebook_path ?? '';
if (!file) process.exit(0);
const rel = path.relative(root, path.resolve(file)).split(path.sep).join(String.fromCharCode(47));
if (rel.startsWith('..')) process.exit(0); // Outside the repo (sibling client sources, etc.)

const failures = [];

// --- dashboard capability / route / locale parity -------------------------------------------------
// AGENTS.md: the client half of a capability has no cross-check, and every failure is silent.
if (/^(Vortex\.Primitives\/Permissions\/Capabilities\.cs|Vortex\.Dashboard\.Web\/src\/lib\/(dashboardPermissions|routes)\.js|Vortex\.Dashboard\.Web\/src\/lib\/locales\/(en|fr)\.js)$/.test(rel)) {
  const r = spawnSync(process.execPath, [path.join(here, 'check-dashboard-capabilities.mjs')], {
    cwd: root,
    encoding: 'utf8',
  });
  if (r.status === 2) failures.push(r.stderr.trim());
}

// --- eslint on dashboard front-end files ----------------------------------------------------------
// `npm run build` does not catch an undefined identifier used in markup (AGENTS.md validation note);
// eslint does. Per-file, so it stays fast enough for a PostToolUse hook.
if (/^Vortex\.Dashboard\.Web\/src\/.*\.(svelte|js)$/.test(rel)) {
  const web = path.join(root, 'Vortex.Dashboard.Web');
  const eslintBin = path.join(web, 'node_modules', 'eslint', 'bin', 'eslint.js');
  if (fs.existsSync(eslintBin)) {
    const r = spawnSync(process.execPath, [eslintBin, path.resolve(file)], { cwd: web, encoding: 'utf8' });
    if (r.status !== 0) {
      failures.push(`eslint failed on ${rel} (npm run build would NOT have caught this):\n${(r.stdout || r.stderr).trim()}`);
    }
  }
  // node_modules absent -> skip silently rather than block every front-end edit.
}

if (failures.length) {
  console.error(failures.join('\n\n'));
  process.exit(2);
}
