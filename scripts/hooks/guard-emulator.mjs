#!/usr/bin/env node
// PreToolUse guard: refuses to stop the running emulator.
//
// The recurring temptation is a file-locked build -- Vortex.Main holds its DLLs, the build fails,
// and the fastest-looking fix is to end the process. It is the wrong fix: the running hotel is the
// live-verification environment (dev game ports 40000/40001, activated RoomGrains, connected
// sessions), and ending it throws that state away for a build error that a rebuild of the offending
// project, or waiting, would have solved.
//
// Exit 2 blocks the call and hands stderr back to Claude.
//
// The verb must be in COMMAND POSITION. Matching the verb anywhere in the string blocks any command
// that merely *mentions* the emulator and a process verb -- a grep pattern, an echo, or a commit
// message describing this very guard, which is exactly how this bug was found. Heredoc bodies are
// input data rather than commands, and are skipped for the same reason.
import fs from 'node:fs';

let payload = {};
try {
  payload = JSON.parse(fs.readFileSync(0, 'utf8') || '{}');
} catch {
  process.exit(0);
}

const command = payload.tool_input?.command ?? '';
if (!command) process.exit(0);

// Drop heredoc bodies: everything between a `<< MARKER` line and its terminator is data.
function commandLines(src) {
  const out = [];
  let terminator = null;
  for (const line of src.split(/\r?\n/)) {
    if (terminator !== null) {
      if (line.trim() === terminator) terminator = null;
      continue;
    }
    out.push(line);
    const here = line.match(/<<-?\s*['"]?([A-Za-z_][A-Za-z0-9_]*)['"]?/);
    if (here) terminator = here[1];
  }
  return out;
}

// The verb only counts at the start of a statement, after optional sudo/env/assignment prefixes.
const VERB_AT_START =
  /^\s*(?:sudo\s+|env\s+|[A-Za-z_][A-Za-z0-9_]*=\S*\s+)*(taskkill|Stop-Process|pkill|killall|kill)\b/i;
const TARGET = /Vortex[.\s]*Main/i;

const offending = commandLines(command)
  .flatMap((line) => line.split(/(?:\|\||&&|[;|&])/))
  .find((segment) => VERB_AT_START.test(segment) && TARGET.test(segment));

if (offending) {
  console.error(
    [
      'Blocked: this command would stop the running emulator (Vortex.Main).',
      '',
      `  ${offending.trim()}`,
      '',
      'The running hotel is the live-verification environment -- activated RoomGrains, connected',
      'sessions, dev game ports 40000/40001. Do not stop it to unblock a file-locked build.',
      'Instead: rebuild only the project you changed, or wait for the lock to clear.',
      'If stopping it really is the intent, the user runs it themselves.',
    ].join('\n'),
  );
  process.exit(2);
}
