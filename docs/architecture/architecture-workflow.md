# VORTEX CLOUD — Target architecture & AI workflow — V4

**Deep Audit Rewrite of the V3 "Deep Reconciliation" — fully replaces V1, V2 and V3 for any implementation planning.**

| | |
|---|---|
| **Objective** | A self-contained specification letting a senior AI audit, plan and then refactor Vortex without scope drift, without losing the runtime invariants, and without leaving behind the value loss/duplication windows proven in the commerce journeys. |
| **Guiding principle** | Keep Vortex's Orleans runtime as it is; evolve what exists (logic registry, Wired engine, hooks/gates/specs); treat commerce cross-grain commit boundaries as failure boundaries; reduce the post-pivot surface **before** instrumenting it. |
| **Scope** | Commerce (Catalog/Gift/Targeted/Marketplace/Wallet), Rooms/Furniture/Wired, Inventory fulfillment, Player/Presence, Protocol/Specs, Reference Data, Persistence/Events, Plugins, Observability, Tests, AI workflow. |
| **Out of scope** | Microservices, global event sourcing, generalized CQRS/MediatR, Orleans Transactions as a substitute for the commerce protocol, multi-silo as long as the `MultiSiloReady` thesis is not lifted, Dashboard/API/Security (a separate workstream). |
| **Vortex baseline** | `afc485be58ffd983b8d96430efe8aed620ad0ade` (main). **Re-verified on 2026-08-25 after fetch: local HEAD = origin/main = baseline.** |
| **References** | Skylight3 `14a9531`, Arceus `97db905`, Arceus Wired Plugin `c2e649c`, Daybreak `2dee52a` — all re-verified equal to their remote branch on 2026-08-25. |
| **Stack** | .NET 10 / `net10.0`, Orleans 10.2.1, EF Core 9.0.8 + Pomelo 9.0.0 pinned together (`AGENTS.md`) [V-AI01]. |
| **Method** | Every structural claim in V3 was re-verified **at line level** in the code at the baseline (§2.2, verification table). The four references were re-read at the frozen SHAs. Orleans/pattern claims are cross-checked against Microsoft Learn (consultation dates in Appendix C). Anything that could not be established is `UNKNOWN` or `NEEDS_EVIDENCE`. |
| **Date** | 25 August 2026 |

---

## Contents

1. Executive summary
2. Baseline, line-by-line verification of V3, evidence hierarchy
3. What V4 corrects and clarifies relative to V3
4. Reference emulator synthesis
5. Non-negotiable principles (V4)
6. Domain-by-domain analysis (17 domains, full grid)
7. Consolidated target architecture
8. Decision register: verdicts on V1→V3 and new decisions
9. Migration and PR order
10. Testing strategy
11. Observability
12. V4 anti-drift AI workflow
13. Final acceptance criteria
— CHANGES FROM V3
— OPEN QUESTIONS / NEEDS EVIDENCE
— FINAL RECOMMENDED REFACTOR ROADMAP
— Appendix A. Repositories and revisions; Appendix B. Code source index (line level); Appendix C. External references

---

# 1. Executive summary

**V3's central finding is confirmed word for word by the code — and it is even deeper than V3 wrote it.** The line-by-line audit (§2.2) validates each of the announced value loss/duplication windows: in `GrantCatalogOfferAsync`, the furniture and badges are **committed** (`SaveChangesAsync`, line 333), synchronized to the cache, notified to the client and published as events (342–379) **before** the creation of pets (395), bots (409) and effects (435) — each in its own commit or its own grain; any exception in those late steps propagates to `ExecutePurchaseAsync`, which refunds the entire price [V-CAT02][V-ECO01]. The final state "goods kept + purchase refunded" is reachable, and the code documents it itself without seeing it: the comment on the effects block says *"A throw here propagates to the wallet's ExecutePurchaseAsync so the purchase auto-refunds"* — an invariant designed for an atomic grant, applied to a grant that stopped being one as the product families piled up. The test harness cannot represent that failure class: its fake `IInventoryGrain` is binary (`GrantThrows ? throw : Task.CompletedTask`) [V-CAT03][V-CAT04].

The other V3 windows are all VERIFIED_CODE: Targeted Offers grants each unit in a separate commit then increments the purchase counter afterwards [V-CAT06][V-INV03]; Marketplace exposes four distinct windows (item removed before offer insert; cancellation committed before restitution; Sold claim before grant with explicitly best-effort compensation; `CreditsOwed=0` committed before the wallet credit) [V-MKT01]; the wallet has **no** operation identity — `TryDebitAsync`/`CreditBackAsync` take lists of requests with no OperationId, so a refund retry can credit twice and a crash between the debit (durable, EF transaction + execution strategy [V-ECO02]) and the grant leaves no trace to resume from [V-ECO03]. Finally `AddEffectAsync` inserts a row unconditionally: a post-timeout retry duplicates the effect [V-EFF01] — direct proof that "retryable after pivot" requires receipts, not just good intentions.

**The four corrections V3 brought to V2 are accepted — all verified.** (1) The V2 rule "every `[AlwaysInterleave]` returns an immutable" was too narrow: `SendComposerAsync` is interleaved to break the Presence→Room→Presence deadlock and it is safe because it performs only a synchronous queue mutation **with no await** before `Task.CompletedTask` — the interface comment says exactly that [V-PRES01][V-PRES02]. The right rule is "interleaving-safe by construction", in two categories. (2) The `RoomItemFactory` proposed in V2 is withdrawn: `RoomItemsProvider` → `RoomObjectModule` → `RoomFurniModule` already form the named materialization→attach→lifecycle chain [V-FUR04][V-FUR05][V-FUR06]. (3) The plugin exemption from strict mode was a mistake: `RegisterLogic` overwrites destructively (`_logics[key] = reg`) and the disposable removes without restoring the overwritten registration — unloading a plugin leaves the core furni on the fallback [V-FUR01]. (4) `WiredPendingStackExecution` really is mutable runtime state (`Version`, `DueAtMs`, `NextActionIndex`, `WaitingActionIndex`, `EffectsStarted` with `set`) [V-WIR03]; the testable "plan" is a separate immutable projection.

**What V4 adds to V3** (detail in §3) comes down to one rule and two consolidations. The rule: **reduce the post-pivot surface before instrumenting it** — furniture, badges, pets and bots are all rows written through the same `_dbCtxFactory` in the same grain; their separate commits are an implementation artifact, not a constraint [V-CAT02][V-INV01][V-INV02]. Consolidating the local grant into **a single commit** in `InventoryGrain`'s turn removes the biggest "goods+refund" window before any journal; what remains post-pivot is only the genuinely cross-grain steps (effects, gift wrapping, notifications, events). The consolidations: the operation journal and the selective outbox share the same support (the journal's terminal transitions *are* the source of the critical event relay — one durable write, one relay, not two pipelines); and the wallet idempotency receipts insert into the transaction `TryDebitAsync` **already opens** (BeginTransaction + execution strategy verified) — the API evolves by adding overloads carrying an OperationId, with no break.

| **Priority** | **Workstream** | **Nature** |
|---|---|---|
| P0-A | Commerce Consistency (Catalog/Gift/Targeted/Marketplace/Wallet) — characterization by fault injection, surface reduction, identity/journal/receipts, forward recovery | Correctness: proven value losses and duplications |
| P0-B | Wired: wiring `WiredMaxDepth` (20 config vs 8 const) + "knob is read" tests | Config correctness |
| P0-C | Golden tests, interleaving manifest, architecture guards, perf baseline, STATE/ADR | Guardrails before extraction |
| P1 | Wired extraction behind `IWiredRoomHost` (capabilities) | Testability, identical semantics |
| P1 | Fulfillment planner + strategies **after** the commerce protocol stabilizes | Structure driven by the pivot |
| P1 | Critical outbox merged into the operation journal | Reliability of business consequences |
| P1/P2 | Furniture registry hardening (collisions FAIL by default, fallback metric) | Plugin robustness |
| P2 | Atomic `FurnitureDefinitionSet`; Specs provenance `repo@sha`; protocol baseline burn-down | Short workstreams |
| DEFER | PlayerPresence split; multi-silo; generic Economy Kernel; Wired shadow (ADR contingency) | Measured triggers |

**What this document does not ask for**: no Orleans Transactions for commerce (they manage Orleans transactional state, not EF/MySQL + multi-grain side effects [M-ORL-04]); no grain per operation by default; no outbox generalized to gameplay; no rewrite of the Wired engine; no RoomItemFactory; no enum parallel to the logic names.

---

# 2. Baseline, line-by-line verification of V3, evidence hierarchy

## 2.1 Baseline = HEAD, re-verified against the remotes

`git fetch` then `rev-parse` on the five repositories on 2026-08-25: local HEAD = remote branch = frozen SHA everywhere (Vortex `afc485be…`, Skylight3 `14a9531b`, Arceus `97db905a`, plugin `c2e649c9`, Daybreak `2dee52a0`). No drift to reconcile; all conclusions hold for the baseline **and** the current state. The rule "every future session compares its HEAD to the baseline and only revalidates the modified `watched_paths`" (V3 §17) is kept as is.

## 2.2 Verification table — every structural V3 claim re-read in the code

This is V4's own audit contribution: V3 asserted, V4 re-read. Status column: `VERIFIED` (exact), `VERIFIED+` (exact, and the code says more than V3 did), `REFINED` (true but V4 clarifies the shape).

| # | V3 claim | Verification (file: lines, symbol) | Status |
|---|---|---|---|
| A1 | Furni+badges committed before pets/bots/effects; late exception ⇒ full refund ⇒ "goods + refund" | `InventoryGrain.Furni.cs`: parsing 152–296; `AddRange` 300; badges 306–331; **`SaveChangesAsync` 333**; cache/events/presence 342–379; pets 395, bots 409, effects 435; "auto-refunds" comment on the effects block | **VERIFIED+** — the comment proves the refund invariant was designed for an atomic grant |
| A2 | Wallet debit durable at grant time | `PlayerWalletGrain.cs`: `TryDebitAsync` 55–130, fresh DbContext per attempt + `IExecutionStrategy` + `BeginTransactionAsync` + `SaveChangesAsync` (130) | VERIFIED |
| A3 | `CreditBackAsync` with no OperationId/dedup; refund not idempotent; debit→grant crash with no trace | `IPlayerWalletGrain.cs`: `TryDebitAsync(List<WalletDebitRequest>)`, `CreditBackAsync(List<WalletDebitRequest>)` — no operation identity anywhere in the contract; `WalletPurchaseExtensions.cs`: in-memory compensation, `LogCritical` if the refund fails | VERIFIED |
| A4 | All-or-nothing catalog harness, unable to model a partial grant | `CatalogPurchaseHarness.cs` 174–188: fake `IInventoryGrain` → `if (GrantThrows) throw; … return Task.CompletedTask`; `CatalogPurchaseTests.cs` 226 `AGrantThatFails_RefundsTheBuyer` | VERIFIED |
| A5 | Targeted Offers: N separate grants then the counter afterwards | `PlayerTargetedOfferGrain.cs` 85–111: `GrantFurnitureDefinitionAsync` loop per unit inside the compensated scope; 124 `IncrementPurchaseCountAsync` after success; event commented "Non-transactional"; each unit grant = its own `Add` + `SaveChangesAsync` (`InventoryGrain.Furni.cs` 603–632) | VERIFIED |
| A6 | Marketplace: 4 windows (list/cancel/buy/redeem) | `MarketplacePurchaseGrain.cs`: MakeOffer 70 `RemoveFurnitureAsync` → 99 `SaveChangesAsync` (offer insert); Cancel 127–128 `Cancelled`+commit → 132 `GrantFurnitureDefinitionAsync`; Buy 189–194 atomic `ExecuteUpdate` claim Active→Sold+CreditsOwed → 211 grant → 221–233 best-effort revert commented "Sold with no item delivered"; Redeem 302–305 `CreditsOwed=0`+commit → 308–310 `GrantCreditsAsync` | **REFINED** — the Buy claim by conditional `ExecuteUpdate` is a **good** concurrency pivot; what is missing is the durability of the completion, not the claim (§6.8) |
| A7 | `SendComposerAsync` is `[AlwaysInterleave]`, safe by synchronous mutation with no await, required for liveness | `IPlayerPresenceGrain.cs` 17–26: deadlock comment Presence→Room→Presence + "only enqueue … synchronously — no awaits"; `PlayerPresenceGrain.cs` 112–136: `EnqueueOutgoing` + `LogAndForget(ProcessOutgoingQueueAsync())` + `Task.CompletedTask` | VERIFIED — the V2 "immutable return" rule is indeed refuted |
| A8 | Furniture creation chain already named ⇒ RoomItemFactory redundant | `RoomItemsProvider.cs` 31–161 (materializes `RoomFloorItem`/`RoomWallItem` from rows/snapshots); `RoomObjectModule.cs`; `RoomFurniModule.Floor.cs` | VERIFIED |
| A9 | `RegisterLogic` overwrites; the dispose does not restore the overwritten registration | `RoomObjectLogicProvider.cs` 51–68: `_logics[key] = reg`; disposable `TryRemove(KeyValuePair(key, reg))` — removal conditional on "still current", no restoration | VERIFIED — the V2 plugin exemption is rightly rejected |
| A10 | `WiredPendingStackExecution` = mutable runtime, not a plan | `WiredPendingStackExecution.cs` 8–28: init-only (Stack/Actions/Policy/Selected/SelectorPool/Signal/ProcessingContext) + `Version`, `DueAtMs`, `NextActionIndex`, `WaitingActionIndex`, `EffectsStarted` as `get; set;` | VERIFIED |
| A11 | The boxes already have a semantic capability contract | `IWiredExecutionContext.cs` 32–88: state updates, floor/wall/user movement, direction, room composer, bot chat/move/follow/figure/walk, hand items | VERIFIED |
| A12 | In-process event bus, parallel handlers, isolated errors; loss possible after commit | `EventRegistry.cs` 42–43: `HandlerMode = Parallel`; isolation by catch; `CatalogPurchaseGrain.cs` 86–123: `PublishAsync(CatalogPurchasedEvent)` **after** `ExecutePurchaseAsync` succeeds, outside the transaction | VERIFIED |
| A13 | `CatalogPurchasedEvent` feeds progression (not just metrics) | `QuestProgressEventHandlers.cs` 86 `IEventHandler<CatalogPurchasedEvent>`; **also** `DailyTaskProgressEventHandlers.cs` [V-PROG02] — an extra consumer V3 did not cite | VERIFIED+ |
| A14 | Orleans: at-most-once by default; retries ⇒ multiple deliveries possible; no durable dedup | Microsoft Learn, *Messaging delivery guarantees* [M-ORL-03], consulted 2026-08-25 | VERIFIED |
| A15 | (new in V4) An `AddEffectAsync` retry duplicates the effect | `PlayerEffectGrain.cs` 71–91: unconditional insert `PlayerEffects.Add(new …)` | **VERIFIED — V4 proof** that "retryable after pivot" requires per-step receipts |

**Conclusion of the verification**: V3 is factually solid; none of its structural claims is refuted. The V4 verdicts (§8) are therefore mostly KEEP, with MODIFY of *shape* (post-pivot surface, journal/outbox merge, wallet evolution path, Marketplace Buy framing) — never a step back towards V2.

## 2.3 Evidence hierarchy (unchanged V1→V3)

1. Official capture / observed behaviour. 2. Official client. 3. Official Habbo documentation. 4. Vortex code at the SHA (authoritative on Vortex, never on Habbo). 5. Reference emulators (comparative evidence). 6. Specialized community sources. 7. Generic patterns. An emulator consensus never becomes a Habbo truth; the unknown stays `UNKNOWN`; delivery choices are `ASSUMED/BEST_EFFORT` in `Vortex.Specs` with a justification. Level 7 justifies **techniques** (saga, outbox, idempotence) — in this document it grounds the *shape* of the commerce protocol, never a Habbo behaviour.

---

# 3. What V4 corrects and clarifies relative to V3

V3 survived the adversarial audit; the lines below are proven refinements, not reversals.

| # | V3 point | V4 verdict | Correction / clarification |
|---|---|---|---|
| B1 | Commerce protocol presented "protocol-first": OperationId + journal + receipts + recovery applied to the current topology | **MODIFY (prior rule)** | **Reduce the post-pivot surface before instrumenting it.** Furniture, badges, pets and bots are rows written through the same `_dbCtxFactory` in the same `InventoryGrain`; their separate commits (Furni 333, Pets 53, Bots 77) are an artifact, not a constraint [V-CAT02][V-INV01][V-INV02]. A single-commit local batch removes the biggest "goods+refund" window; the journal/receipts are then sized for the genuinely cross-grain **residue** (effects [V-EFF01], gift, notifications, events). V3 said "Inventory applies local batch" in its diagram without making it a design rule or citing the pets/bots evidence. |
| B2 | Operation journal + selective outbox = two durable mechanisms | **MODIFY (merge)** | The critical events identified (CatalogPurchased → quests **and** daily tasks [V-PROG01][V-PROG02], Targeted, Marketplace) are all backed by an operation. The journal's terminal transitions become the **source** of the relay: one durable write per transition, one at-least-once relay, consumer dedup by OperationId/EventId [M-AZ-03][M-AZ-04]. An independent outbox table is only created if a critical event with no operation appears (none identified at the baseline). |
| B3 | "Wallet: debit/refund/credit receive OperationId/StepKey" (a prescription with no path) | **MODIFY (concrete path)** | `TryDebitAsync` **already** opens an EF transaction with an execution strategy [V-ECO02]: the receipt inserts into that same transaction (mutation + receipt atomic). Evolution by **adding** overloads carrying `CommerceOperationId`/StepKey and returning the earlier result on replay; the current signatures remain for non-commerce grants during the migration, then the legacy `CreditBackAsync` is removed. `WalletDebitResult` becomes serializable so it can be replayed from the receipt. |
| B4 | Marketplace Buy listed as one window among others | **REFINED** | The conditional `ExecuteUpdate` claim Active→Sold [V-MKT01, l.189–194] is a **correct concurrency pivot** — it already prevents double selling. The defect is not the claim but what follows: best-effort compensation (revert to Active) instead of durable forward recovery after the pivot. The V4 fix: claim = journalled pivot; after it, idempotent replayable completion (grant + notification); the revert only exists before the pivot. Crediting what is already good avoids "repairing" it. |
| B5 | AlwaysInterleave categories (A: immutable read; B: bounded synchronous liveness operation) described in prose | **KEEP + mechanize** | The property that makes category B safe is **the absence of an await before the mutation completes** [V-PRES01]. The manifest becomes a tested data file: every `[AlwaysInterleave]`/`[Reentrant]`/`MayInterleave` method in the repository must appear there with its category; an architecture test fails on an unlisted method, and mechanically verifies for category B that no `await` precedes completion (body analysis or the `Task.CompletedTask` returned convention + review). |
| B6 | Principle 11 "the request's CancellationToken does not govern post-pivot completion" | **KEEP + proof of the current violation** | Today the request's `ct` runs through the whole grant (pets/bots/effects) — and the `WalletPurchaseExtensions` comment identifies cancellation as **the most frequent** cause of grant failure [V-ECO01]. The refund already made the right choice (`CancellationToken.None`); the post-pivot steps must do the same: a token tied to the host's shutdown, never to the connection. |
| B7 | Targeted Offers: "count consistent after recovery" required with no shape | **MODIFY (shape)** | The counter becomes a journalled step of the operation (receipt `operationId+step:count`), replayable; or its increment joins the pivot's transaction if the pivot is on the catalog side. The current limit drift (grant succeeded, crash before `IncrementPurchaseCountAsync` l.124 [V-CAT06]) is covered by the same mechanism as the rest — no "special" counter. |
| B8 | Outbox sources: [M-AZ-03] points at a Cosmos DB sample | **REFINED (sourcing)** | The canonical reference is the Architecture Center's Transactional Outbox guide [M-AZ-04]; the sample stays cited as an illustration. No substantive impact. |
| B9 | Document structure: V3 dropped the mission's 17 domains × 11 subsections grid | **RESTORED** | V4 restores the full grid (§6) — the stabilized domains are deliberately dense there and refer back to the §2.2 verifications instead of re-deriving. |
| B10 | `AddEffectAsync` implicitly "retryable" among the post-pivot steps | **NEW PROOF** | Unconditional insert [V-EFF01]: a retry duplicates the row. Each post-pivot step gets its receipt **or** a naturally idempotent write (conditional upsert) — decided step by step in the slice, never assumed. |

---

# 4. Reference emulator synthesis

Substantively unchanged since V2/V3 — re-verified at the frozen SHAs, recalled here for self-containment:

| **Project** | **Lesson taken** | **Runtime rejection** |
|---|---|---|
| Skylight3 | `Items/{Builders,Interactions}` readability; **contributive** catalog products — `ICatalogProduct.ClaimAsync(ICatalogTransactionContext)`, `CatalogProductBadge` → `context.Commands.AddBadge` [S-CAT01][S-CAT02]: the shape of the future planner, stripped of its transaction | Dedicated thread + per-room `SpinLock` [S-RUN01][S-RUN02]; an `IDbContextTransaction` spanning the entire purchase [S-CAT03][S-CAT04] — a single-process luxury **structurally impossible between grains**, and precisely what the V4 commerce protocol replaces with pivot+idempotence |
| Arceus | Extensible interaction registry, explicit room component [A-FUR01] | String+reflection on the hot path; manual concurrency |
| Arceus Wired Plugin | **Named** pipeline phases (variables→selectors→conditions→effects→addons) and a room-scoped manager [AP-WIR01..04] — the naming of the §6.4 extraction components | `ConcurrentHashMap`/`Future`/thread manager; embryonic semantics (45 files) — never a Wired authority |
| Daybreak | `WiredServices` = a capability catalog [D-WIR01]; runtime protections (depth, rate limit) [D-WIR03]; experience report on legacy/new coexistence [D-WIR02] | Global/static manager; invalidatable stack cache [D-WIR04] (Vortex resolves live under a single turn — no stale window to manage); parallel mode = double mutation, the anti-model for shadowing |
| Vortex | Orleans + strong ownership, live stack, the most complete Wired pipeline of the four, industrialized hooks/gates/Specs | To be preserved; extract without reintroducing the concurrency the other runtimes have to manage themselves |

The goal remains `best ideas + Vortex constraints`, never an average of the four. A pattern only enters if it solves a verified pain in Vortex and respects the actor model.

---

# 5. Non-negotiable principles (V4)

The fifteen V3 principles are kept; the amendments are in bold.

1. `RoomGrain` remains the sole owner of a room's mutable state; no grain per item, Wired, avatar, pet, bot or stack.
2. Orleans remains the room's sole scheduler/concurrency boundary. No lock, `SemaphoreSlim`, Thread, `Task.Run` or external callback that mutates `RoomLiveState`.
3. `RoomGrain` remains non-reentrant. Every interleave is **INTERLEAVING-SAFE BY CONSTRUCTION**: category A (pure read of an immutable/stable snapshot) or category B (bounded synchronous operation **with no await before completion**, required for liveness — model: `SendComposerAsync` [V-PRES01]). **Every interleaved method appears in the tested manifest (§12.3); outside A/B ⇒ ADR.**
4. Timers via `RegisterGrainTimer`; business delays as absolute deadlines on a monotonic clock, never "N ticks" — the period restarts when the callback resolves [M-ORL-02].
5. Wired boxes speak through `IWiredContext`/`IWiredExecutionContext`; no box gets `RoomGrain`, a `DbContext` or the `RoomLiveState` collections. **The engine itself speaks to the grain through `IWiredRoomHost` (capabilities): no `Dictionary`/`HashSet`/mutable array crosses the boundary.**
6. The existing Furniture registry is the single source of behaviours. No second registry, no enum parallel to the DB logic names. **Every key collision fails at load — core/core, plugin/core and plugin/plugin — as long as an explicit registration-stack override does not exist as a feature [V-FUR01].**
7. Every configurable budget has exactly one source of truth, one runtime consumer, one metric and one wiring test (RFW-101: `WiredMaxDepth` 20 config vs 8 const).
8. Every derived cache/index declares its source of truth, invalidation, rebuild and metric. A Wired stack is never cached: it is resolved live at fire time.
9. `StuffData` mutations stay centralized on `MarkDirty`/`PersistStuffData`; no write path bypasses the historical invariant.
10. Every flow that moves value declares: `OperationId`, phases, **explicit pivot**, compensations before the pivot, idempotent steps after it, a recovery owner, critical events. **And first: the post-pivot surface is minimized by design — anything that can join the pivot's local commit does (principle B1).**
11. After a pivot, cancelling the client request is not a rollback order: the completion lives under a token tied to the shutdown, not to the connection. **The current refund already applies that rule (`CancellationToken.None`) — extend it to the delivery steps [V-ECO01].**
12. Retries only exist with idempotence: Orleans is at-most-once by default and does not deduplicate durably in the presence of retries [M-ORL-03]. **A post-pivot step is either covered by a receipt or naturally idempotent — proven by a replay test, never assumed (counter-example: `AddEffectAsync` [V-EFF01]).**
13. Selective outbox: only the post-pivot business events whose loss changes progression/reward/audit — **implemented by the operation journal (relay on terminal transitions), not as a second pipeline** [M-AZ-04]. Room events/tick/Wired stay in-memory.
14. No generic repository/UoW, no generalized CQRS/MediatR, no global event sourcing, no microservices, no simulated global ACID transaction, no Economy framework, **and no Orleans Transactions for commerce** [M-ORL-04].
15. Every modified Habbo behaviour goes through `Vortex.Specs` first; the unknown stays `UNKNOWN`.

---

# 6. Domain-by-domain analysis

The full grid is restored (B9). The domains stabilized since V2/V3 are dense and refer to the §2.2 verifications; the domains touched by the commerce P0 are detailed. "Nothing usable" = the reference was re-read and brings nothing Vortex does not have better, or nothing transposable under Orleans.

## 6.1 Runtime / Orleans / concurrency

#### CURRENT VORTEX
A single Orleans 10.2.1 silo; non-reentrant `RoomGrain`; `RegisterGrainTimer` tick with ordered steps isolated by `RunTickStepAsync` (a throw takes down neither the tick nor the flushes) [V-RUN01]; absolute deadlines + `AdvanceBoundaryPast` for every cadence. Existing interleaves: immutable snapshot reads (`GetSnapshot` room/presence) and `SendComposerAsync` (category B verified, A7). Single-silo guard at startup, thesis documented in `OrleansHostConfig` [V-MULTI01].

#### WHAT VORTEX DOES WELL
Zero `Task.Run`/lock/`SpinLock` in `Vortex.Rooms`; the period-after-callback semantics of the timers is already absorbed by the deadlines [M-ORL-02]; per-step recovery after a throw distinguishes Vortex from the three references.

#### PROBLEMS
(a) No manifest of the interleaved methods: the safety of `SendComposerAsync` lives in an interface comment [V-PRES01], not in a test — and the V2 "immutable return" rule would have forbidden that legitimate case. (b) The emission order of `LogAndForget` composers relative to subsequent turns is not contracted (OQ-5). (c) Nothing prevents adding a new process-local singleton that silently worsens the multi-silo debt.

#### SKYLIGHT3 LESSONS
Dedicated thread + `SpinLock` per room [S-RUN01][S-RUN02]: manually rebuilding what Orleans gives per activation. An onboarding counter-example, nothing to import.

#### ARCEUS LESSONS
`IThreadManager` injected all the way into the plugin's Wired pipeline [AP-WIR04]: the same lesson in Java.

#### DAYBREAK LESSONS
Runtime protections at engine level (depth, rate limit) [D-WIR03]: the idea that "the runtime protects itself from its players" — Vortex already has it (bounded queue, budgets, chain guard).

#### PATTERNS TO REJECT
`[Reentrant]` on `RoomGrain`; legacy `RegisterTimer` (interleaves by default); any "just to be safe" lock; "N ticks" as a business delay.

#### TARGET ARCHITECTURE
Unchanged + three guards: a tested interleaving manifest (§12.3 — categories A/B, unlisted method = architecture test failure, category B verified as "no `await` before completion"); a "no `[Reentrant]` on RoomGrain" test; a tested registry of single-silo components (`ReloadAsync` providers, aggregators, memory streams) so the multi-silo debt stops growing silently.

#### MIGRATION STRATEGY
No movement: freeze by tests, delivered in P0-C (PR-G1).

#### TESTS REQUIRED
Complete and exact interleaving manifest; fake tick clock; "a long callback ≠ overlap or double tick" [M-ORL-02]; single-silo singletons registered.

#### RISKS
Low; the risk is regressive (a "temporary" `Task.Run` during an extraction) — the architecture tests are the countermeasure.

## 6.2 Rooms

#### CURRENT VORTEX
`RoomGrain` (613-line core) + ~28 partials; Modules (Action/Furni/Map/Object/Security/Event/…) and Systems (Wired, Roller, Pathing, Pet, Bot, Chat, Banzai, Freeze, Trading, Moderation, GameTimer, minigames) composed at activation, all within the turn [V-RUN01]. `RoomLiveState` + `RoomItemIndex`; bounded flushes (`MaxDirtyItemsPerFlush=100`, `MaxTileHeightsPerFlush=200`) [V-WIR02].

#### WHAT VORTEX DOES WELL
The Modules/Systems separation V1 aimed at is in place; the `RoomGrain.Wired.*` partials are already 39–405 line façades; doorbell, moderation, trades and mystery boxes are out of the core.

#### PROBLEMS
Localized accretion in `Furni.Interactive`/`Furni.Edit`/`Settings` (detailed orchestrations) — no central type switch (dispatch goes through the registry), so the work is progressive delegation, not a rewrite.

#### SKYLIGHT3 LESSONS
The `Items/{Builders,Interactions}` tree readable in one pass [S-CAT01 context]: a naming constraint for the target, not an implementation to copy.

#### ARCEUS LESSONS
Room-scoped components registered on the room [AP-WIR02]: Vortex does the equivalent by composition at activation.

#### DAYBREAK LESSONS
Nothing usable (Room god object derived from Arcturus).

#### PATTERNS TO REJECT
Re-merging the Systems into the grain; exposing the whole `RoomGrain` to the behaviours; aiming at a file size rather than at the absence of behaviour details.

#### TARGET ARCHITECTURE
`RoomGrain` = activation/hydration, `RoomLiveState` ownership, facets, tick order, persistence boundary; the Furni.\* orchestrations move to the Furniture Engine (§6.3), one-line façades kept.

#### MIGRATION STRATEGY
One capability at a time (use/click, edit, …), under golden tests; never "all of Furni.\* at once".

#### TESTS REQUIRED
Golden place/move/rotate/pickup/use + derived map/height/pathing (inventory of the existing ones in P0-C); architecture test "no RoomGrain partial references a concrete logic class".

#### RISKS
Medium: the mutation → map/index → event → dirty order is easy to break when moving code — named order tests before any move (style canon: the settlement order tests [V-ECO04]).

## 6.3 Furniture / Room Items

#### CURRENT VORTEX
Object type + flat capabilities (`FurnitureDefinitionSnapshot.LogicName`, `CanSit/CanLay/...`); `FurnitureLogic` (lifecycle, centralized `PersistStuffDataAsync`/`MarkDirty`, historical invariant commented) [V-FUR07]; `RoomObjectLogicProvider` registry (key (name, family), logged `default_floor/wall` fallback — "the warning is the to-do list", comment verified in A9) [V-FUR01]; `[RoomObjectLogic]` scan + `ActivatorUtilities` factories [V-FUR02]; named creation chain `RoomItemsProvider` → `RoomObjectModule` → `RoomFurniModule` (A8) [V-FUR04][V-FUR05][V-FUR06]; `check-logic-groups` hook deriving the Dashboard menu from the attributes [V-FUR03]. 29 specialized floor logics + 2 wall + 171 Wired boxes.

#### WHAT VORTEX DOES WELL
Adding a behaviour = a class + an attribute + tests, zero core modification; plugins through the same mechanism; a bounded context rather than RoomGrain; the logic-groups hook is already a guardrail against generated drift.

#### PROBLEMS
(a) `RegisterLogic` overwrites destructively and the dispose does not restore the overwritten registration (A9): a plugin/core collision followed by an unload leaves the core furni on the fallback — the V2 "plugin exemption" is precisely the bug. (b) The fallback is counted nowhere (the to-do list lives in the logs). (c) `ActivatorUtilities` cost per creation: measure first (creation/hydration, not the tick) — OQ-7.

#### SKYLIGHT3 LESSONS
Builders separated from interactions: satisfied by the existing A8 chain — hence the REJECT of the V2 `RoomItemFactory`.

#### ARCEUS LESSONS
Reflective Map+Constructor `RoomItemFactory` [A-FUR01]: validates the Vortex choice (attribute+factory), nothing more.

#### DAYBREAK LESSONS
A giant manual `ItemManager` [D-FUR01]: the exact anti-model of the attribute scan.

#### PATTERNS TO REJECT
An enum parallel to the DB logic names (REJECT confirmed); a second registry; rewriting the `FurnitureLogic` hierarchy; a decorative factory on top of the A8 chain.

#### TARGET ARCHITECTURE
Registry hardened per the verified V3 matrix: core/core collision = FAIL at boot; plugin/core = FAIL by default; plugin/plugin = FAIL unless explicitly ordered as an override — the override only exists as a **registration-stack** feature (priority/origin, restoring the previous one on dispose), never as an overwrite [V-FUR01]. Metric `furniture.logic.fallback{logic_name,family}`. Creation chain unchanged, documented.

#### MIGRATION STRATEGY
Hardening = additions with no movement (PR-F1); the registration stack is only implemented if a real plugin needs it (otherwise FAIL is enough).

#### TESTS REQUIRED
Resolution by family (extend `RoomObjectLogicFamilyTests`); core/core, plugin/core, plugin/plugin collisions; hot unload: the core registration becomes active again (a test that fails today — that is the point); fallback counted; StuffData round-trip per type.

#### RISKS
Low; take care not to break legitimate hot reload (re-registering the same plugin after a reload = same key, different reg — the unload test covers it).

## 6.4 Wired

#### CURRENT VORTEX
`RoomWiredSystem` (1,236 lines + Variables partial 224 lines) + `Vortex.Rooms/Wired/*` runtime + 171 boxes by category. The pipeline was fully verified in V2 and re-confirmed: bounded queue of 512 with drops counted per tick; trigger index by type + self-healing `_indexDirty`; **live** `BuildStackFromTileAsync` resolution ordered by object id; `MatchesEvent` → context (Event/Stack/Trigger/Signal) → base selection → selectors **unioned** → add-ons `MutatePolicyAsync` → conditions `PrepareAsync` → `CanTrigger` **before** condition evaluation (distinguishing not-fired from conditions-false for the negative branch) → **sliding** allowance window → 7 condition modes not short-circuited → branches → FirstOnly/Random(history per stack)/Unseen(cycle) → `PriorityQueue` by deadline with versioned keys → per-action delays + **co-location revalidation** `IsOnTile` → Before/AfterEffects hooks → grouped flush; execute-stacks: deliberate bypass of the target's triggers/conditions, inherited selection, cycle guard by tiles + `MaxCallChainDepth = 8` (const) [V-WIR01]. Boxes → `IWiredContext`/`IWiredExecutionContext` (capabilities verified in A11) [V-WIR05]. RFW-101: `RoomConfig.WiredMaxDepth = 20` never read [V-WIR02].

#### WHAT VORTEX DOES WELL
Functionally above the three references combined; the invariants are commented in the code (the engine is its own spec on several points); the live-vs-cache choice is correct under a single turn — Daybreak, being multithreaded, *needed* its invalidatable stack index [D-WIR04], Vortex does not.

#### PROBLEMS
(a) RFW-101 (P0-B). (b) The full pipeline is only testable with a nearly complete `RoomGrain`: `RoomWiredSystem(RoomGrain)` reads `_state`, `_roomConfig`, `MapModule`, `NowMs()` directly — the leaf bricks are heavily tested (33+ files), the orchestrator is not. (c) Observability: room logs + error counters by type, but no exported counters, no normalized `StopReason`, no `ExecutionId` correlating the chains. (d) Growth (new Wired 2.0 primitives [H-COM-01][H-OFF-01]) justifies the extraction — not the current state, which is readable.

#### SKYLIGHT3 LESSONS
Nothing beyond the Triggers/Effects classification, already in place.

#### ARCEUS LESSONS
**Named** phases of the plugin pipeline (variables→selectors→conditions→effects→addons) [AP-WIR04]: taken as component and trace-step boundaries — not the code (`ConcurrentHashMap`/`Future` rejected).

#### DAYBREAK LESSONS
`WiredServices` = a capability catalog [D-WIR01]: supports the `IWiredRoomActions` shape; budgets [D-WIR03] already present; the parallel mode [D-WIR02] remains the shadow anti-model (double mutation).

#### PATTERNS TO REJECT
A cache of resolved stacks; execution outside the turn; a global scan of the boxes per event; a `DbContext` inside a box; exposing the `RoomLiveState` collections across the boundary (V3 guardrail kept); a parallel rewrite of the engine.

#### TARGET ARCHITECTURE
Extraction at the seams the code already draws, on top of the capability host:

```
Vortex.Rooms/Wired/Engine/
├── WiredEngine              (OnEvent/ProcessWired/Fire orchestrator)      order 4
├── WiredTriggerIndex        (_triggersByEventType, _timedTriggers, repair) order 1
├── WiredStackResolver       (BuildStackFromTileAsync, IsOnTile)            order 1
├── WiredExecutionScheduler  (pending map, priority queue, versions)        order 2
├── WiredExecutionPolicy     (allowance windows, random, unseen, choice)    order 2
├── WiredSelectionEngine     (base, signal, selectors)                      order 3
└── WiredCallChainGuard      (tiles, CONFIGURED depth, cycles)              order 3

IWiredRoomHost (internal)
 ├─ IWiredRoomView        TryGetItem / EnumerateTileFloorStack / TryGetAvatar / ToIdx / NowMs
 ├─ IWiredRoomActions     move item/user, states, bots, room composer       (≈ IWiredExecutionContext on the engine side)
 └─ IWiredDiagnostics     WriteWiredLog / counters / opt-in trace
```

`RoomGrain` implements the host; the tests fake it (the `FakeFurniAccess` pattern already exists). No `Dictionary`/`HashSet`/mutable array crosses it: `EnumerateTileFloorStack` returns a sequence materialized within the turn, `TryGetItem` an object, never the map. `WiredPendingStackExecution` stays the runtime state (A10); the immutable `WiredExecutionPlanSnapshot` projection (StackId, TriggerId, branch, ordered actions, policy, selections/signal, deadlines) exists only for test/trace, is never persisted and is never a DSL.

#### MIGRATION STRATEGY
The table's order: (0) RFW-101 + minimal counters (P0-B, independent); (1) host + consumption through the interface (mechanical diff); (2) TriggerIndex + StackResolver; (3) Scheduler + Policy (the unseen/windows/random/pending runtime state moves with them); (4) SelectionEngine + CallChainGuard; (5) WiredEngine assembler. Each step: compiles, parity green, zero packet change; observability (§6.15) laid down at the seams along the way.

#### TESTS REQUIRED
The V2 parity matrix in full, on a fake host + fake clock + injected RNG: same-tile/different-tile; physical order with no add-on = no observable effect; add-on OR over its scope; negative branch never combined with the positive one; execute-stacks target bypass + cycle/depth guard **with the depth coming from the config**; signals (inherited sources); sliding window (a documented Vortex choice, Habbo `UNKNOWN` — OQ-6); unseen cycle; random anti-repetition; clock-based delay (a late tick ≠ altered duration ≠ double execution); full queue (drop counted, FIFO preserved); rebuild after add/remove/move/reconfigure and after a phantom trigger; intra-tick zero-delay re-drain pinned.

#### RISKS
High but contained (gameplay core): `CanTrigger`-before-conditions, intra-tick re-drain, index self-repair, choice anti-recomputation — each has its named test before any move.

## 6.5 Player / Presence / Sessions

#### CURRENT VORTEX
Ownership by dedicated grains (Player, Presence + partials, Wallet, Inventory, Navigator, Effect, Clothing, directories); `PlayerPresenceGrain.Room.cs` = 225 lines; the player's outbound path is contracted through `SendComposerAsync` (A7) [V-PRES01][V-PRES02].

#### WHAT VORTEX DOES WELL
The split is already better for Orleans than the references' single-process compositions; the contract's only interleave is documented, bounded and necessary (the Presence→Room→Presence deadlock).

#### PROBLEMS
None structural. `SendComposerAsync` must move from a comment to a tested manifest (§12.3) — that is the domain's single action.

#### SKYLIGHT3 / ARCEUS / DAYBREAK LESSONS
Composed `User`/`IHabbo`/`HabboInventory`: all confirm "explicit composition", achieved in Vortex by grains — do not merge to imitate.

#### PATTERNS TO REJECT
Merging the player grains; wrapper modules with no state of their own; a split "to be like Daybreak".

#### TARGET ARCHITECTURE
Status quo + a written trigger criterion: a partial only becomes an internal module if it exceeds ~500 lines **and** has state of its own **and** is only testable through the whole grain.

#### MIGRATION STRATEGY / TESTS REQUIRED / RISKS
No migration. Tests: room-transition coverage kept; a category B manifest entry for both `SendComposerAsync` overloads. Zero risk as long as DEFER holds.

## 6.6 Inventory

#### CURRENT VORTEX
`InventoryGrain` is the owner; `GrantCatalogOfferAsync` (A1): multi-family parsing (152–296) → `AddRange` furniture + badge loop with an `alreadyOwned` guard → **commit 333** → cache/client/events sync (342–379) → pets (395, own commit Pets.cs:53 [V-INV01]) → bots (409, own commit Bots.cs:77 [V-INV02]) → cross-grain effects (435, `AddEffectAsync` [V-EFF01]) — the comment at l.428 arms the full refund on any late exception [V-CAT02]. The unit grant `GrantFurnitureDefinitionAsync` = Add + commit per unit [V-INV03].

#### WHAT VORTEX DOES WELL
Business invariants encoded and commented (guild extra data on `StringKey` only, quantity × `product.Quantity`, StuffData rebuilt from the blob at grant time — the "blank legacy default" bug is documented in the code); a single owner = the consolidation is purely local.

#### PROBLEMS
The monolithic dispatch (a V2 finding, unchanged) **and** — more seriously — the commit topology: four separate local commits for rows written through the same `_dbCtxFactory` in the same grain, under the refund invariant of an atomic grant that it no longer is (A1). Each new `ProductType` lengthens the method and adds a window.

#### SKYLIGHT3 LESSONS
The contributive model `ICatalogProduct.ClaimAsync(ICatalogTransactionContext)` → `context.Commands.AddBadge` [S-CAT01][S-CAT02]: the shape of the strategies — each product **contributes** typed instructions to a plan, the coordinator applies them. Its global EF transaction [S-CAT03][S-CAT04] remains untransplantable between grains: it is the commerce protocol that replaces it, not a simulated transaction.

#### ARCEUS LESSONS
`ICatalogPurchaseHandler`: a separate purchase contract, nothing more.

#### DAYBREAK LESSONS
Inventory decomposed by family: supports the split by `ProductType`.

#### PATTERNS TO REJECT
A generic workflow framework; a cross-grain EF transaction; moving ownership out of `InventoryGrain`; strategies carrying a `DbContext`/delegates (the plan is **data**).

#### TARGET ARCHITECTURE
Two ordered moves. **(1) Local grant consolidation (B1, P0-A)**: furniture + badges + pet rows + bot rows join **a single commit** in `InventoryGrain`'s turn (same factory, same grain — the separate commits are an artifact); what remains post-commit is the genuinely cross-grain steps: effects, presence notifications, events, gift wrapping. The biggest "goods+refund" window disappears by design before any journal. **(2) Structure (P1, after the protocol)**: `Vortex.Inventory/Fulfillment/` — an `ICatalogProductGrantStrategy` per `ProductType` (registered at startup) contributes to a `FulfillmentPlan` (typed lists: furni entities, badges, pet/bot requests, effect grants); `CatalogFulfillmentPlanner` = a **pure and deterministic** preflight run before the debit (no deterministic error must survive the pivot); the plan is serializable if the operation journal needs it; `InventoryGrain` applies the local batch in one commit, the coordinator drives the residue.

#### MIGRATION STRATEGY
PR-C1 (fault-injection characterization) → PR-C4a (single-commit consolidation, client behaviour unchanged) → PR-C4b (journal/receipts on the residue) → PR-S1/S2 (strategies + plan, Badge first, then Effect, Floor/Wall+guild, Bot/Pet) → removal of the branches.

#### TESTS REQUIRED
Fault injection per committed step (the binary A4 harness is no longer enough): "furni+badge committed, pet fails" ⇒ never "goods+refund" after consolidation; parsing/quantities/guild stamping per strategy in pure form; expected plan per offer (bundles, multi-product); end-to-end parity on 3 representative offers before/after; `OperationId` replay = no-op/earlier result.

#### RISKS
High (monetary correctness): quantity×product, guild stamping, StuffData-from-blob are traps to pin with tests **before** the consolidation; the consolidation changes the size of the local transaction (measure on large bundles).

## 6.7 Catalog / Fulfillment (standard purchases, gifts, targeted offers)

#### CURRENT VORTEX
`CatalogPurchaseGrain` orchestrates: preparation → `ExecutePurchaseAsync` (86) → inventory grant → `PublishAsync(CatalogPurchasedEvent)` (122) **after** success, outside the transaction (A12) [V-CAT01]. Gift: the purchase is wrapped then packaged as a present for the recipient (plain fallback), cross-catalog offer lookup commented [V-CAT05]. Targeted: a loop of **unit** grants in the compensated scope, `IncrementPurchaseCountAsync` after success (A5), analytics event commented "Non-transactional" [V-CAT06]. Immutable catalog snapshots via `Volatile.Write` [V-REF02].

#### WHAT VORTEX DOES WELL
A clean purchase/fulfillment boundary; exemplary atomic snapshots; the comment "Couldn't afford it: the wallet auto-refunded any partial debit" shows that **pre-pivot** multi-currency compensation is already thought through — it is the post-pivot that is not.

#### PROBLEMS
(a) Targeted: N unit commits [V-INV03] inside a scope that refunds everything — k-1 free products possible; the counter is incremented outside the operation — limit drift on a crash (A5, B7). (b) Gift: the wrapping is a post-debit step that is not journalled — a crash between purchase and wrapping = a debit with no present. (c) `CatalogPurchasedEvent` published outside the transaction feeds quests **and** daily tasks [V-PROG01][V-PROG02]: loss possible after the commit (§6.13). (d) The request's `ct` runs through the whole grant (B6).

#### SKYLIGHT3 LESSONS
See §6.6 — contributive yes, global transaction no.

#### ARCEUS LESSONS / DAYBREAK LESSONS
Nothing usable.

#### PATTERNS TO REJECT
Re-coupling purchase and grant; mutating the catalog in place; non-critical UI notifications/events inside the zone that decides refund/commit (V3 rule kept).

#### TARGET ARCHITECTURE
The catalog coordinator becomes a commerce operation (§6.8): full preflight (offer, plan, definitions, parsing) **before** the debit; recommended pivot = the debit validated after the preflight (OQ-2 settles the variant); single-commit local batch; residue (effects, gift wrapping, notifications, critical events) = journalled idempotent steps under a shutdown token; targeted counter = a journalled step (`operationId+step:count`) or merged into the pivot commit (B7); the critical `CatalogPurchasedEvent` relayed from the journal's terminal transition (B2).

#### MIGRATION STRATEGY
P0-A order: characterization (PR-C1) → identity/journal (PR-C2) → wallet receipts (PR-C3) → Catalog/Gift/Targeted recovery (PR-C4) — detail in §9.

#### TESTS REQUIRED
Simulated crash/restart at every phase (preflight, debit, local batch, each residual step, completion); gift: a crash between purchase and wrapping ⇒ resumption delivers the present, never two; targeted: the k-th failure ⇒ zero free products, a consistent count after recovery; duplicate `OperationId` retry = no-op.

#### RISKS
High: this is the P0. The preflight must be genuinely deterministic (any validation depending on concurrent state — balance, stock — belongs to the pivot, not to the preflight).

## 6.8 Economy / Wallet / Marketplace / Trading — the Commerce Consistency P0

#### CURRENT VORTEX
The common primitive `ExecutePurchaseAsync`: durable debit (EF transaction + execution strategy, fresh DbContext per attempt — anti half-state comment verified in A2 [V-ECO02]) → grant → `CreditBackAsync` compensation on `CancellationToken.None` if the grant throws, `LogCritical` if the refund fails [V-ECO01]. A wallet contract **with no operation identity**: `TryDebitAsync(List<WalletDebitRequest>)`, `CreditBackAsync(List<...>)`, `GrantCreditsAsync(int)` (A3) [V-ECO03]. Marketplace: four flows, four windows (A6) — MakeOffer removes the item **then** inserts the offer; Cancel commits `Cancelled` **then** returns the item; Buy atomically claims `ExecuteUpdate` Active→Sold+CreditsOwed **then** grants, with a best-effort revert commented "Sold with no item delivered"; Redeem commits `CreditsOwed=0` **then** credits [V-MKT01]. `WiredTradeSettlement`: documented pivot, a reasoned refusal of the common primitive, named order tests (`GoodsThatCannotBeSaved_GiveThePaymentBack`, `ARewardTheWalletRefuses_IsNotLoggedAsPaid`) [V-ECO04].

#### WHAT VORTEX DOES WELL
Three achievements to credit before correcting: the debit itself is exemplary (retry-safe by construction); the Buy's conditional `ExecuteUpdate` claim is a **correct concurrency pivot** that already prevents double selling (B4); the Wired settlement proves the repository knows how to design a pivot, order it and test it — it is the workstream's style canon, not a case to "harmonize".

#### PROBLEMS
Table of the windows, all VERIFIED_CODE (§2.2):

| Flow | Window | Possible final state |
|---|---|---|
| Catalog/Gift | furni+badges commit (333) → pets/bots/effects fail → full refund | goods kept + purchase refunded (A1) |
| Targeted | k unit grants committed → k+1 fails → refund | k free products; grant→count crash = limit drift (A5) |
| Marketplace MakeOffer | RemoveFurniture → offer insert | item removed, offer absent |
| Marketplace Cancel | `Cancelled` committed → grant | item not returned |
| Marketplace Buy | debit → Sold claim → grant | buyer debited / offer Sold / item absent; the best-effort revert can itself fail |
| Marketplace Redeem | `CreditsOwed=0` committed → `GrantCreditsAsync` | seller's credits lost |
| Wallet | refund/credit replayed with no OperationId | double credit (A3); an `AddEffectAsync` retry duplicates (A15) |
| Cross-cutting | crash between the durable debit and the grant | permanent debit, no trace to resume from |

Common cause: every local commit is treated as if it were global; the client's cancellation (`ct`) runs through post-commit steps (B6); Orleans provides neither durable dedup nor exactly-once — a retry potentially delivers twice [M-ORL-03].

#### SKYLIGHT3 LESSONS
The spanning EF transaction [S-CAT03] is the single-process luxury the V4 protocol replaces with pivot + idempotence — cite it in the protocol's documentation as "why not a transaction".

#### ARCEUS LESSONS / DAYBREAK LESSONS
Nothing usable (classic DAO/SQL, the same untreated windows).

#### PATTERNS TO REJECT
A generic Economy Kernel (two proven divergent topologies: compensable purchase vs pivoted settlement [V-ECO04]); Orleans Transactions for this problem (Orleans transactional state ≠ EF/MySQL + multi-grain side effects [M-ORL-04]); a simulated global ACID transaction; a refund after the pivot; a grain per operation by default; retries with no receipts.

#### TARGET ARCHITECTURE
A minimal protocol, shared primitives, orchestration per domain:

```
CommerceOperationId (ULID)            CommerceOperationState
operations: catalog_purchase,           Prepared → Debited → Pivoted
gift, targeted, marketplace_list/       → Completing → Completed
cancel/buy/redeem                       | FailedBeforePivot | NeedsIntervention
```

Each flow declares: its pivot; compensable steps before it; retryable+idempotent steps after it (receipt `(scope, owner, operationId, stepKey)` inserted **in the same transaction** as the mutation when it is local — the debit already opens that transaction, B3); a recovery owner (restart from the journal, independent of the connection); critical events (relayed from terminal transitions, B2). Surface reduction first (B1): everything that can join the pivot's commit does. Per-flow application: Catalog/Gift/Targeted §6.7; **MakeOffer** — insert the `PendingRemoval` offer, then remove the item, then activate (or journal the removal): no more evaporated item; **Cancel** — `Cancelled` = a journalled pivot, restitution = a replayable idempotent step; **Buy** — the `ExecuteUpdate` claim = the pivot (kept as is), grant + notification = a replayable idempotent completion, the revert only exists **before** the pivot (B4); **Redeem** — zeroing and crediting tied by a receipt (`operationId+step:credit`), replayable. Wallet: additive overloads carrying `OperationId/StepKey` returning the earlier result on replay, `WalletDebitResult` serializable; legacy signatures kept during the migration then bare `CreditBackAsync` removed (B3). After the retries are exhausted: `NeedsIntervention` + a correlated alert — never an invented compensation.

#### MIGRATION STRATEGY
PR-C1 characterization (the table's windows become red tests) → PR-C2 identity/journal/states + observability → PR-C3 wallet receipts → PR-C4 Catalog/Gift/Targeted (B1 consolidation included) → PR-C5 Marketplace (four flows) → PR-C6 relay of the critical events. Each PR leaves client behaviour unchanged outside the corrected windows.

#### TESTS REQUIRED
Simulated crash at **every** boundary in the table + recovery verified on the final business state (not "refund was called"); replays by `OperationId/StepKey` on debit/refund/credit/grant/effect = a single application, a stable result; `AGrantThatFails_RefundsTheBuyer` kept for the pre-pivot only; the settlement order tests untouched (canon).

#### RISKS
Very high on the business side (value), medium on the code side: the protocol is additive and per flow. Main risk: over-generification — the guardrail is the REJECT of the generic kernel and the "third flow before abstraction" rule.

## 6.9 Social / Progression / Collectibles

#### CURRENT VORTEX
Separate projects (Social 23, Progression 37, Collectibles 8 files); `QuestProgressEventHandlers` and `DailyTaskProgressEventHandlers` consume `CatalogPurchasedEvent` (A13) [V-PROG01][V-PROG02].

#### WHAT VORTEX DOES WELL
The split is done; the progression consumers are identified and located.

#### PROBLEMS
The handlers do not deduplicate by event: when the at-least-once relay arrives (B2), a replayed event would increment a quest twice. The overall test coverage of those projects remains to be inventoried (an inherited OQ, not blocking).

#### SKYLIGHT3 / ARCEUS / DAYBREAK LESSONS
Nothing specifically usable.

#### PATTERNS TO REJECT
Re-merging into Players; an outbox generalized to gameplay.

#### TARGET ARCHITECTURE
Consumers of the relayed events idempotent by `OperationId/EventId` (a local guard in each handler concerned — quests, daily tasks, audit); the rest of the bus unchanged.

#### MIGRATION STRATEGY / TESTS REQUIRED / RISKS
Delivered with PR-C6; test: a replayed event = progression unchanged; low risk.

## 6.10 Protocol / Revisions / PacketHandlers

#### CURRENT VORTEX
`Vortex.Protocol` 1,091 files; `Vortex.Revisions` (default `Revision20260701` embedded, additional revisions as plugins); orchestration-only handlers, header-registry (14 baselined) and wire-conflicts (23 baselined) hooks [V-AI01][V-AI03].

#### WHAT VORTEX DOES WELL
A counted, hooked, baselined boundary — the most industrialized state of the four codebases.

#### PROBLEMS
Baselines = known debt with no burn-down owner (P2, entry by entry).

#### LESSONS (3 refs)
Nothing usable; the Daybreak architecture document remains functional secondary evidence for the specs.

#### PATTERNS TO REJECT
Business logic in the handlers; placeholder payloads (forbidden by the existing contract).

#### TARGET / MIGRATION / TESTS / RISKS
Status quo + a burn-down plan (PR-Q2); the hooks are already in FastCheck/QualityGate; low risk.

## 6.11 Habbo Specs / behavioural conformance

#### CURRENT VORTEX
Engine + CLI (`analyze`, `conflicts`, `unknowns --severity`, reproducible `bootstrap`, `validate` in FastCheck); 741 classified unknowns; `verified:/manual:` blocks surviving regeneration; `SpecWorkspace` detects external trees but merges any Arcturus-like layout under the `arcturus` origin [V-SPEC01].

#### WHAT VORTEX DOES WELL
The §2.3 evidence hierarchy is executable, not declarative; specs are diffable in a PR.

#### PROBLEMS
Provenance is not repository-aware: Daybreak (derived from Arcturus) and Arcturus = a single origin — two pieces of evidence merged.

#### LESSONS (3 refs)
The repositories are **inputs** to the engine, not models for it.

#### PATTERNS TO REJECT
Majority voting; promoting community sources to authority.

#### TARGET ARCHITECTURE
`reference:<repo>@<sha>` origins (detection: git remote, signature files); V3 schema kept: `client:flash:<rev>`, `client:nitro:<rev>`, `capture:official:<id>`, `reference:*@<sha>`, `vortex@<sha>` → claim matrix → conflicts/unknowns/confidence.

#### MIGRATION / TESTS / RISKS
An isolated `SpecWorkspace` workstream + re-scan (PR-Q1); Daybreak≠Arcturus fixtures; `bootstrap` non-regression; take care to migrate the existing origins without invalidating the 741 unknowns.

## 6.12 Reference Data / caching

#### CURRENT VORTEX
`FurnitureDefinitionProvider` publishes ById then ByName in two non-atomic assignments (ByName deduplication commented) [V-REF01]; `CatalogSnapshotProvider` = the correct pattern (full build → `Volatile.Write`) [V-REF02].

#### WHAT VORTEX DOES WELL
The target pattern is already the local norm; one provider is missing it.

#### PROBLEMS
A mixed-version window during an admin reload — low probability, near-zero fix cost.

#### LESSONS / PATTERNS TO REJECT
Convergence with the Skylight snapshot; reject: multiple indexes published separately, read locks.

#### TARGET / MIGRATION / TESTS / RISKS
`FurnitureDefinitionSet { ById, ByName, Version }` published in one write, version exported as a metric; mechanical diff (PR-Q1); test "a reader only sees N or N+1"; minimal risk.

## 6.13 Persistence / Events

#### CURRENT VORTEX
EF Core 9 + Pomelo pinned; memory-first rooms: centralized dirty tracking [V-FUR07], bounded end-of-tick flushes + best-effort deactivation [V-RUN01]; `Vortex.Events` in-process, `EventRegistry` `HandlerMode = Parallel` + error isolation (A12) [V-EVT01][V-EVT02].

#### WHAT VORTEX DOES WELL
Three effective levels of state (static/ephemeral/persistent); bounded flush against bursts; the gameplay bus is simple and fast — exactly what it should be.

#### PROBLEMS
Two needs conflated by default: tolerated loss (rooms — the window since the last flush, `OnDeactivate` not guaranteed on a crash) and intolerable loss (post-pivot business consequences). A crash between the business commit and `PublishAsync` loses the event — proven by the code order (A12); that is the state+event dual-write problem the outbox solves [M-AZ-03][M-AZ-04].

#### LESSONS (3 refs)
Nothing usable (Skylight: a local transaction; Arceus/Daybreak: DAO).

#### PATTERNS TO REJECT
Generic repository/UoW; generalized event durability; SaveChanges from behaviours; a second outbox pipeline independent of the journal (B2).

#### TARGET ARCHITECTURE
Rooms: status quo + documentation of the loss window per domain (items, pets, permanent Wired variables) and of idempotence on reload. Commerce: a durable operation journal (a simple table, queryable for ops/recovery — not a grain per operation) whose terminal transitions feed the at-least-once relay of critical events; the consumers concerned made idempotent (§6.9). `EventSystem` unchanged for everything else.

#### MIGRATION / TESTS / RISKS
PR-C2 (journal) + PR-C6 (relay); tests: a commit→publish crash ⇒ the critical event still goes out (from the journal), a replayed event deduplicated; Wired persistence round-trip kept; low-to-medium risk.

## 6.14 Plugins / extensibility

#### CURRENT VORTEX
Lifecycle/rollback/hot reload; real extension points: logics by attribute (same processor, plugin ServiceProvider composed with the host [V-FUR01][V-FUR02]), protocol revisions by plugin [V-AI01].

#### WHAT VORTEX DOES WELL
Extensible without turning Wired into a plugin (strong contracts + hot path + turn guarantees — a choice that stands).

#### PROBLEMS
Key collision is the only hole (A9): destructive overwrite + dispose with no restoration ⇒ unloading a colliding plugin leaves the core on the fallback.

#### ARCEUS LESSONS
The Wired plugin proves packets + interactions + a room-scoped component per plugin — Vortex offers the equivalent, better guarded; an example for the plugin documentation.

#### DAYBREAK LESSONS
A static/global `PluginManager` [D-PLUG01]: a counter-example.

#### PATTERNS TO REJECT
Global statics; a collision exemption (the bug, not the feature); Wired as a plugin.

#### TARGET / MIGRATION / TESTS / RISKS
Collision policy from §6.3 (FAIL everywhere, override = a registration stack if a real need emerges); PR-F1; hot-unload-restores test; low risk.

## 6.15 Observability

#### CURRENT VORTEX
Opt-in per-step tick metrics ("one boolean and nothing else"), a per-room Wired log channel + error counters by type [V-WIR01]; `Vortex.Observability`; process-local aggregators (known multi-silo debt [V-MULTI01]).

#### WHAT VORTEX DOES WELL
The tick instrumentation is in the right place at the right cost; the per-room Wired log is already the in-game debug tool.

#### PROBLEMS
Wired: no exported counters, no normalized `StopReason`, no `ExecutionId`. Commerce: **no** operation signal at all — a saga with no observability is "possibly inconsistent for a long time" (V3's phrasing, kept).

#### DAYBREAK LESSONS
The `WiredServices` debug channel validates the room-scoped log; nothing to copy.

#### PATTERNS TO REJECT
An always-on trace; massive text logs instead of counters.

#### TARGET ARCHITECTURE
Wired: `ExecutionId/ParentExecutionId`, a normalized `StopReason` (no-match, condition-false, execution-limit, cycle, depth, queue-drop, stale-stack, target-missing, exception), received/ignored/dropped/processed counters + rebuilds + delayed-revalidation-cancelled, opt-in p95/p99 of the step per room, opt-in room-scoped trace. Commerce: per operation — OperationId, flow, phase, pivot timestamp, current step, attempts, last error, age, recovery state; alerts: stuck post-pivot, receipt conflict, refund failure, relay backlog/age/dead-letter, `NeedsIntervention`. The `FurnitureDefinitionSet` version exported.

#### MIGRATION / TESTS / RISKS
Wired: laid down at the seams during the extraction; Commerce: in PR-C2 (the journal IS the source of the signals). Tests: every StopReason reached by a scenario; every operation state observable. Low risk.

## 6.16 Testing

#### CURRENT VORTEX
~120 Rooms.Tests files (33+ Wired, fakes, settlement order tests, round trips), per-domain projects; FastCheck/QualityGate gates + hook self-tests [V-AI03]. A binary catalog harness (A4) — the central hole.

#### WHAT VORTEX DOES WELL
The culture of "named order tests that pin a semantics" exists; the Wired fakes show the way to the host.

#### PROBLEMS
(a) No test class can say "step k committed, step k+1 throws" — the §6.8 windows are invisible to the harness. (b) The Wired pipeline is not testable without a grain. (c) No versioned baseline benchmark.

#### LESSONS (3 refs)
Nothing usable (the three references are below).

#### PATTERNS TO REJECT
`Thread.Sleep`/global RNG; instantiating a RoomGrain to test a box; "refund was called" as proof that a multi-product grant is safe.

#### TARGET ARCHITECTURE
Four tiers: (1) **commerce fault injection** — a stepped harness that marks each committed step and throws at the next, with simulated crash/restart at every boundary, assertions on the final business state + recovery + replays; (2) bricks (existing); (3) the Wired pipeline on a fake host + fake clock + injected RNG = the full parity matrix, each rule tagged `VERIFIED_CODE | VERIFIED_CAPTURE | VERIFIED_PUBLIC_DOC | BEST_EFFORT`; (4) architecture tests (interleaving manifest §12.3, non-reentrancy, no-lock/no-Task.Run, wired knobs, partials with no concrete logics, single-silo singletons, STATE.yaml schema). Versioned benchmarks: empty/loaded room, event storm, no-trigger ≈ O(1), hydrating thousands of items (OQ-7).

#### MIGRATION / RISKS
PR-C1 and PR-G1 first (the tests precede the fixes and the extractions). Risk: golden tests that freeze a bug — countermeasure: evidence tagging + `Specs analyze` before pinning a protocol rule.

## 6.17 AI development workflow

#### CURRENT VORTEX
Canonical `AGENTS.md` (stack, path-triggered skills, Orleans rules, checklists); `CLAUDE.md` (context order, automations); hooks + self-tests; `grain-rules-reviewer`, `wire-truth-auditor` agents; MSBuild gates; Specs CLI; `docs/patterns/`; dated audit [V-AI01..V-AI04].

#### WHAT VORTEX DOES WELL
The enforcement mechanisms exist and are **executable** — the workflow was never the weak link; what it lacks is memory.

#### PROBLEMS
(a) No cross-session state (STATE.yaml); (b) no ADRs; (c) no slice contract; (d) nothing says which audits stay valid when HEAD moves; (e) no "economy" reviewer for the commerce paths.

#### LESSONS (3 refs)
Not applicable.

#### PATTERNS TO REJECT
A parallel system duplicating hooks/gates; audits "eternally valid" or "systematically to redo"; a single long session.

#### TARGET ARCHITECTURE
§12: existing infrastructure + STATE.yaml with `watched_paths`/SHA invalidation (V3 contribution kept), ADRs, a slice contract with a commerce extension, the interleaving manifest, an economy reviewer.

#### MIGRATION / TESTS / RISKS
PR-G1 creates `docs/architecture-v4/` + ADR-000; QualityGate validates the STATE schema; risk: workflow theatre — countermeasure: any artifact not wired to a gate is deleted.

---

# 7. Consolidated target architecture

## 7.1 Overall view

```
Network / PacketHandlers                    (orchestration-only, hooked)
          │ Orleans call
   ┌──────┴────────────┬──────────────────────────┐
RoomGrain          Player grains             InventoryGrain
[owner, turn]      (Player/Presence/          │
   │                Wallet/Effect/…)     Fulfillment (planner+strategies+plan)
   ├ Modules            │                      │
   ├ Systems       wallet receipts        single-commit local batch
   │  └ WiredEngine ─► IWiredRoomHost          │
   ├ Furniture Engine (hardened registry, Commerce coordinators
   │  Provider→ObjectModule→FurniModule   Catalog / Gift / Targeted /
   │  chain, logics)                      Marketplace  [explicit pivot]
   └ RoomLiveState + persistence               │
                                     CommerceOperation Journal
                                     (states, receipts, recovery)
                                          │ terminal transitions
                                     at-least-once relay ─► consumers
                                     (quests, daily tasks) idempotent
Reference Data: immutable sets Volatile.Write   ·   Vortex.Specs: oracle (repo@sha)
```

The Room runtime stays local/actor and highly consistent; the cross-grain journeys that move value become explicitly recoverable operations. Everything stays in the modular monolith — the commit boundaries are simply treated as failure boundaries.

## 7.2 Structural contracts

**`IWiredRoomHost`** (internal to `Vortex.Rooms`) = `IWiredRoomView` (TryGetItem, EnumerateTileFloorStack, TryGetAvatar, ToIdx, NowMs, wired `IWiredLimits`) + `IWiredRoomActions` (item/user movement, states, bots, room composer — the engine-side equivalent of `IWiredExecutionContext` [V-WIR05]) + `IWiredDiagnostics` (log, counters, trace). Every addition to the contract = a justification in the PR; no mutable collection crosses.

**Commerce primitives** (`Vortex.Primitives/Commerce/`): `CommerceOperationId` (ULID), `CommerceOperationState`, `CommerceOperationDescriptor` (flow, pivot, declared steps), receipt `(scope, owner, operationId, stepKey)`; per-domain journal in `Vortex.Database` (a simple queryable table). The coordinators stay in their own projects (Catalog, Marketplace); **no** `Vortex.Economy` project.

**Interleaving manifest** (`docs/architecture-v4/interleaving-manifest.yaml`): every `[AlwaysInterleave]/[Reentrant]/MayInterleave` method with its A/B category, justification and owner — consumed by an architecture test (§12.3).

Dependency direction unchanged: only genuinely cross-project contracts move up into `Vortex.Primitives`; the Wired components stay internal to `Vortex.Rooms`.

---

# 8. Decision register: V1→V3 verdicts and new decisions

## 8.1 Consolidated verdicts

| Decision (origin) | V4 verdict | Rationale |
|---|---|---|
| RoomGrain sole owner (V1) | KEEP | Verified end to end; frozen by architecture tests. |
| No grain per item/box/avatar (V1) | KEEP | Would destroy live resolution and mutation ordering. |
| Furniture registry = the existing one, hardened (V2) | KEEP + HARDEN | Collision policy A9, fallback metric; no second registry. |
| Furniture enum key (V1) | REJECT (confirmed) | The DB/client LogicName is the canonical vocabulary. |
| RoomItemFactory (V2) | REJECT (confirmed in V3) | Provider→ObjectModule→FurniModule chain verified in A8. |
| Plugin exemption from strict mode (V2) | REJECT | Destructive overwrite with no restoration on dispose (A9); FAIL everywhere, override = a registration stack. |
| "Immutable return" interleave rule (V2) | REJECT (confirmed in V3) | `SendComposerAsync` is the legitimate counter-example (A7); replaced by **mechanized** A/B categories (B5). |
| Collection-based IWiredRoomAccess (V2) | MODIFY → `IWiredRoomHost` capabilities | No mutable collection crosses (V3 guardrail kept). |
| WiredPendingStackExecution = a plan (V2) | MODIFY (confirmed in V3) | Mutable runtime verified in A10; `WiredExecutionPlanSnapshot` projection for test/trace only. |
| Wired shadow (V1/V2) | CONTINGENCY (confirmed) | Extraction + parity are enough; Daybreak's parallel mode = double mutation [D-WIR02]. |
| TriggerIndex, live stack, scheduler, variables (V2) | KEEP | Verified; extraction with no semantic change. |
| WiredMaxDepth (V2 RFW-101) | FIX P0-B | 20 config vs 8 const; wiring + a "knob is read" test; value via OQ-1. |
| Fulfillment = mostly structure (V2) | MODIFY (confirmed in V3) | Consistency precedes structure; the plan becomes the operation's validated input. |
| Economy Kernel (V1→V3) | REJECT for now (confirmed) | Proven divergent topologies [V-ECO01][V-ECO04]; safety primitives only. |
| **Commerce safety primitives (V3)** | **KEEP P0 — reinforced** | All windows VERIFIED_CODE (§2.2) + V4 proofs (A15, V-PROG02). |
| **Post-pivot surface reduction (V4, B1)** | **NEW — prior rule** | A single-commit local batch before journal/receipts; removes the biggest window by design. |
| **Journal/outbox merge (V4, B2)** | **NEW** | Terminal transitions = the relay's source; one durable pipeline, not two [M-AZ-03][M-AZ-04]. |
| **Wallet path by overloads (V4, B3)** | **NEW** | Receipt inside the transaction `TryDebitAsync` already opens; additive evolution, legacy removed at the end. |
| **Buy claim = pivot kept (V4, B4)** | **NEW (framing)** | The `ExecuteUpdate` claim is correct; the fix is the durable completion, not the claim. |
| Selective outbox (V3) | KEEP (B2 shape) | Only critical post-pivot business consequences; gameplay in-memory. |
| Orleans Transactions for commerce (V3) | REJECT (confirmed) | Outside the problem's scope (EF/MySQL + multi-grain side effects) [M-ORL-04]. |
| PlayerPresence split (V1→V3) | DEFER (confirmed) | Small partials; thresholds written in §6.5. |
| Atomic FurnitureDefinitionSet (V2) | KEEP | A local fix, a pattern already normalized [V-REF01][V-REF02]. |
| Specs provenance repo@sha (V2/V3) | KEEP | Prevents the Daybreak/Arcturus merge [V-SPEC01]. |
| STATE/ADR/slices workflow + watched_paths (V2/V3) | KEEP + IMPROVE | Interleaving manifest §12.3 + economy reviewer + commerce extension of the slice contract. |
| Multi-silo (V1→V3) | DEFER (confirmed) | The `MultiSiloReady` thesis is not lifted [V-MULTI01]; a tested registry to freeze the debt. |
| Dashboard/API/Security | A separate workstream (confirmed) | Never mixed with this document's PRs. |

## 8.2 New V4 decisions (identifiers for the ADRs)

**D-V4-1** Post-pivot surface reduction before instrumentation (B1) — P0-A. **D-V4-2** The operation journal = the source of the critical event relay (B2) — P0-A/P1. **D-V4-3** Wallet receipts inside the existing debit transaction, evolution by overloads (B3) — P0-A. **D-V4-4** Marketplace Buy: the claim kept as the pivot, forward recovery after the claim (B4) — P0-A. **D-V4-5** Mechanized interleaving manifest, category B = "no await before completion" verified (B5) — P0-C. **D-V4-6** Post-pivot completion token tied to shutdown, never to the connection (B6, proof of the current violation: `ct` runs through the grant) — P0-A. **D-V4-7** Targeted counter = a journalled step or merged into the pivot (B7). **D-V4-8** Every post-pivot step proven idempotent by a replay test, never assumed (B10, counter-example A15). **D-V4-9** Registry collision policy: FAIL core/core, plugin/core, plugin/plugin; override = a registration-stack feature (A9). **D-V4-10** The 17×11 documentary grid restored as the canonical format for future revisions (B9).

---

# 9. Migration and PR order

| Step | PR | Contents | Risk |
|---|---|---|---|
| P0-C | **PR-G1** | Workflow files (STATE.yaml, ADR-000, slice contracts), interleaving manifest + test, no-lock/no-Task.Run/non-reentrancy guards, single-silo registry, versioned baseline benchmark, golden test inventory | Low |
| P0-B | **PR-G2** | RFW-101: wire `WiredMaxDepth` (value from OQ-1), test "every Wired\* knob has a reader", minimal Wired counters | Low |
| P0-A | **PR-C1** | Commerce characterization by fault injection: the 8 windows in §6.8 become (red) tests; no structural fix | Low — it reveals |
| P0-A | **PR-C2** | `CommerceOperationId` + journal + states + pivot model + operation observability | Medium |
| P0-A | **PR-C3** | Wallet receipts: `OperationId/StepKey` overloads, replay = earlier result, serializable `WalletDebitResult` | High (correctness) |
| P0-A | **PR-C4** | Catalog/Gift/Targeted: deterministic preflight, **single-commit consolidation (D-V4-1)**, journalled idempotent residue under a shutdown token, journalled targeted counter, forward recovery + crash/replay tests | High |
| P0-A | **PR-C5** | Marketplace: four flows as journalled state machines (MakeOffer reordered, Cancel/Redeem receipts, Buy durable post-claim completion) | High |
| P1 | **PR-C6** | Relay of the critical events from the terminal transitions + consumer dedup (quests, daily tasks) + backlog/age/dead-letter | Medium |
| P1 | **PR-W1** | `IWiredRoomHost` + consumption through the interface (mechanical diff, zero semantics) | Low |
| P1 | **PR-W2** | TriggerIndex + StackResolver extraction | Medium |
| P1 | **PR-W3** | Scheduler + Policy extraction (runtime state included) + structured diagnostics | High content |
| P1 | **PR-W4** | SelectionEngine + CallChainGuard + WiredEngine assembler | Medium |
| P1 | **PR-S1** | Fulfillment: pure strategies + `FulfillmentPlan` + planner (preflight), Badge first | Medium |
| P1 | **PR-S2** | Migrate Effect, Floor/Wall+guild, Bot/Pet; remove the branches | Medium |
| P1/P2 | **PR-F1** | Furniture registry hardening (collision policy, fallback metric, hot-unload test) | Low |
| P2 | **PR-Q1** | Atomic `FurnitureDefinitionSet`; Specs provenance `repo@sha` | Low |
| P2 | **PR-Q2** | Protocol baseline burn-down; persistence loss-window doc; full Wired observability if anything remains | Low |
| Closing | **PR-Z1** | Final benchmarks vs baseline, docs "adding a furni / a Wired / a commerce flow", ADR review, acceptance matrix | Low |

Ordering constraints: PR-C1 precedes every commerce fix (the red tests are the specification); PR-C2/C3 precede C4/C5; PR-S1 waits for C4 ("otherwise we clean a method up then re-split it around the pivot" — V3, kept); PR-W1 can run in parallel with commerce (disjoint files); PR-G1/G2 immediately. **Named semantic risks** (blocking until pinned by a test): the `CanTrigger`→conditions order; zero-delay re-drain; index self-repair; the sliding window; the mutation→map→event→dirty order; quantity×product and guild stamping; StuffData-from-blob at grant; the settlement pivot (never "harmonized"); the Buy claim kept as is.

---

# 10. Testing strategy

**Commerce fault injection first.** A stepped harness: each step declares `Committed` when its durable effect is applied; the harness can throw at step k+1, kill/restart the orchestration at every boundary, replay by `OperationId`. Assertions on the **final business state** (balances, inventory, offers, counters, presence of the present) and on the recovery — never just "refund was called". Blocking matrix: the 8 windows of §6.8 + gift wrapping + duplicate wallet stepKey (a single application) + `AddEffectAsync` replay under a receipt (a single row) + commit→publish crash (the critical event goes out from the journal; replayed = progression unchanged).

**Wired**: the full parity matrix (§6.4) on a fake host + fake clock + injected RNG; depth coming from the config (RFW-101 proof).

**Furniture**: family, collision policy (4 cases), hot-unload-restores, fallback counted, lifecycle, StuffData round-trip per type.

**Architecture**: exact and complete interleaving manifest (unlisted method = failure; category B: no `await` before completion); `RoomGrain` not `[Reentrant]`; no-lock/no-Task.Run in Rooms; wired knobs; partials with no concrete logics; single-silo singletons registered; STATE.yaml schema.

**Reference data**: readers only see version N or N+1. **Specs**: Daybreak ≠ Arcturus origins + SHA; reproducible bootstrap. **Versioned benchmarks**: empty/loaded room, event storm, no-trigger O(1), massive hydration (OQ-7) — baseline in PR-G1, comparison in PR-Z1, a significant regression refused without a justification.

Every new protocol test goes through `Specs analyze` first; every `BEST_EFFORT` rule visible in the code gets a capture or a test + a risk note.

---

# 11. Observability

§6.15 restated as requirements: **Wired** — counters (received/ignored/dropped/processed, index size/rebuilds, chains stopped by reason, delayed cancelled at revalidation), a normalized `StopReason`, `ExecutionId/ParentExecutionId` on execute-stacks and signals, opt-in p95/p99 of the step per room, an opt-in room-scoped trace giving the logical chronology (box id, StackIdentity, event type, selections by source, condition+policy results, effects chosen/skipped with order/deadline/revalidation). **Commerce** — per operation: OperationId, flow, player/offer, phase, pivot timestamp, current step, attempts, last error, age, recovery/compensation state; alerts: stuck post-pivot, receipt conflict, refund failure, relay backlog/age/retries/dead-letter, `NeedsIntervention`. **Reference** — the version of the published sets. The identifiers (box id, stack/tile id, trigger/action type, OperationId) appear in every signal; the cost when off stays "one boolean and nothing else" (the existing tick's norm).

---

# 12. V4 anti-drift AI workflow

The principle is unchanged since V2: **orchestrate what exists** (hooks, gates, reviewers, skills, Specs CLI), add only memory and decision. V3 contributions kept (`watched_paths` invalidation), V4 contributions: the interleaving manifest, the commerce extension of the slice, the economy reviewer.

## 12.1 Persistent memory

```
docs/architecture-v4/
├── README.md                      (how to resume)
├── STATE.yaml                     (source of truth, schema validated by QualityGate)
├── ARCHITECTURE-V4.md             (this document)
├── interleaving-manifest.yaml     (§12.3)
├── decisions/                     (ADR-000 = §8; one ADR per decision thereafter)
├── plans/                         (one active slice = one contract)
└── reviews/                       (reviewer outputs per slice/epic)
```

```yaml
workflow_version: 4
baseline: { repository: absolutezeroo/vortex-cloud, commit: afc485be58ffd983b8d96430efe8aed620ad0ade }
references_verified: 2026-08-25
phase: p0            # p0 | commerce | wired | fulfillment | shorts | closing
active_slices: []
accepted_decisions: [ADR-000]
open_questions: [OQ-1, OQ-2, OQ-3, OQ-4, OQ-5, OQ-6, OQ-7, OQ-8]
audits:
  commerce:
    vortex_sha: afc485be58ffd983b8d96430efe8aed620ad0ade
    watched_paths: [Vortex.Catalog/**, Vortex.Marketplace/**, Vortex.Inventory/**,
                    Vortex.Players/Grains/PlayerWalletGrain.cs, Vortex.Players/Grains/PlayerEffectGrain.cs,
                    Vortex.Primitives/Players/Wallet/**, Vortex.Events/**]
    status: valid
  wired:
    vortex_sha: afc485be58ffd983b8d96430efe8aed620ad0ade
    watched_paths: [Vortex.Rooms/**, Vortex.Primitives/Rooms/**]
    status: valid
  furniture:
    vortex_sha: afc485be58ffd983b8d96430efe8aed620ad0ade
    watched_paths: [Vortex.Rooms/Providers/**, Vortex.Rooms/Object/**, Vortex.Furniture/**]
    status: valid
forbidden:
  - implementation_before_architecture_gate
  - split_room_into_item_or_wired_grains
  - manual_locks_inside_grains
  - reference_emulator_as_protocol_authority
  - shadow_engine_without_adr
  - behavior_change_inside_structural_slice
  - value_moving_slice_without_commerce_contract
```

At SYNC: `git diff <audit_sha>..HEAD -- <watched_paths>`; no file touched → audit `valid`; touched → `stale` → **targeted** revalidation (never a full reanalysis for a front-end change).

## 12.2 ADRs and the slice contract

The ADR format is unchanged (Status/Context/Decision/Rejected alternatives/Consequences/Evidence, file+SHA); ADR-000 encodes the §8 register; contradicting an accepted ADR requires announcing the conflict and providing new evidence. The slice contract is unchanged (Goal, Preconditions, Allowed/Forbidden files, Invariants, `behavior_change`, Semantic risks, Required tests, Abort/rollback, Done) + a **mandatory commerce extension** for any slice that moves value: OperationId, Pivot, CompensableBeforePivot, RetryableAfterPivot, IdempotencyKey/StepKey, RecoveryOwner, CriticalOutboxEvents, tested crash points. A finding out of scope = a `CROSS-DOMAIN FINDING` recorded in STATE.yaml, never fixed quietly.

## 12.3 Interleaving manifest (D-V4-5)

`interleaving-manifest.yaml`: one entry per `[AlwaysInterleave]/[Reentrant]/MayInterleave` method in the repository — symbol, category (`ImmutableRead` | `SynchronousBoundedLivenessOperation`), justification, owner. Architecture test: (a) every method carrying the attribute is listed; (b) every listed entry carries the attribute; (c) category B: the body contains no `await` before the mutation completes (body analysis; the expected pattern is `synchronous mutation → LogAndForget(...) → Task.CompletedTask`, model `SendComposerAsync` [V-PRES02]). Initial entries: both `SendComposerAsync` overloads (B), the interleaved `GetSnapshot`s (A). Outside A/B ⇒ an ADR before merge.

## 12.4 Phases

| Phase | Action | Gate |
|---|---|---|
| SYNC | STATE + ADRs; `audit_sha→HEAD` diff on watched_paths; targeted stale | Coherent baseline |
| PLAN | Slice contract (commerce extension if value); `Specs analyze` if Habbo; invariants + fault points identified | Architecture gate |
| IMPLEMENT | Minimal diff; hooks active; nothing out of scope | FastCheck |
| REVIEW | Reviewer by path: `grain-rules-reviewer` (grains), `wire-truth-auditor` (wire), **`commerce-consistency-reviewer`** (new: Catalog/Marketplace/Wallet/Inventory — checks the declared pivot, receipts, shutdown token, no refund after the pivot); results persisted | QualityGate + STATE update |

---

# 13. Final acceptance criteria

1. `RoomGrain` sole owner, non-reentrant, zero manual synchronization — frozen by architecture tests.
2. Every interleaved method appears in the manifest with a mechanically verified category; outside A/B ⇒ an ADR (D-V4-5).
3. Every configurable Wired budget has a runtime reader and a test; RFW-101 closed, a single depth.
4. The full Wired pipeline runs in tests on a fake `IWiredRoomHost` + fake clock + injected RNG, with no RoomGrain.
5. Live stack, branches, zero-delay re-drain, delays/revalidation, execute-stacks, cycle/depth semantically identical outside an ADR; parity matrix green, each rule tagged by evidence level.
6. Adding a Furniture behaviour = a class + an attribute + tests; a key collision can no longer overwrite silently; unloading a plugin restores the registry's previous state.
7. No redundant `RoomItemFactory`; the Provider→ObjectModule→FurniModule chain is documented.
8. Every Catalog/Gift/Targeted/Marketplace purchase carries an `OperationId`, an explicit pivot, durable progress and a recovery; **no tested crash point produces goods+refund, a debit with no resumption, an evaporated item, or a silent value loss**.
9. The local grant of an offer is **a single commit** in `InventoryGrain`'s turn (D-V4-1); the post-pivot residue is journalled, idempotent, under a shutdown token.
10. Wallet: debit/refund/credit replayed with the same `OperationId/StepKey` apply only once and return the earlier result.
11. Critical post-pivot business events go out from the journal's terminal transitions; replayed, they do not modify progression twice (quests, daily tasks).
12. `GrantCatalogOfferAsync` is no longer a monolithic dispatch; the strategies produce a testable `FulfillmentPlan`, the operation's validated input.
13. Reference data: ById/ByName published as a single atomic version; version exported.
14. `Vortex.Specs` distinguishes each reference by `repo@sha`; no consensus promoted to authority.
15. A new session resumes from STATE/ADRs; only the audits whose `watched_paths` changed are revalidated.
16. FastCheck/QualityGate, characterizations and closing benchmarks green, with no significant unjustified CPU/alloc regression.

---

# CHANGES FROM V3

| V3 point | Verdict | V4 decision | Why |
|---|---|---|---|
| Commerce finding (Catalog/Targeted/Marketplace/Wallet windows) | **KEEP — VERIFIED+** | Kept in full, reinforced with line evidence (SaveChanges 333, "auto-refunds" comment 428, Pets 53/Bots 77, claim 189–194, receipts absent from the wallet contract) | Table §2.2 A1–A6; the code documents the outdated invariant itself |
| Minimal protocol (OperationId, pivot, journal, receipts, recovery) | KEEP + a prior rule | **D-V4-1: reduce the post-pivot surface before instrumenting** — single-commit local batch first | Furni/badges/pets/bots = the same factory, the same grain (B1); instrumenting an artificial topology would be waste |
| Operation journal + selective outbox (two mechanisms) | MODIFY (merge) | **D-V4-2: the journal's terminal transitions = the relay's source**; no independent outbox table as long as no critical event without an operation exists | Every critical event identified is backed by an operation (A13) [M-AZ-03][M-AZ-04] |
| "The wallet receives OperationId/StepKey" (a prescription) | MODIFY (path) | **D-V4-3: receipt inside the transaction `TryDebitAsync` already opens; additive overloads; legacy removed at the end** | A2: the transaction exists; additive evolution avoids breakage |
| Marketplace Buy = one window among others | REFINED | **D-V4-4: the `ExecuteUpdate` claim is a correct pivot to keep; the fix is the durable post-claim completion** | A6: the claim already prevents double selling; credit what is good |
| A/B interleave categories (prose) | KEEP + mechanize | **D-V4-5: a tested manifest; category B verified as "no await before completion"** | A7: the safety property is mechanizable |
| Post-pivot token/cancellation principle | KEEP + proof | **D-V4-6**: the request's `ct` runs through the grant today, and the wallet comment identifies cancellation as the number-one cause of failure | B6 [V-ECO01] |
| Targeted: "consistent count" (a requirement with no shape) | MODIFY | **D-V4-7: the counter = a journalled step or merged into the pivot** | B7; no "special" counter |
| Post-pivot steps "idempotent" (assumed) | KEEP + proof required | **D-V4-8: idempotence proven by a replay test** — counter-example `AddEffectAsync` (unconditional insert) | A15 [V-EFF01] — new V4 proof |
| Registry collision: FAIL + explicit override | KEEP | **D-V4-9** substantively unchanged, hot-unload-restores test added | A9 verified in the code |
| Outbox source [M-AZ-03] = a Cosmos sample | REFINED (sourcing) | The Architecture Center guide [M-AZ-04] promoted to canonical reference, the sample kept as an illustration | B8 |
| Dropping the 17×11 grid | RESTORED | **D-V4-10: grid restored (§6), stable domains dense** | The mission's canonical format; V3 remained auditable but less comparable |
| `CatalogPurchasedEvent` → quests | KEEP + extension | An extra consumer discovered: daily tasks [V-PROG02] — the dedup covers both | A13 VERIFIED+ |
| Everything else (runtime, Rooms, Wired host/plan/shadow, Furniture, Presence DEFER, Specs, Reference Data, watched_paths workflow) | KEEP | Kept as is, re-verified | §2.2 — no structural claim refuted |

---

# OPEN QUESTIONS / NEEDS EVIDENCE

| ID | Question | State / provisional decision |
|---|---|---|
| OQ-1 | Wired chain depth: keep 8 (the effective const) or enable 20 (the config)? | Instrument `depth-stop` (PR-G2); a product decision before the final wiring; official Habbo behaviour `UNKNOWN`. |
| OQ-2 | Exact Catalog pivot: the debit validated after a full preflight (the V4 recommendation — minimizes the residue thanks to D-V4-1) or a later pivot with full compensations? | Settled in the PR-C2 design slice; the invariants (explicit, testable pivot) are imposed, the point is not. |
| OQ-3 | Receipts: one minimal common table or per-owner tables? | A minimal reusable DB primitive **if** it can be inserted in the same transaction as the mutation; never a central service. |
| OQ-4 | The exact scope of the critical relay: which subscribers of `CatalogPurchasedEvent`/Targeted/Marketplace are business-critical? | Quests + daily tasks + audit required first [V-PROG01][V-PROG02]; best-effort metrics separable; list them in PR-C6. |
| OQ-5 | Emission order of `LogAndForget` composers relative to subsequent turns | Probably benign (continuations on the same scheduler); to document or test before relying on it. |
| OQ-6 | The official semantics of modern Wired allowances/orders (sliding vs fixed window, etc.) | `UNKNOWN` without a capture/client; the Vortex choices are documented; community evidence [H-COM-*] to steer the scenarios only. |
| OQ-7 | `ActivatorUtilities` cost when hydrating very large rooms | Benchmark in PR-G1 before any registry optimization. |
| OQ-8 | Idempotence of permanent cross-room Wired variables (concurrent multi-room writes) | Outside the commerce P0; to be investigated before any public variables API. |

---

# FINAL RECOMMENDED REFACTOR ROADMAP

| Prio | Workstream | Size | Dependencies | Risk | PRs |
|---|---|---|---|---|---|
| P0-A | Commerce Consistency (characterization → identity/journal → wallet receipts → Catalog/Gift/Targeted with the D-V4-1 consolidation → Marketplace → critical relay) | XL | Fault-injection tests first | Very high (business) | C1–C6 |
| P0-B | Wired config correctness (RFW-101) + minimal counters | S | — | Low | G2 |
| P0-C | Guardrails: workflow files, interleaving manifest, guards, baseline benchmark, golden inventory | M | — | Low | G1 |
| P1 | Wired extraction behind `IWiredRoomHost` | XL | G1/G2; parallelizable with P0-A | High content | W1–W4 |
| P1 | Fulfillment planner + strategies | M/L | C4 stabilized | Medium | S1–S2 |
| P1/P2 | Furniture registry hardening | S/M | — | Low | F1 |
| P2 | Atomic `FurnitureDefinitionSet`; Specs `repo@sha`; protocol baselines; loss-window doc | S/M | — | Low | Q1–Q2 |
| Closing | Final benchmarks, docs, ADR review, acceptance matrix | S | Everything | Low | Z1 |
| DEFER | PlayerPresence modules; multi-silo; generic Economy Kernel | — | Thresholds/triggers §6.5, §6.1, §8.1 | — | ADR required |
| CONTINGENCY | Plan-only Wired shadow | M | A future semantic rewrite by ADR | — | — |

Dashboard/API/Security: a separate workstream, never mixed in (V1→V3 rule kept).

---

# Appendix A. Repositories, branches and verified revisions (2026-08-25)

| Project | Repository | Branch | Frozen commit | Verification |
|---|---|---|---|---|
| Vortex Cloud | absolutezeroo/vortex-cloud | main | `afc485be58ffd983b8d96430efe8aed620ad0ade` | `git fetch` + `rev-parse`: HEAD = origin = baseline |
| Skylight3 | aromaa/Skylight3 | master | `14a9531ba4368140d26435ae5b0819fd29d13592` | same |
| Arceus Emulator | lmrick/arceus-emulator | main | `97db905ad985c738ee0555c87dd97523ddfe757d` | same |
| Arceus Wired Plugin | lmrick/arceus-emulator-wireds-plugin | main | `c2e649c9fe281c4feef75138ccbc176bbea8f517` | same |
| Habbo-Daybreak | habbo-cc/Habbo-Daybreak | develop | `2dee52a0ecd131828cf86c3b0e9e736d93f434eb` | same |

Every future session compares its HEAD to the baseline and only revalidates the audits whose `watched_paths` changed (§12.1).

# Appendix B. Code source index (line level, verified on 2026-08-25)

Vortex base: `https://github.com/absolutezeroo/vortex-cloud/blob/afc485be58ffd983b8d96430efe8aed620ad0ade/` (abbreviated `V/`). The lines cited are the ones verified in §2.2.

## B.1 Vortex — commerce
- **[V-ECO01]** `V/Vortex.Primitives/Players/Wallet/WalletPurchaseExtensions.cs` — debit→grant→refund; compensation on `CancellationToken.None` (cancellation = the number-one cause of grant failure, per the comment); `LogCritical` if the refund fails.
- **[V-ECO02]** `V/Vortex.Players/Grains/PlayerWalletGrain.cs` — `TryDebitAsync` 55–130: fresh DbContext per attempt + `IExecutionStrategy` + `BeginTransaction` + commit; the debit is durable and retry-safe by construction.
- **[V-ECO03]** `V/Vortex.Primitives/Players/Grains/IPlayerWalletGrain.cs` — a contract with no operation identity (`TryDebitAsync(List<…>)` l.11, `CreditBackAsync(List<…>)` l.15, `GrantCreditsAsync(int)` l.18).
- **[V-ECO04]** `V/Vortex.Rooms/Grains/Systems/WiredTrading/WiredTradeSettlement.cs` — documented pivot, reasoned refusal of the common primitive, named order tests (style canon).
- **[V-CAT01]** `V/Vortex.Catalog/Grains/CatalogPurchaseGrain.cs` — `ExecutePurchaseAsync` l.86, `PublishAsync(CatalogPurchasedEvent)` l.122 (after success, outside the transaction).
- **[V-CAT02]** `V/Vortex.Inventory/Grains/InventoryGrain.Furni.cs` — `GrantCatalogOfferAsync`: parsing 152–296; `AddRange` ~300; badges (`alreadyOwned` guard) 306–331; **`SaveChangesAsync` 333**; cache/client/events sync 342–379; pets 395; bots 409; effects 435; "auto-refunds" comment l.428.
- **[V-CAT03]** `V/Vortex.Database.Tests/Catalog/CatalogPurchaseTests.cs` — `AGrantThatFails_RefundsTheBuyer` l.226 (pre-pivot only).
- **[V-CAT04]** `V/Vortex.Database.Tests/Catalog/CatalogPurchaseHarness.cs` — binary fake `IInventoryGrain` 174–190 (`GrantThrows ? throw : Task.CompletedTask`).
- **[V-CAT05]** `V/Vortex.Catalog/Grains/CatalogPurchaseGrain.Gift.cs` — purchase l.66 then wrapping as a present (l.124, plain fallback); cross-catalog offer lookup commented l.186.
- **[V-CAT06]** `V/Vortex.Catalog/Grains/PlayerTargetedOfferGrain.cs` — loop of unit grants 85–111 inside the compensated scope; `IncrementPurchaseCountAsync` l.124 after success; "Non-transactional" analytics event.
- **[V-INV01]** `V/Vortex.Inventory/Grains/InventoryGrain.Pets.cs` — `CreatePetAsync`: its own commit l.53.
- **[V-INV02]** `V/Vortex.Inventory/Grains/InventoryGrain.Bots.cs` — `CreateBotAsync`: its own commit l.77.
- **[V-INV03]** `V/Vortex.Inventory/Grains/InventoryGrain.Furni.cs` — `GrantFurnitureDefinitionAsync` 603–635: Add + commit **per unit**; StuffData rebuilt from the blob (the "blank legacy default" bug documented).
- **[V-MKT01]** `V/Vortex.Marketplace/Grains/MarketplacePurchaseGrain.cs` — MakeOffer: `RemoveFurnitureAsync` ~69 → insert+commit ~99; Cancel: `Cancelled`+commit 127–128 → grant 132; Buy: `ExecuteUpdate` claim Active→Sold+CreditsOwed 189–194 → grant 211 → best-effort revert 221–233 ("Sold with no item delivered", `CancellationToken.None`); Redeem: `CreditsOwed=0`+commit 302–305 → `GrantCreditsAsync` 308–310.
- **[V-EFF01]** `V/Vortex.Players/Grains/PlayerEffectGrain.cs` — `AddEffectAsync` 71–91: unconditional insert (`PlayerEffects.Add`) — not idempotent on retry.
- **[V-EVT01]** `V/Vortex.Events/EventSystem.cs` — in-process bus. **[V-EVT02]** `V/Vortex.Events/Registry/EventRegistry.cs` — `HandlerMode = Parallel` l.42, error isolation.
- **[V-PROG01]** `V/Vortex.Progression/Quests/Events/QuestProgressEventHandlers.cs` — `IEventHandler<CatalogPurchasedEvent>` l.86. **[V-PROG02]** `V/Vortex.Progression/Quests/Events/DailyTaskProgressEventHandlers.cs` — same l.110 (an extra consumer, a V4 discovery).

## B.2 Vortex — runtime, rooms, wired, furniture
- **[V-RUN01]** `V/Vortex.Rooms/Grains/RoomGrain.cs` — `RegisterGrainTimer` tick, steps isolated by `RunTickStepAsync`, `AdvanceBoundaryPast` deadlines, best-effort deactivation flush.
- **[V-WIR01]** `V/Vortex.Rooms/Grains/Systems/RoomWiredSystem.cs` (+ `RoomWiredSystem.Variables.cs`) — the full §6.4 pipeline; `MaxCallChainDepth = 8` (const); log channel + error counters.
- **[V-WIR02]** `V/Vortex.Rooms/Configuration/RoomConfig.cs` — `WiredMaxDepth = 20` **never read** (RFW-101); budgets 64/64, queue 512, tick 50 ms, flush caps.
- **[V-WIR03]** `V/Vortex.Rooms/Wired/WiredPendingStackExecution.cs` — init-only 8–19 + `Version/DueAtMs/NextActionIndex/WaitingActionIndex/EffectsStarted` as `get; set;` 21–28.
- **[V-WIR04]** `V/Vortex.Rooms/Wired/WiredExecutionContext.cs` — side-effect collection, grouped flush. **[V-WIR05]** `V/Vortex.Primitives/Rooms/Wired/IWiredExecutionContext.cs` — capabilities 32–88 (states, movement, composer, bots, hand items).
- **[V-FUR01]** `V/Vortex.Rooms/Providers/RoomObjectLogicProvider.cs` — `RegisterLogic`: `_logics[key] = reg` (overwrite) 51–68; dispose `TryRemove(KeyValuePair(key, reg))` with no restoration; fallback commented "the warning is the to-do list".
- **[V-FUR02]** `V/Vortex.Rooms/Object/Logic/RoomObjectLogicFeatureProcessor.cs` — `[RoomObjectLogic]` scan, `ActivatorUtilities` factories, composed plugin ServiceProvider.
- **[V-FUR03]** `V/scripts/hooks/check-logic-groups.mjs` — Dashboard menu derived from the attributes (guard against generated drift).
- **[V-FUR04]** `V/Vortex.Rooms/Providers/RoomItemsProvider.cs` — item materialization from rows/snapshots. **[V-FUR05]** `V/Vortex.Rooms/Grains/Modules/RoomObjectModule.cs` — attach state+logic+index+map. **[V-FUR06]** `V/Vortex.Rooms/Grains/Modules/RoomFurniModule.Floor.cs` — place/move/use/edit lifecycle.
- **[V-FUR07]** `V/Vortex.Rooms/Object/Logic/Furniture/FurnitureLogic.cs` — lifecycle, centralized `PersistStuffDataAsync`/`MarkDirty`, historical invariant commented.

## B.3 Vortex — players, reference, specs, infra
- **[V-PRES01]** `V/Vortex.Primitives/Players/Grains/IPlayerPresenceGrain.cs` — 15–26: deadlock rationale + "only enqueue … synchronously — no awaits" + `[AlwaysInterleave]`. **[V-PRES02]** `V/Vortex.Players/Grains/PlayerPresenceGrain.cs` — 112–137: `EnqueueOutgoing` + `LogAndForget(ProcessOutgoingQueueAsync())` + `Task.CompletedTask`.
- **[V-REF01]** `V/Vortex.Furniture/Providers/FurnitureDefinitionProvider.cs` — two indexes published separately (mixed-version window). **[V-REF02]** `V/Vortex.Catalog/Providers/CatalogSnapshotProvider.cs` — full build → `Volatile.Write` (the target pattern).
- **[V-SPEC01]** `V/Vortex.Specs/Sources/SpecWorkspace.cs` — tree detection, merging `arcturus` origin (the `repo@sha` workstream). **[V-SPEC02]** `V/Vortex.Specs/Analysis/Reference/ArcturusReferenceAnalyzer.cs` — reference analyzer.
- **[V-MULTI01]** `V/Vortex.Main/Configuration/OrleansHostConfig.cs` — reasoned single-silo thesis (`ReloadAsync` providers, aggregators, memory streams).
- **[V-PROTO01]** `V/tree/Vortex.Protocol` — 1,091 message files.
- **[V-AI01]** `V/AGENTS.md` — the canonical contract (pinned stack, skills, Orleans rules, checklists). **[V-AI02]** `V/CLAUDE.md` — context order, automations. **[V-AI03]** `V/scripts/hooks/` — post-edit, guard-emulator, check-header-registry (14 baselined), check-wire-conflicts (23 baselined), hook self-tests. **[V-AI04]** `V/.claude/agents/` — `grain-rules-reviewer`, `wire-truth-auditor`.
- **[V-AUD01]** `V/docs/audits/2026-07-02-full-technical-audit.md` — a dated audit (a source of hypotheses, never of HEAD).

## B.4 References (frozen SHAs, see Appendix A)
- **[S-RUN01]** `src/Skylight.Server/Game/Rooms/Room.cs`; **[S-RUN02]** `.../Rooms/Scheduler/RoomTaskScheduler.cs` — dedicated thread + SpinLocks.
- **[S-CAT01]** `src/Skylight.API/Game/Catalog/Products/ICatalogProduct.cs`; **[S-CAT02]** `.../Products/CatalogProductBadge.cs`; **[S-CAT03]** `.../Catalog/CatalogTransaction.cs`; **[S-CAT04]** `.../CatalogTransaction.Context.cs` — contributive model + local EF transaction.
- **[A-FUR01]** `emulator/src/main/java/habbo/rooms/components/objects/items/RoomItemFactory.java` — Map<String,Class> + Constructor cache.
- **[AP-WIR01]** `README.md`; **[AP-WIR02]** `emulator.wireds/src/main/java/org/emulator/wireds/RoomInjector.java`; **[AP-WIR03]** `.../component/WiredManager.java`; **[AP-WIR04]** `.../component/WiredExecutionPipeline.java` — named phases, ConcurrentHashMap/Future/IThreadManager.
- **[D-WIR01]** `src/main/java/com/eu/habbo/habbohotel/wired/core/WiredServices.java`; **[D-WIR02]** `.../WiredManager.java` (parallel/exclusive flags); **[D-WIR03]** `.../WiredEngine.java` (depth 10, rate limit); **[D-WIR04]** `.../RoomWiredStackIndex.java` (invalidatable cache).
- **[D-FUR01]** `.../habbohotel/items/ItemManager.java` — a giant manual registry. **[D-PLUG01]** `.../plugin/PluginManager.java` — global statics.

# Appendix C. External references (consulted on 2026-08-25)

- **[M-ORL-01]** Microsoft Learn — *Orleans request scheduling / reentrancy* — https://learn.microsoft.com/en-us/dotnet/orleans/grains/request-scheduling — single-threaded turns, opt-in interleaving.
- **[M-ORL-02]** Microsoft Learn — *Timers and reminders* — https://learn.microsoft.com/en-us/dotnet/orleans/grains/timers-and-reminders — the period is measured from the callback's resolution; `RegisterGrainTimer` non-interleaving by default; `RegisterTimer` obsolete.
- **[M-ORL-03]** Microsoft Learn — *Messaging delivery guarantees* — https://learn.microsoft.com/en-us/dotnet/orleans/implementation/messaging-delivery-guarantees — at-most-once by default; retries ⇒ multiple deliveries possible; no durable deduplication.
- **[M-ORL-04]** Microsoft Learn — *Orleans transactions* — https://learn.microsoft.com/en-us/dotnet/orleans/grains/transactions — Orleans transactional state (outside the scope of the EF/MySQL + side-effects commerce problem).
- **[M-AZ-01]** Azure Architecture Center — *Saga pattern* — https://learn.microsoft.com/en-us/azure/architecture/patterns/saga — compensable / **pivot** / retryable.
- **[M-AZ-02]** Azure Architecture Center — *Compensating Transaction pattern* — https://learn.microsoft.com/en-us/azure/architecture/patterns/compensating-transaction — record the progress; compensation steps idempotent and resumable.
- **[M-AZ-03]** Microsoft Learn — *Transactional Outbox (Cosmos DB sample)* — https://learn.microsoft.com/en-us/samples/azure-samples/cosmos-db-design-patterns/transactional-outbox/ — an illustration of the state+event dual write.
- **[M-AZ-04]** Azure Architecture Center — *Transactional Outbox pattern (guide)* — https://learn.microsoft.com/en-us/azure/architecture/databases/guide/transactional-outbox-cosmos — the canonical reference: state+outbox atomicity, at-least-once relay, idempotent consumers.
- **[M-AZ-05]** Azure Architecture Center — *Minimize coordination* — https://learn.microsoft.com/en-us/azure/architecture/guide/design-principles/minimize-coordination — idempotence and reduced coordination.
- **[H-OFF-01]** Habbo Customer Support — *What is Wired Furni?* — https://help.habbo.com/hc/en-us/articles/360011620099-What-is-Wired-Furni. **[H-OFF-02]** — *What is Furni?* — https://help.habbo.com/hc/en-us/articles/360011512940-What-is-Furni.
- **[H-OFF-03]** Habbo.com — *Wired Variables are here!* — https://www.habbo.com/community/article/34067/wired-variables-are-here-2. **[H-OFF-04]** — *Wired 2.0 Dev Update* — https://www.habbo.com/community/article/35029/wired-2-0-dev-update (official, public Variables/Wired 2.0 semantics).
- **[H-COM-01]** HabbGames — *HelpWired: Présentation du Batch 12* — https://habbgames.fr/news/6864-help-wired-presentation-du-batch-12. **[H-COM-02]** HabboWIRED — *Effet WIRED : Exécuter piles Wired* — https://habbowired.fr/Effet%20WIRED%20%3A%20Ex%C3%A9cuter%20piles%20Wired. **[H-COM-03]** HabboWIRED — *Présentation des Sélecteurs* — https://habbowired.fr/Pr%C3%A9sentation%20des%20S%C3%A9lecteurs. **[H-COM-04]** HabboWIRED — *WIRED Variable : Variable de contexte* — https://habbowired.fr/WIRED%20Variable%20%3A%20Variable%20de%20contexte. Community sources: they steer the test scenarios, never promoted to authority.

---

| **Consolidated conclusion** — V3 survived the adversarial audit: none of its structural claims is refuted, and its commerce finding is deeper than its own text — the code itself documents the refund invariant designed for an atomic grant that it no longer is (l.428). V4 is therefore not a reversal but a tightening: reduce the post-pivot surface before instrumenting it (one local commit where there are four), merge journal and outbox into a single durable pipeline, move the wallet receipts into the transaction that already exists, keep the Marketplace claim as the correct pivot it is, mechanize the interleaving manifest, and restore the documentary grid that makes each revision comparable to the previous one. The Room runtime, for its part, does not change: we extract the Wired engine behind capabilities, we harden a registry that exists, and we touch nothing of what the code already does better than the three references combined. |
|---|

*Vortex Cloud — Target architecture & AI workflow — V4 — Deep Audit Rewrite — 25 August 2026. Replaces V1, V2 and V3 for any implementation planning.*
