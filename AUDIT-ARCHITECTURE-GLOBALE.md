# Vortex Cloud — Global Architecture Audit

Audited at HEAD (`7693874`, 2026-08-23), fresh clone. ~300k lines of C# across 30 runtime
projects + 15 test projects. Method: measured the real dependency graph from csproj files,
counted real usage (not declared usage), read the composition root, the dispatch pipeline, the
event system, the four economy flows, the cache/invalidation paths, the Orleans configuration,
and the admin surfaces; compared against the official Orleans guidance and current .NET
practice where relevant. Prior audits (`docs/audits/2026-07-02-full-technical-audit.md`,
`CONSOLIDATION.md`) were read so this audit does not re-litigate what is already fixed.

---

## 0. General diagnostic

**This is a disciplined modular monolith on Orleans in unusually good health for its size —
and its five real problems are not the ones a codebase this large usually has.** There is no
big ball of mud: handlers are thin, state has single writers, protocol is isolated per
revision, pure logic is extracted and tested, decisions are written down, and previous audit
findings were actually absorbed (grain error strategy, rate limiting, security fixes, the
wired-trading extraction). The house patterns are good and — crucially — they are *applied*.

The dangers are subtler, and they are dangers of scale and of success:

1. **The economy is consistent by artisanry, not by construction.** Four money-moving flows
   (catalog purchase, marketplace, player trade, wired settlement) each hand-build their own
   debit/compensate choreography around one good shared primitive that only some of them use —
   and the three modules that move the most money (`Catalog`, `Marketplace`, `Inventory`) have
   **zero tests**.
2. **The deployment thesis is ambiguous.** The configuration supports multi-silo (ADO.NET
   clustering); the semantics assume one silo (silo-local caches with local-only reload, a
   silo-local event bus, silo-local metrics aggregation, memory streams). Today this is
   harmless; the day a second silo starts, it becomes a family of silent-staleness bugs.
3. **`Vortex.Primitives` is becoming the god-hub**, and wire types have started leaking into
   grain contracts, welding protocol churn (1,092 message files) to every project's build.
4. **`Vortex.Players` is a grab-bag domain** (36 grains: players, groups, forums, messenger,
   quests, NFT, polls, guides, community goals, rentals) heading where `RoomGrain` was before
   its Systems extraction.
5. **The boundaries live in prose.** `CONTEXT.md` states excellent rules; nothing enforces
   them, and the stale project references prove the drift has already begun.

None of these requires a rewrite. All five are addressable incrementally, and the roadmap in
§8 orders them so the project stays shippable throughout.

---

## 1. The architecture as it actually is

### 1.1 Module inventory (measured)

| Project | LOC | Role |
|---|---|---|
| Vortex.Rooms | 51,346 | Room engine: RoomGrain (5,964 across partials) + 15 Modules + ~40 Systems (12,653) + ~105 wired logics |
| Vortex.Primitives | 36,546 | The hub: ids, enums, snapshots, grain interfaces, events, **and 1,092 message files** |
| Vortex.Database | 36,312 | EF Core: 146 entities, 127 migrations, one 584-line `VortexDbContext` |
| Vortex.Revisions | 26,468 | Protocol codecs for the embedded revision (parsers/serializers per message) |
| Vortex.PacketHandlers | 25,058 | 536 handler files; thin orchestration; markers for `NotImplemented`/stub returns: **0 found** |
| Vortex.Players | 23,832 | 36 grains — players *and* groups/forum/messenger/quests/NFT/polls/guides/goals/rentals |
| Vortex.Dashboard.API (+Web) | 22,129 | In-proc admin: `DashboardApiService` (DB reads) + `DashboardOperationsService` (31 files, **0 direct writes — mutations go through grains**) |
| Catalog / Marketplace / Inventory / Furniture / Navigator / Authentication | 3,694 / — / 1,622 / 1,228 / 2,219 / 1,564 | Domain modules with grains + silo-singleton services |
| Networking / Messages / Pipeline / Runtime / Events | 1,638 / — / 752 / 757 / — | SuperSocket TCP+WS, message registry + rate limits, envelope dispatch, reflection helpers, in-proc event bus |
| Observability | 5,318 | Audit sinks (14 `IEventHandler<…>` audit handlers), live stats, incident detection, metrics |
| WebApi / Supervisor / Plugins / Specs* | 1,999 / 1,056 / 1,832 / ~16k | SSO+registration API; process watchdog; `AssemblyLoadContext` plugin host with hot reload; Roslyn-driven spec generator (dev-only) |

Tests: 1,674 cases. Rooms 704, Players 252, Revisions 239, Specs 128, Database 91, Hosting 87,
misc ~170. **Zero test projects for Catalog, Marketplace, Inventory, Furniture, Networking,
Messages, Events, PacketHandlers.**

### 1.2 Dependency graph — declared vs real

Declared (csproj, simplified):

```text
Vortex.Main (composition root)
 ├── Networking ── Messages ── Pipeline ── Runtime          } 4-project dispatch infra
 ├── PacketHandlers ──► Rooms, Catalog, Furniture, Auth, …  } declared
 ├── Revisions ──► Players, Furniture                        } declared
 ├── Rooms ──► Players                                       } declared
 ├── Rooms | Players | Catalog | Marketplace | Inventory | Furniture | Navigator | Auth
 │        └───────────► Database ───► Primitives  (every project ends here)
 ├── Events ◄── handlers in Observability & Players
 ├── Dashboard.API, WebApi, Observability, Plugins, Benchmark, LoadGen
```

Real (measured by `using` analysis): **PacketHandlers uses nothing from Rooms, Furniture, or
Authentication (only `Catalog.Exceptions`); Rooms uses nothing from Players; Revisions uses
nothing from Players or Furniture.** The actual coupling flows exclusively through
`Vortex.Primitives` (grain interfaces, snapshots, wire records) — which is the *right*
topology for Orleans. The declared-but-unused edges are dead weight: they lengthen incremental
builds, and they silently *permit* the erosion the graph currently avoids. Direction of
dependencies is otherwise healthy: infrastructure never depends on domain, domain modules
never depend on each other's implementations, `Main` alone sees everything.

Two measured hub facts worth keeping in mind:
- `Vortex.Primitives/Messages` is 1,092 of Primitives' ~1,700 files. Any composer field change
  recompiles **every** project.
- 11 Primitives files outside `Messages/` import `Messages.*` — 5 of them grain interfaces
  (e.g. `IRoomWired` passing `TradeContract`, a wire record, as a domain parameter). The
  contract/protocol wall has started to leak.

### 1.3 The important execution flows

**Game packet:** TCP/WS (SuperSocket) → `SessionContext` (per-session RC4, send serialized by
a `SemaphoreSlim`) → revision parser → message registry (rate limits, behaviors) → envelope
pipeline (`HandlerExecutionMode.Parallel|Sequential`) → `IMessageHandler<T>` →
`IGrainFactory.GetXxxGrain(...)` → grain method (state + EF) → grain sends composers itself
via `PlayerPresenceGrain.SendComposerAsync` (per CONTEXT rule) → revision serializer → socket.

**Room fan-out:** `RoomGrain` publishes `RoomOutbound` on a per-room **memory stream**
(`ROOM_STREAM_PROVIDER`); each occupant's `PlayerPresenceGrain` subscribes and forwards to its
sessions. Pub-sub state sits in `PubSubStore` (ADO.NET or memory per config).

**Domain events:** `EventSystem` (silo-local, DI-registered) → `EventRegistry` →
`IEventHandler<T>` subscribers. Load-bearing use today: **audit** (14 handlers in
Observability writing the audit ledger) plus 6 in Players. Publisher and handler always
co-locate, so this bus is multi-silo-*safe for its current use* — audit lands wherever the
event fires. The cancellable seam (`PublishCancellableAsync`) is wired and used by
`GroupDirectoryGrain`, but zero production `IEventBehavior<T>` implementations exist.

**Admin:** Dashboard reads DB directly (read-only, one `DashboardApiService`) + live data from
grains; **all mutations** go `DashboardOperationsService` → grain (verified: 0 `SaveChanges`
in Operations; e.g. credits grant → `PlayerWalletGrain.GrantCreditsAsync`) with an
audit-wrapping `ExecuteAsync`. This respects the single-writer rule. WebApi handles
SSO/registration with its own DB access (auth tables — its aggregate, acceptable).

### 1.4 State, persistence, and transactional boundaries

No grain uses `[PersistentState]`; every grain persists through EF Core directly (the config
comment says so explicitly, and only `PubSubStore` is registered). This is a **sanctioned**
pattern — the official docs state grains can access databases directly without the persistence
model (learn.microsoft.com/dotnet/orleans/grains/grain-persistence) — and here it is the right
call: relational data, admin tooling, and migrations all want a real schema, not serialized
state blobs.

Single-writer discipline: each aggregate's rows are written only by its grain's activation
(rooms/furniture by `RoomGrain`+`RoomPersistenceGrain`, currencies by `PlayerWalletGrain`
inside a real DB transaction with an execution strategy, inventory by `InventoryGrain`, offers
by `MarketplacePurchaseGrain`, chest rows by the room the furni stands in). Dashboard writes
route through grains. The one systemic enforcement gap: nothing *mechanically* prevents a new
`SaveChanges` appearing in a handler or an admin path — the rule lives in `CONTEXT.md`.

Cross-grain money moves have **no distributed transactions** (correct) and instead use
ordering + compensation — but in four dialects:

| Flow | Mechanism | Shared primitive? |
|---|---|---|
| Catalog purchase (+ gifts, room ads, rentables, group creation) | `WalletPurchaseExtensions.ExecutePurchaseAsync`: debit → work (ordered least-to-most reversible) → refund on failure | ✅ yes |
| Marketplace buy | atomic claim (`ExecuteUpdate` guarded on `State == Active`) → grant → re-list + refund on failure | ✖ hand-rolled |
| Player trade | revalidate → single-`SaveChanges` ownership swap → best-effort resync/audit | ✖ hand-rolled |
| Wired settlement | validate (pure) → debit → persist goods → grant → ledger-after-grant → refund compensation | ✖ hand-rolled |

All four are *individually* defensible today (the wired one was just fixed to be). The
divergence itself is the debt: every new money feature re-derives the choreography, every
guarantee must be re-audited per flow, and no idempotence convention exists anywhere (the only
double-apply guard is grain single-threading, which is real but implicit).

### 1.5 Caches and their invalidation (the multi-silo fault line)

Silo-local singletons holding DB-derived state: `FurnitureDefinitionProvider`,
`CatalogService` (both `AddSingleton`, both with a `ReloadAsync` that reloads **this silo
only**, triggered by admin services), plus Observability aggregators (`LiveStatsAggregator`,
`RoomPerformanceAggregator`, incident detection) and the dashboard itself, which reports the
node it runs on. Grain-held caches (`InventoryGrain`, wallet, `PlayerDirectoryGrain`) are
cluster-correct by construction. Streams are `AddMemoryStreams` for both providers.

On one silo — the only deployment that exists today — all of this is correct. On two silos:
an admin furniture/catalog edit reloads one silo and leaves the other serving stale
definitions indefinitely; the dashboard shows one node's live stats; room streams keep working
(memory streams are cluster-wide) but lose in-flight events on failure, and the official
position is blunt: in-memory streaming is for development and testing, not for production
workloads needing durability (learn.microsoft.com/dotnet/orleans/streaming/stream-providers).

### 1.6 Central components and mixed responsibilities

- `RoomGrain`: 5,964 lines of partials **after** the wired extraction, orchestrating 15
  Modules + ~40 Systems (12,653 lines) — the Systems pattern works; the remaining partial mass
  (`Furni.*`, `Avatar`, `Map`, `Crackable`, `MysteryBox`, `Pets` glue) is the next accretion
  front.
- `PlayerGrain` (1,234 lines, 24 public methods) plus the 35 other grains in one project —
  cohesion by folder, not by module.
- `VortexDbContext`: one context for 146 entities across all domains — fine functionally,
  but it makes "which module owns this table" a convention, and the factory is
  `AddDbContextFactory` (not pooled) while the whole codebase creates a context per operation.
- Twelve `[KeepAlive]` singleton manager grains + `RoomDirectoryGrain` +
  `PlayerDirectoryGrain` + `ModerationQueueGrain`: cluster-wide single activations on hot
  paths (every room registers/pings the directory; every profile lookup hits the player
  directory). The Orleans FAQ's rule of thumb: individual grains handling hundreds of
  requests per second are a decomposition warning sign.

---

## 2. Findings

Severity: 🔴 correctness/money · 🟠 strategic · 🟡 hygiene. Categories per the brief:
**A** current problems, **B** future risks, **C** architectural debt, **D** local
improvements, **E** major restructurings.

### A — Current problems

**A1 🔴 The money paths are the least-tested code in the repository.**
- *Where:* `Vortex.Catalog` (purchase, gifts, LTD raffle, vouchers, targeted offers),
  `Vortex.Marketplace`, `Vortex.Inventory` — 0 tests; their invariants exist only as prose
  comments (`SEC-06`, "least-to-most reversible").
- *Why it's a problem:* the wired audit just demonstrated that exactly this class of code —
  debit/persist/grant with compensation — harbored two real money-destruction bugs despite
  being carefully written. Catalog and Marketplace are the same class of code, with more
  paths (discounts, club pricing, multi-currency debits, gifts, raffles, re-listing) and no
  safety net.
- *Consequences:* silent credit destruction/duplication or item loss under failure
  interleavings; regressions invisible until a player reports them; every refactor of these
  modules is a leap of faith.
- *Why the architecture leads here:* grains were untestable-by-default until the
  system-extraction pattern; the economy modules predate it, so tests were never cheap to add.
- *Solution:* an **economy invariant test kit** (SQLite-in-memory `IDbContextFactory` + fake
  wallet/inventory grains + fault-injecting factory — the exact harness `Rooms.Tests` already
  proved on the wired settlement) applied to: purchase success/insufficient/refund-on-work-
  failure, gift delivery failure, marketplace claim races + re-list-on-grant-failure, raffle
  refund batches, voucher double-redeem. Assert the four absolutes: never create credits,
  never destroy credits (observable-failure paths), never duplicate a furni, never lose one.
- *Files:* new `Vortex.Catalog.Tests`, `Vortex.Marketplace.Tests`; seams already exist
  (grains take `IGrainFactory` + factories).
- *Impact/risk:* additive; the risk is *finding* bugs — which is the point. Effort M.

**A2 🟠 One economy, four choreographies (§1.4).**
- *Where:* `WalletPurchaseExtensions` vs `MarketplacePurchaseGrain.BuyOfferAsync` vs
  `RoomTradingSystem.CommitTradeAsync` vs `WiredTradeSettlement`.
- *Why:* each new flow re-derives ordering, compensation, audit, and ledger conventions;
  guarantees drift (the wired flow's ledger-after-grant rule exists nowhere else); reviewers
  must re-verify from first principles each time.
- *Consequences:* the next money feature (upgrades? auctions? the chest-capacity purchase the
  wired code refuses today) will produce a fifth dialect, and the bug classes P1/P2 from the
  wired audit will keep re-appearing.
- *Solution (E2):* grow `WalletPurchaseExtensions` into a small **economy kernel** in the
  contracts project: the three shapes that actually exist — *debit→work→refund*,
  *claim→fulfil→release*, *validate→debit→persist→grant→ledger* — as named primitives with one
  audit/ledger convention and one shared invariant-test kit. Not a framework: three static
  helpers + conventions + tests.
- *Impact:* Catalog already conforms; Marketplace and Wired migrate onto it (behavior-
  preserving); Trading can stay (its swap is genuinely different) but adopts the audit
  convention. Effort M. Risk: low if migrated one flow at a time under A1's tests.

**A3 🟠 Primitives is fusing contracts with protocol.**
- *Where:* `Vortex.Primitives/Messages` (1,092 files) inside the hub every project references;
  11 non-Message files already import `Messages.*`, 5 of them grain interfaces
  (`IRoomWired.Chests.cs` taking `TradeContract`, presence taking composers — by design, but
  the wired case passes wire records as *domain terms*).
- *Why:* protocol is the highest-churn layer (536 handlers, 26k lines of codecs track it);
  welding it into the hub means every composer tweak rebuilds Database, all domains, and all
  tests; and wire shapes start becoming the domain model (the `TradeContract` tree is now the
  contract-terms domain type).
- *Consequences today:* slow incremental builds, protocol changes with repo-wide blast
  radius. *Later:* the client revision evolves (plugins already own alternate revisions) and
  the "default" wire shapes are fossilized inside grain contracts.
- *Solution (E3):* two moves, in order. First, an architecture test: **no new `Messages.*`
  imports outside `Primitives/Messages`, handlers, revisions, and presence-send paths** —
  freeze the leak at 11 files. Second, when convenient, split physically:
  `Vortex.Contracts` (ids, enums, snapshots, grain interfaces, economy kernel) ←
  `Vortex.Protocol` (message records) ← Revisions/Handlers/Networking. The 5 leaky grain
  interfaces get domain twins for the offending parameters (mechanical: `TradeContract` ⇄ a
  contracts-side record with the same shape).
- *Impact:* large build-graph win (protocol churn stops touching Database/domains); the split
  itself is file moves + 11 detangles. Risk: the detangle must not alter wire layout — the 239
  Revisions tests pin that. Effort M–L.

**A4 🟡 Declared dependencies lie (stale csproj edges).**
`Rooms→Players`, `PacketHandlers→{Rooms,Furniture,Authentication}`, `Revisions→{Players,
Furniture}` are unused (measured, §1.2). Delete them; they cost build time and are pre-drilled
holes for boundary violations. Effort S; risk none (compiler arbitrates).

**A5 🟡 EF factory is unpooled on a 146-entity model with context-per-operation.**
`AddDbContextFactory` → `AddPooledDbContextFactory` (grains are single-threaded per
activation; the per-operation lifetime pattern is exactly what pooling is for), plus evaluate
EF compiled models for startup. Effort S. Measure with the existing Benchmark project.

### B — Future risks

**B1 🟠 The single-silo/multi-silo ambiguity (§1.5) — the strategic fork.**
- *Why the architecture leads here:* multi-silo *configuration* was added (correctly) during
  hardening, but the application semantics grew up single-silo and nothing marks which
  components assume it.
- *Consequences when silo #2 starts:* stale furniture/catalog definitions after admin edits
  (indefinitely, per-silo), dashboards and incident detection reporting a random node,
  best-effort room delivery whose loss modes are undocumented.
- *Solution (E1):* **decide and encode the thesis.** Recommended: *single-silo-first,
  multi-silo-capable-by-declaration.* Concretely: (a) a startup capability check — if the
  cluster has >1 silo, refuse unless `Cluster:MultiSiloReady=true`; (b) an inventory of
  silo-local components in `CONTEXT.md` with their multi-silo story; (c) when the day comes,
  the cheap fixes in order: cache invalidation via a broadcast (an Orleans broadcast channel
  or a tiny "reload" stream — the providers already expose `ReloadAsync`), metrics aggregation
  behind the existing `IVortexMetrics` seam, and an explicit decision on stream durability
  (memory streams' loss-on-failure is arguably *fine* for room composers — clients resync on
  re-enter — but that acceptance must be written down, because the official guidance says
  memory streams are not for production durability).
- *Impact:* (a)+(b) are a day of work now and convert a silent bug family into a checklist.

**B2 🟠 Hub grains on hot paths.**
`RoomDirectoryGrain` (every room registration/ping + navigator queries),
`PlayerDirectoryGrain` (every lookup), and twelve `[KeepAlive]` managers are cluster-wide
single activations. Fine at hundreds of CCU; the Orleans guidance flags grains sustaining
hundreds of req/s as decomposition candidates (FAQ), and the documented remedies are sharding
by key and stateless-worker read layers with periodic pre-aggregation. *Don't build this
now.* Do: add per-grain request-rate metrics (Observability already has the plumbing) and a
one-page sharding design (`RoomDirectoryGrain` → N shards by roomId hash + a stateless-worker
navigator read cache) so the trigger and the plan pre-exist the incident. Effort S now.

**B3 🟡 RoomGrain re-accretion.** The Systems pattern won; make it a rule with teeth: partial
files are delegation-only, logic lives in Modules/Systems, enforced by an architecture test
(e.g. "RoomGrain partials contain no `SaveChangesAsync`" + a line budget in CI). The remaining
logic-bearing partials (`Furni.*`, `MysteryBox`, `Crackable`) migrate opportunistically.

**B4 🟡 `Vortex.Players` grab-bag (36 grains).** Works today; every new social/progression
feature lands there by gravity. Split by cohesion when convenient (E4): **Social** (groups,
forum, messenger, guides), **Progression** (quests, achievements, dailies, polls, community
goals), **Collectibles/NFT**, leaving Players = identity, wallet, presence, effects,
navigator prefs. Grain keys and interfaces don't change — this is file moves + csproj — but
it's cheap now and expensive after two more years of accretion.

**B5 🟡 Admin surface inside the game host.** Dashboard.API+Web (22k lines) share the game
process: an admin-path fault or heavy query competes with gameplay; the security surface
rides the game host. Mitigations exist (Supervisor, capability policies). The architecture
already permits the fix — operations go through grains — so an out-of-proc admin host is a
*deployment* option via an Orleans client, not a rewrite. Keep as P2/P3; document it.

**B6 🟡 Plugin trust boundary.** Plugins load into the host `AssemblyLoadContext` with hot
reload and can own protocol revisions — full-trust in-proc code. That's the right call for a
solo-operated emulator; write it down as an explicit trust statement so a future
"community plugins" idea doesn't inherit the assumption silently.

### C — Architectural debt

**C1 Speculative seams.** The cancellable-event pipeline is functional but has zero
production behaviors; `PLAYER_STORE`/`ROOM_STORE` grain storage was configured for a
persistence model nothing uses (the config comment admits it). Neither hurts; both teach the
wrong lesson. Rule: seams are built with their first consumer. Give the cancellable seam one
real consumer (the room word-filter from the roadmap is a natural fit) or demote it to a
branch.

**C2 Micro-project fragmentation.** `Runtime` (757) + `Pipeline` (752) + `Events` + the infra
half of `Messages` are one dispatch subsystem across four projects with entangled references.
`CONSOLIDATION.md` already flagged over-modularization once (`Vortex.Contracts` was merged
away). Merge into one `Vortex.Messaging` (or fold into Networking); likewise `Logging` →
`Observability`. Fewer, bigger, cohesive projects beat many thin ones — this is the inverse
error of A3 and both are real.

**C3 Boundaries by prose.** `CONTEXT.md`'s rules ("no DB in handlers", "mutations through
grains", placement rules) are exactly right and enforced by nothing. One `Vortex.Architecture.
Tests` project (NetArchTest/ArchUnitNET or hand-rolled reflection) encoding: dependency
matrix, no `IDbContextFactory` in PacketHandlers/Dashboard.Operations, no `SaveChanges`
outside grain assemblies + allowlist, no `Messages.*` outside the protocol allowlist (A3), no
new logic-bearing RoomGrain partials (B3). This is the highest-leverage cheap change in the
audit.

**C4 Wire records as domain types.** The `TradeContract` case (A3) generalized: snapshots are
the intended cross-layer currency; composers/messages are not. State it, test it.

**C5 One DbContext for all domains.** Acceptable at 146 entities with migrations centralized;
the debt is discoverability (which module owns `wired_chest_transactions`?). Cheap remedy:
entity folders already exist — add ownership to the entity XML docs + a generated table→module
map in `DATA-MODEL.md`. A per-module context split is *not* recommended (cross-domain queries
and one migration history are worth more than the purity).

### D — Local improvements (do regardless)

D1 Architecture tests project (C3) — S.
D2 Delete stale project references (A4) — S.
D3 Pooled DbContext factory + compiled-model evaluation (A5) — S.
D4 Per-grain hot-path metrics + directory sharding design note (B2) — S.
D5 Multi-silo capability guard + silo-local inventory in CONTEXT.md (B1a/b) — S.
D6 Merge Logging→Observability; plan Runtime+Pipeline+Events(+Messages-infra) merge (C2) — S/M.
D7 Table→module ownership map (C5) — S.

### E — Major restructurings (recommended, ordered)

E1 **Deployment thesis** (B1) — decide, encode, inventory. *Prereq for honest scaling talk.*
E2 **Economy kernel + invariant test kit** (A1+A2) — the one change that most reduces the
probability of the worst bug class this system can have.
E3 **Contracts/Protocol split** (A3) — freeze the leak now (test), split physically when the
detangle count is ~0.
E4 **Players → Players/Social/Progression/Collectibles** (B4) — mechanical, big cohesion win.
E5 **Infra consolidation** (C2) — fewer projects, same code.

Explicitly **not** recommended: microservices (one operator, one DB, chatty real-time domain —
the modular monolith on Orleans is the correct shape); CQRS/MediatR layers (handlers already
are the command layer; grains already serialize); per-module DbContexts; event sourcing
(JournaledGrain) — the relational model with an audit ledger already serves the actual need;
replacing the in-proc event bus with streams for audit (co-location is a feature there).

---

## 3. Comparison with ecosystem guidance (why these calls)

- **EF-direct grain persistence** is explicitly supported by Orleans ("grains can also access
  databases directly") — keeping it is aligned, not contrarian. The codebase even documents
  that `[PersistentState]` is unused by intent.
- **Memory streams**: official docs restrict them to dev/test where durability matters; the
  audit's position is "accept knowingly for room fan-out, decide before multi-silo" rather
  than "replace now".
- **Hot singleton grains**: the FAQ's hundreds-req/s warning and the documented remedies
  (sharding, stateless-worker read layers, pre-aggregation) back B2's
  measure-now/shard-later stance.
- **Modular monolith practice**: boundary rules enforced by architecture tests rather than by
  project explosion mirrors how large .NET monoliths keep module walls without microservice
  overhead — which is what C2+C3 implement together.

---

## 4. The five most important problems (ranked)

1. **Untested money paths** — Catalog/Marketplace/Inventory at 0 tests (A1).
2. **Economy consistency by artisanry** — four compensation dialects, no idempotence
   convention (A2).
3. **Single-vs-multi-silo ambiguity** — silo-local caches/metrics/events with a clustering
   config that promises more (B1).
4. **Primitives→god-hub trajectory** — 1,092 message files in the build hub + wire types in
   grain contracts (A3/C4).
5. **Un-enforced boundaries + module cohesion drift** — prose rules, stale refs, the Players
   grab-bag, micro-project infra (C3/A4/B4/C2).

---

## 5. Target architecture

**Current (real, simplified):**

```text
Vortex.Main ──────────────────────────────────────────────── composition root
 ├─ Networking ─ Messages ─ Pipeline ─ Runtime               4-project dispatch infra
 ├─ PacketHandlers ─┐
 ├─ Revisions ──────┤            (stale edges → Rooms/Players/Furniture/Auth)
 ├─ Rooms ─ Players ─ Catalog ─ Marketplace ─ Inventory ─ Furniture ─ Navigator ─ Auth
 │            all ↓
 │        Vortex.Database (146 entities) ─► Vortex.Primitives
 │                                          (contracts + snapshots + events
 │                                           + 1,092 message files  ← the hub)
 ├─ Events (in-proc bus) ◄ audit handlers in Observability/Players
 ├─ Dashboard.API(+Web) in-proc │ WebApi │ Observability │ Plugins │ Supervisor
```

**Proposed (same system, boundaries made real):**

```text
Vortex.Main
 ├─ Vortex.Networking ── Vortex.Messaging      (= Runtime+Pipeline+Events+Messages-infra)
 ├─ Vortex.Protocol                            (message/composer records — churn quarantined)
 ├─ Vortex.Revisions │ Vortex.PacketHandlers   (depend on Protocol + Contracts only)
 ├─ Vortex.Contracts                           (ids, enums, snapshots, grain interfaces,
 │                                              economy kernel, event contracts — the hub,
 │                                              now protocol-free and slow-churn)
 ├─ Domain modules, each: grains + systems + tests
 │    Rooms │ Players │ Social │ Progression │ Collectibles │ Catalog │ Marketplace
 │    Inventory │ Furniture │ Navigator │ Authentication
 │            all ─► Vortex.Database ─► Vortex.Contracts
 ├─ Vortex.Observability (audit, metrics, incident, +Logging)
 ├─ Admin: Dashboard.API(+Web) — grain-mediated writes, out-of-proc-capable
 └─ Vortex.Architecture.Tests                  (the walls, executable)
```

Same runtime shape (one host, one DB, Orleans inside), roughly the **same number of projects**
(merges offset splits) — the change is where churn lives and what enforces the walls.

---

## 6. Keep absolutely / redo completely

**Keep absolutely:** the modular-monolith-on-Orleans shape; thin handlers + grain-owned state
+ single-writer-per-aggregate; EF-direct persistence with the wallet's transactional core; the
Modules/Systems extraction pattern and its testability dividend; the Revisions protocol
isolation and its 239 wire tests; presence-grain outbound ownership; the audit-via-events
design; Specs, Supervisor, LoadGen, Benchmark, the docs discipline; Dashboard mutations
through grains.

**Redo / remove:** the four-dialect economy (→ kernel); stale csproj edges; the
Runtime/Pipeline/Events/Messages fragmentation (→ one Messaging project); prose-only
boundaries (→ architecture tests); wire types in grain contracts (freeze, then detangle);
the unused `PLAYER_STORE`/`ROOM_STORE` remnants and the behavior-less cancellable registry
(first consumer or out); `Vortex.Players` as a bucket (→ Social/Progression/Collectibles).

---

## 7. Roadmap

**P0 — fix imperatively (correctness now)**
1. Economy invariant test kit + suites for Catalog & Marketplace (A1). Fix what it finds. *M*
2. Multi-silo capability guard + silo-local component inventory (B1a/b). *S*
3. Architecture tests encoding CONTEXT.md + delete stale refs (C3/A4). *S*

**P1 — before heavy new development**
4. Economy kernel; migrate Marketplace, then Wired settlement onto it; one audit/ledger
   convention + idempotence note per flow (A2/E2). *M*
5. Freeze the Messages leak (arch test) + detangle the 5 grain-interface offenders (A3 step 1). *S/M*
6. RoomGrain partial-budget rule in CI; migrate remaining logic-bearing partials
   opportunistically (B3). *S + ongoing*
7. Pooled DbContext factory + benchmark; hot-grain metrics + sharding design note (A5/B2). *S*

**P2 — structural, incremental**
8. Split Players → Social / Progression / Collectibles (E4). *M, mechanical*
9. Merge infra projects → Vortex.Messaging; Logging → Observability (C2). *S/M*
10. Physical Contracts/Protocol split once offenders ≈ 0 (E3 step 2). *M*
11. Cancellable-event seam: first real consumer (room word-filter) or removal (C1). *S*

**P3 — quality/optimization**
12. EF compiled models; N+1 pass on room/inventory load with the Benchmark project. *S/M*
13. Out-of-proc admin host option behind an Orleans client (B5). *M, optional*
14. Broadcast-channel evaluation for cache invalidation & room fan-out when multi-silo
    becomes real (B1c). *design-gated*
15. Directory sharding implementation — only when D4's metrics cross the documented
    threshold (B2). *gated*

**Migration order rationale:** P0 installs the safety net and the tripwires before anything
moves; P1 consolidates the highest-risk logic onto shared primitives *under* that net and
stops the two active erosions (protocol leak, grain re-accretion); P2 is file-motion whose
safety P0's tests and the compiler guarantee; P3 is gated on measurements, not on faith. At
every step the solution builds, the wire behavior is pinned by Revisions tests, and each
change is a revertible commit series — the project stays shippable throughout.
