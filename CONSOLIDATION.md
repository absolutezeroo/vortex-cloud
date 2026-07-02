# Vortex Cloud — Consolidation Work (non-feature)

Hardening backlog, audited on `main` (commit `fa1e6e8`). Real data, not estimates.
This complements `ROADMAP.md` (which covers features): this covers the **quality of the
existing system**. Goal: close the gap between architectural ambition and implementation depth.

---

## Audit snapshot

| Metric | Value | Interpretation |
|---|---|---|
| Projects | 29 | some too small (over-modularization); `Turbo.Contracts` already merged away |
| Test projects | 2 (`WebApi.Tests`, `Rooms.Tests`) | in progress, but sensitive paths not covered |
| Test cases | 21 | WebApi (16) + wired round-trip; **policies + ledger missing** |
| Handlers | 501 | including **~300 empty stubs (~60%)** — down from 78%, still majority |
| TODO/FIXME/HACK | 276 | scattered technical debt, down from 327 |
| `TODO handle exceptions` | **0** | ✅ error strategy now applied |
| Catch blocks | 164 | 13 confirmed silent (`catch (Exception) { }` with no logging), all in `Turbo.Rooms` — see below |
| `EventContext` | no longer empty (`Cancel`, `CancelReason`, `CorrelationId`, `Items`); `PublishCancellableAsync` is wired and used in prod by `GroupDirectoryGrain` | interception plumbing works, but **zero production `IEventBehavior<T>` implementations** exist (only test doubles) — seam is functional, just unused |
| Hardcoded `= false` (Rooms) | not re-audited since this snapshot | verify before relying on this line |

**Already done (your credit):** grain error strategy (0 TODO exceptions), wired round-trip test,
WebApi migration + integration tests.

---

## Prioritized backlog

### P1 — Test sensitive paths (highest leverage)
**Evidence:** 21 cases, but `RoomSecurityPolicy`, `ModerationPolicy` (pure functions) and
`economy_ledger` are not covered. Those are places where silent regressions are expensive
(privilege escalation, duplicated currency).
**Work:** expand `Turbo.Rooms.Tests` (or a `Turbo.Permissions.Tests`) to cover both policies
(all branches) + invariants on the ledger (debit/credit, idempotence, insufficient balance
rejection, no path moves currency without ledger entry).
**Done when:** policies and ledger are covered; the gate executes these tests.

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

### P5 — Audit catch block uniformity (in progress)
**Evidence:** 0 `TODO exception` (good). Of 164 catch blocks, 13 confirmed silent
(`catch (Exception) { }`, no logging) — all concentrated in `Turbo.Rooms`. The rest of the repo
(Catalog, Inventory, Observability, and `Turbo.Rooms/Grains/RoomGrain.Moderation.cs` itself) already
logs correctly via `ILogger<T>`.
**Fixed so far:** `RoomGrain.cs` `OnDeactivateAsync` (silently ate `FlushDirtyItemsAsync` /
`RemoveActiveRoomAsync` failures — real risk of item loss + stale `RoomDirectoryGrain` state) now
logs via `_logger.LogWarning`. `PlayerPresenceGrain.SendComposerAsync` fire-and-forget (functionally
equivalent to the forbidden `.Ignore()`) now routes through a `LogAndForget` helper matching the
existing `MessengerGrain` pattern.
**Remaining:** `RoomService.Floor.cs:64`, `RoomService.Wall.cs:61`, `RoomWiredSystem.cs`,
`RoomRollerSystem.cs`, `RoomPathingSystem.cs`, `RoomAvatarTickSystem.cs`,
`RoomMapModule.Avatar.cs`, `RoomAvatarModule.cs`.
**Work:** review remaining catches and ensure none swallow without logging (using the shared error strategy).
**Done when:** no silent swallowing.

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

**P1 (tests) → P2 (shells) → P3 (projects) → P4 (gate)**, then P5/P6/P7 continuously.
Less exciting than feature work, but this is exactly what separates a beautiful architecture from a
production-ready hotel.
