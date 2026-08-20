#!/usr/bin/env node
// Cross-checks the dashboard's client half against the server half.
//
// AGENTS.md ("Add dashboard capability or admin page") documents four files that must move
// together. The SERVER half is self-checking: Capabilities.Dashboard.All is the one list and
// CapabilityDeclarationTests fails the build if a declared constant is missing from it. The CLIENT
// half has no cross-check at all -- nothing in `dotnet build`, `dotnet test` or `npm run build`
// looks at it -- and every failure mode is silent at runtime:
//   - capability missing from dashboardPermissions.js -> the page is hidden from every operator
//   - routes.js naming a ROUTE_PERMISSIONS key that does not exist -> caps: undefined guard
//   - nav labelKey missing from a locale -> the sidebar renders the raw i18n key
//   - a typo'd page import path -> the chunk 404s on first navigation, not at build time
//   - en.js/fr.js structural drift -> en.js is every other locale's fallback, so it breaks twice
// This script is that missing cross-check. Exit 2 = parity broken.
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath, pathToFileURL } from 'node:url';

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..', '..');
const web = path.join(root, 'Vortex.Dashboard.Web', 'src', 'lib');
const at = (...p) => path.join(...p);
const load = (p) => import(pathToFileURL(p).href);

const errors = [];
const warnings = [];
const fail = (m) => errors.push(m);

const capabilitiesCs = at(root, 'Vortex.Primitives', 'Permissions', 'Capabilities.cs');
const permissionsJs = at(web, 'dashboardPermissions.js');
const routesJs = at(web, 'routes.js');

for (const f of [capabilitiesCs, permissionsJs, routesJs]) {
  if (!fs.existsSync(f)) {
    console.error(`check-dashboard-capabilities: expected file is missing: ${path.relative(root, f)}`);
    process.exit(1);
  }
}

// ---- 1. server constants vs client CAPABILITIES -------------------------------------------------
const serverCaps = new Set(
  [...fs.readFileSync(capabilitiesCs, 'utf8').matchAll(/"(dashboard\.[a-z0-9_.]+)"/g)].map((m) => m[1]),
);
const { CAPABILITIES, ROUTE_PERMISSIONS } = await load(permissionsJs);
const clientCaps = new Set(Object.values(CAPABILITIES));

for (const c of serverCaps) {
  if (!clientCaps.has(c)) {
    fail(`${c}: declared in Capabilities.cs but absent from dashboardPermissions.js CAPABILITIES -- the page it guards is hidden from every operator.`);
  }
}
for (const c of clientCaps) {
  if (!serverCaps.has(c)) {
    fail(`${c}: used in dashboardPermissions.js but never declared in Capabilities.cs -- the policy does not exist server-side.`);
  }
}

// ---- 2. routes.js NAV rows ----------------------------------------------------------------------
// Parsed as text on purpose: importing routes.js would pull in .svelte components, which plain node
// cannot load.
const routesSrc = fs.readFileSync(routesJs, 'utf8');
const navBlock = routesSrc.slice(routesSrc.indexOf('export const NAV'));
const navRows = [...navBlock.matchAll(/\{\s*path:\s*'([^']+)'[^}]*\}/g)].map((m) => ({ path: m[1], raw: m[0] }));

if (navRows.length === 0) fail('routes.js: could not parse any NAV row -- the NAV table shape changed, update this checker.');

const referencedRouteKeys = new Set();
for (const row of navRows) {
  for (const [, key] of row.raw.matchAll(/ROUTE_PERMISSIONS\.(\w+)/g)) {
    referencedRouteKeys.add(key);
    if (!(key in ROUTE_PERMISSIONS)) {
      fail(`routes.js ${row.path}: ROUTE_PERMISSIONS.${key} does not exist in dashboardPermissions.js -- caps is undefined, so the route guard cannot evaluate.`);
    }
  }
  if (!/ROUTE_PERMISSIONS\.\w+/.test(row.raw)) {
    fail(`routes.js ${row.path}: no capability declared (expected caps: ROUTE_PERMISSIONS.<key>).`);
  }
  // A page is either eagerly imported (component:) or code-split behind a literal import thunk.
  const lazy = row.raw.match(/load:\s*\(\)\s*=>\s*import\('([^']+)'\)/);
  if (!lazy && !/component:\s*\w+/.test(row.raw)) {
    fail(`routes.js ${row.path}: needs either component: (eager) or load: () => import('../pages/X.svelte') with a literal path -- Vite cannot code-split a computed path.`);
  }
  if (lazy) {
    const target = path.resolve(web, lazy[1]);
    if (!fs.existsSync(target)) {
      fail(`routes.js ${row.path}: import('${lazy[1]}') resolves to a file that does not exist -- the chunk 404s on first navigation, and the build stays green.`);
    }
  }
}
for (const key of Object.keys(ROUTE_PERMISSIONS)) {
  if (!referencedRouteKeys.has(key)) {
    warnings.push(`ROUTE_PERMISSIONS.${key} is declared but no NAV row uses it -- unreachable page, or a leftover.`);
  }
}

// ---- 3. locales: nav keys present, and en/fr structurally identical ------------------------------
const flatten = (o, prefix = '') =>
  Object.entries(o).flatMap(([k, v]) =>
    v && typeof v === 'object' && !Array.isArray(v) ? flatten(v, `${prefix}${k}.`) : [`${prefix}${k}`],
  );

const locales = {};
for (const name of ['en', 'fr']) {
  const p = at(web, 'locales', `${name}.js`);
  if (!fs.existsSync(p)) { fail(`locales/${name}.js is missing.`); continue; }
  locales[name] = new Set(flatten((await load(p)).default));
}

if (locales.en && locales.fr) {
  for (const k of locales.en) if (!locales.fr.has(k)) fail(`locale key '${k}' exists in en.js but not fr.js -- fr falls back to en for it, so the drift is invisible until fr is audited.`);
  for (const k of locales.fr) if (!locales.en.has(k)) fail(`locale key '${k}' exists in fr.js but not en.js -- en.js is every other locale's fallback and must be the superset.`);

  for (const row of navRows) {
    for (const [, attr, key] of row.raw.matchAll(/(labelKey|shortKey):\s*'([^']+)'/g)) {
      for (const name of ['en', 'fr']) {
        if (!locales[name].has(key)) {
          fail(`routes.js ${row.path}: ${attr} '${key}' has no entry in locales/${name}.js -- the sidebar renders the raw key.`);
        }
      }
    }
  }
}

// ---- report -------------------------------------------------------------------------------------
for (const w of warnings) console.error(`warning: ${w}`);
if (errors.length) {
  console.error(`\nDashboard capability parity broken (${errors.length}):`);
  for (const e of errors) console.error(`  - ${e}`);
  console.error('\nSee AGENTS.md "Add dashboard capability or admin page" for the four files that must move together.');
  process.exit(2);
}
console.error(`check-dashboard-capabilities: OK (${serverCaps.size} capabilities, ${navRows.length} routes, ${locales.en?.size ?? 0} locale keys).`);
