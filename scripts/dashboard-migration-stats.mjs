#!/usr/bin/env node
// Prints the §37 numbers from docs/architecture/dashboard-architecture-rules.md, so "the debt went
// down" is a measurement instead of an impression. Run from the repository root:
//
//   node scripts/dashboard-migration-stats.mjs
//
// Append the line it prints to the tracking table in the rulebook after each domain extraction.

import { existsSync, readFileSync, readdirSync, statSync } from 'node:fs';
import { join } from 'node:path';

const ROOT = 'Vortex.Dashboard.API';

function filesUnder(dir) {
  const out = [];
  for (const entry of readdirSync(dir)) {
    const path = join(dir, entry);
    if (statSync(path).isDirectory()) out.push(...filesUnder(path));
    else if (entry.endsWith('.cs')) out.push(path);
  }
  return out;
}

/** Public methods of a partial class, counted across every one of its parts. */
function publicMethods(dir, className) {
  let count = 0;
  for (const file of filesUnder(join(ROOT, dir))) {
    const text = readFileSync(file, 'utf8');
    if (!text.includes(`class ${className}`)) continue;
    count += (text.match(/^ {4}public (?!.*\bclass\b).*\(/gm) ?? []).length;
  }
  return count;
}

/** Primary-constructor parameters, i.e. what the class admits it needs (§3). */
function constructorDeps(file, className) {
  const path = join(ROOT, file);
  // A god service that no longer exists is the goal, not an error.
  if (!existsSync(path)) return 0;
  const text = readFileSync(path, 'utf8');
  const start = text.indexOf(`class ${className}(`);
  if (start < 0) return 0;
  const end = text.indexOf('\n)', start);
  return (text.slice(start, end).match(/^ {4}[A-Za-z]/gm) ?? []).length;
}

/** Hand-declared types in the second DI container — the debt §29 says not to grow forever. */
function forwardedTypes() {
  const text = readFileSync(join(ROOT, 'Hosting/DashboardWebHost.cs'), 'utf8');
  const start = text.indexOf('ForwardedServiceTypes');
  const end = text.indexOf('];', start);
  return (text.slice(start, end).match(/typeof\(/g) ?? []).length;
}

const rows = [
  ['DashboardApiService', publicMethods('Api', 'DashboardApiService'),
    constructorDeps('Api/DashboardApiService.cs', 'DashboardApiService')],
  ['DashboardOperationsService', publicMethods('Operations', 'DashboardOperationsService'),
    constructorDeps('Operations/DashboardOperationsService.cs', 'DashboardOperationsService')],
];

for (const [name, methods, deps] of rows) {
  console.log(`${name.padEnd(28)} ${String(methods).padStart(4)} methods  ${String(deps).padStart(3)} deps`);
}
console.log(`${'DI forwarding'.padEnd(28)} ${String(forwardedTypes()).padStart(4)} types`);
