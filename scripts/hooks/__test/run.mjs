// Self-contained smoke test for the hook scripts. Creates its own fixtures, removes them, and
// asserts the exit codes Claude Code reads: 0 = allow, 2 = block and hand stderr back.
//   node scripts/hooks/__test/run.mjs
import { spawnSync } from 'node:child_process';
import fs from 'node:fs';
import os from 'node:os';
import path from 'node:path';

const root = process.cwd();
const probe = path.join(root, 'Vortex.Dashboard.Web', 'src', 'pages', '__HookProbe.svelte');
fs.writeFileSync(probe, '<script>\n  let count = 0;\n</script>\n<p>{undefinedThing}</p>\n');

const run = (script, payload) =>
  spawnSync(process.execPath, [path.join('scripts', 'hooks', script)], {
    input: JSON.stringify(payload),
    encoding: 'utf8',
  });

const edit = (rel) => ({ tool_input: { file_path: path.join(root, rel) } });
const cmd = (command) => ({ tool_name: 'Bash', tool_input: { command } });

const cases = [
  ['post-edit.mjs', edit('README.md'), 0, 'fichier hors perimetre'],
  ['post-edit.mjs', edit(path.join('Vortex.Dashboard.Web', 'src', 'lib', 'routes.js')), 0, 'routes.js coherent'],
  ['post-edit.mjs', edit(path.join('Vortex.Dashboard.Web', 'src', 'pages', '__HookProbe.svelte')), 2, 'no-undef dans le markup'],
  ['post-edit.mjs', edit(path.join('Vortex.Dashboard.Web', 'src', 'pages', 'AccessDeniedPage.svelte')), 0, 'svelte sain'],
  ['guard-emulator.mjs', cmd('dotnet build Vortex.Main/Vortex.Main.csproj'), 0, 'build normal'],
  ['guard-emulator.mjs', cmd('taskkill /F /IM Vortex.Main.exe'), 2, 'taskkill emulateur'],
  ['guard-emulator.mjs', cmd('Stop-Process -Name Vortex.Main -Force'), 2, 'Stop-Process emulateur'],
  ['guard-emulator.mjs', cmd('taskkill /F /IM chrome.exe'), 0, 'taskkill sans rapport'],
  // Le verbe doit etre en position de commande. Ces trois cas ne tuent rien : ils en PARLENT.
  // Le troisieme a reellement bloque le commit qui introduisait ce garde.
  ['guard-emulator.mjs', cmd('grep -rn "taskkill Vortex.Main" scripts/'), 0, 'grep qui mentionne le verbe'],
  ['guard-emulator.mjs', cmd('echo taskkill /F /IM Vortex.Main.exe'), 0, 'echo qui mentionne le verbe'],
  [
    'guard-emulator.mjs',
    cmd("git commit -F - <<'EOF'\nrefuses a command that would kill the running Vortex.Main\nEOF"),
    0,
    'corps de heredoc qui mentionne le verbe',
  ],
  // ... mais un vrai kill enchaine derriere une autre commande doit toujours passer a la trappe.
  ['guard-emulator.mjs', cmd('dotnet build || taskkill /F /IM Vortex.Main.exe'), 2, 'kill enchaine apres ||'],
  ['post-edit.mjs', edit(path.join('Vortex.Revisions', 'Revision20260701', 'Headers.cs')), 0, 'headers sous le plafond'],
];

// Scripts autonomes : pas de payload sur stdin, on les appelle avec leurs arguments.
const headerProbe = path.join(os.tmpdir(), '__HeaderProbe.cs');
fs.writeFileSync(headerProbe, 'public const int FabricatedMessageEvent = 9999;\npublic const int RealMessageEvent = 1234;\n');

const direct = [
  ['check-header-ceiling.mjs', [], 0, 'plafond des headers : depot propre'],
  ['check-header-ceiling.mjs', ['--file', headerProbe, '--ceiling', '4101'], 2, 'id fabrique au-dessus du plafond'],
];

let failed = 0;
for (const [script, payload, want, label] of cases) {
  const r = run(script, payload);
  const ok = r.status === want;
  if (!ok) failed++;
  console.log(`${ok ? 'OK  ' : 'FAIL'} ${label} (attendu=${want} obtenu=${r.status})`);
  if (!ok && r.stderr) console.log('     ' + r.stderr.trim().split('\n').slice(0, 4).join('\n     '));
}

for (const [script, argv, want, label] of direct) {
  const r = spawnSync(process.execPath, [path.join('scripts', 'hooks', script), ...argv], { encoding: 'utf8' });
  const ok = r.status === want;
  if (!ok) failed++;
  console.log(`${ok ? 'OK  ' : 'FAIL'} ${label} (attendu=${want} obtenu=${r.status})`);
  if (!ok && r.stderr) console.log('     ' + r.stderr.trim().split('\n').slice(0, 4).join('\n     '));
}

fs.rmSync(probe, { force: true });
fs.rmSync(headerProbe, { force: true });
console.log(failed ? `\n${failed} test(s) en echec.` : '\nTous les hooks se comportent comme attendu.');
process.exit(failed ? 1 : 0);
