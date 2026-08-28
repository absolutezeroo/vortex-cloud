#!/usr/bin/env node
// PreToolUse guard: refuses a commit that would sweep in work nobody asked this session to commit.
//
// Twice now a bare `git commit` has carried away changes that were not this session's:
//   * the user had staged deletions of his own; `git add -A <paths>` + a bare `git commit` took them
//     along and left main non-compilable.
//   * a concurrent Claude session works the same tree, and an unscoped `git add` files ITS
//     uncommitted edits under THIS session's message.
//
// The index is shared mutable state between the user, this session, and any other session. A commit
// must therefore name what it is committing. Two rules:
//
//   1. `git commit` must carry `-o` / `--only` (or an explicit `-- <pathspec>`), so the commit is
//      built from named paths rather than from whatever the index happens to hold.
//   2. `git add` must not sweep the whole tree (`-A`, `--all`, `.`) without a pathspec.
//
// Exit 2 blocks the call and hands stderr back to Claude.
//
// As in guard-emulator.mjs, the verb only counts in COMMAND POSITION: a grep for "git commit", an
// echo, or a commit message that quotes one of these commands must still run.
import fs from 'node:fs';
import { statements } from './lib/command-lines.mjs';

let payload = {};
try {
  payload = JSON.parse(fs.readFileSync(0, 'utf8') || '{}');
} catch {
  process.exit(0);
}

const command = payload.tool_input?.command ?? '';
if (!command) process.exit(0);

const PREFIX = /^\s*(?:sudo\s+|env\s+|[A-Za-z_][A-Za-z0-9_]*=\S*\s+)*/;

/** The argv of `git <sub>` for this statement, or null if the statement is not that command. */
function gitArgs(segment, sub) {
  const rest = segment.replace(PREFIX, '');
  const m = rest.match(new RegExp(`^git\\s+(?:-C\\s+\\S+\\s+)?${sub}\\b(.*)$`, 's'));
  if (!m) return null;
  // Quoted arguments are opaque to this guard -- it only ever inspects flags, never paths.
  return m[1].trim();
}

const problems = [];

for (const segment of statements(command)) {
  const commitArgs = gitArgs(segment, 'commit');
  if (commitArgs !== null) {
    const scoped = /(^|\s)(-o(\s|$)|--only(\s|=))/.test(commitArgs) || /(^|\s)--(\s+\S)/.test(commitArgs);
    if (!scoped) {
      problems.push([
        `Blocked: \`git commit\` with nothing naming what it commits.`,
        '',
        `  ${segment.trim()}`,
        '',
        'This commits whatever the index holds -- and the index is shared with the user and with any',
        'other Claude session on this tree. Both times that happened, work that was not this',
        "session's went out under this session's message; once it left main non-compilable.",
        '',
        'Use `git commit -o <paths> -m "..."` and name every path you actually changed.',
        'Check `git status` first: anything staged that you did not stage is not yours to commit.',
      ].join('\n'));
    }
  }

  const addArgs = gitArgs(segment, 'add');
  if (addArgs !== null) {
    const sweeps = /(^|\s)(-A|--all|\.)(\s|$)/.test(addArgs);
    // Whatever is left once the flags, the `--` separator and the bare `.` are removed is a path.
    const pathspec = addArgs
      .split(/\s+/)
      .filter((token) => token && token !== '.' && token !== '--' && !token.startsWith('-'));
    if (sweeps && pathspec.length === 0) {
      problems.push([
        `Blocked: \`git add\` sweeping the whole tree.`,
        '',
        `  ${segment.trim()}`,
        '',
        'A concurrent session and the user both have uncommitted edits in this tree. An unscoped add',
        'stages theirs alongside yours.',
        '',
        'Stage the paths you changed: `git add <paths>` (or `git add -A -- <paths>` if you need',
        'deletions within those paths).',
      ].join('\n'));
    }
  }
}

if (problems.length) {
  console.error(problems.join('\n\n'));
  process.exit(2);
}
