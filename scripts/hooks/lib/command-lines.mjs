// Shared shell-command parsing for the PreToolUse guards.
//
// Extracted from guard-emulator.mjs when guard-commit.mjs needed the same two rules. Both guards
// answer the same question -- "is this verb actually being RUN, or merely mentioned?" -- and both
// got it wrong the same way before: a grep pattern, an echo, or a commit message describing the
// guard itself would trip a naive substring match.

/**
 * Drops heredoc bodies: everything between a `<< MARKER` line and its terminator is input data,
 * not commands. A commit message that quotes a blocked command must not be read as one.
 */
export function commandLines(src) {
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

/**
 * Every statement in the command, heredoc bodies removed and pipelines/chains split apart, so a
 * guard can test each one for a verb in COMMAND POSITION rather than anywhere in the string.
 */
export function statements(command) {
  return commandLines(command).flatMap((line) => line.split(/(?:\|\||&&|[;|&])/));
}
