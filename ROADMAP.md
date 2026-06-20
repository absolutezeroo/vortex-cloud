# Vortex Cloud — Roadmap (BMAD format, brownfield)

Completion roadmap structured as **epics → stories → acceptance criteria**, ordered by dependencies,
with a **Definition of Done** applied to each story. This is an existing project (brownfield): we do not
rebuild from scratch; we layer missing epics on top of a solid core while the client-facing integration
is still mostly stubbed.

---

## 1. Current state (brownfield assessment)

**Solid, do not rebuild:** room engine (real A\*, ~105 wired files, furniture logic, map module),
event-driven observability (Orleans correlation, audit, ledger, OTel-ready), permission engine
(PermissionSet/IPermissionService/Capabilities, pure policies `RoomSecurityPolicy` / `ModerationPolicy`),
correct Orleans architecture (thin handlers, isolated grains, streams).

**Central gap:** the engine is deep, but **handlers** exposing it to clients are mostly stubs. Measured
coverage:

| Domain | Coverage |
|---|---|
| Handshake | 44% |
| Catalog | 39% |
| Users | 26% |
| Room | 23% |
| Moderator (staff tool) | 21% |
| Inventory | 19% |
| Navigator | 8% |
| RoomSettings / Sound / Camera | 0% |

**Test coverage:** WebApi surface tested (16 integration tests on branch
`feat/webapi-aspnetcore-migration`), core gameplay/economy/permissions **not tested**.

**In progress:** WebApi migration to ASP.NET Core (feature branch, pending merge).

**Known debt:** delayed grain error handling (`// TODO handle exceptions`), ~118 files not passing
csharpier, no RGPD audit retention, unsecured remote dashboard access.

---

## 2. Guiding principles

1. **Inside-out, not outside-in.** Finish the playable core before expanding perimeter features.
2. **Complete vertical slices at 100%**, not wide partial scaffolding.
3. **Prioritize sensitive logic testing** (economy, permissions) using existing harness.
4. **A stub is debt, not progress.** Every empty handler is an unmet promise.
5. **Strategies over patching-by-need** (error handling, permission placement, etc.).

---

## 3. Epic overview

| # | Epic | Objective | Status | Depends on |
|---|---|---|---|---|
| 0 | Quality foundation | Core test harness, error strategy, formatting green | To start (cross-cutting) | — |
| 1 | Playable core loop | login→room→walk→chat→furni→navigator→leave at 100% | Partial | 0 (in progress) |
| 2 | Permissions & moderation | Complete group rights + staff tool + policy tests | Near complete | 1 |
| 3 | WebApi ASP.NET | Merge, finish, harden, remove HttpListener | In progress | — |
| 4 | Economy & inventory | Buy/gift/sell end-to-end + tested ledger | Partial | 1 |
| 5 | Social | Friends, messaging, groups | Partial | 1 |
| 6 | Trading | Secure end-to-end trade | Stub | 4 |
| 7 | Operations & compliance | RGPD retention, remote dashboard, OTel export | Partial | — |

**Recommended order:** 0 (in parallel) → 3 (almost done, close the loop) → 1 (highest priority)
→ 2 → 4 → 5 → 6 → 7.

---

## 4. Detailed epics

### EPIC 0 — Quality foundation (cross-cutting)

**Goal:** add multiplier work that makes everything else safe and fast. Run in parallel with other epics,
not before them.

**Story 0.1 — Extend core test harness**
*As a* developer, *I want* to test pure policies and critical grain logic,
*so I can* refactor without silent regressions in economy and rights.
- [ ] New project `Turbo.Permissions.Tests` (or equivalent) based on
      `Turbo.WebApi.Tests` (xunit + FluentAssertions, already used).
- [ ] Unit tests for `RoomSecurityPolicy.ResolveControllerLevel` (all cases:
      System, superuser, ModerateAny, BuildAny, explicit owner, rights, none).
- [ ] Unit tests for `ModerationPolicy.IsAllowed` (each action × specific capability × ModerateAny × wildcard × deny).
- [ ] Orleans TestKit setup for at least one grain as proof of concept.
- [ ] Target: these tests run in the quality gate.

**Story 0.2 — Grain error and resilience strategy**
*As a* developer, *I want* a single grain failure contract,
*so that* a grain state is never left inconsistent.
- [ ] Define the contract for a failed grain operation (state rollback in memory? structured logs? observability error event?).
- [ ] Replace `// TODO handle exceptions` in `RoomGrain.Furni.cs` (and others) using this strategy.
- [ ] Audit the 24 existing catch blocks: none should swallow errors without logging.

**Story 0.3 — Formatting gate**
- [ ] `csharpier check` passes across the whole repo (all ~118 non-compliant files formatted).
- [ ] Blocking pre-commit hook.

**Epic 0 DoD:** quality gate runs policy tests and at least one grain test; no `// TODO handle exceptions`;
formatting green.

---

### EPIC 1 — Fully playable core loop (**ABSOLUTE PRIORITY**)

**Goal:** player can connect, navigate, enter a room, walk, chat, manipulate furniture, configure room,
leave — without ever hitting a dead packet path. This is the slice that turns an impressive engine into a playable game.

**Story 1.1 — Complete Navigator (8% → 100%)**
*As a* player, *I want* to browse, search, and enter rooms, *so I can* enter the game.
This is the entry point and the sparsest domain.
- [ ] All handlers in `Turbo.PacketHandlers/Navigator` (+ `NewNavigator`) implemented.
- [ ] Categories, search, favorites, my rooms, popular rooms return real data through grains.
- [ ] End-to-end functional room creation.

**Story 1.2 — Enter / exit / initial room state fully**
- [ ] Entry, exit, heightmap/initial-state handlers implemented.
- [ ] Player receives the full room state on entry (avatars, furniture, rights).

**Story 1.3 — Furniture: place / move / pickup / use (Room 23% → 100%)**
*As a* player with rights, *I want* to manipulate furniture, *so I can* decorate/interact.
- [ ] Place/move/rotate/pickup/use handlers wired through `RoomSecurityModule`.
- [ ] Checks flow through `CanManipulateFurniAsync` / `CanPlaceFurniAsync` / `CanUseFurniAsync`.
- [ ] Stuff data updated and broadcast to room clients.

**Story 1.4 — Room settings (RoomSettings 0% → 100%)**
- [ ] Settings handlers (name, description, rights, access, max users, etc.) implemented.
- [ ] Gated by ownership/controller via `GetControllerLevelAsync`.

**Story 1.5 — Finish `RoomSecurityModule`**
- [ ] Replace hardcoded `isGroupRoom = false` with real detection.
- [ ] Implement `canGroupDecorate` and GroupRights/GroupAdmin branches.
- [ ] Remove `// TODO placement rules?` in `CanPlaceFurniAsync`.

**Story 1.6 — Room audio (Sound 0%)** *(lowest priority in epic)*
- [ ] Trax/jukebox handlers if V1 includes this feature, otherwise call out deprecation explicitly.

**Epic 1 DoD:** test account can do login → navigator → enter → walk → chat → place/move/pickup a
furniture → set room options → leave, end-to-end, with no stub on this path. Rights are checked at every step.

---

### EPIC 2 — Permissions & moderation (near complete, needs finishing)

**Goal:** close remaining gaps in an already wired rights system.

**Story 2.1 — Login reads and assigns roles from DB**
- [ ] Verify `account → player_account_roles` path (seeder `DefaultRoles` exists).
- [ ] New account gets appropriate default role (not Administrator).
- [ ] Client session rank is consistent with capabilities, but client rank remains UI hint only; server decision remains `IPermissionService`.

**Story 2.2 — Group room rights**
- [ ] Real `isGroupRoom` + `canGroupDecorate` + GroupMember/GroupRights/GroupAdmin levels in `RoomSecurityModule`.

**Story 2.3 — Staff moderation tool (Moderator 21% → 100%)**
*As staff, I want* moderation tooling (CFH/tickets, alerts, sanctions),
*so I can* operate hotel moderation.
- [ ] `Turbo.PacketHandlers/Moderator` handlers implemented, gated by capabilities
  `Capabilities.Moderation.*`.
- [ ] Every action emits moderation audit event (category Moderation), both success and denial.

**Story 2.4 — Policy tests** *(overlaps Story 0.1, do not duplicate)*
- [ ] Complete coverage of `RoomSecurityPolicy` and `ModerationPolicy`.

**Epic 2 DoD:** no sensitive action reaches game logic without tested capability checks; staff can operate
throughout; group rooms handled; moderation tool is functional and audited.

---

### EPIC 3 — WebApi ASP.NET Core migration

**Goal:** one hardened HTTP public surface under ASP.NET, fully tested.

**Story 3.1 — Merge feature branch**
- [ ] `feat/webapi-aspnetcore-migration` reviewed and merged into `main`.
- [ ] 16 integration tests run in gate.

**Story 3.2 — Finish and harden**
- [ ] All endpoints migrated (parity with old HttpListener).
- [ ] Strict rate limiting verified on `/login`, `/registration/new`, `/ssotoken` (429 test already present).
- [ ] Explicit CORS + HTTPS/HSTS toggled via config.

**Story 3.3 — Remove legacy**
- [ ] Remove `WebApiService.cs` (HttpListener) and `WebApiResponseWriter`.

**Epic 3 DoD:** no `HttpListener` remains; public surface runs on ASP.NET, tested and hardened.

---

### EPIC 4 — Economy & inventory (currency is sensitive)

**Goal:** buy, receive, sell/retrieve — every currency movement goes through tested ledger.

**Story 4.1 — Complete Inventory (19% → 100%)**
- [ ] Handlers `Turbo.PacketHandlers/Inventory` implemented (view, move to room, etc.).

**Story 4.2 — End-to-end catalog purchase (Catalog 39% → 100%)**
*As a* player, *I want* to buy from catalog, *so I can* get furniture.
- [ ] Full purchase flow: page → offer → debit (via ledger) → inventory add.
- [ ] Club (HC) and member pricing management.

**Story 4.3 — Critical ledger tests**
*As a* developer, *I want* to test every currency movement, *so I avoid* silent currency duplication/loss.
- [ ] Tests for ledger operations (debit, credit, idempotence, insufficient balance rejection).
- [ ] Invariant test: no path credits/debits currency without ledger entry.

**Story 4.4 — Gifts**
- [ ] Complete gift flow: purchase gift → wrapping → delivery → opening, audited.

**Story 4.5 — Marketplace**
- [ ] Full flow: list → search → buy → payout, with ledger on each step.

**Epic 4 DoD:** buying / receiving gifts / reselling works; every monetary movement is in ledger and covered by a test.

---

### EPIC 5 — Social

**Goal:** functional friends, messaging, and groups.

**Story 5.1 — Friends & messaging**
- [ ] `FriendList` (+ messenger) handlers: requests, accept/deny, messages, presence.

**Story 5.2 — Groups**
- [ ] Group creation, member management, forums (`GroupForums`) gated by group permissions (see Epic 2).

**Epic 5 DoD:** player can add friend, message, create and manage a group.

---

### EPIC 6 — Trading

**Goal:** secure and atomic player item exchange.

**Story 6.1 — Full trade flow**
- [ ] Open, add/remove items, lock both sides, confirm, atomic commit (both inventories change or none).
- [ ] Anti-cheat: server-side possession validation at each step.
- [ ] Exchange audit event (`item_events`).
- [ ] Tests for atomicity and failure cases (disconnect mid-trade, removed item).

**Epic 6 DoD:** two players exchange items atomically and audited; items can never be duplicated or lost.

---

### EPIC 7 — Operations & compliance

**Goal:** harden the already stable operations perimeter.

**Story 7.1 — RGPD audit retention / purge**
- [ ] Configurable retention policy + scheduled purge for audit tables.

**Story 7.2 — Secure remote dashboard access**
- [ ] Reverse proxy / TLS in front of ASP.NET dashboard (currently localhost-only).

**Story 7.3 — OTel export** *(if desired)*
- [ ] Wire existing ActivitySource/meter to an OpenTelemetry exporter.

**Epic 7 DoD:** RGPD-compliant audit retention; dashboard accessible remotely and securely; exportable metrics.

---

## 5. Global Definition of Done (per story)

A story is `Done` only if all are true:

1. **Code**: feature/handler implemented, wired through grains via interfaces, zero business logic in handlers,
   no new stubs introduced.
2. **Rights**: every sensitive action passes through capabilities (`IPermissionService` / policies), never through trusted client flag.
3. **Audit**: sensitive actions (moderation, currency, items) emit observability events on success and denial.
4. **Tests**: at minimum pure functions and sensitive invariants are covered; for HTTP surface, one contract
   integration test per route.
5. **Errors**: failure paths follow Epic 0 strategy, never use TODO fallbacks.
6. **Format**: `csharpier check` green.
7. **Stubs**: stub count for the relevant domain is decreasing, never increasing.

---

## 6. The trap to avoid (strategic reminder)

This project’s risk is not capability — the engine and observability prove that.
The real risk is **scale failure**: continue shipping attractive peripherals while core gameplay remains ~20%.

The golden rule: **do not start any perimeter epic until Epic 1**
(playable loop) is done. A hotel that is truly playable on a reduced scope is far better than a
20%-capable hotel with world-class peripheral systems.
