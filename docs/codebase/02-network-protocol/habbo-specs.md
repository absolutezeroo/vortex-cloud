# Habbo specs

## Purpose

`docs/habbo-specs/` answers two questions this codebase cannot answer for itself: *what does this
packet carry*, and *what does the official server do with it*. The second one is usually `unknown`,
and the tree's whole value is that it says so.

## Why it exists

We do not have Habbo's server. The **structure** of a packet is knowable from the official client's
own code. What the server **does** with it is not. Every other emulator resolves that gap by guessing
and then forgetting it guessed.

Three rules the tree rests on:

- Existing emulator behaviour is **evidence, not authority**.
- Reference emulator behaviour is **evidence, not authority**.
- Unknown official behaviour must stay **explicitly unknown**.

`docs/habbo-specs/` is **generated**, never hand-authored. Read `docs/habbo-specs/README.md` before
relying on it and `docs/walkthroughs/use-the-habbo-specs.md` before adding to it.

## The tree

3672 YAML files at this commit:

| Directory | Files | Holds |
|---|---:|---|
| `packets/` | 1446 | per-packet layout and field confidence, `incoming/` and `outgoing/` by domain |
| `features/` | 499 | behavioural specs — the flow a feature actually takes |
| `scenarios/` | 499 | behaviours generated from each feature's own guards |
| `unknowns/` | 814 | what nobody knows, by severity |
| `conflicts/` | 403 | where two sources disagree |
| `revisions/` | 6 | **the only place header ids live** |
| `evidence/` | 6 | source manifests, plus `captures/README.md` — **no captures** |

## Sources and their authority

`Vortex.Specs` reads four kinds of source and records which one every claim came from:

| Authority | Source | Weight |
|---|---|---|
| `client_code` | the official AS3 client under `vortex-modern-client/sources/<build>/src/**` | structure is authoritative |
| `multi_implementation` | Nitro | corroboration |
| `reference_emulator` | Arcturus / Daybreak | **evidence only** |
| `vortex_emulator` | this repository, scanned with Roslyn | evidence about *us* |

`Vortex.Specs/Pipeline/SpecPipeline.cs` — `EmulatorProjects.Default` names the 17 projects the Roslyn
scan indexes, explicitly excluding dashboard, benchmark and test projects because they contribute
nothing to protocol truth.

## Header ids live in exactly one place

`docs/habbo-specs/revisions/`. Behavioural specs name packets **symbolically**; a numeric id in a
behavioural spec is a validation error. `Vortex.Specs.Cli`'s `headers` command exists as a separate
verb for the same reason.

```
$ dotnet run --project Vortex.Specs.Cli -- headers

  registry                       as3:WIN63-202607011411-782849652
  authority                      client_code
  comparable with this emulator  yes — same client build
  incoming                       30      outgoing  17

  registry                       vortex (WIN63-202607011411-782849652)
  authority                      vortex_emulator
  comparable with this emulator  yes — same client build
  incoming                       539     outgoing  533

  registry                       habbo-arcturus-daybreak   authority  reference_emulator
  comparable with this emulator  no — different build
  registry                       nitro                     authority  multi_implementation
  comparable with this emulator  no — different build
  registry                       as3:PRODUCTION-201601012205-226667486  (a 2016 build)
  comparable with this emulator  no — different build
```

**Only two of the five tables may legitimately be compared** — the tool says so per row. The WIN63
client registry resolves only 30 incoming / 17 outgoing *symbolic names*: the rest of the client's
ids exist but bind to classes the scanner cannot name.

## Reading a conflict

```
$ dotnet run --project Vortex.Specs.Cli -- conflicts --kind field_count --limit 20

  cf_da15a63c25  incoming/AddSpamWallPostIt: field count
      as3:WIN63-…            (client_code):       4 fields: int32, string, string, string
      habbo-arcturus-daybreak (reference_emulator): 6 fields: …
      vortex                 (vortex_emulator):   4 fields: int32, string, string, string

  No conflict here has been arbitrated. Where the official answer is unknown, the
  disagreement is the answer until evidence closes it.
```

Read the authorities, not the count. Here **Vortex agrees with the official client** and disagrees
with a reference emulator — that is evidence about Daybreak, not a bug here.
`check-wire-conflicts.mjs` applies exactly this filter for you: it reports only `vortex` vs
`client_code` disagreements. → [Revisions](revisions.md)

## Reading an unknown

```
$ dotnet run --project Vortex.Specs.Cli -- unknowns --severity critical

Unknowns (134)
  critical: 134
    uk_a4c5e49f04  advertisement.get_interstitial
        The handler for GetInterstitial reaches no domain operation and sends nothing.
    uk_c0ba9770d3  as3:WIN63-…: client-to-server messages this emulator has no header for
        The official client can send 50 messages whose header ids appear nowhere in this
        emulator's table (for example 75, 129, 145, 245, 272, 293, 501, 521).
```

The dominant shape is *"the handler exists, is registered, parses fine, and does nothing"* — 539 of
540 messages have a handler, and 134 of those handlers are stubs. `uk_c0ba9770d3` is the one
system-wide entry: 50 client-sendable ids with no entry on our side.

## Validation

```
$ dotnet run --project Vortex.Specs.Cli -- validate
Validated 3672 spec files
  errors 0     warnings 0
```

Runs inside `VortexCloudFastCheck`, so a spec left contradictory fails the gate rather than the next
reader.

## Editing rules

| Block | Ownership |
|---|---|
| `generated:` | the tool's. An edit here is detected by digest and **blocks the next regeneration** rather than being silently reverted |
| `verified:` | yours. Survives regeneration |
| `manual:` | yours. Survives regeneration |

To record a behaviour you had to pick because the official one is unknown, put it in `verified:` with
`confidence: assumed` and the reason. Never turn an `unknown` into an assumption silently.

## Workflow

```bash
dotnet run --project Vortex.Specs.Cli -- analyze <feature-id|PacketName>   # start here
dotnet run --project Vortex.Specs.Cli -- conflicts --kind field_count --limit 200
dotnet run --project Vortex.Specs.Cli -- unknowns --severity critical
dotnet run --project Vortex.Specs.Cli -- headers
dotnet run --project Vortex.Specs.Cli -- bootstrap    # regenerate; unchanged checkout = identical tree
dotnet run --project Vortex.Specs.Cli -- validate
```

`analyze` returns the reconstructed chain: the trigger packet with per-field confidence, the
implementation flow through handler → service → grain → modules, the observed guards, the state
changes, the outgoing packets with per-recipient confidence, the conflicts, and the scenarios. For
`room.move_floor_item_in_room` that is 16 flow steps, 24 guards, 3 state changes, 3 outgoing packets,
1 conflict, 13 scenarios — and `official_behavior: unknown` throughout.

## Caveats that change what a number means

Four, all verified, none of them currently written down anywhere else.

### 1. The read commands re-scan; they do not read the tree

`ReviewCommand.Load` calls `new SpecPipeline(workspace).Scan(...)` fresh. So live output diverges from
the on-disk tree:

| | Live command | Files on disk |
|---|---|---|
| Critical unknowns | 134 | 137 |
| `field_count` conflicts | 225 | 255 |

Neither is wrong. The live number reflects the current source; the files reflect the last
`bootstrap`. **Anyone quoting a count must say which.**

### 2. The tree accumulates orphans

`REPORT.md` records 43 written + 3480 unchanged = 3523 files for the last bootstrap; the tree holds
3672. Grep for `Delete`/`prune`/`orphan`/`Remove` in `SpecBootstrapper.cs` and `SpecStore.cs` returns
**no hits** — nothing removes a spec whose subject disappeared.

Concrete case: `docs/habbo-specs/revisions/arcturus.yaml` is a leftover from the first bootstrap,
superseded by `habbo-arcturus-daybreak.yaml` when `SpecWorkspace.ReferenceId` started naming reference
trees after their directory. `headers` shows 5 registries; the folder holds 6.

### 3. `reaches_persistence` is false-negative for read-only queries

`Vortex.Specs/Analysis/Emulator/EmulatorFlowAnalyzer.cs` — `IsPersistence` fires on
`SaveChangesAsync`/`ExecuteUpdateAsync`/`ExecuteDeleteAsync`/`AddAsync`/`FlushAsync`, or a receiver
type ending in `DbContext`. It is evaluated only at **invocation** member-access nodes, and
`dbCtx.CfhTickets` is a plain member access, not an invocation.

Worked example: `features/moderation/get_report_history_for_reporter.yaml` records
`reaches_persistence: false`, while `Vortex.Rooms/CfhTicketService.cs` —
`GetReportHistoryForReporterAsync` opens a `VortexDbContext` and runs a real query. This is a
systematic class, not a one-off.

### 4. `flow:` is indicative, not exhaustive

The same spec lists a flow step as `WiredActionBranch.Select` — a LINQ `.Select(...)` resolved to an
unrelated domain method.

## No captures exist

`docs/habbo-specs/evidence/captures/` holds only its README. **Every `official_behavior` across 499
feature specs is `unknown`**, and nothing but a capture can change that.

Do not let any of them be read as confirmed. Import one with
`dotnet run --project Vortex.Specs.Cli -- import-capture`.

## Known unknowns

- **Unknown:** the official server behaviour for essentially every feature.
  - Inspected: all 499 feature specs; `evidence/captures/` is empty.
  - Why unresolved: it requires a packet capture against a live official server.
  - What would resolve it: `import-capture` with a real session recording.
- **Unknown:** whether the 50 client-sendable header ids with no entry here (`uk_c0ba9770d3`)
  represent missing features or messages the client never actually sends.
  - Inspected: the client registry via `headers`; the ids are present in the client's table.
  - What would resolve it: a capture, or reading each id's AS3 sender to see whether any code path
    reaches it.

## Sources

- `docs/habbo-specs/README.md`, `docs/habbo-specs/REPORT.md`
- `docs/walkthroughs/use-the-habbo-specs.md`
- `Vortex.Specs/Pipeline/{SpecPipeline,SpecBootstrapper}.cs`, `Persistence/SpecStore.cs`
- `Vortex.Specs/Analysis/Emulator/EmulatorFlowAnalyzer.cs` — `IsPersistence`
- `Vortex.Specs.Cli/Commands/ReviewCommand.cs`
- `AGENTS.md` — "Habbo protocol behaviour: consult the specs first"
- command output captured at commit `e57f0be7`
