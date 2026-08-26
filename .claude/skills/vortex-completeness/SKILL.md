---
name: vortex-completeness
description: Measure and close functional gaps in Vortex after architecture work is complete. Uses document-vortex for current codebase truth and habbo-spec for target-client/protocol evidence, then drives one evidence-backed vertical slice at a time. Use when asked what Vortex is missing, how complete it is, what to implement next, or to close a specific client-visible gap.
argument-hint: "[status|bootstrap|next|domain <name>|implement <packet-or-feature>|validate]"
allowed-tools: Read, Grep, Glob, Bash, Task, Write, Edit
---

# Vortex Functional Completeness

This skill is the **orchestrator for Phase 2**.

It does not replace:

- `.claude/skills/document-vortex/SKILL.md`
- `.claude/skills/habbo-spec/SKILL.md`

Read both when the requested work touches their responsibilities.

## Core contract

```text
document-vortex
  -> what Vortex actually does

habbo-spec
  -> what the target client/protocol evidence says

vortex-completeness
  -> the gap between those two
  -> next vertical slice
```

Never build a second architecture scanner or a second protocol truth system.

Canonical program document:

`docs/completeness/COMPLETENESS-V1.md`

State:

`docs/completeness/STATE.yaml`

## Modes

### `/vortex-completeness status`

Read-only.

1. Read `docs/completeness/STATE.yaml`.
2. Read `docs/codebase/.documentation-state.json`.
3. Resolve `HEAD`.
4. Check whether implementation changes since the documented SHA make relevant codebase docs stale.
5. Run `habbo-spec completeness` if implemented.
6. Report surface counts, status counts, highest-value gaps and stale documentation.
7. Do not edit code.

### `/vortex-completeness bootstrap`

Infrastructure only.

- If `habbo-spec completeness` does not exist, implement **FC-1** from `docs/completeness/FIRST-SLICE-FC1.md`.
- Do not fix any gameplay feature in the same change.
- Generate `docs/completeness/generated/`.
- Run quality gates.

### `/vortex-completeness next`

Read-only triage.

1. Ensure codebase docs are fresh enough for affected domains.
2. Run/generate completeness.
3. Rank gaps by the program priority policy.
4. Read the relevant `docs/codebase/<domain>/` pages for the top candidates.
5. Query `habbo-spec analyze` for the candidate packet/feature.
6. Surface conflicts and unknowns.
7. Recommend **one** next vertical slice with a precise reason.
8. Do not implement unless the user asked to implement.

### `/vortex-completeness domain <name>`

Read-only domain audit.

- Read domain codebase docs first.
- Generate/filter completeness for the domain.
- Distinguish MISSING, PARTIAL, IMPLEMENTED, COMPLETE, UNKNOWN and N/A.
- List the next three coherent vertical slices.
- Do not re-audit global architecture.

### `/vortex-completeness implement <packet-or-feature>`

Write mode.

Run the slice workflow in `references/slice-workflow.md`.

Mandatory:

1. establish target-client entrypoint;
2. read current codebase docs;
3. run `habbo-spec analyze`;
4. write the slice contract before edits;
5. implement only that slice;
6. add risk-appropriate tests;
7. run FastCheck + QualityGate + `habbo-spec validate`;
8. update codebase documentation if behaviour changed;
9. regenerate completeness;
10. prove the status improved for a real reason.

### `/vortex-completeness validate`

Read-only except regenerated reports if explicitly requested.

Check:

- target client is same revision;
- denominator is client-derived;
- no invalid N/A entries;
- verification records cite actual evidence;
- generated matrix is deterministic;
- no MISSING item is marked COMPLETE;
- codebase docs are not materially stale for scored domains;
- `habbo-spec validate` is green.

## Mandatory source order

For a feature slice:

1. `AGENTS.md`
2. `CONTEXT.md`
3. `CLAUDE.md`
4. `docs/codebase/README.md`
5. relevant `docs/codebase/<domain>/` pages
6. `docs/completeness/generated/`
7. `habbo-spec analyze <feature|packet>`
8. relevant conflicts/unknowns
9. current source code

The current code wins over stale `docs/codebase/` for implementation facts; then update the docs.
Unknown official Habbo behaviour stays unknown.

## Freshness rule

Read `docs/codebase/.documentation-state.json`.

If implementation changed since `documentedCommit` in paths relevant to the work, invoke the
`document-vortex` workflow (`update` or targeted topic as appropriate) before treating the docs as current.

Do not regenerate 52 pages merely because HEAD advanced through docs-only or skill-only commits.

After an implementation slice that changes documented behaviour, update the documentation before closure.

## Protocol rule

Follow `.claude/skills/habbo-spec/SKILL.md`.

Before changing handler/parser/serializer/composer/header behaviour:

```bash
dotnet run --project Vortex.Specs.Cli -- analyze <feature-id|PacketName>
```

Do not promote reference-emulator behaviour into official truth.
Do not guess a revision-specific header.

## Denominator rule

The primary incoming surface is the official same-revision target client.

Never use `ResolvedSpecs.Features.Count` as the denominator: `FeatureBuilder` is Vortex-flow-derived
and therefore cannot represent a feature that has no Vortex flow.

Read `references/source-contract.md`.

## Status rule

Read `references/classification-policy.md`.

Static analysis may establish `MISSING`, `PARTIAL` or `IMPLEMENTED`.

`COMPLETE` requires verification evidence. Compilation is not verification.

## Architecture freeze

Runtime V4 and Dashboard/API/Security are not reopened by this program.

A missing feature is implemented **inside the accepted boundaries**.

If the only viable solution appears to violate an accepted architecture rule:

```text
STOP
state the conflict
show evidence
propose an ADR
do not silently redesign the subsystem
```

## Anti-score-gaming

Reject:

- empty handlers;
- N/A without proof;
- removing obligations from the target surface;
- lowering unknown/conflict severity without evidence;
- serializers/composers that never become reachable;
- reference-emulator guesses presented as Habbo truth;
- architecture changes made only to improve a metric;
- manual edits to generated matrix files.

## Completion loop

```text
measure
-> choose one gap
-> understand real owner
-> understand protocol evidence
-> implement
-> test
-> update docs
-> re-measure
```

The output of the skill is not "we changed code." The output is:
**the client-visible obligation moved to a better status for a provable reason.**
