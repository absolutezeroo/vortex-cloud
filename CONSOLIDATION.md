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

**Priority 2 items closed (2026-07-04):**
- S4/S5 — `ClientPacketDecoder.TryRead` rejects a declared body length that is negative or exceeds
  `Turbo:Networking:MaxPacketBodyBytes` (default 64 KiB) *before* the `length + 4` addition that used
  to be overflow-prone. `RemoveFriendMessageParser`/`SaveRoomSettingsMessageParser.ParseTags` now
  clamp their wire-declared counts (`Turbo:Protocol:MaxFriendRemovalIds` / `MaxRoomTags`, defaults
  1000/100) before allocating.
- S6 — `WsPackageHandler` catches the decoder's rejection, logs the session key/remote IP, clears
  `WsBuffer`, and closes the session instead of buffering indefinitely.
- O1 — `IRoomGrain.DeactivateRoom`/`DelayRoomDeactivation` are now `DeactivateRoomAsync`/
  `DelayRoomDeactivationAsync` returning `Task`; `RoomDirectoryGrain.CheckRoomsAsync` awaits them
  via `Task.WhenAll` instead of firing one-way void calls.
- H6 — `SessionGateway`'s `_sessionToPlayer`/`_playerToSession` pair is now mutated under a single
  `SemaphoreSlim` (`_mappingGate`), so interleaved connects/disconnects can no longer leave a
  half-updated (ghost) mapping. `AddSessionToPlayerAsync` gained an optional `CancellationToken`
  that threads through instead of hardcoded `CancellationToken.None`.
- O4 — `PlayerPresenceGrain`'s outgoing composer queue is capped (drop-oldest + warning log) via
  `Turbo:PlayerPresence:MaxOutgoingQueueSize` (default 500); `OnErrorAsync` logs stream faults
  instead of swallowing them; `OnDeactivateAsync` unsubscribes the room-outbound stream handle.
- D7 — `AddPluginTablePrefix` no longer ignores its own computed fallback: the `.Select(...).ToString()`
  bug (which returned the enumerable's type name, not the joined prefix) is fixed with
  `new string(...ToArray())`, and the returned delegate now honors `ExplicitlyNoTablePrefix` instead
  of re-reading `manifest.TablePrefix` directly.
- D8 — `pets.sql` (legacy MyISAM DDL, zero code references, partially superseded by
  `PetFoodEntity`/`PetCommandEntity`) removed at the user's direction.

All new limits follow the existing `IOptions<T>` pattern (`RoomConfig`, `MessengerConfig`,
`NetworkingConfig`) rather than hardcoded constants — see `Turbo.Revisions.Configuration.ProtocolLimitsConfig`
and `Turbo.Players.Configuration.PlayerPresenceConfig`.

**Priority 3 items closed (2026-07-04):**
- O2/O3/H4 — one shared `Turbo.Logging.Extensions.TaskLoggingExtensions.LogAndForget` (async local
  function, no `async void`, no `ContinueWith(TaskScheduler.Current)`) replaces the three per-grain
  copies (`PlayerPresenceGrain`, `MessengerGrain`; `LtdRaffleGrain`'s was already removed in R7).
  Applied to the room-stream publishes that used to discard the task (`RoomGrain.Map.cs`,
  `RoomAvatarModule.cs` ×7, `RoomRollerSystem.cs`) and to handler fire-and-forgets
  (`MessengerInitMessageHandler.NotifyOnlineAsync`, `SendRoomInviteMessageHandler`'s invite fan-out,
  which is now also parallelized with `Task.WhenAll` instead of discarding each call in the loop).
- O5 — `RoomPetSystem.MovePetAsync` no longer opens a DB write on every pet move; position now
  rides the existing periodic `FlushDirtyPetsAsync` timer-flush alongside stats (same pattern as
  furniture). `RoomGrain.OnDeactivateAsync` now also flushes dirty pets, not just dirty items.
  `PlacePetAsync`/`PickUpPetAsync`/`ConfirmPetBreedingAsync`/`PlantMonsterplantSeedAsync` were left
  as immediate writes — each is a state transition (inventory↔room, new entity creation) a
  subsequent read depends on, and this subsystem has zero test coverage today, so a broader rewrite
  wasn't worth the regression risk in one pass.
- O6 — `MessengerGrain.DeclineFriendRequestsAsync`/`RemoveFriendsAsync` batch their
  `ExecuteDeleteAsync` with a single `Contains(...)`-based `WHERE ... IN` instead of one delete per
  id in a loop; `RemoveFriendsAsync`'s per-friend event publish is now parallelized with
  `Task.WhenAll`. `AcceptFriendRequestsAsync` hoists the self `PlayerEntity` lookup above the loop
  instead of re-fetching it per request. `InventoryGrain.GrantCatalogOfferAsync` hoists the
  presence-grain lookup above its pet-creation loop. The furniture-grant loop's sequential awaits
  were left as-is: `_furniModule.AddFurnitureAsync` capacity-check semantics weren't verified safe
  for concurrent invocation and batch sizes here are small, so parallelizing risked a correctness
  regression for negligible gain.

**Priority 3 items closed (2026-07-04, session 2):**
- D4 — `RoomAvatarTickSystem` reuses one instance-field `List<RoomAvatarSnapshot>` (cleared per
  tick) instead of allocating a fresh list every tick per room; its room-composer send was also
  routed through the shared `LogAndForget` (was a bare `_ =` discard, same O3 pattern fixed
  elsewhere this session).
- O7 — verified already fixed, no change needed: `InventoryFurniModule.LoadFurnitureAsync` calls
  its own module-local `AddFurnitureAsync` (state-only, no notification), not
  `InventoryGrain`'s public `AddFurnitureAsync` (which does call `presence.OnFurnitureAddedAsync`).
  Confirmed `OnFurnitureAddedAsync` has no other call path from hydration.
- O8 — `RoomDirectoryGrain.OnActivateAsync` and `RentableSpaceGrain.ScheduleExpiryTimer` now use
  the `RegisterGrainTimer<TState>(Func<TState, CancellationToken, Task>, ...)` overload (mirroring
  `RoomPersistenceGrain`'s existing correct usage) instead of the state-only overload that forced
  them to capture the stale activation `ct` / pass `CancellationToken.None`. The timer now gets a
  cancellation token tied to its own tick, not the grain's activation.
- D1 — `MarketplaceSearchGrain`: added `AsNoTracking()` to both queries; `GetItemStatsAsync` no
  longer materializes every matching offer row — `CountAsync`/`AverageAsync`/`MinAsync`/`MaxAsync`
  run as four scalar SQL aggregates instead. `GetOffersAsync`'s per-`SpriteId` grouping (avg price,
  cheapest full row) was deliberately left client-side: pushing "cheapest row per group" to SQL via
  `GroupBy` needs a subquery/window-function shape that I could not validate against a live MySQL
  instance in this session (the configured connection string is a placeholder) — shipping an
  unverified query-translation rewrite for a player-facing search endpoint was judged not worth the
  risk versus the `AsNoTracking()` win already taken.
- D2 — `NavigatorProvider.BuildRoomQuery`/`ToRoomInfoSnapshots`: the `Select` into
  `RoomInfoSnapshot` now happens *before* `ToListAsync` (translated by EF into a SQL projection
  with joins only for the `PlayerEntity.Name`/`GroupEntity.Name`/`GroupEntity.Badge` fields
  actually used), replacing the previous `Include(PlayerEntity).Include(GroupEntity)` + full-entity
  `ToListAsync` + client-side `Select`. All six search methods' existing `.Where()` filters were
  left untouched (still filter on raw entity columns, unchanged risk profile) — only the terminal
  projection changed. No paging cap was added: the audit's suggested cap needs a policy decision
  (what size, and whether the client protocol expects a specific page count) that wasn't mine to
  make unilaterally.
- H5 — evaluated, deferred. Removing per-packet allocations means changing `ISerializer`'s
  contract (currently returns a buffer that gets `.ToArray()`'d), `IRc4Engine.Process` (currently
  allocates a new array instead of transforming in place), and `WebSocketSessionContext`'s per-send
  `ArrayBufferWriter` — a change to interfaces used by 284+ serializers, with no byte-level
  round-trip test coverage and no live client to verify against. Judged too broad and unverifiable
  to attempt safely in this session; left as pure follow-up work.

**Priority 4 items closed (2026-07-04, session 3):**
- O10/H3 — `GroupDirectoryGrain`'s `50`/`20` forum page-size cap and `GroupForumGrain`'s identical
  `NormalizeAmount` cap now bind from `GroupConfig.MaxForumPageSize`/`DefaultForumPageSize`
  (`GroupForumGrain` gained `IOptions<GroupConfig>`, matching `GroupDirectoryGrain`'s existing
  pattern). `PlayerInventoryModule`'s 100-item fragment size, `MessengerGrain`'s 10s delivered-flush
  interval, and `PlayerGrain`'s 1h club-maintenance interval now bind from
  `PlayerPresenceConfig`/`MessengerConfig`/`ClubConfig` respectively (all already-injected config
  objects, just unused for these specific constants). Handler side: `MessengerInitMessageHandler`'s
  `FragmentSize`/`UserFriendLimit`/`NormalFriendLimit`/`ExtendedFriendLimit` and
  `HabboSearchMessageHandler`'s `SearchLimit` now bind from a new `FriendListConfig`
  (`Turbo:FriendList`, matching the section AGENTS.md already claimed existed per C3) instead of
  being hardcoded — this is the first real implementation of that documented pattern.
- A4 — Orleans silo endpoint (advertised IP, silo port, gateway port) now binds from
  `Turbo:Orleans` via a new `OrleansHostConfig`, defaults unchanged. Clustering/storage/stream
  *providers* were deliberately left as `UseLocalhostClustering()` + in-memory: picking a real
  production provider (ADO.NET clustering, Redis/SQL storage, etc.) is a deployment-target decision
  I can't make unilaterally without knowing the target infrastructure, and guessing wrong means
  shipping untested provider wiring. Instead, a startup warning now prints to stderr outside
  Development, making the single-node/in-memory limitation visible instead of a silent trap.
- H8 — the five near-identical navigator search handlers (`GuildBaseSearchMessageHandler`,
  `RoomAdSearchMessageHandler`, `GetOfficialRoomsMessageHandler`, `MyFriendsRoomsSearchMessageHandler`,
  `MyRoomRightsSearchMessageHandler`) now delegate to one shared
  `NavigatorSearchHandlerHelper.SendSimpleSearchResultsAsync`; each handler is now just its search
  code constant plus a one-line `HandleAsync`.
- Doc reconciliation — `AGENTS.md`'s "Foundational context" now says SDK `10.0`/`net10.0`/SuperSocket
  `2.1.0` (was `9.0.310`/`net9.0`/`2.0.2`; Orleans/EF/Pomelo versions were already accurate).
  `DATA-MODEL.md`: §2 (Groups), §3 (Rentable Space), §4 (Pets) were still headed "TO CREATE" despite
  being fully implemented with migrations — relabeled "IMPLEMENTED" with a migration pointer each.
  §3.1 now documents `room_rentable_space_terms` keyed on the *placed instance* (`furniture_id`),
  matching `RentableSpaceTermsEntity.cs`, not the type-level key the doc previously claimed. §4.1
  now documents `pet_food`'s actual composite unique index (`furniture_definition_id`, `pet_type`)
  and its `energy`/`max_uses` columns, matching `PetFoodEntity.cs`. §5 (Bots) was already correctly
  labeled "TO CREATE" — verified no `BotEntity`/migration exists, left as-is.
- O9 — scoped before touching anything: `RoomGrain` (2262 l. total) turned out to already be
  well-decomposed into 8 partial files (79–529 l. each) plus composed modules/systems (`MapModule`,
  `AvatarModule`, `PetSystem`, …) — the audit's raw line count summed across partials, not a real
  god-class. No change needed there. `MessengerGrain` (1071 l., one file) and `RoomPetSystem`
  (2117 l., one file, zero test coverage) were the real candidates. Both split into partial files
  by concern — pure reorganization, no logic touched:
  - `MessengerGrain.cs` (lifecycle/hydration/shared helpers) + `.Friends.cs` (friend list/requests/
    blocking/search) + `.Messaging.cs` (instant messages) + `.Presence.cs` (online/offline fan-out).
  - `RoomPetSystem.cs` (loading/tick loop/dirty-flush/shared helpers) + `.Placement.cs`
    (place/move/pick-up) + `.Motion.cs` (wander/decay/feeding AI) + `.Care.cs` (respect/commands/XP)
    + `.Breeding.cs` (breeding/monsterplant).
  Verified line-for-line before/after each split (method/field declaration counts matched exactly)
  and via `dotnet test` on the affected test projects — no regressions.
- .NET 10 migration finished (2026-07-05) — TFM/SDK were already `net10.0`/`10.0`, but Orleans was
  still pinned to the .NET 9-era `9.2.1`. Bumped `Microsoft.Orleans.*` to `10.2.1` (unblocked, no
  Pomelo dependency); this pulled in Roslyn `>= 5.0.0` transitively, conflicting with
  `Microsoft.EntityFrameworkCore.Design` 9.0.8's Roslyn `4.8.0` pin under CPM transitive pinning —
  resolved by adding explicit central `PackageVersion` pins for the five `Microsoft.CodeAnalysis.*`
  packages at `5.6.0`. The Orleans 10 analyzer then flagged 172 `ConfigureAwait(false)` call sites
  in grain code as `ORLEANS0014` (grain continuations must stay on the captured/grain context, not
  escape it) — fixed via `dotnet format analyzers` + `dotnet csharpier format .`; all 99 tests and
  the full `TurboCloudQualityGate` pass clean (0 warnings, 0 errors). `Microsoft.EntityFrameworkCore*`
  and `Pomelo.EntityFrameworkCore.MySql` remain pinned to the 9.x line: Pomelo has no EF Core
  10-compatible release yet (tracked upstream: `PomeloFoundation/Pomelo.EntityFrameworkCore.MySql#2007`).
  EF Core 9 packages run fine on the `net10.0` TFM/runtime, so this is not a build defect — bump both
  together once Pomelo ships. Also fixed remaining doc drift: `AGENTS.md` §Required standards still
  said "Target framework/tooling: .NET 9" despite its own Foundational Context already saying SDK
  `10.0`; `docs/client-server-architecture.md` said ".NET 9/10"; `README.md`'s example
  `DevPluginPaths` pointed at a `net9.0` plugin build output.

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

### P2 — Fill the empty shells — done
**Evidence:** `EventContext` is no longer empty — `Cancel`, `CancelReason`, `CorrelationId`, `Items`
are implemented, and `PublishCancellableAsync` is wired + used in production by `GroupDirectoryGrain`,
with test coverage (`EventSystemTests`, `GroupDirectoryGrainCreationTests`) proving cancellation works.
**Closed (2026-07-04):** shipped a real production `IEventBehavior<GroupCreatingEvent>` —
`Turbo.Players.Events.GroupNameValidationBehavior` rejects guild creation with an empty/whitespace
name or a name over `GroupConfig.MaxNameLength` (default 50). This was a genuine, previously-unguarded
gap: neither `GroupDirectoryGrain.CreateGroupAsync` nor `CreateGuildMessageHandler` validated the
name, and `GroupEntity.Name` has no DB-level length constraint either. Auto-discovered via the
existing assembly-scan mechanism (`PluginBootstrapper` scans every `IHostPluginModule` assembly for
`IEventHandler<>`/`IEventBehavior<>`) — zero changes to `GroupDirectoryGrain` were needed, proving the
seam works end-to-end exactly as designed. Covered by
`GroupNameValidationBehaviorTests` (`Turbo.Rooms.Tests/Events/`).
**Hardcoded `= false` capability flags re-audited:** `AdminOnlyDecoration`/`MembersCanDecorate`
(the "group room decoration" example originally cited) turned out to already be fully wired
(real DB column, read/write in `GroupGrain.cs`) — resolved since the original snapshot, not a bug.
`RoomRatingMessageComposer.CanRate = false` is a genuine stub (room rating was never implemented) but
that is P7 (stub debt/feature work), not a hardening bug. `GroupDirectoryGrain`'s
`CanChangeSettings = false` / `IsStaff = false` in `GetForumsListAsync` is already flagged by an
inline comment as a deliberate simplification ("ForumsList only serializes the base fields").
No undocumented hardcoded-capability bug found.
**Done when:** at least one production `IEventBehavior<T>` exists; no *undocumented* capability is
hardcoded via `false`. ✅

### P3 — Reduce over-modularization — reassessed, no action taken
**Re-verified (2026-07-04):** `Turbo.Contracts` — the one clear violator this section named — no
longer exists as a project at all; it was merged away before this session (28 `.csproj` today,
none named `Contracts`). The other three still-small projects cited
(`Turbo.Events` 231 l., `Turbo.Logging` 365 l., `Turbo.Messages` 357 l.) are not disguised
folders: each is a thin specialization of the shared generic pipeline engine in `Turbo.Pipeline`
(735 l.) — `Turbo.Events` defines `IEventBehavior<T>`/`EventContext`/`EventRegistry`,
`Turbo.Messages` defines `IMessageHandler<T>`/`MessageContext`/`MessageSystem`, both delegating
their actual dispatch/behavior-chain mechanics to `Turbo.Pipeline`. They're small *because* they
delegate, not because they were arbitrarily split off a bigger module. All three are referenced by
essentially every domain project (`Turbo.Rooms`, `Turbo.Players`, `Turbo.Catalog`, …) as
cross-cutting infrastructure — there is no single "logical parent" domain to fold them into without
creating an artificial dependency (e.g. folding `Turbo.Events` into `Turbo.Players` would force
`Turbo.Rooms`/`Turbo.Catalog`/etc. to depend on `Turbo.Players` just for event plumbing).
**Conclusion:** no merge performed. The remaining small projects are legitimate shared-infrastructure
boundaries, not the over-modularization anti-pattern this section originally described.

### P4 — Lock the quality gate — done
**Evidence:** `dotnet csharpier check .` is green repo-wide (3656 files, 0 failures).
**Closed (2026-07-04):** `TurboCloudQualityGate` now runs `dotnet format Turbo.Cloud.sln
style|analyzers --verify-no-changes` (was scoped to `Turbo.Main.csproj` only, so `Turbo.Rooms`,
`Turbo.PacketHandlers`, `Turbo.Players`, etc. — where the actual logic lives — were never gated).
Widening the scope surfaced real, fixable debt that had been invisible until now:
- **69 `CS8618`** in `TurboDbContext.cs` — every `DbSet<T>` property lacked its (harmless, standard
  EF Core) `= null!;` initializer. Added mechanically.
- **96 `CA2007`** ("call ConfigureAwait") across 14 files — every single one turned out to be the
  same unfixable-without-restructuring pattern: `await using` disposal has no `.ConfigureAwait()`
  call site of its own. `CA2007` was *already* disabled per-project for this exact reason
  (`*Grain.cs`, `Grains/**`, `Turbo.Rooms`, `Turbo.Inventory`, `Turbo.Players`) — this is a pure
  Orleans + ASP.NET Core minimal-API backend, so no `SynchronizationContext` is ever captured
  anywhere; the rule provides zero value repo-wide. Finished the trend: `.editorconfig` now disables
  `CA2007` for `[*.cs]` instead of allow-listing project by project.
- **4 `VSTHRD200`** (missing `Async` suffix) — renamed `NavigatorProvider.ToRoomInfoSnapshots` →
  `...Async`, `WebApiEndpointsTests.ReadJson` → `...Async`, `DashboardEndpoints.Ok`/`OkNullable` →
  `...Async` (all call sites updated).
- **3 `VSTHRD003`** ("awaiting a task not started in your context") — the two `DashboardEndpoints`
  helpers and `TaskLoggingExtensions.AwaitAndLogAsync` are *designed* to await a task handed in by
  the caller (that's the fire-and-forget/response-wrapping contract, not a deadlock risk); suppressed
  inline with `#pragma warning disable/restore VSTHRD003` and a one-line justification each.
- **`IDE1006`** (naming, ~1220 instances across 61 files) is the one category left unaddressed:
  genuine pre-existing debt (the project's `ALL_CAPS` const/static-readonly convention, never
  enforced outside `Turbo.Main`), but mass-renaming 1220 identifiers — some public, some Orleans
  `[Id(n)]`-serialized — is a separate, much larger and riskier undertaking than "fix the gate."
  Excluded via `--exclude-diagnostics IDE1006` on both `dotnet format` invocations; tracked here as
  the gate's one remaining known gap rather than silently ignored.
**Done when:** gate covers the whole solution (✅) and every non-naming violation it finds passes
clean; `IDE1006` remains open, tracked debt. Making `pre-push` blocking on the full gate is a CI/hook
change, not something this session touched — left for whoever owns that pipeline.

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

### P6 — Metrics hygiene — done
**Evidence:** `performance_logs` carried legacy client data (`flash_version`) and every connected
client wrote a row periodically — unbounded transactional-DB growth for data nobody queried per-row,
only ever summed as a total.
**Closed (2026-07-05):** per user decision, routed to OTel and dropped the table.
`PerformanceLogMessageHandler` → `IPerformanceLogSink` is now implemented by
`Turbo.Observability.Metrics.ClientPerformanceMetrics` (registered in place of the old
`ChannelPerformanceLogSink`): histograms for elapsed time/memory/frame rate, a counter for GC count,
tagged only by OS/Browser (bounded cardinality, matching `TurboMetrics`'/`ClubMetrics`' documented
convention — never tag by player id). An `ObservableCounter` plus a plain `TotalSamples` property
(same dual-purpose pattern as `ClubMetrics.ActiveSubscribers`) replaces the old `performance_logs`
row-count dashboard stat — it's in-memory since process start, not a durable lifetime total, which is
the accepted tradeoff of moving from "one DB row per event" to "aggregated metric." Removed
`PerformanceLogEntity`, its `DbSet`, `ChannelPerformanceLogSink`, `PerformanceLogRecord`, and
`AuditWriterService`'s DB-mapping for it. Migration `20260704221850_RemovePerformanceLogs` drops the
table — it also folds in one small piece of unrelated pre-existing drift the model diff surfaced
(`FurnitureDefinitionEntity.StuffDataType`'s backing enum was already `byte` in code but the schema
was still `int`; nothing else had migrated that gap), called out with an inline comment in the
migration rather than silently bundled.
**Done when:** transactional DB no longer stores high-volume telemetry. ✅

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
