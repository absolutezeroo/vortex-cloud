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
| `ExecutionId` / `ParentExecutionId` on execute-stacks | `WiredCallChainGuardTests`; stamped on every `room_wired_logs` row | ✅ |
| Room-scoped opt-in trace: selections by source, condition and policy results, effects chosen/skipped with order and deadline | — | ❌ |

> The identity half is what makes the existing log a chronology: two piles firing in the same tick
> used to produce one interleaved list nobody could separate, and a reader can now filter to one
> chain and walk up to the one that called it. The exhaustive half is still open, and is opt-in in
> the note for a reason — a line per selection per condition per effect would be a log nobody reads
> and a table that grows faster than the room it describes.

## Furniture (§6.9, §10)

| Requirement | Proof | |
|---|---|:--:|
| A logic name claimed twice is a collision | `RoomObjectLogicProvider` throws; `RoomObjectLogicCollisionTests` | ✅ |
| Re-registering the same implementation is idempotent | same test — inert disposable rather than a throw | ✅ |
| Hot-unload restores | `RoomObjectLogicCollisionTests` | ✅ |
| The family fallback is counted | `Vortex.furniture.logic.fallback{logic_name,family}` | ✅ |
| Both definition indexes published as one version | `FurnitureDefinitionSetTests` — one `Volatile.Write` over `{ById, ByName, Version}` | ✅ |
| A reader sees version N or N+1, never a mix | same test | ✅ |
| StuffData round-trip, per type | `StuffDataRoundTripTests` — 22 cases over the eight encodings | ✅ |

> It found two. `extra_data` is a free string column — imports, admin edits and one-off SQL write it
> too — and both a malformed value and `{"stuff":null}` threw, inside a room's activation. One bad row
> stopped a whole room from opening. Both are guarded now: an item with no readable state is a
> defaulted item, and a room nobody can enter is worse.

## Architecture (§12.3, §10)

| Requirement | Proof | |
|---|---|:--:|
| The interleaving manifest is exact and complete | `InterleavingManifestTests` — a method not listed fails the build | ✅ |
| Category B: no `await` before the mutation completes | same test | ✅ |
| `RoomGrain` is not `[Reentrant]` | `RoomGrainConcurrencyTests` | ✅ |
| No manual locks, no `Task.Run` inside Rooms | same test | ✅ |
| Every tuning knob is wired to configuration | `ConfiguredBudgetTests` | ✅ |
| Partials carry no concrete logics | `check-architecture-walls.mjs` wall 6 — no `[RoomObjectLogic]` in the grain, and a 900-line façade ceiling | ✅ |
| Single-silo singletons are registered as such | `SingleSiloInventoryTests` — 14 providers, 2 aggregators | ✅ |
| `STATE.yaml` keeps its schema | `WorkflowStateTests`, inside `VortexCloudFastCheck` | ✅ |

> Two halves, because one of them cannot be read from code. The exact half is that no concrete
> furniture behaviour is declared inside the grain. The proxy half is size: a façade that reaches nine
> hundred lines has stopped being one whatever it contains. `RoomGrain.Settings.cs` is at 847 and is
> the file to watch — crossing the ceiling means a System is waiting to be lifted out of it, not that
> the number needs raising.

## Protocol and specs (§6.10, §6.11)

| Requirement | Proof | |
|---|---|:--:|
| A new wire disagreement with the client fails the gate | `check-wire-conflicts.mjs`, exit 2, baselined | ✅ |
| A mapped header the client cannot reach fails the gate | `check-header-registry.mjs` — 979 mapped, 3 known unreachable, each with a reason | ✅ |
| Outgoing layouts compared against the build we target | `As3ClientAnalyzer` binds the parser behind each `MessageEvent`; 514 of 842 outgoing specs carry WIN63 evidence, was 17 | ✅ |
| `bootstrap` is reproducible — unchanged checkout, identical tree | `habbo-spec validate` in `VortexCloudFastCheck`; 3668 spec files | ✅ |
| Hand-written `verified:` / `manual:` survive regeneration | digest check; an edit inside `generated:` blocks rather than reverts | ✅ |
| Daybreak and Arcturus are distinct origins, each with a SHA | reference trees are named after their directory (`habbo-arcturus-daybreak`); the SHA is read from `.git/HEAD` when there is one | ⚠️ |

> The origin half is done: two derived emulators can no longer be merged into one authority, so
> agreement between them will read as two sources rather than as one lineage repeating itself. The
> SHA half is implemented and inert — the tree present was copied rather than cloned and has no
> `.git`, so nothing is recorded. ⚠️ rather than ✅ because the code is there and untested against a
> real checkout.
>
> Fixing this also caught a test that had stopped testing: `ExternalTreeAnalyzerTests` selected the
> tree by the literal id `"arcturus"` and returned early when it did not match, so eight assertions
> quietly became no-ops. It selects by kind now.

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

Two, plus one implemented-but-unproven:

1. **Wired `ExecutionId` / room-scoped trace** — the in-room log already answers this inside one room.
2. **Hydration benchmark** — waiting on OQ-7 rather than on effort.
3. ⚠️ **Provenance SHA** — written, and inert until a reference tree arrives as a git checkout.

And one that is not a test at all: refusing a benchmark regression is a person reading the baseline in
review. Writing that down is the honest version; pretending a script does it would be worse than the
gap.
