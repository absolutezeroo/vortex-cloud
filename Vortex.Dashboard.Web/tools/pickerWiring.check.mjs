// Every picker kind must have a directory, and every directory must be mapped on the server.
//
// This exists because the opposite shipped: PickerModal listed seven new kinds and the row template
// rendered them, but their URLs never made it into ENDPOINTS. The old `?? ENDPOINTS.user` fallback
// then made every one of them quietly return the player directory -- a guild picker that listed
// players and looked like it worked. Nothing failed: not the build, not eslint, not a test.
//
// Three checks, each catching a different half of that:
//   1. a kind the editor asks for that no directory declares
//   2. a directory asking for a row layout that does not exist
//   3. a directory whose endpoint the server does not map

import { readFileSync } from 'node:fs';

const read = (p) => readFileSync(new URL(p, import.meta.url), 'utf8');

const registry = read('../src/lib/pickers/directories.js');
const rows = read('../src/components/pickers/index.js');
const page = read('../src/pages/RewardTracksPage.svelte');
const routes = read('../../Vortex.Dashboard.API/Hosting/DashboardEndpoints.Directory.cs');

const failures = [];

// What the registry can actually fetch, and which layout each asks for.
const entries = [
  ...registry.matchAll(/(\w+):\s*\{\s*endpoint:\s*'([^']+)',\s*row:\s*'(\w+)'/g),
].map((m) => ({ kind: m[1], url: m[2], row: m[3] }));

const endpoints = new Map(entries.map((e) => [e.kind, e.url]));
const layouts = new Set(
  [...rows.matchAll(/^\s\s(\w+):\s*\w+,$/gm)].map((m) => m[1])
);

// What the editor asks it for.
const requested = new Set(
  [...page.matchAll(/^\s{4}\w+:\s*'(\w+)',$/gm)]
    .map((m) => m[1])
    .filter((kind) => /^[a-z]/.test(kind))
);

// What the server maps.
const mapped = new Set(
  [...routes.matchAll(/ApiDirectory \+ "(\/[a-z-]+)"/g)].map((m) => `/api/v1/directory${m[1]}`)
);

for (const kind of requested) {
  if (!endpoints.has(kind)) {
    failures.push(`RewardTracksPage asks for picker kind "${kind}", which no directory declares`);
  }
}

for (const entry of entries) {
  if (!layouts.has(entry.row)) {
    failures.push(`directory "${entry.kind}" asks for row layout "${entry.row}", which does not exist`);
  }
}

for (const [kind, url] of endpoints) {
  if (!mapped.has(url)) {
    failures.push(`directory "${kind}" calls ${url}, which the server does not map`);
  }
}

if (failures.length > 0) {
  console.error('Picker wiring is broken:\n  ' + failures.join('\n  '));
  process.exit(1);
}

console.log(
  `picker wiring ok: ${endpoints.size} directories, ${layouts.size} layouts, all mapped server-side`
);
