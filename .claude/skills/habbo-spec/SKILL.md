---
name: habbo-spec
description: Start any Habbo protocol change at the specs instead of at the code — what is known about a packet, what is only assumed, and which checks will fail if you guess. Use before writing or modifying a handler, parser, serializer, composer or header id.
---

# Protocol work starts here

We do not have Habbo's server. The *structure* of a packet is knowable from the client; what the
server *does* with it is not. The specs tree separates those two, and the whole point is that it
also records what nobody knows. Full contract: `AGENTS.md`, "Habbo protocol behaviour: consult the
specs first". Adding evidence: `docs/walkthroughs/use-the-habbo-specs.md`.

## 1. Ask the specs first

```bash
dotnet run --project Vortex.Specs.Cli -- analyze <feature-id|PacketName>
```

Read, in this order: the packet layout and the **confidence on each field**, the behavioural
scenarios, the evidence cited (every claim points at a file and a line), then the conflicts and
unknowns it lists. The unknowns are exactly where the obvious change is a guess.

Three rules the tree rests on, and they decide every argument:

- Existing emulator behaviour is **evidence, not authority**.
- Reference emulator behaviour is **evidence, not authority**.
- Unknown official behaviour stays **explicitly unknown**.

`client_confirmed` structure is respected exactly. If a behaviour has to be picked to ship, it goes
in the spec's `verified:` block with `confidence: assumed` and the reason — never silently into the
code.

## 2. Before touching a serializer or parser

```bash
dotnet run --project Vortex.Specs.Cli -- conflicts --kind field_count --limit 200
node scripts/hooks/check-wire-conflicts.mjs
```

`conflicts` mixes authorities: a disagreement with a reference emulator is evidence, one with
`client_code` means one of the two is wrong about bytes the client is parsing right now. The script
is that filter, already applied, and it fails the QualityGate on a new one. The 23 known ones live
in `scripts/hooks/wire-conflicts-baseline.json`.

Field order, field width (`readInteger` / `readShort` / `readByte` are not interchangeable) and bool
encoding (this client reads `flag ? 1 : 0` as a **4-byte int**) all have to match the client class
that parses the message. The `wire-truth-auditor` subagent does that comparison against the AS3
source; use it before shipping, and never trust an `AS3-verified` comment in `Headers.cs` — several
were false and each one cost a live bug.

## 3. Header ids

Ids are per-revision and live only in `docs/habbo-specs/revisions/` — a numeric id inside a
behavioural spec is a validation error, specs speak in symbolic names. An id the client's registry
does not contain registers without error and can **never fire**:

```bash
node scripts/hooks/check-header-registry.mjs
```

It reads the registry out of the client sources and refuses a new absent id. The 14 already in that
state are in `scripts/hooks/header-registry-baseline.json` — each one is a dead feature.

## 4. Close the loop

```bash
dotnet run --project Vortex.Specs.Cli -- bootstrap    # only if you added evidence
dotnet run --project Vortex.Specs.Cli -- validate     # also runs in VortexCloudFastCheck
```

`verified:` and `manual:` blocks survive regeneration. An edit inside a `generated:` block is caught
by its digest and **blocks** the next regeneration instead of being reverted — so put your findings
in the right block. An unchanged checkout regenerates byte-identically; a diff after `bootstrap`
means something real changed.

Then the usual: keep serialization in the revision tree and domain logic in the domain, handlers
orchestration-only, and add or update a behavioural test when behaviour changes.
