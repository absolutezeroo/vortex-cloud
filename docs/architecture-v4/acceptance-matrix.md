# V4 acceptance matrix

Every requirement the architecture note makes blocking, and the thing that proves it. A requirement
with no artefact in the right-hand column is **not met**, whatever the code looks like — that is the
whole reason this table exists rather than a paragraph saying the refactor went well.

Read it as of the last row of `STATE.yaml`'s `completed:` list. Regenerate the judgement, not the
prose, when something moves.

---

## Commerce (§6.8, §10 "fault-injection first")

| Requirement | Proof | |
|---|---|:--:|
| The eight windows characterised before being closed | `Vortex.Database.Tests/Commerce/` — fault injection against the real `InventoryGrain`, asserting final business state | ✅ |
| Goods delivered + payment refunded is unreachable | `[Theory]` over every post-pivot step in `CatalogGrantWindowTests`, `TargetedOfferWindowTests` | ✅ |
| Gift wrapping | `Vortex.Database.Tests/Catalog/CatalogGiftPurchaseTests.cs` | ✅ |
| A duplicate wallet `stepKey` applies once | `PlayerWalletGrain` receipt inside the debit's own transaction; `CommerceJournalTests` | ✅ |
| Replay under a receipt writes one row | `CommerceJournalTests` — unique index on `(operationId, stepKey)` | ✅ |
| Crash between commit and publish still delivers the event | `CommerceRelayTests` — the event leaves from the journal | ✅ |
| A replayed event leaves progression unchanged | `CommerceReplayGuard.FirstDeliveryAsync`, one key per consumer; quests and daily tasks | ✅ |
| Marketplace's four flows as journalled state machines | `MarketplaceWindowTests`, `MarketplaceListingTests`; the divergence is ADR-002 | ✅ |
| Post-pivot exhaustion escalates rather than compensating | `CommerceOperationState.NeedsIntervention` + `Vortex.commerce.operation{state}` | ✅ |

## Wired (§6.4, §10)

| Requirement | Proof | |
|---|---|:--:|
| The engine runs without a `RoomGrain` | `WiredEngineOnAFakeRoomTests` — 8 tests on `IWiredRoomHost` | ✅ |
| Behavioural parity matrix, fake clock, seeded RNG | `WiredParityTests` — 9 rules: same-tick zero delay, pile order, clock-anchored delay, late tick once, left the pile, stayed on it, picked up, ghost trigger, queue cap | ✅ |
| Depth comes from configuration (RFW-101) | `ConfiguredBudgetTests`; `RoomWiredSystem.MaxCallChainDepth` reads `Room.MaxCallChainDepth` | ✅ |
| Normalised `StopReason`, chains counted by reason | `WiredStopReason` — DEPTH, CYCLE, QUEUE_DROP, EXECUTION_LIMIT, REVALIDATION | ✅ |
| Events counted: ignored / processed / dropped | `Vortex.wired.event{outcome}` + `chain.stopped{queue-drop}`; `WiredEngineOnAFakeRoomTests` | ✅ |
| Index rebuilds counted | `Vortex.wired.index.rebuilt`; `TheIndexIsRebuiltOnceAndThenLeftAlone` | ✅ |
| A delayed effect that lost its pile is visible | `REVALIDATION` + a room-log line; asserted in `WiredParityTests` | ✅ |
| `ExecutionId` / `ParentExecutionId` on execute-stacks, room-scoped opt-in trace | — | ❌ |

> The last row is the one wired item left. The room's own wired log gives the in-room chronology; what
> the note asks for is the same thing hotel-wide and correlatable. Nothing depends on it.

## Furniture (§6.9, §10)

| Requirement | Proof | |
|---|---|:--:|
| A logic name claimed twice is a collision | `RoomObjectLogicProvider` throws; `RoomObjectLogicCollisionTests` | ✅ |
| Re-registering the same implementation is idempotent | same test — inert disposable rather than a throw | ✅ |
| Hot-unload restores | `RoomObjectLogicCollisionTests` | ✅ |
| The family fallback is counted | `Vortex.furniture.logic.fallback{logic_name,family}` | ✅ |
| Both definition indexes published as one version | `FurnitureDefinitionSetTests` — one `Volatile.Write` over `{ById, ByName, Version}` | ✅ |
| A reader sees version N or N+1, never a mix | same test | ✅ |
| StuffData round-trip, per type | — | ❌ |

> `StuffDataType` has eight encodings and no test walks a value out and back through each. That is the
> gap most likely to be a live bug: an encoding that does not round-trip shows
> up as a dialog that opens blank, which reads as an unimplemented furni.

## Architecture (§12.3, §10)

| Requirement | Proof | |
|---|---|:--:|
| The interleaving manifest is exact and complete | `InterleavingManifestTests` — a method not listed fails the build | ✅ |
| Category B: no `await` before the mutation completes | same test | ✅ |
| `RoomGrain` is not `[Reentrant]` | `RoomGrainConcurrencyTests` | ✅ |
| No manual locks, no `Task.Run` inside Rooms | same test | ✅ |
| Every tuning knob is wired to configuration | `ConfiguredBudgetTests` | ✅ |
| Partials carry no concrete logics | — | ❌ |
| Single-silo singletons are registered as such | `SingleSiloInventoryTests` — 14 providers, 2 aggregators | ✅ |
| `STATE.yaml` keeps its schema | `WorkflowStateTests`, inside `VortexCloudFastCheck` | ✅ |

> `check-architecture-walls.mjs` holds five other boundaries CONTEXT.md states in prose — handlers
> stay orchestration-only, the admin surface writes through grains, protocol does not leak into
> contracts, a password is verified in one place — but none of them is the partials rule. That one is
> still prose, and prose erodes; it is small enough to encode the same way when somebody wants it.

## Protocol and specs (§6.10, §6.11)

| Requirement | Proof | |
|---|---|:--:|
| A new wire disagreement with the client fails the gate | `check-wire-conflicts.mjs`, exit 2, baselined | ✅ |
| A mapped header the client cannot reach fails the gate | `check-header-registry.mjs` — 979 mapped, 3 known unreachable, each with a reason | ✅ |
| Outgoing layouts compared against the build we target | `As3ClientAnalyzer` binds the parser behind each `MessageEvent`; 514 of 842 outgoing specs carry WIN63 evidence, was 17 | ✅ |
| `bootstrap` is reproducible — unchanged checkout, identical tree | `habbo-spec validate` in `VortexCloudFastCheck`; 3668 spec files | ✅ |
| Hand-written `verified:` / `manual:` survive regeneration | digest check; an edit inside `generated:` blocks rather than reverts | ✅ |
| Daybreak and Arcturus are distinct origins, each with a SHA | — | ❌ |

> Today every tree with the Arcturus java layout is labelled `arcturus`, so
> `HABBO-ARCTURUS-DAYBREAK` reports as Arcturus. With one reference tree present that is a mislabel
> rather than a merge; it becomes a real problem the moment a second one lands, because two derived
> emulators agreeing would read as corroboration instead of as one lineage repeating itself. The tree
> here is not a git checkout, so the SHA half needs a source that has one.

## Persistence (§6.12)

| Requirement | Proof | |
|---|---|:--:|
| The loss window is documented per domain | `persistence-loss-window.md`, measured off the code | ✅ |
| Reload is idempotent | same document, §"Idempotence on reload" | ✅ |
| A failed flush does not lose its batch | `RoomPersistenceLossWindowTests` | ✅ |
| Deactivation drains rather than flushing once | same test, plus the no-progress bound | ✅ |

## Benchmarks (§10)

| Requirement | Proof | |
|---|---|:--:|
| Empty room, loaded room, event storm, chain firing | `docs/architecture-v4/benchmarks/wired-engine.md`, `VORTEX_BENCH=1` | ✅ |
| No-trigger ≈ O(1) | `WiredEngineCostTests` — asserted as a scan count, not a duration, so it holds on any machine | ✅ |
| Massive hydration (OQ-7) | — | ❌ |
| A significant regression is refused without justification | the baseline exists and is committed; the refusal is a human reading it in review | ⚠️ |

> Hydration was never benchmarked because OQ-7 is still open: what the target is depends on a decision
> nobody has taken. Measuring it now would produce a number with nothing to compare it to.

## Documentation (PR-Z1)

| Requirement | Proof | |
|---|---|:--:|
| "Add a commerce flow" | `docs/walkthroughs/add-a-commerce-flow.md` | ✅ |
| "Add a Wired" | `docs/walkthroughs/add-a-wired-box.md` | ✅ |
| "Add a furni" | `docs/walkthroughs/add-a-furni.md` | ✅ |
| ADR review | ADR-000, ADR-001, ADR-002 in `decisions/`; ADR-002 records a deliberate divergence | ✅ |
| Acceptance matrix | this file | ✅ |

---

## What is not met

Five, and none of them blocks anything shipped:

1. **StuffData round-trip per type** — the one worth doing next. Eight encodings, no test.
2. **Daybreak / Arcturus provenance** — a mislabel today, a merged authority the day a second
   reference tree appears.
3. **Partials carry no concrete logics** — the one architecture rule of §12.3 still living only in
   prose. The other seven are tests.
4. **Wired `ExecutionId` / room-scoped trace** — the in-room log already answers this inside one room.
5. **Hydration benchmark** — waiting on OQ-7 rather than on effort.

And one that is not a test at all: refusing a benchmark regression is a person reading the baseline in
review. Writing that down is the honest version; pretending a script does it would be worse than the
gap.
