# Vortex Cloud — Consolidation Work (non-feature)

Hardening backlog, audited on `main` (commit `fa1e6e8`). Real data, not estimates.
This complements `ROADMAP.md` (which covers features): this covers the **quality of the
existing system**. Goal: close the gap between architectural ambition and implementation depth.
Dated audit snapshots are archived under `docs/audits/` (latest:
`docs/audits/2026-07-02-full-technical-audit.md`; earlier same-day snapshot:
`docs/audits/2026-07-02-technical-audit.md`).

---

## Audit snapshot

| Metric | Value | Interpretation |
|---|---|---|
| Projects | 29 | some too small (over-modularization); `Turbo.Contracts` already merged away |
| Test projects | 4 (`WebApi.Tests`, `Rooms.Tests`, `Database.Tests`, `Revisions.Tests`) | `Rooms.Tests` now covers policies, ledger, and the purchase-refund invariant |
| Test cases | 40 in `Rooms.Tests` alone (was 21 total incl. WebApi) | policies (all branches), ledger mapping, purchase refund-on-failure, wired round-trip, group creation grain test all covered |
| Handlers | 501 | including **~300 empty stubs (~60%)** — down from 78%, still majority |
| TODO/FIXME/HACK | 276 | scattered technical debt, down from 327 |
| `TODO handle exceptions` | **0** | ✅ error strategy now applied |
| Catch blocks | 167 | repo-wide re-audit found 0 silent swallows outside `Turbo.Rooms`; last 3 in `Turbo.Rooms`/`Turbo.Inventory` fixed; one tracked gap remains in `FurnitureWiredLogic.cs` (needs a wider DI change) — see below |
| `EventContext` | no longer empty (`Cancel`, `CancelReason`, `CorrelationId`, `Items`); `PublishCancellableAsync` is wired and used in prod by `GroupDirectoryGrain` | interception plumbing works, but **zero production `IEventBehavior<T>` implementations** exist (only test doubles) — seam is functional, just unused |
| Hardcoded `= false` (Rooms) | not re-audited since this snapshot | verify before relying on this line |

**Already done (your credit):** grain error strategy (0 TODO exceptions), wired round-trip test,
WebApi migration + integration tests, policy/ledger/purchase-refund test coverage (P1), repo-wide
catch-block re-audit (P5).

---

## Prioritized backlog

### P1 — Test sensitive paths (highest leverage) — done
**Evidence:** `RoomSecurityPolicyTests.cs` and `ModerationPolicyTests.cs`
(`Turbo.Rooms.Tests/Permissions/`) now cover every branch of both policies. Ledger mapping is
covered by `EconomyLedgerTests.cs`, and the specific bug this section used to flag — a failure
after a successful wallet debit permanently losing the player's credits — now has a regression
test: `WalletPurchaseExtensionsTests.cs` (`Turbo.Rooms.Tests/Observability/`) exercises
`IPlayerWalletGrain.ExecutePurchaseAsync` directly against a recording fake wallet: insufficient
balance never invokes the grant step, a successful grant never refunds, a throwing grant triggers
exactly one `CreditBackAsync` call with the original debit requests before the exception rethrows,
and an empty debit list (nothing to refund) is not refunded on grant failure. `Turbo.Rooms.Tests` is
now 40/40 green (was 36) and runs in the gate via `dotnet test Turbo.Cloud.sln` in
`TurboCloudFastCheck`.
**Done when:** policies and ledger are covered; the gate executes these tests. ✅

### P2 — Fill the empty shells (partially done)
**Evidence:** `EventContext` is no longer empty — `Cancel`, `CancelReason`, `CorrelationId`, `Items`
are implemented, and `PublishCancellableAsync` is wired + used in production by `GroupDirectoryGrain`,
with test coverage (`EventSystemTests`, `GroupDirectoryGrainCreationTests`) proving cancellation works.
**Remaining gap:** zero production `IEventBehavior<T>` implementations exist outside test doubles
(`CancellingGroupCreatingBehavior`, `ThrowingGroupCreatingBehavior` in `*.Tests` only) — no plugin or
domain module actually registers a real interception behavior yet. Hardcoded `= false` capability
flags in Rooms (group room decoration etc.) have not been re-audited since the original snapshot.
**Work:** ship at least one real `IEventBehavior<T>` consumer to prove the seam end-to-end in
production, not just in tests. Re-audit hardcoded `= false` capability flags.
**Done when:** at least one production `IEventBehavior<T>` exists; no capability is hardcoded via `false`.

### P3 — Reduce over-modularization
**Evidence:** 28 projects; `Turbo.Contracts` (100l), `Turbo.Events` (186l), `Turbo.Logging`
(330l), `Turbo.Messages` (337l) are small.
**Work:** audit each boundary. Any folder that is not a real plugin seam and is purely
decomposed by size should be merged into the logical parent. `Turbo.Contracts` is the clearest case.
**Done when:** project count reflects true boundaries, not just disguised folders.

### P4 — Lock the quality gate (partially done, new gap found)
**Evidence:** `dotnet csharpier check .` is now green repo-wide (3656 files, 0 failures) — the
formatting gate itself is no longer the problem.
**New gap:** `TurboCloudQualityGate` in `Directory.Build.targets` runs
`dotnet format Turbo.Main/Turbo.Main.csproj style|analyzers --verify-no-changes` — this only checks
**`Turbo.Main`** (the composition root, nearly no domain logic), not the solution. Analyzer/style
violations in `Turbo.Rooms`, `Turbo.PacketHandlers`, `Turbo.Players`, etc. — where the actual logic
lives — are never gated.
**Work:** point the `format style`/`format analyzers` steps at `Turbo.Cloud.sln` instead of
`Turbo.Main.csproj`, then make `pre-push` blocking on the full gate.
**Done when:** gate covers the whole solution and is enforced in pre-push.

### P5 — Audit catch block uniformity (re-audited repo-wide; one known gap left)
**Evidence:** repo now has 167 `catch` blocks (up from 164 as new logging/tests were added); 0
`TODO exception`. A full repo-wide sweep (not just `Turbo.Rooms`) found the previously-listed
`Turbo.Rooms` files (`RoomService.Floor.cs`, `RoomService.Wall.cs`, `RoomWiredSystem.cs`,
`RoomRollerSystem.cs`, `RoomPathingSystem.cs`, `RoomMapModule.Avatar.cs`, `RoomAvatarModule.cs`)
already fixed by an earlier pass (each now logs via `_roomGrain._logger.LogWarning`). Three more
silent swallows were found and fixed this round: `RoomAvatarTickSystem.cs` (walk-step failure
recovery ran with no log line), `RoomItemsProvider.cs` and `InventoryFurnitureLoader.cs` (both
silently dropped a furniture item on load failure with `catch (Exception) { continue; }` — real
risk of furniture/inventory items vanishing with zero diagnostic trail; both now log a warning with
the item id and owning room/player before skipping). `InventoryFurnitureLoader` gained a
constructor-injected `ILogger<InventoryFurnitureLoader>` (was DI-constructed with no logger before).
**Known remaining gap (out of scope for this pass):** `FurnitureWiredLogic.cs` (`Turbo.Rooms/Object/
Logic/Furniture/Floor/Wired/`) has two fully-silent `catch { }` (lines ~314, ~340, swallowing
`Activator.CreateInstance` failures when rehydrating wired definition/type specifics) and one
`catch (Exception ex) { Console.WriteLine(ex); return false; }` (line ~362, bypassing structured
logging entirely). Fixing this properly requires threading an `ILogger` through the base
`FurnitureWiredLogic` constructor, which cascades through all 6 intermediate abstract wired-kind
classes (Action/Addon/Condition/Selector/Trigger/Variable) and all 83 concrete wired-leaf classes
constructed via `ActivatorUtilities.CreateInstance(sp, concrete, ctx)` in
`RoomObjectLogicFeatureProcessor.cs` — none of them currently declare a logger parameter, so it
can't be added at the base without touching every leaf constructor. Given wireds are already a
tracked P0 area (see WIN63 protocol audit), this should be its own follow-up, not folded into
general catch-block hardening.
**Done when:** no silent swallowing outside the tracked `FurnitureWiredLogic.cs` gap above.

### P6 — Metrics hygiene
**Evidence:** `performance_logs` still carries legacy client data (`flash_version`).
**Work:** decide on `performance_logs` (delete or keep). Route high-volume telemetry to OTel meter
rather than transactional DB (see `DATA-MODEL.md` §8).
**Done when:** transactional DB no longer stores high-volume telemetry.

### P7 — Stub debt (state honesty)
**Evidence:** 393/498 handlers are empty (78%) + 327 TODOs. The project looks more complete than it is.
**Work:** by domain, decide — implement (which becomes feature work, out of immediate scope here) or
explicitly track stubs so empty scaffolding is not mistaken for implemented work. At minimum, do not
add new stubs without marking them.
**Done when:** actual progress is readable, not hidden by scaffolding.

---

## The meta-work (what prevents regressions from returning)

Enforce an **applied Definition of Done**: nothing lands in `main` without being tested on sensitive
logic, without newly introduced untracked stubs, and with gating/auditing in place. It is already
documented in `AGENTS.md` but not consistently followed — following it turns sequencing discipline
from reactive into deterministic.

---

## Recommended order

**P1 (tests, done) → P2 (shells) → P3 (projects) → P4 (gate)**, then P5 (done, bar the tracked
`FurnitureWiredLogic.cs` gap) /P6/P7 continuously.
Less exciting than feature work, but this is exactly what separates a beautiful architecture from a
production-ready hotel.
