#!/usr/bin/env node
// The dashboard's default theme is meant to BE docs/design/habbo-unity-mockup.png, not an impression of it. Every
// value below was sampled from that file with a flattest-patch scan (the poster is a compressed
// raster, so a single pixel read is noise; these are the medians of provably flat regions).
//
// This check exists because "close enough" is exactly how a design system drifts: nothing breaks,
// nothing fails to build, and six months later no one can say which of the two is right. If a token
// here has to change, re-sample the mockup and change BOTH -- do not silently loosen the check.
//
//   node scripts/hooks/check-design-tokens.mjs
//
// Exit 0 = the theme matches the mockup, exit 2 = it has drifted.
import { readFileSync } from 'node:fs';
import { fileURLToPath } from 'node:url';
import { dirname, resolve } from 'node:path';

const here = dirname(fileURLToPath(import.meta.url));
const cssPath = resolve(here, '../../Vortex.Dashboard.Web/src/styles.css');

/** token -> the colour measured in docs/design/habbo-unity-mockup.png */
const MEASURED = {
  // chrome
  '--page': '#0d2e50',
  '--surface': '#204466',
  '--surface-strong': '#1e3f5e',
  '--surface-raised': '#1e4063',
  '--sidebar-bg': '#092945',
  '--sidebar-active': '#1d4a78',
  '--topbar-bg': '#07203b',
  '--line-strong': '#3b648a',
  '--table-header-bg': '#1e3f5e',
  '--table-row-bg': '#173958',
  // buttons
  '--button-bg': '#276ab2',
  '--button-bg-hover': '#125499',
  '--button-bg-active': '#193754',
  '--button-bg-disabled': '#264a6b',
  '--button-secondary-bg': '#dddddc',
  '--button-secondary-ink': '#000309',
  '--button-danger-bg': '#a94033',
  '--button-danger-active': '#70302f',
  // form fields (light boxes on dark panels -- the mockup's most surprising choice)
  '--field-bg': '#e7e7e7',
  '--field-bg-focus': '#eaeaea',
  '--field-ink': '#101633',
  '--field-placeholder': '#5c6168',
  // ramps
  '--primary-50': '#5394da',
  '--primary-300': '#0757b6',
  '--primary-500': '#0b3768',
  '--primary-900': '#091b2c',
  '--secondary-50': '#dedfe0',
  '--secondary-500': '#313a41',
  '--secondary-900': '#141a20',
  // semantic + role badges
  '--ok': '#75bd5d',
  '--warning': '#edb739',
  '--danger': '#b7514b',
  '--purple': '#6f5dbd',
  '--accent': '#2e7cc9',
  '--gold': '#fbc300',
  '--toggle-on': '#439038',
  '--toggle-off': '#606f7e',
  '--badge-admin': '#a43d32',
  '--badge-moderator': '#2767ac',
  '--badge-support': '#43853d',
  '--badge-builder': '#b98f2c',
  '--badge-event': '#5441b2',
  '--success-bg': '#41714a',
  '--info-bg': '#2b6089',
  '--warning-bg': '#605c3d',
  '--danger-bg': '#5f3544',
  '--muted': '#cad4da',
  '--ink': '#ffffff',
};

const css = readFileSync(cssPath, 'utf8');

// Only the default theme is the mockup; :root[data-theme='dark'|'white'] are deliberate variants.
const start = css.indexOf(":root[data-theme='blue']");
if (start === -1) {
  console.error("check-design-tokens: the :root[data-theme='blue'] block is gone.");
  process.exit(2);
}
const block = css.slice(start, css.indexOf('\n}', start));

const drift = [];
const missing = [];

for (const [token, expected] of Object.entries(MEASURED)) {
  const match = block.match(new RegExp(`\\n\\s*${token}\\s*:\\s*([^;]+);`));

  if (!match) {
    missing.push(token);
    continue;
  }

  const actual = match[1].trim().toLowerCase();
  if (actual !== expected) drift.push({ token, expected, actual });
}

if (missing.length) {
  console.error(`check-design-tokens: absents du theme par defaut -- ${missing.join(', ')}`);
}

for (const { token, expected, actual } of drift) {
  console.error(`check-design-tokens: ${token} = ${actual}, la maquette dit ${expected}`);
}

if (missing.length || drift.length) {
  console.error(
    `\n${missing.length + drift.length} ecart(s) avec docs/design/habbo-unity-mockup.png. Re-echantillonnez la maquette avant de changer une valeur.`
  );
  process.exit(2);
}

console.log(`check-design-tokens: OK (${Object.keys(MEASURED).length} tokens conformes a la maquette).`);
