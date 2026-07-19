# Full Technical Audit — Vortex Cloud (Habbo emulator, C# / .NET / Microsoft Orleans)

**Date:** 2026-07-02
**Branch audited:** `copilot/audit-habbo-emulateur` (HEAD `84c6190`)
**Scope:** existing implementation only — no new-feature proposals. Every finding below was
verified directly in the source; where verification was not possible, this is stated explicitly.
**Normative baseline:** official C#/.NET documentation, official Microsoft Orleans documentation,
Microsoft Framework Design Guidelines, SOLID/DRY/KISS/YAGNI, Clean Code, and recognized practices
for high-concurrency server applications.

Related documents: `CONSOLIDATION.md` (live backlog), `ROADMAP.md`,
`docs/audits/2026-07-02-technical-audit.md` (earlier dated snapshot of fixes shipped on this branch).

---

## 1. Executive summary and scores

| Dimension | Score | Rationale (one line) |
|---|---:|---|
| **Global** | **68 / 100** | Sound modular architecture undermined by silent failure paths, security defaults, thin tests, and doc drift |
| Architecture | 78 / 100 | Acyclic 28-project layout with a clean composition root; coarse coupling to `Vortex.Database` and a "gravity well" `Vortex.Primitives` |
| Code quality | 66 / 100 | Consistent formatting (csharpier green) but 29 bare `catch { }`, several 1000–3500-line files, and duplicated handler orchestration |
| Maintainability | 62 / 100 | 72 tests for ~3 670 files; 10+ production modules with no dedicated test project; oversized grains/services |
| Performance | 70 / 100 | Good use of caching/timer-flush; per-packet allocations, unbounded queues/buffers, N+1 grain/DB loops remain |
| Orleans usage | 72 / 100 | Correct actor-model instincts (no locks, no blocking, per-responsibility grains) but fire-and-forget without observation, `void` grain methods, hot-path DB writes |

**Technical-debt estimate.** Medium-high and *concentrated*: ~80 % of the debt lives in
(1) the wired subsystem (`FurnitureWiredLogic.cs` + `WiredExecutionContext.cs` + selectors — ~29
silent catches, blocked on a DI cascade across 83 leaf classes), (2) five oversized files
(`Revision20260112.cs` 3 459 l., `DashboardApiService.cs` 2 585 l., `RoomPetSystem.cs` 2 110 l.,
`MessengerGrain.cs` 1 089 l., `GroupGrain.cs` 926 l.), (3) protocol completeness debt (284 empty
serializers, ~300 stub handlers per `CONSOLIDATION.md`), and (4) security defaults
(committed secrets, 128-bit DH, disabled ticket TTL). Items (1), (2) and (4) are addressable
without functional change; item (3) is feature work already tracked in `ROADMAP.md`/P7.

**Verified strengths**
- Acyclic project dependency graph across 28 projects; `Vortex.Main` is a genuine composition root
  (`Program.cs`) with ordered registration (Orleans → infrastructure → domain modules).
- No handler source file accesses `TurboDbContext`/repositories (grep-verified) — the
  "orchestration-only handler" boundary holds at the code level.
- No `.Result`/`.Wait()`/`Task.Run`/manual `lock`/`SemaphoreSlim` inside audited grain code; zero
  `async void` repo-wide; ~89 % of async signatures (1 394/1 560) accept a `CancellationToken`.
- Timer-flush persistence pattern (`RoomPersistenceGrain`) exists and is used for furniture writes;
  `[KeepAlive]` is confined to directory grains as documented.
- Purchase-refund invariant is now shared (`WalletPurchaseExtensions.ExecutePurchaseAsync`) and
  regression-tested (`WalletPurchaseExtensionsTests.cs`).
- Plugin system uses collectible `AssemblyLoadContext` with byte-loading and unload polling
  (`Vortex.Runtime/AssemblyProcessing/`).
- DbContext lifetime is factory-based (`AddDbContextFactory<TurboDbContext>`); no scoped-context
  capture in singletons was found.
- `dotnet csharpier check` is green repo-wide; central package management is in place.

---

## 2. Findings — Architecture and organization

### A1. `Vortex.PacketHandlers` carries an unused reference to `Vortex.Database`
1. **File:** `Vortex.PacketHandlers/Vortex.PacketHandlers.csproj` (line 12)
2. **Class:** — (project file)
3. **Method:** —
4. **Problem:** the project references `Vortex.Database` while no handler source uses it.
5. **Impact:** the compiler no longer enforces the documented hard boundary ("no DB access in
   handlers", `CONTEXT.md` §Hard boundaries); a future violation would build silently.
6. **Reference:** dependency-inversion / enforce architecture via assembly boundaries (Microsoft
   architecture guidance on project references).
7. **Severity:** Medium
8. **Improvement:** remove the `ProjectReference`; the boundary becomes compiler-enforced with no
   behavior change.

### A2. Duplicate and conflicting central package versions
1. **File:** `Directory.Packages.props` (lines 43–46 and 50–53)
2–3. — 
4. **Problem:** `Microsoft.NET.Test.Sdk`, `xunit`, `xunit.runner.visualstudio` and
   `FluentAssertions` are each declared twice; `FluentAssertions` pins both `8.10.0` (l. 46) and
   `6.12.2` (l. 53).
5. **Impact:** ambiguous central-package resolution; test behavior can differ across machines/CI.
6. **Reference:** NuGet Central Package Management documentation — one `PackageVersion` per id.
7. **Severity:** Important
8. **Improvement:** deduplicate to a single version per package.

### A3. Toolchain drift: contract says .NET 9, repository builds .NET 10
1. **Files:** `global.json` (SDK `10.0`), all `*.csproj` (`net10.0`), vs `AGENTS.md`
   ("Foundational context: .NET SDK 9.0.310 … net9.0") and `CONTEXT.md`/adapter files.
2–3. —
4. **Problem:** the canonical AI/contributor contract documents a different SDK/TFM than the code.
5. **Impact:** contributor and CI SDK drift; instructions produce patterns validated against the
   wrong runtime docs.
6. **Reference:** `global.json` documentation (pinning SDK for reproducible builds).
7. **Severity:** Medium
8. **Improvement:** update `AGENTS.md`/adapters to the actual `net10.0` toolchain (doc-only change).

### A4. Orleans host is hard-wired to localhost clustering and memory storage
1. **File:** `Vortex.Main/Extensions/HostApplicationBuilderExtensions.cs` (lines 28–33)
2. **Class:** `HostApplicationBuilderExtensions`
3. **Method:** silo configuration
4. **Problem:** `UseLocalhostClustering()` and in-memory streams/storage are unconditional.
5. **Impact:** the host cannot be deployed multi-silo or survive restarts without code changes;
   acceptable for the current single-node scope, but it is a hard limit, not a configuration.
6. **Reference:** Orleans docs — Server configuration / clustering providers.
7. **Severity:** Important (for any production deployment); Low for local development
8. **Improvement:** select clustering/stream/storage providers from `IConfiguration` with the
   current values as defaults (no behavior change in dev).

### A5. `Vortex.Primitives` is a contracts gravity well
1. **File:** `Vortex.Primitives/**` (1 458 files, ~19.6 k lines; `Messages/` alone 1 019 files)
2–3. —
4. **Problem:** every module depends on one very large contracts assembly.
5. **Impact:** build/navigation friction and coupling gravity; any contract touch rebuilds the
   world. Not a layering violation — the split is intentional and coherent.
6. **Reference:** FDG — assembly partitioning by cohesion.
7. **Severity:** Low (monitor; splitting now would violate YAGNI)
8. **Improvement:** none required today; consider splitting `Messages` from grain contracts only if
   build times degrade. `CONSOLIDATION.md` P3 (merging tiny projects) remains the better lever.

### A6. Plugins can resolve any host service
1. **File:** `Vortex.Plugins/HostServices.cs` (lines 11–12)
2. **Class:** `HostServices`
3. **Method:** service resolution surface
4. **Problem:** plugins receive the root `IServiceProvider` surface rather than a scoped capability
   facade.
5. **Impact:** no isolation boundary: a plugin can reach `IDbContextFactory`, crypto services, etc.
6. **Reference:** least-privilege design; FDG on exposing minimal surface area.
7. **Severity:** Medium
8. **Improvement:** define an explicit capability interface for plugins (additive; existing plugins
   keep working through it).

---

## 3. Findings — Robustness and error handling

> Repo-wide metrics (this audit, grep-verified): ~210–219 `catch` occurrences (count varies with
> comment/test inclusion), **29 bare `catch { }` in 14 files**, 72 catch blocks without logging,
> 0 `async void`.

### R1. Wired subsystem swallows exceptions across 14 files
1. **Files:** `Vortex.Rooms/Object/Logic/Furniture/Floor/Wired/FurnitureWiredLogic.cs` (9 bare
   catches), `Vortex.Rooms/Wired/WiredExecutionContext.cs` (l. 50, 93, 141),
   `…/Wired/Selectors/WiredSelectorItemsInNeighborhood.cs` (4), plus 9 more wired files and
   `Vortex.Rooms/Grains/Systems/RoomWiredSystem.cs` (l. 356, `ProcessPendingActionAsync`).
2. **Classes:** `FurnitureWiredLogic`, `WiredExecutionContext`, wired selectors/actions/addons,
   `RoomWiredSystem`
3. **Methods:** rehydration (`Activator.CreateInstance` paths), execution, pending-action flush
4. **Problem:** fully silent `catch { }` around wired rehydration and execution.
5. **Impact:** wired behavior can silently diverge or vanish per room with zero diagnostics —
   directly contradicts the repo's own grain rule "No bare catch {}" (`AGENTS.md`).
6. **Reference:** .NET docs — Best practices for exceptions ("do not catch without handling/logging").
7. **Severity:** Critical (aggregate)
8. **Improvement:** thread `ILogger` through the wired base constructor (the known 83-leaf cascade
   tracked in `CONSOLIDATION.md` P5); as an interim, log via an ambient/static logger factory in the
   catch sites — no functional change either way.

### R2. Plugin hosted-service startup failures are invisible
1. **File:** `Vortex.Plugins/PluginManager.cs` (lines 470–490)
2. **Class:** `PluginManager`
3. **Method:** `StartPluginAsync`
4. **Problem:** `catch { /* bubble via plugin Start */ }` swallows `IHostedService.StartAsync`
   failures.
5. **Impact:** a plugin can appear "started" while its services never ran.
6. **Reference:** .NET exception best practices; generic host `IHostedService` docs.
7. **Severity:** Critical
8. **Improvement:** log the exception and mark the plugin start failed (logging-only change).

### R3. Reloadable-export swap/subscriber failures are silent
1. **File:** `Vortex.Plugins/Exports/ReloadableExport.cs` (lines 25–45, 48–62; 3 bare catches)
2. **Class:** `ReloadableExport`
3. **Methods:** `SwapAsync`, `Subscribe`
4. **Problem:** disposal and subscriber-callback exceptions are swallowed.
5. **Impact:** hot-reload failures (leaked old export instances, dead subscribers) are undetectable.
6. **Reference:** .NET exception best practices.
7. **Severity:** Critical
8. **Improvement:** inject a logger (type is already DI-constructed) and log each failure.

### R4. Wallet debit failure masquerades as "insufficient balance"
1. **File:** `Vortex.Players/Grains/PlayerWalletGrain.cs` (line 90)
2. **Class:** `PlayerWalletGrain`
3. **Method:** `TryDebitAsync`
4. **Problem:** a broad unlogged catch rolls back and returns the insufficient-balance result.
5. **Impact:** DB or logic faults in the money path are indistinguishable from a normal business
   outcome; economy incidents become undiagnosable.
6. **Reference:** .NET exception best practices; repo rule `AGENTS.md` §Never swallow exceptions.
7. **Severity:** Important
8. **Improvement:** `catch (Exception ex)` + `LogError`, preserving the existing rollback/result.

### R5. `MessengerGrain` friend-request accept is non-atomic
1. **File:** `Vortex.Players/Grains/MessengerGrain.cs`
2. **Class:** `MessengerGrain`
3. **Method:** `AcceptFriendRequestsAsync`
4. **Problem:** `ExecuteDeleteAsync` removes the request immediately, before the tracked friendship
   inserts are saved.
5. **Impact:** a failure between delete and insert loses the request without creating the
   friendship — exactly the anti-pattern the repo contract forbids ("use tracked deletes when
   atomicity with inserts is required", `AGENTS.md`).
6. **Reference:** EF Core docs — `ExecuteDelete` bypasses the change tracker and commits immediately.
7. **Severity:** Critical
8. **Improvement:** tracked `Remove` + inserts under one `SaveChangesAsync` (or an explicit
   transaction); identical external behavior on success.

### R6. Marketplace offer marked *Sold* before the inventory grant completes
1. **File:** `Vortex.Marketplace/Grains/MarketplacePurchaseGrain.cs` (lines ~173–187)
2. **Class:** `MarketplacePurchaseGrain`
3. **Method:** `BuyOfferAsync` (grant step passed to `ExecutePurchaseAsync`)
4. **Problem:** the grant step sets `offer.State = Sold` + `CreditsOwed` and `SaveChangesAsync`
   *before* awaiting `GrantFurnitureDefinitionAsync`. If the grant throws, the wallet is refunded by
   the shared helper, but the offer remains persisted as Sold.
5. **Impact:** buyer refunded, seller owed credits, item neither delivered nor re-listed — state
   divergence in the economy.
6. **Reference:** transactional consistency; Orleans docs on grain state + external storage
   coordination.
7. **Severity:** Critical
8. **Improvement:** persist the Sold transition after the grant succeeds, or compensate offer state
   in the same failure path that triggers the refund.

### R7. LTD raffle refunds are fire-and-forget before finalization
1. **File:** `Vortex.Catalog/Grains/LtdRaffleGrain.cs` (lines ~311–325, 408–421)
2. **Class:** `LtdRaffleGrain`
3. **Methods:** `RunRaffleAsync`, `LogAndForgetRefund`
4. **Problem:** non-winner refunds are dispatched with `CancellationToken.None` and never awaited;
   the batch is finalized while refunds may still fail.
5. **Impact:** raffle results can be persisted with refunds silently lost (money path).
6. **Reference:** Orleans best practices — observe grain call results; TAP guidelines.
7. **Severity:** Critical
8. **Improvement:** collect refund tasks and `await Task.WhenAll(...)` with per-failure logging
   before finalizing (same externally visible outcome on success).

### R8. Authentication ticket expiry is disabled
1. **File:** `Vortex.Authentication/AuthenticationService.cs` (lines ~61–81 commented out)
2. **Class:** `AuthenticationService`
3. **Method:** `GetPlayerIdFromTicketAsync`
4. **Problem:** the whole TTL/expiry block is commented out; `_ticketTtlSeconds` (l. 25) is read but
   unused; on success the ticket is even re-inserted for reconnect.
5. **Impact:** SSO tickets are replayable indefinitely (bounded only by IP-lock, if enabled).
6. **Reference:** OWASP session management (bounded token lifetime); Microsoft identity guidance.
7. **Severity:** Important
8. **Improvement:** restore the expiry block (it already exists in-file) — this re-enables
   documented behavior rather than adding features.

### R9. WebApi registration ignores `PasswordRepeated`
1. **File:** `Vortex.WebApi/Http/WebApiRequests.cs` (lines 14–18); endpoint
   `Vortex.WebApi/Hosting/WebApiEndpoints.cs` (lines 118–140)
2. **Class:** `WebApiRequests` / `WebApiEndpoints`
3. **Method:** register endpoint (`MapUser`)
4. **Problem:** the confirmation field is accepted but never compared to `Password`.
5. **Impact:** the client-contract validation the field implies is silently absent.
6. **Reference:** input-validation best practices (ASP.NET Core minimal API validation).
7. **Severity:** Important
8. **Improvement:** enforce `PasswordRepeated == Password` in the existing validation path.

---

## 4. Findings — Security and cryptography

### S1. Diffie-Hellman parameters are 128-bit and randomly generated
1. **File:** `Vortex.Crypto/DiffieService.cs` (lines 11, 24–25)
2. **Class:** `DiffieService`
3. **Method:** constructor
4. **Problem:** `DH_PRIMES_BIT_SIZE = 128`; both "prime" and "generator" are random probable
   primes with no subgroup validation.
5. **Impact:** the negotiated shared secret is within reach of discrete-log attacks; the RC4 session
   keys derived from it inherit the weakness.
6. **Reference:** NIST SP 800-56A / Microsoft crypto guidance (≥ 2048-bit finite-field DH).
7. **Severity:** Critical
8. **Improvement:** adopt a fixed, vetted safe-prime group of adequate size (protocol-compatible
   parameter change, no feature change). If the legacy client caps sizes, document the accepted
   residual risk explicitly.

### S2. Live secrets committed in `appsettings.json`
1. **File:** `appsettings.json`
2–3. —
4. **Problem:** MySQL `root/admin` connection string, `Turbo:Crypto:PrivateKey` (RSA private key),
   `IpHashSecret`, and a dashboard token default of `admin` are all committed.
5. **Impact:** anyone with repo access holds production-shaped credentials and can decrypt/sign the
   handshake; copy-paste deployments ship them verbatim.
6. **Reference:** Microsoft configuration guidance — user-secrets/env/secret stores for secrets;
   never commit private keys.
7. **Severity:** Critical
8. **Improvement:** replace with placeholders + environment/user-secrets binding and rotate the RSA
   key. (`AuthenticationConfig.IpHashSecret` default `"local-dev-ip-hash-secret"` should fail fast
   in non-dev environments.)

### S3. Destructive interpolated SQL in migration uninstall
1. **File:** `Vortex.Database/Migrations/MigrationHelper.cs`
2. **Class:** `MigrationHelper`
3. **Method:** `UninstallAsync`
4. **Problem:** builds `LIKE '{tablePrefix}%'` (and related raw statements) by interpolation; only
   backticks are escaped, not quotes or LIKE wildcards.
5. **Impact:** a malformed/hostile plugin table prefix can widen the destructive scope.
6. **Reference:** EF Core docs — use parameters with `FromSqlRaw`/`ExecuteSqlRaw`; OWASP SQLi.
7. **Severity:** Critical
8. **Improvement:** parameterize the prefix and escape `%`/`_`, keeping identical uninstall
   behavior for valid prefixes.

### S4. Client-controlled loop counts in packet parsers are uncapped
1. **Files:** `Vortex.Revisions/Revision20260112/Parsers/FriendList/RemoveFriendMessageParser.cs`
   (`Parse`), `…/Parsers/RoomSettings/SaveRoomSettingsMessageParser.cs` (`ParseTags`) — pattern
   likely recurs in sibling parsers (not exhaustively verified).
2. **Classes:** respective parsers
3. **Methods:** `Parse` / `ParseTags`
4. **Problem:** list sizes are read from the wire and drive allocation/loops with no maximum.
5. **Impact:** memory/CPU denial-of-service with a single crafted packet.
6. **Reference:** secure deserialization guidance — validate lengths before allocation.
7. **Severity:** Important
8. **Improvement:** clamp counts to sane maxima and reject negatives (rejects only malformed input;
   no legitimate behavior changes).

### S5. Packet framing lacks length bounds
1. **File:** `Vortex.Networking/Package/ClientPacketDecoder.cs`
2. **Class:** `ClientPacketDecoder`
3. **Method:** `TryRead`
4. **Problem:** client-supplied `length` is used without negative/max checks; `length + 4` can
   overflow.
5. **Impact:** malformed frames force exceptions or excessive buffering (DoS vector).
6. **Reference:** SuperSocket pipeline filter guidance; defensive parsing.
7. **Severity:** Important
8. **Improvement:** reject `length < 0`, cap maximum packet size, use checked arithmetic.

### S6. WebSocket receive buffer is unbounded
1. **File:** `Vortex.Networking/Ws/WsPackageHandler.cs`
2. **Class:** `WsPackageHandler`
3. **Method:** `ProcessPackageAsync`
4. **Problem:** partial data accumulates in `ctx.WsBuffer` with no cap; remainder kept via
   `ToArray()`.
5. **Impact:** slow-drip malformed traffic grows memory per connection.
6. **Reference:** high-concurrency server guidance — bound all per-connection buffers.
7. **Severity:** Important
8. **Improvement:** enforce a max buffered-bytes threshold that closes the offending session.

### S7. RC4 without keystream drop
1. **File:** `Vortex.Crypto/Rc4Engine.cs`
2. **Class:** `Rc4Engine`
3. **Method:** constructor (`dropN = 0` default)
4. **Problem:** raw RC4 keystream, no integrity protection.
5. **Impact:** known RC4 early-keystream biases; protocol-constrained, but drop-N is free.
6. **Reference:** RFC 7465 (RC4 deprecation context); crypto agility guidance.
7. **Severity:** Medium (protocol constraint acknowledged)
8. **Improvement:** enable the existing `dropN` parameter at the safest client-compatible value.

---

## 5. Findings — Microsoft Orleans

> Positive verifications: no `[PersistentState]` misuse (persistence is EF-based by design), no
> `.Ignore()` in audited grains, no `.Result`/`.Wait()`/`Task.Run`/locks in audited grain code,
> `[KeepAlive]` restricted to directory grains, snapshots/composers carry
> `[GenerateSerializer]`/`[Id]`. No `[Reentrant]`/`[AlwaysInterleave]` anywhere — no concrete
> call-cycle deadlock was verified, but reentrancy posture is implicit rather than designed.

### O1. `IRoomGrain` exposes `void` grain methods
1. **File:** `Vortex.Primitives/Rooms/Grains/IRoomGrain.cs` (lines 12–13); call sites
   `Vortex.Rooms/Grains/RoomDirectoryGrain.cs` (lines 151, 156)
2. **Class:** `IRoomGrain` / `RoomDirectoryGrain`
3. **Methods:** `DeactivateRoom`, `DelayRoomDeactivation`; caller `CheckRoomsAsync`
4. **Problem:** grain interface methods return `void`.
5. **Impact:** Orleans grain calls must return `Task`/`ValueTask`; `void` methods are one-way calls
   whose failures are unobservable — the directory cannot know a deactivation failed.
6. **Reference:** Orleans docs — grain interface rules (methods must return awaitable types).
7. **Severity:** Important
8. **Improvement:** change signatures to `Task` and await at the two call sites (no behavior change).

### O2. Fire-and-forget continuations rely on `TaskScheduler.Current`
1. **Files:** `Vortex.Players/Grains/PlayerPresenceGrain.cs` (l. 170),
   `Vortex.Players/Grains/MessengerGrain.cs` (l. 1041), `Vortex.Catalog/Grains/LtdRaffleGrain.cs`
   (l. 420)
2. **Classes:** respective grains
3. **Method:** local `LogAndForget` helpers
4. **Problem:** `ContinueWith(..., TaskScheduler.Current)` inside grain context.
5. **Impact:** fault logging depends on the ambient scheduler; behavior under the Orleans
   `TaskScheduler` is subtle and easy to break.
6. **Reference:** Orleans docs — external tasks and grains (scheduler semantics); TAP guidelines
   (prefer `async`/`await` over `ContinueWith`).
7. **Severity:** Medium
8. **Improvement:** one shared `LogAndForget` extension implemented with an async local function
   (`try { await t; } catch (Exception ex) { log; }`) — repo contract already calls for this helper.

### O3. Room stream publishes discarded without observation
1. **Files:** `Vortex.Rooms/Grains/RoomGrain.Map.cs` (l. 51, 61),
   `Vortex.Rooms/Grains/Modules/RoomAvatarModule.cs` (l. 314, 347, 371, 398, 420, 461, 481),
   `Vortex.Rooms/Grains/Systems/RoomRollerSystem.cs` (l. 314)
2–3. `FlushDirtyTilesAsync`, avatar mutators, roller broadcast
4. **Problem:** `_ = SendComposerToRoomAsync(...)` discards the task.
5. **Impact:** stream publish failures (broken subscribers, provider faults) are invisible;
   contradicts the repo's own `.Ignore()`→`LogAndForget` rule in spirit.
6. **Reference:** Orleans streams docs — handle publish/subscribe failures.
7. **Severity:** Medium
8. **Improvement:** route through the shared logged fire-and-forget helper (O2).

### O4. `PlayerPresenceGrain` outgoing queue is unbounded and stream errors are dropped
1. **File:** `Vortex.Players/Grains/PlayerPresenceGrain.cs` (l. 28, 96–157, 72–75, 128–132) and
   `PlayerPresenceGrain.Room.cs` (l. 65–72, 139–144)
2. **Class:** `PlayerPresenceGrain`
3. **Methods:** `SendComposerAsync`, `ProcessOutgoingQueueAsync`, `OnErrorAsync`,
   `OnDeactivateAsync`
4. **Problem:** (a) `_outgoingQueue` (`Queue<IComposer>`) has no cap; (b) `OnErrorAsync` returns
   `Task.CompletedTask` without logging; (c) room-stream subscription is released only in
   `ClearActiveRoomAsync`, not on deactivation.
5. **Impact:** slow/disconnected observers grow memory; stream faults invisible; possible dangling
   subscriptions — violates the repo rule "cap in-memory per-event collections".
6. **Reference:** Orleans streams — explicit subscription lifecycle; high-concurrency guidance on
   bounded queues.
7. **Severity:** Important
8. **Improvement:** configurable queue cap with drop-oldest policy + logging; log `OnErrorAsync`;
   unsubscribe in `OnDeactivateAsync` when a subscription is active.

### O5. Pet operations write to the DB inside room hot paths
1. **File:** `Vortex.Rooms/Grains/Systems/RoomPetSystem.cs` (l. 288, 334, 389, 1549, 2063)
2. **Class:** `RoomPetSystem`
3. **Methods:** `PlacePetAsync`, `MovePetAsync`, `PickUpPetAsync`, `ConfirmPetBreedingAsync`,
   `PlantMonsterplantSeedAsync`
4. **Problem:** immediate `SaveChangesAsync` on common pet actions, inside the room grain's turn.
5. **Impact:** room turn blocks on DB I/O; inconsistent with the documented furniture timer-flush
   pattern (`RoomPersistenceGrain`).
6. **Reference:** Orleans best practices — keep grain turns short; repo rule "timer-flush for
   housekeeping writes".
7. **Severity:** Important
8. **Improvement:** queue pet-position/state dirties into the existing persistence-grain pattern;
   keep immediate writes only where a subsequent read depends on them.

### O6. Sequential grain/DB calls in loops
1. **Files:** `Vortex.Players/Grains/MessengerGrain.cs` — `DeclineFriendRequestsAsync`
   (l. 359–372), `RemoveFriendsAsync` (l. 384–398): per-id `ExecuteDeleteAsync` in loops;
   `AcceptFriendRequestsAsync`: per-request `FindAsync` incl. repeated self-lookup.
   `Vortex.Inventory/Grains/InventoryGrain.Furni.cs` — `GrantCatalogOfferAsync` (l. 200–259):
   sequential awaits and a repeated presence-grain lookup inside the loop.
2–3. as listed
4. **Problem:** O(n) sequential round-trips for independent operations; repeated identical lookups.
5. **Impact:** batch operations scale linearly in wall time; violates two explicit repo grain rules
   (`Task.WhenAll` for independent calls; batched `WHERE IN` deletes; hoist repeated calls).
6. **Reference:** Orleans best practices — parallelize independent grain calls; EF Core batching.
7. **Severity:** Important
8. **Improvement:** batch deletes with `Contains(...)`; hoist the self/presence lookups; group
   independent notifications with `Task.WhenAll` where ordering is not semantically required.

### O7. Inventory hydration emits "item added" notifications
1. **File:** `Vortex.Inventory/Grains/Modules/InventoryFurniModule.cs` (l. 89–91)
2. **Class:** `InventoryFurniModule`
3. **Method:** `LoadFurnitureAsync`
4. **Problem:** hydration reuses `AddFurnitureAsync`, which triggers presence notifications.
5. **Impact:** loading an existing inventory can fan out spurious "added" composers.
6. **Reference:** Orleans lifecycle docs — activation should rebuild state, not replay side effects.
7. **Severity:** Medium
8. **Improvement:** internal load path that populates state without the outbound notification.

### O8. Timer callbacks capture the wrong cancellation token
1. **Files:** `Vortex.Rooms/Grains/RoomDirectoryGrain.cs` (l. 37, `OnActivateAsync`/`CheckRoomsAsync`),
   `Vortex.Players/Grains/RentableSpaceGrain.cs` (l. 557–559, `ScheduleExpiryTimer`)
2–3. as listed
4. **Problem:** timer callbacks capture the activation `ct` or pass `CancellationToken.None`
   instead of using the token supplied to the timer callback.
5. **Impact:** timer work cannot observe deactivation/cancellation correctly.
6. **Reference:** Orleans docs — `RegisterGrainTimer` callback token semantics.
7. **Severity:** Medium
8. **Improvement:** thread the timer-callback token through.

### O9. God-grain scale: `RoomGrain`, `RoomPetSystem`, `MessengerGrain`
1. **Files:** `Vortex.Rooms/Grains/RoomGrain.cs` + partials (~2 246 l. incl. module wiring at
   l. 52–66), `Vortex.Rooms/Grains/Systems/RoomPetSystem.cs` (2 110 l.),
   `Vortex.Players/Grains/MessengerGrain.cs` (1 089 l.), `Vortex.Players/Grains/GroupGrain.cs`
   (926 l.), `Vortex.Players/Grains/GroupForumGrain.cs` (743 l.)
2–3. class-level
4. **Problem:** single classes accumulate placement, movement, breeding, stats, persistence,
   composer fan-out, etc.
5. **Impact:** high change risk, hard to test, single grain turn serializes unrelated features.
6. **Reference:** SOLID (SRP); Orleans "one grain per responsibility" — also the repo's own rule.
7. **Severity:** Important
8. **Improvement:** extract cohesive internal services/modules (pattern already exists:
   `RoomGrain` modules) without changing grain interfaces.

### O10. Hardcoded limits and intervals in grains
1. **Files:** `Vortex.Players/Grains/GroupDirectoryGrain.cs` (l. 373 — `50`),
   `GroupForumGrain.cs` (l. 722 — `20`), `Modules/PlayerInventoryModule.cs` (l. 30 — fragment
   `100`), `MessengerGrain.cs` (l. 890–891 — 10 s flush), `PlayerGrain.cs` (l. 359–360 — 1 h timer)
2–3. as listed
4. **Problem:** paging sizes, fragment sizes and timer intervals are compile-time constants.
5. **Impact:** deployment tuning requires code changes; contradicts the repo rule "no hardcoded
   limits in grains".
6. **Reference:** .NET Options pattern documentation.
7. **Severity:** Low
8. **Improvement:** bind from `IConfiguration`, defaulting to current values.

---

## 6. Findings — Handlers, networking, protocol

### H1. Handlers send directly to the session via `MessageContext`
1. **File:** `Vortex.Messages/Registry/MessageContext.cs` (l. 33–34); representative callers:
   `Vortex.PacketHandlers/FriendList/MessengerInitMessageHandler.cs` (l. 44–75),
   `HabboSearchMessageHandler.cs` (l. 60–68),
   `Vortex.PacketHandlers/Users/ApproveAllMembershipRequestsMessageHandler.cs` (l. 34–44)
2. **Class:** `MessageContext` + many handlers
3. **Method:** `SendComposerAsync`
4. **Problem:** `MessageContext.SendComposerAsync` wraps `_session.SendComposerAsync` directly and
   is used broadly for player-targeted output.
5. **Impact:** documented rule says player outbound routes through
   `PlayerPresenceGrain.SendComposerAsync` (`CONTEXT.md` §Session and room runtime flow). Note:
   replying on the *requesting* session to its own request is arguably legitimate; the doc does not
   distinguish request-reply from player-targeted push. This is as much a documentation-precision
   gap as a code gap.
6. **Reference:** internal contract (`AGENTS.md`, `CONTEXT.md`).
7. **Severity:** Important (as a doc/code coherence issue)
8. **Improvement:** either document the request-reply exception explicitly, or back
   `MessageContext.SendComposerAsync` by the presence grain. Pick one; today the rule is ambiguous
   and violated on its literal reading.

### H2. Business rules embedded in handlers
1. **Files:** `Vortex.PacketHandlers/Users/ApproveNameMessageHandler.cs` (`Validate` — name
   length/regex policy), `Vortex.PacketHandlers/Room/Pets/GetPetInfoMessageHandler.cs`
   (`HandleAsync` — monster-plant type `16`, max wellbeing `86_400`, remaining-wellbeing math)
2–3. as listed
4. **Problem:** domain policy lives at the transport layer.
5. **Impact:** violates "orchestration-only handlers"; policy changes require handler edits and can
   drift from grain-side behavior.
6. **Reference:** internal contract; SRP.
7. **Severity:** Medium
8. **Improvement:** move the policy into the owning grain/service and have the handler consume a
   computed snapshot.

### H3. Hardcoded limits in handlers
1. **Files:** `Vortex.PacketHandlers/FriendList/MessengerInitMessageHandler.cs` (`FragmentSize=500`,
   `UserFriendLimit=300`, `NormalFriendLimit=300`, `ExtendedFriendLimit=2000`),
   `HabboSearchMessageHandler.cs` (`SearchLimit=30`)
2–3. `HandleAsync`
4. **Problem:** limits the contract says must come from `IConfiguration` are constants.
5. **Impact:** no deployment tuning; direct divergence from `AGENTS.md` ("Handlers already read
   configuration values (e.g. Turbo:FriendList:UserFriendLimit)") — the documented config key is
   **not** actually read here.
6. **Reference:** internal contract; Options pattern.
7. **Severity:** Medium (plus doc divergence, see §8)
8. **Improvement:** bind from configuration with current values as defaults.

### H4. Unobserved fire-and-forget in handlers
1. **Files:** `MessengerInitMessageHandler.cs` (`_ = grain.NotifyOnlineAsync(ct);`),
   `SendRoomInviteMessageHandler.cs` (invite fan-out loop discarding results)
2–3. `HandleAsync`
4. **Problem:** grain calls discarded without a logging helper; invite fan-out also unbounded.
5. **Impact:** online-notify/invite failures vanish.
6. **Reference:** repo `LogAndForget` rule; TAP guidelines.
7. **Severity:** Medium
8. **Improvement:** shared logged fire-and-forget helper (see O2) + `Task.WhenAll` for fan-out.

### H5. Per-packet allocations on both directions
1. **Files:** `Vortex.Networking/Package/ClientPacketDecoder.cs` (`TryRead` — `hdr.ToArray()`, full
   packet `ToArray()`), `Vortex.Networking/Package/PackageEncoder.cs` (`Encode` —
   `Serialize(...).ToArray()` + encryption copy), `Vortex.Networking/Ws/WebSocketSessionContext.cs`
   (`SendComposerAsync` — new `ArrayBufferWriter` + `ToArray()` per send; also logs all send
   failures at Debug only)
2–3. as listed
4. **Problem:** every packet allocates one or more transient arrays.
5. **Impact:** GC pressure scales with traffic; broadcast storms multiply it.
6. **Reference:** .NET performance guidance — `Span<T>`, `IBufferWriter<byte>`, `ArrayPool<T>`.
7. **Severity:** Important
8. **Improvement:** span/pool-based encode/decode paths (pure optimization, no behavior change);
   raise unexpected WS send failures to Warning.

### H6. Session↔player bidirectional map updates are non-atomic
1. **File:** `Vortex.Networking/Session/SessionGateway.cs` (l. 96–143)
2. **Class:** `SessionGateway`
3. **Methods:** `AddSessionToPlayerAsync`, `RemovePlayerSessionAsync`
4. **Problem:** `_sessionToPlayer` and `_playerToSession` are updated with separate concurrent
   dictionary operations; reassignment also passes `CancellationToken.None` twice.
5. **Impact:** interleavings can strand stale/orphan mappings (ghost sessions).
6. **Reference:** high-concurrency guidance — encapsulate multi-map invariants under one
   synchronization scope.
7. **Severity:** Important
8. **Improvement:** encapsulate the pair behind a small lock or single-writer structure; thread the
   caller's token.

### H7. 284 registered serializers have empty bodies; placeholder payload fields
1. **Files:** representative
   `Vortex.Revisions/Revision20260112/Serializers/Competition/CompetitionVotingInfoMessageComposerSerializer.cs`
   (`Serialize` body is a lone `//`; 284 serializers match this pattern),
   `…/Serializers/Handshake/UserObjectMessageSerializer.cs` (`Serialize` writes hardcoded `0` for
   respect totals / pet-respect remaining)
2–3. as listed
4. **Problem:** composers are registered but emit empty/placeholder packets.
5. **Impact:** features fail silently client-side; contradicts the contract rule against
   placeholder payload writes. Known stub debt (tracked as P7) — flagged here because *registered*
   empty serializers are worse than absent ones: they mask the gap.
6. **Reference:** internal contract (`AGENTS.md` — no placeholder constants).
7. **Severity:** Important (state honesty), acknowledging P7 tracks it
8. **Improvement:** unregister unimplemented composers or emit a one-time warning log on first use.

### H8. Navigator search handlers duplicate orchestration
1. **Files:** `Vortex.PacketHandlers/Navigator/GuildBaseSearchMessageHandler.cs`,
   `RoomAdSearchMessageHandler.cs`, `GetOfficialRoomsMessageHandler.cs`,
   `MyFriendsRoomsSearchMessageHandler.cs`, `MyRoomRightsSearchMessageHandler.cs`
2–3. `HandleAsync` in each
4. **Problem:** near-identical search/compose flows copy-pasted.
5. **Impact:** DRY violation; fixes must be applied N times.
6. **Reference:** DRY / Clean Code.
7. **Severity:** Medium
8. **Improvement:** shared private helper for navigator search responses.

---

## 7. Findings — Data access and performance

### D1. Marketplace search loads and aggregates in memory
1. **File:** `Vortex.Marketplace/Grains/MarketplaceSearchGrain.cs`
2. **Class:** `MarketplaceSearchGrain`
3. **Methods:** `GetOffersAsync`, `GetItemStatsAsync`
4. **Problem:** tracked full-entity loads without `AsNoTracking`; grouping/sorting/avg/min/max done
   client-side after materializing all matching offers.
5. **Impact:** memory/CPU cost grows with marketplace volume on every search/stat request.
6. **Reference:** EF Core docs — no-tracking queries for read-only paths; server-side aggregation.
7. **Severity:** Important
8. **Improvement:** `AsNoTracking` + SQL-side projection/`GroupBy` aggregates (same results).

### D2. Navigator loads full entities with no paging cap
1. **File:** `Vortex.Navigator/NavigatorProvider.cs`
2. **Class:** `NavigatorProvider`
3. **Methods:** `GetAllRoomsAsync`, `GetRoomsBy*Async`, `ToRoomInfoSnapshots`
4. **Problem:** includes owner/group entities and materializes all matches before mapping.
5. **Impact:** navigator queries can pull large room sets into memory.
6. **Reference:** EF Core projection guidance.
7. **Severity:** Medium
8. **Improvement:** project directly to snapshot shape and cap/paginate.

### D3. `GroupForumGrain.PostAsync` saves twice
1. **File:** `Vortex.Players/Grains/GroupForumGrain.cs` (l. 228, 243)
2. **Class:** `GroupForumGrain`
3. **Method:** `PostAsync`
4. **Problem:** thread saved, then post saved in a second `SaveChangesAsync`.
5. **Impact:** partial-persistence window + extra round-trip.
6. **Reference:** EF Core relationship fixup / unit-of-work.
7. **Severity:** Medium
8. **Improvement:** attach post via navigation and save once.

### D4. Hot-path allocation in the avatar tick
1. **File:** `Vortex.Rooms/Grains/Systems/RoomAvatarTickSystem.cs` (l. 22–87)
2. **Class:** `RoomAvatarTickSystem`
3. **Method:** `ProcessAvatarsAsync`
4. **Problem:** a fresh `List<RoomAvatarSnapshot>` per tick per room.
5. **Impact:** steady GC churn proportional to room count × tick rate.
6. **Reference:** .NET performance guidance (reuse buffers on hot paths).
7. **Severity:** Medium
8. **Improvement:** reuse a cleared instance field (grain is single-threaded — safe).

### D5. Observability aggregation queues are time- but not count-bounded
1. **File:** `Vortex.Observability/Runtime/LiveStatsAggregator.cs` (l. 12–22, 145–230)
2. **Class:** `LiveStatsAggregator`
3–4. sample queues are trimmed by time window only.
5. **Impact:** a burst inside the window grows memory without cap.
6. **Reference:** repo rule "cap in-memory per-event collections".
7. **Severity:** Medium
8. **Improvement:** add max-sample caps (drop-oldest) alongside the time trim.

### D6. Sequential provider reloads at startup
1. **Files:** `Vortex.Main/TurboEmulator.cs` (`StartAsync`, l. 58–68)
2–3. as listed
4. **Problem:** independent cache reloads run sequentially after their real dependencies.
5. **Impact:** startup latency is the sum, not the max.
6. **Reference:** TAP composition (`Task.WhenAll`).
7. **Severity:** Low
8. **Improvement:** group independent reloads with `Task.WhenAll`.

### D7. Plugin table-prefix computation is dead code
1. **File:** `Vortex.Database/Extensions/ServiceCollectionExtensions.cs`
2. **Class:** `ServiceCollectionExtensions`
3. **Method:** `AddPluginTablePrefix`
4. **Problem:** a fallback prefix is computed (and would be wrong anyway —
   `.Select(...).ToString()` on the enumerable, not a join) but the method returns
   `manifest.TablePrefix ?? string.Empty`, ignoring both the computed value and
   `ExplicitlyNoTablePrefix`.
5. **Impact:** plugins without an explicit prefix create unprefixed tables → schema collisions.
6. **Reference:** — (logic bug).
7. **Severity:** Important
8. **Improvement:** return the finalized local prefix; build the fallback with `string.Concat`.

### D8. `pets.sql` at repository root bypasses EF migrations
1. **File:** `pets.sql`
2–3. —
4. **Problem:** legacy MyISAM DDL/data (`DROP TABLE`, `pet_actions`, `pet_breeding_races`) with no
   code references and no EF representation.
5. **Impact:** if executed, creates schema that migrations don't know about; ambiguity about the
   schema source of truth.
6. **Reference:** EF Core migrations as single schema authority.
7. **Severity:** Important
8. **Improvement:** move to a clearly-labeled reference folder or convert needed seed data into
   migrations; document either way.

### D9. `PetEntity` breeding lineage has no referential integrity
1. **File:** `Vortex.Database/Entities/Pets/PetEntity.cs`
2. **Class:** `PetEntity`
3. **Properties:** `ParentOneId`, `ParentTwoId`
4. **Problem:** plain nullable ints, no FK/navigation.
5. **Impact:** lineage can reference deleted pets.
6. **Reference:** EF Core relationships documentation.
7. **Severity:** Medium
8. **Improvement:** optional self-referencing FKs (`SetNull` on delete) — schema-only change.

---

## 8. Coherence with internal documentation (required §6 check)

Divergences verified between the internal docs and the code as of `84c6190`:

| # | Document (claim) | Observed in code | Assessment |
|---|---|---|---|
| C1 | `CONSOLIDATION.md` (P5): "0 silent swallows outside `Vortex.Rooms`; one tracked gap in `FurnitureWiredLogic.cs`" | **29 bare `catch { }` across 14 files**, including 3 in `Vortex.Plugins/Exports/ReloadableExport.cs` (outside `Vortex.Rooms`) and 13 in wired files *other than* `FurnitureWiredLogic.cs` (`WiredExecutionContext.cs`, `WiredSelectorItemsInNeighborhood.cs`, `RoomWiredSystem.cs`, …) | Doc materially understates the gap; update P5 scope |
| C2 | `AGENTS.md`: ".NET SDK 9.0.310 … net9.0", Orleans 9.2.1, EF 9.0.8 | `global.json` pins SDK `10.0`; all projects target `net10.0` (package versions match) | TFM/SDK statement outdated |
| C3 | `AGENTS.md`: "Handlers already read configuration values (e.g. `Turbo:FriendList:UserFriendLimit`) and pass them to grains" | `MessengerInitMessageHandler.cs` hardcodes `UserFriendLimit = 300` etc.; the documented key is not read | Documented pattern not implemented in the flagship example |
| C4 | `CONTEXT.md`: player outbound must go through `PlayerPresenceGrain.SendComposerAsync` | `MessageContext.SendComposerAsync` sends directly to the session and is used pervasively for request replies | Rule ambiguous (request-reply vs push) and violated on literal reading — clarify doc or change code (H1) |
| C5 | `AGENTS.md`: "Replace `.Ignore()` with a `LogAndForget` helper" | No `.Ignore()` remains (good), but the three existing `LogAndForget` implementations are per-grain copies using `TaskScheduler.Current`, and many sites use raw `_ =` discards instead | Rule partially applied; helper not shared |
| C6 | `DATA-MODEL.md`: `rentable_space_terms` keyed 1:1 on `furniture_definition_id` | Code/migration implement `room_rentable_space_terms` 1:1 with the *placed* `furniture_id` (`RentableSpaceTermsEntity.cs`) | Type-config vs instance-config disagreement |
| C7 | `DATA-MODEL.md`: pet food = unique index + `nutrition` only | `PetFoodEntity.cs` adds `energy`, `max_uses` with composite unique key | Spec drift |
| C8 | `DATA-MODEL.md` §5: `bots` / `bot_messages` tables | No `BotEntity`/`BotMessageEntity`/`DbSet`/migration exists | Documented tables not implemented |
| C9 | `CONSOLIDATION.md` audit snapshot: "Handlers 501", "catch blocks 167" | 502 handler files; ~210–219 catch occurrences (methodology-dependent) | Minor metric drift — snapshot is stale, as its own header warns |

Where the audit could **not** conclude: whether the `MessageContext` direct-send pattern is an
accepted design decision (no ADR found) — flagged as C4/H1 rather than assumed; and whether any
parser beyond the two cited has uncapped loops (pattern sampled, not exhaustively enumerated).

---

## 9. Improvement axes by priority

**Priority 1 — Critical (correctness & security, no functional change required)**
1. Economy atomicity: marketplace Sold-before-grant (R6), messenger accept non-atomic delete (R5),
   raffle fire-and-forget refunds (R7).
2. Secrets & crypto: remove committed RSA key/credentials and rotate (S2), replace 128-bit DH
   parameters (S1), parameterize migration uninstall SQL (S3).
3. Silence in failure paths: plugin start (R2), reloadable exports (R3), wallet debit (R4), wired
   subsystem logging plan (R1 — the 83-leaf DI cascade tracked in P5 needs to actually start).

**Priority 2 — Important (robustness & protocol hardening)**
4. Re-enable ticket TTL (R8); enforce password confirmation (R9).
5. Bound everything client-controlled: packet length caps (S5), parser loop caps (S4), WS buffer cap
   (S6), presence outgoing-queue cap (O4).
6. Fix `IRoomGrain` `void` methods (O1) and the session-gateway dual-map race (H6).
7. Deduplicate `Directory.Packages.props` (A2); fix plugin table-prefix return bug (D7); decide the
   fate of `pets.sql` (D8).

**Priority 3 — Medium (performance & Orleans hygiene)**
8. One shared `LogAndForget` (O2) applied to stream sends (O3) and handler discards (H4).
9. Batch/parallelize loops (O6); move pet writes to timer-flush (O5); marketplace/navigator query
   projection (D1, D2); span/pool packet paths (H5); avatar-tick list reuse (D4).
10. Timer-token fixes (O8); inventory-hydration notification suppression (O7).

**Priority 4 — Low / continuous**
11. Documentation reconciliation: fix all §8 divergences (C1–C9) — cheap, high trust value.
12. Configuration-driven limits in grains (O10) and handlers (H3); Orleans providers from config (A4).
13. Decompose god files (O9, `DashboardApiService.cs`, `Revision20260112.cs` registration into
    partials per domain); navigator handler dedup (H8); grow test coverage into the 10 untested
    modules.

---

## 10. Priority refactor list (files)

1. `Vortex.Players/Grains/MessengerGrain.cs` — atomicity bug (R5) + batched deletes (O6) + size (1 089 l.)
2. `Vortex.Marketplace/Grains/MarketplacePurchaseGrain.cs` — Sold-before-grant consistency (R6)
3. `Vortex.Catalog/Grains/LtdRaffleGrain.cs` — refund fire-and-forget (R7)
4. `Vortex.Crypto/DiffieService.cs` — 128-bit DH (S1)
5. `appsettings.json` + `Vortex.Database/Migrations/MigrationHelper.cs` — secrets (S2) / SQL safety (S3)
6. `Vortex.Rooms/Object/Logic/Furniture/Floor/Wired/FurnitureWiredLogic.cs` + `Vortex.Rooms/Wired/WiredExecutionContext.cs` — silent-catch epicenter (R1)
7. `Vortex.Plugins/PluginManager.cs` + `Vortex.Plugins/Exports/ReloadableExport.cs` — silent lifecycle failures (R2, R3)
8. `Vortex.Players/Grains/PlayerPresenceGrain.cs` — unbounded queue, stream errors, subscription lifecycle (O4)
9. `Vortex.Networking/Package/ClientPacketDecoder.cs` + `Ws/WsPackageHandler.cs` + `Session/SessionGateway.cs` — framing caps, buffer caps, mapping race (S5, S6, H6)
10. `Vortex.Rooms/Grains/Systems/RoomPetSystem.cs` — 2 110 l., hot-path DB writes (O5, O9)
11. `Vortex.Dashboard.API/Api/DashboardApiService.cs` — 2 585 l. service (split by concern)
12. `Vortex.Authentication/AuthenticationService.cs` — re-enable TTL (R8)

---

*Audit performed strictly on the existing implementation. All file/line references verified against
commit `84c6190`. Points where evidence was insufficient are marked as such rather than assumed.*
