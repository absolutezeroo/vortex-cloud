# Claude Adapter (Vortex Cloud)

This adapter points Claude to the canonical AI contract for this repository.

## Required context load order
1. `AGENTS.md`
2. `CONTEXT.md`
3. One relevant sample in `docs/patterns/`
4. `.github/copilot-instructions.md` (tool adapter parity rules)

## Non-negotiable constraints
- Keep packet handlers orchestration-only.
- Do not query database contexts/repositories from packet handlers.
- Do not send composers directly to sockets/sessions from handlers; route via `PlayerPresenceGrain.SendComposerAsync`.
- For `Revision<id>` parser/serializer work, edit `../turbo-sample-plugin/TurboSamplePlugin/Revision/**`, not `vortex-cloud`.
- A new dashboard capability must be added to **all four** files listed in `AGENTS.md`
  ("Add dashboard capability or admin page"). The build and the tests do not catch a missing copy;
  `scripts/hooks/check-dashboard-capabilities.mjs` does, and runs automatically after any edit to
  one of those files.

## Repository automations
Configured in `.claude/settings.json`; the scripts are plain node and run standalone too.

| Automation | Fires on | Catches |
|---|---|---|
| `scripts/hooks/post-edit.mjs` (PostToolUse) | any `Edit`/`Write` | dashboard capability/route/locale parity; `eslint` on touched `.svelte`/front-end `.js` (`npm run build` misses an undefined identifier in markup) |
| `scripts/hooks/guard-emulator.mjs` (PreToolUse) | `Bash`/`PowerShell` | a command that would kill the running `Vortex.Main` |
| `.claude/agents/wire-truth-auditor.md` | on request | serializer-vs-AS3-client drift, fabricated header ids |
| `.claude/agents/grain-rules-reviewer.md` | on request | the Orleans rules in `AGENTS.md` that no analyzer enforces |
| `/new-dashboard-page`, `/sql-fix` | user-invoked | the checklists that get half-remembered |

`node scripts/hooks/__test/run.mjs` asserts the hooks still behave (exit 0 allow / exit 2 block).

## Validation commands
```bash
dotnet build Vortex.Main/Vortex.Main.csproj -t:VortexCloudFastCheck
dotnet build Vortex.Main/Vortex.Main.csproj -t:VortexCloudQualityGate
```
