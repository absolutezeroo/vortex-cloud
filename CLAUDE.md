# Claude Adapter (Vortex Cloud)

This adapter points Claude to the canonical AI contract for this repository.

## Required context load order
1. `AGENTS.md`
2. `CONTEXT.md`
3. One relevant sample in `docs/patterns/`
4. `.github/copilot-instructions.md` (tool adapter parity rules)

## Non-negotiable constraints
- **Any change to Habbo protocol behaviour starts at the specs**, not at the code:
  `dotnet run --project Vortex.Specs.Cli -- analyze <feature-id|PacketName>`. Existing emulator
  behaviour is evidence, not authority; reference emulator behaviour is evidence, not authority;
  unknown official behaviour must stay explicitly unknown. Full contract in AGENTS.md
  ("Habbo protocol behaviour: consult the specs first").
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
| `Vortex.Specs.Cli` (`habbo-spec`) | on request | what is actually known about a packet or feature, and what is only assumed |

`node scripts/hooks/__test/run.mjs` asserts the hooks still behave (exit 0 allow / exit 2 block).

## Habbo Specs
`docs/habbo-specs/` is generated from evidence — the official client, the reference emulators, and
any captures present — never hand-authored. It answers "what do we actually know about this packet"
and, just as importantly, "what do we not". Read `docs/habbo-specs/README.md` before relying on it,
and `docs/walkthroughs/use-the-habbo-specs.md` before adding to it.

```bash
dotnet run --project Vortex.Specs.Cli -- analyze room.move_floor_item_in_room
dotnet run --project Vortex.Specs.Cli -- conflicts
dotnet run --project Vortex.Specs.Cli -- unknowns --severity critical
dotnet run --project Vortex.Specs.Cli -- bootstrap    # regenerate; unchanged checkout = identical tree
dotnet run --project Vortex.Specs.Cli -- validate
```

Hand-written `verified:` and `manual:` blocks in a spec file survive regeneration. An edit inside
`generated:` blocks the next regeneration instead of being reverted.

## Validation commands
```bash
dotnet build Vortex.Main/Vortex.Main.csproj -t:VortexCloudFastCheck
dotnet build Vortex.Main/Vortex.Main.csproj -t:VortexCloudQualityGate
```
