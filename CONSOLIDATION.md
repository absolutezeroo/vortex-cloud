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
| Catch blocks | 167 | repo-wide re-audit found 0 silent swallows outside `Turbo.Rooms`; last 3 in `Turbo.Rooms`/`Turbo.Inventory` fixed; the wired-subsystem gap (26 sites in `FurnitureWiredLogic.cs` and friends) is now closed too — see P5 below |
| `EventContext` | no longer empty (`Cancel`, `CancelReason`, `CorrelationId`, `Items`); `PublishCancellableAsync` is wired and used in prod by `GroupDirectoryGrain` | interception plumbing works, but **zero production `IEventBehavior<T>` implementations** exist (only test doubles) — seam is functional, just unused |
| Hardcoded `= false` (Rooms) | not re-audited since this snapshot | verify before relying on this line |

**Already done (your credit):** grain error strategy (0 TODO exceptions), wired round-trip test,
WebApi migration + integration tests, policy/ledger/purchase-refund test coverage (P1), repo-wide
catch-block re-audit (P5).

**Fixed since the 2026-07-02 full audit** (findings from
`docs/audits/2026-07-02-full-technical-audit.md`):
- R5 — `MessengerGrain.AcceptFriendRequestsAsync` now uses tracked deletes so the request removal
  and friendship inserts commit atomically in one `SaveChangesAsync`.
- R6 — `MarketplacePurchaseGrain.BuyOfferAsync` claims the offer atomically (`ExecuteUpdate`
  guarded on `State == Active`) and re-lists it if the inventory grant fails, so a refunded buyer
  can never leave the offer stranded as Sold.
- R7 — `LtdRaffleGrain` refunds are awaited (`Task.WhenAll` with per-failure logging) before the
  batch is finalized; the `ContinueWith`-based fire-and-forget helper was removed.
- R2/R3/R4 — plugin hosted-service start failures, `ReloadableExport` swap/subscriber failures,
  and `PlayerWalletGrain` debit faults are all logged (no more bare `catch { }` in these paths).
- R8 — SSO ticket TTL/expiry is re-enabled in `AuthenticationService`; successful logins refresh
  a bounded expiry instead of re-inserting a never-expiring ticket.
- R9 — WebApi registration rejects a `PasswordRepeated` that does not match `Password`.
- S1 — `DiffieService` uses a fixed 384-bit safe-prime group (g = 2, p ≡ 7 mod 8) with client
  public-key range validation, replacing random 128-bit probable primes. The size remains
  protocol-capped by the single-RSA-block handshake encoding (documented residual risk).
- S3 — `MigrationHelper.UninstallAsync` escapes quotes/LIKE wildcards in the table prefix and
  refuses an empty prefix (which would have dropped the whole schema).
- A2 — duplicate `PackageVersion` entries removed from `Directory.Packages.props`
  (`FluentAssertions` pinned to the effective 8.10.0).
- S2 — `appsettings.json` (the file that applies outside `Development`, per `launchSettings.json`
  setting `DOTNET_ENVIRONMENT=Development` only for local `dotnet run`) no longer carries working
  secrets: `Database:ConnectionString` and `Crypto:PublicKey`/`PrivateKey` are `CHANGE_ME_*`
  placeholders that must be supplied via `TURBO__Turbo__*` environment variables or user-secrets.
  `Observability:DashboardToken` (dead config — the dashboard has used per-account
  email/password + capability auth for a while, nothing ever bound this key) was deleted.
  `AuthenticationModule` now fails fast outside `Development` if `IpHashSecret` is unset or still
  one of the placeholder defaults. The real RSA keypair that was committed is rotated (a fresh
  1024-bit e=3 keypair, round-trip verified against `RsaService.Encrypt`/`Decrypt`) and now lives
  only in `appsettings.Development.json` as the local dev default — the old key should be treated
  as compromised (it is still in git history) and never reused.
- R1 — the 26 bare `catch { }` in the wired subsystem (`FurnitureWiredLogic.cs` and its
  Action/Addon/Condition/Selector/Trigger base+leaf classes, `WiredExecutionContext.cs`,
  `RoomWiredSystem.ProcessPendingActionAsync`) are now logged. The 83-leaf DI cascade the earlier
  P5 pass worried about turned out to be unnecessary: every one of these classes already carries a
  `_roomGrain` field (`FurnitureWiredLogic`/`WiredContext` constructor parameter), and `RoomGrain`
  already exposes `internal readonly ILogger<IRoomGrain> _logger`, reachable from anywhere in
  `Turbo.Rooms`. Every site now logs via `_roomGrain._logger.LogWarning(ex, …)` with the wired
  item/tile/furni id for diagnosis — no constructor signatures changed, no behavior changed on
  the success path.

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

### P5 — Audit catch block uniformity — done
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
**Wired-subsystem gap (R1) closed:** `FurnitureWiredLogic.cs` and its Action/Addon/Condition/
Selector/Trigger base+leaf classes, `WiredExecutionContext.cs`, and
`RoomWiredSystem.ProcessPendingActionAsync` had 26 fully-silent `catch { }` blocks (rehydration
`Activator.CreateInstance` failures, malformed wired-update params, tile/furni lookups during
selection, pending-action execution). The previously-assumed DI cascade across all 6 abstract
wired-kind classes and 83 concrete leaf classes was unnecessary: every one of these classes already
holds a `_roomGrain` field, and `RoomGrain` already exposes `internal readonly
ILogger<IRoomGrain> _logger` (accessible repo-wide within `Turbo.Rooms`). All 26 sites now log via
`_roomGrain._logger.LogWarning(ex, …)` with contextual ids (wired item, tile, furni/player id) — no
constructor signatures changed.
**Done when:** no silent swallowing remains. ✅

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
