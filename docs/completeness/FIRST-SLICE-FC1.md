# FC-1 — Implement `habbo-spec completeness`

**Baseline observed while designing this slice:** `a1c4b74794b916dbff8087b44d3bc18312736d31`  
**Target client:** `WIN63-202607011411-782849652`

## Goal

Add a read-only completeness command on top of the existing `SpecPipeline`.

Do not modify gameplay, protocol behaviour, architecture, or `FeatureBuilder` semantics merely to
make coverage look better.

## Read first

```text
AGENTS.md
CONTEXT.md
CLAUDE.md
docs/codebase/README.md
docs/codebase/.documentation-state.json
docs/codebase/02-network-protocol/
.claude/skills/habbo-spec/SKILL.md
docs/completeness/COMPLETENESS-V1.md
```

## Expected files

```text
Vortex.Specs.Cli/Program.cs
Vortex.Specs.Cli/Commands/CompletenessCommand.cs
Vortex.Specs.Tests/**
docs/completeness/generated/**        # only with --write
```

Supporting model/classifier code under `Vortex.Specs` is allowed if it is pure and testable.

## Forbidden files by default

```text
Vortex.PacketHandlers/**
Vortex.Rooms/**
Vortex.Players/**
Vortex.Catalog/**
Vortex.Inventory/**
Vortex.Marketplace/**
Vortex.Revisions/**
```

FC-1 measures; it does not fix feature gaps.

## Algorithm

1. `SpecPipeline.Scan()` once.
2. `SpecPipeline.Resolve()` once.
3. Select `ClientScan` where:
   - `Authority == EvidenceAuthority.ClientCode`;
   - `TargetsSameRevision == true`.
4. Incoming client packets:
   - `Direction == Incoming`;
   - `HeaderId != null`;
   - dedupe by `Canonical`.
5. Join to `PacketSpec` by direction + canonical name.
6. Join to `FeatureSpec` by `TriggerPackets`.
7. Apply `decisions.yaml` only after validating required evidence.
8. Apply `verification.yaml` only after implementation status is at least `IMPLEMENTED`.
9. Emit deterministic output.

## Conservative classifier

```text
no PacketSpec
    => MISSING

!MappedInVortex || VortexHandler == null
    => MISSING

mapped + handler but no FeatureSpec
    => PARTIAL

FeatureSpec exists but !ObservedInVortex
    => PARTIAL

ObservedInVortex
    => IMPLEMENTED

valid explicit N/A decision
    => NOT_APPLICABLE

insufficient analyzer join/reachability evidence
    => UNKNOWN

HeaderId == null
    => UNRESOLVED_SURFACE (separate surface)
```

Do not assign `COMPLETE` solely from static analysis.

## CLI

```bash
dotnet run --project Vortex.Specs.Cli -- completeness
dotnet run --project Vortex.Specs.Cli -- completeness --domain room
dotnet run --project Vortex.Specs.Cli -- completeness --status missing
dotnet run --project Vortex.Specs.Cli -- completeness --write
dotnet run --project Vortex.Specs.Cli -- completeness --fail-on missing
```

## Required output

- target revision;
- number of target incoming obligations;
- unresolved target surface count;
- mapped / missing / partial / implemented / complete / unknown / N/A counts;
- per-domain summary;
- sorted gap list;
- no percentage when a target same-revision official client cannot be identified.

## Required tests

1. target packet absent from Vortex → `MISSING`;
2. target packet unmapped → `MISSING`;
3. mapped but no handler → `MISSING`;
4. handler but no meaningful feature → `PARTIAL`;
5. observed feature → `IMPLEMENTED`;
6. target packet without header → `UNRESOLVED_SURFACE`;
7. other revision → excluded from score;
8. duplicate canonical → one obligation;
9. N/A missing evidence → validation failure;
10. verification cannot promote missing → failure;
11. no target client → no fake 100%, non-zero exit;
12. generated files are byte-deterministic for unchanged input.

## Quality

```bash
dotnet build Vortex.Main/Vortex.Main.csproj -t:VortexCloudFastCheck
dotnet build Vortex.Main/Vortex.Main.csproj -t:VortexCloudQualityGate
dotnet run --project Vortex.Specs.Cli -- validate
dotnet run --project Vortex.Specs.Cli -- completeness --write
```

## Done when

The generated denominator comes from `WIN63-202607011411-782849652` and a feature with **zero Vortex code** still
appears as a gap.
