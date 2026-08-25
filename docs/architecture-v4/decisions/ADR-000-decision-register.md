# ADR-000 — V4 decision register

- **Status**: Accepted (2026-08-25)
- **Supersedes**: the V1, V2 and V3 architecture notes for all planning purposes.
- **Source**: `docs/architecture/architecture-workflow.md` §8, verified against the code at
  `426efc4f` before acceptance.

## Context

Three prior revisions of the architecture note accumulated decisions in prose. A decision that lives
only in prose gets re-litigated by the next session, which is how the V2 rule "every
`[AlwaysInterleave]` returns an immutable" survived long enough to contradict a legitimate, working
design (`SendComposerAsync`). This ADR freezes the register so a later session has to *announce* a
conflict instead of quietly reversing one.

## Decision

The register below is binding. An entry marked KEEP or REJECT is not reopened without a new ADR
citing new evidence.

| Decision | Verdict | Why |
|---|---|---|
| `RoomGrain` is the single owner of a room's mutable state | KEEP | Frozen by `RoomGrainConcurrencyTests`. |
| No grain per item / wired box / avatar / pet / bot / stack | KEEP | Would destroy live stack resolution and mutation ordering. |
| Furniture registry = the existing one, hardened | KEEP + HARDEN | Collision policy, fallback metric. No second registry. |
| Enum key for furniture logics | REJECT | `logic_name` in the DB and the client is the canonical vocabulary. |
| `RoomItemFactory` | REJECT | `RoomItemsProvider` → `RoomObjectModule` → `RoomFurniModule` already is that chain. |
| Plugin exemption from strict collision mode | REJECT | `RegisterLogic` overwrites destructively and the dispose does not restore — the exemption *is* the bug. |
| Interleave rule "returns an immutable" | REJECT | `SendComposerAsync` is the legitimate counter-example. Replaced by categories A/B, mechanised. |
| `IWiredRoomAccess` exposing collections | MODIFY → `IWiredRoomHost` capabilities | No mutable collection crosses the boundary. |
| `WiredPendingStackExecution` treated as a plan | MODIFY | It is mutable runtime state; the immutable projection is a separate test/trace type. |
| Shadow Wired engine | CONTINGENCY | Extraction plus a parity matrix is enough; a parallel engine double-mutates. |
| `WiredMaxDepth` 20 config vs 8 const (RFW-101) | FIX (P0-B) | One source of truth, one runtime reader, one test. Value settled by OQ-1. |
| Generic Economy Kernel | REJECT for now | Two proven-divergent topologies (compensable purchase vs pivot settlement). |
| Orleans Transactions for commerce | REJECT | They cover Orleans transactional state, not EF/MySQL plus cross-grain side effects. |
| Commerce safety primitives (OperationId, pivot, journal, receipts, recovery) | KEEP — P0 | Every window is verified in code. |
| **D-V4-1** Reduce the post-pivot surface before instrumenting it | NEW — precondition | Furni, badges, pets and bots are rows written through the same factory in the same grain; four commits is an artefact, not a constraint. |
| **D-V4-2** The operation journal's terminal transitions are the source of the critical-event relay | NEW | One durable pipeline, not two. No standalone outbox table until a critical event with no operation exists. |
| **D-V4-3** Wallet receipts go inside the transaction `TryDebitAsync` already opens; the API grows by overload | NEW | The transaction exists; additive evolution avoids a breaking change. |
| **D-V4-4** Marketplace Buy: the `ExecuteUpdate` claim stays as the pivot | NEW (framing) | The claim already prevents a double sale. The defect is the completion after it, not the claim. |
| **D-V4-5** Interleaving manifest, mechanised; category B = no `await` before completion | NEW | The safety property is checkable, so it is checked. |
| **D-V4-6** Post-pivot completion runs under a shutdown-linked token, never the request's | NEW | The refund already does this; delivery must too. |
| **D-V4-7** The targeted-offer counter is a journalled step, not a special case | NEW | Same mechanism as everything else. |
| **D-V4-8** Every post-pivot step is proven idempotent by a replay test | NEW | `AddEffectAsync` inserts unconditionally — assumption was already wrong once. |
| **D-V4-9** Registry collision policy: FAIL for core/core, plugin/core, plugin/plugin | NEW | An override is a registration *stack*, never an overwrite. |
| **D-V4-10** The 17-domain grid is the canonical format for future revisions | NEW | Keeps revisions comparable. |
| PlayerPresence split | DEFER | Partials are small; the threshold is written in §6.5. |
| Multi-silo | DEFER | The `MultiSiloReady` thesis is not lifted. |
| Dashboard / API / Security | Separate workstream | `docs/architecture/dashboard-api-security.md`, FROZEN. Never mixed into a V4 runtime PR. |

## Rejected alternatives

- A generic workflow/saga framework: two flows are not a pattern; the third one earns the abstraction.
- A grain per commerce operation: the journal is a queryable table, which is what recovery and ops
  actually need; a grain per operation adds activation cost and no durability.
- A repository/unit-of-work layer, generalised CQRS, global event sourcing, microservices: all
  rejected in every prior revision and re-rejected here.

## Consequences

- Any PR touching Catalog / Marketplace / Wallet / Inventory declares a commerce contract
  (see `plans/SLICE-TEMPLATE.md`).
- Any new `[AlwaysInterleave]` needs a manifest entry in the same commit, or the build fails.
- Any new wired budget needs a runtime reader, or the build fails.

## Evidence

`docs/architecture/architecture-workflow.md` §2.2 (line-level verification table), re-read at
`426efc4f`: `InventoryGrain.Furni.cs` (SaveChanges before pets/bots/effects), `InventoryGrain.Pets.cs`
and `.Bots.cs` (own commits), `IPlayerWalletGrain.cs` (no operation identity on the contract),
`MarketplacePurchaseGrain.cs` (four windows), `PlayerEffectGrain.cs` (unconditional insert),
`RoomWiredSystem.cs` (`MaxCallChainDepth` const), `RoomConfig.cs` (`WiredMaxDepth` unread).
