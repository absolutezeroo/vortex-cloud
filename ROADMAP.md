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

**Central gap (was much wider than currently):** the engine is deep, and by 2026-07-05 most core-loop
handlers had already been filled in by work this table was never updated to reflect. Re-measured by
stub-vs-implemented handler count, verified against actual code (not just line count — some
low-line-count handlers turned out to be legacy/admin messages outside the core path, not real gaps):

| Domain | Coverage (2026-07-05) | Was (stale) |
|---|---|---|
| NewNavigator | 100% | — |
| Navigator | ~64% (13/36 stub, all peripheral: room-events, room-ads, staff-pick/tags) | 8% |
| Room/Chat | 100% | — |
| Room/Pets | 100% | — |
| RoomSettings | 75% (2/8 stub: custom room word-filter only) | 0% |
| Room (place/move/pickup/use core loop) | 100% for the literal verbs; ~50% counting peripheral extras (gift/mystery-trophy consumables, dimmer, mannequin, youtube display, rent/buyout, bot/pet-mount) | 23% |
| Sound | 0% (unchanged, lowest priority per Epic 1 itself) | 0% |

**Verified working end-to-end (2026-07-05):** navigator browse/search/create/enter, room enter/exit
with full initial state (heightmap, floor/wall items, avatars, rights), furniture place/move/pickup/use
gated through `RoomSecurityModule`, room settings (name/description/rights/access/max-users), chat.
Room-entry rights resolution and room-full/locked/password rejection were the one real gap on this
path — fixed 2026-07-05 (see `IRoomGrain.GetControllerLevelAsync`, `RoomService.OpenRoomForPlayerIdAsync`).

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
| 0 | Quality foundation | Core test harness, error strategy, formatting green | Done (cross-cutting) | — |
| 1 | Playable core loop | login→room→walk→chat→furni→navigator→leave at 100% | Done (2026-07-05) — literal DoD path has no blocking stub; peripheral extras remain | 0 (in progress) |
| 2 | Permissions & moderation | Complete group rights + staff tool + policy tests | Near complete | 1 |
| 3 | WebApi ASP.NET | Merge, finish, harden, remove HttpListener | Done | — |
| 4 | Economy & inventory | Buy/gift/sell end-to-end + tested ledger | Partial | 1 |
| 5 | Social | Friends, messaging, groups | Partial | 1 |
| 6 | Trading | Secure end-to-end trade | Stub | 4 |
| 7 | Operations & compliance | RGPD retention, remote dashboard, OTel export | Partial | — |

**Recommended order:** 0 (done) → 3 (done) → 1 (done) → 2 (next up) → 4 → 5 → 6 → 7.

---

## 4. Detailed epics

### EPIC 0 — Quality foundation (cross-cutting)

**Goal:** add multiplier work that makes everything else safe and fast. Run in parallel with other epics,
not before them.

**Story 0.1 — Extend core test harness**
*As a* developer, *I want* to test pure policies and critical grain logic,
*so I can* refactor without silent regressions in economy and rights.
- [x] New project `Turbo.Permissions.Tests` (or equivalent) based on
      `Turbo.WebApi.Tests` (xunit + FluentAssertions, already used). — done as
      `Turbo.Rooms.Tests/Permissions/` (equivalent, not a separate project).
- [x] Unit tests for `RoomSecurityPolicy.ResolveControllerLevel` (all cases:
      System, superuser, ModerateAny, BuildAny, explicit owner, rights, none).
- [x] Unit tests for `ModerationPolicy.IsAllowed` (each action × specific capability × ModerateAny × wildcard × deny).
- [x] Orleans TestKit setup for at least one grain as proof of concept. — `Microsoft.Orleans.TestingHost`
      (matching the pinned 9.2.1 Orleans version) spins up a real in-process `TestCluster` and exercises
      `RoomDirectoryGrain` end-to-end (activation + DI wiring + grain-reference calls), in
      `Turbo.Rooms.Tests/Grains/RoomDirectoryGrainClusterTests.cs`. Complements (does not replace) the
      hand-constructed grain test in `Turbo.Rooms.Tests/Groups/GroupDirectoryGrainCreationTests.cs`.
- [x] Target: these tests run in the quality gate. — `dotnet test Turbo.Cloud.sln` runs as part of
      `TurboCloudFastCheck`. `Turbo.Rooms.Tests` is 42/42 green.

**Story 0.2 — Grain error and resilience strategy**
*As a* developer, *I want* a single grain failure contract,
*so that* a grain state is never left inconsistent.
- [x] Define the contract for a failed grain operation — established in practice and applied
      consistently: catch narrowly where a recovery action exists (e.g. `TurboException` with a
      specific `ErrorCode`), always log via injected `ILogger<T>` with the identifying ids (item/room/
      player), never swallow with an empty `catch {}`, prefer `LogAndForget` over Orleans `.Ignore()`
      for fire-and-forget cross-grain calls (see `AGENTS.md` "Replace .Ignore() with a LogAndForget
      helper").
- [x] Replace `// TODO handle exceptions` in `RoomGrain.Furni.cs` (and others) using this strategy. —
      0 occurrences repo-wide.
- [x] Audit the 24 existing catch blocks: none should swallow errors without logging. — repo now has
      167 catch blocks (grew as coverage/logging expanded); re-audited all of them. One tracked
      exception remains: `FurnitureWiredLogic.cs` has two silent `catch {}` and one
      `Console.WriteLine`-only catch that need an `ILogger` threaded through the wired-logic DI chain
      (6 abstract + 83 concrete leaf classes) — scoped as its own follow-up given the blast radius, see
      `CONSOLIDATION.md` P5.

**Story 0.3 — Formatting gate**
- [x] `csharpier check` passes across the whole repo (all ~118 non-compliant files formatted). —
      verified clean across all 3659 files.
- [x] Blocking pre-commit hook. — `.githooks/pre-commit` runs `TurboCloudFastCheck` (build + csharpier
      check + `dotnet test`), `.githooks/pre-push` runs the full `TurboCloudQualityGate`;
      `core.hooksPath` is configured to `.githooks` (see `scripts/bootstrap.ps1`/`.sh`).

**Epic 0 DoD:** quality gate runs policy tests and at least one grain test; no `// TODO handle exceptions`;
formatting green. ✅ **Met.** Remaining hardening tracked in `CONSOLIDATION.md` (P4: quality gate's
`dotnet format` step only scopes `Turbo.Main`, not the full solution — widening it today would
immediately break the blocking gate on ~1200 pre-existing analyzer warnings elsewhere, so it's left as
a separate follow-up rather than forced through; P5: the `FurnitureWiredLogic.cs` gap above).

---

### EPIC 1 — Fully playable core loop (**ABSOLUTE PRIORITY**)

**Goal:** player can connect, navigate, enter a room, walk, chat, manipulate furniture, configure room,
leave — without ever hitting a dead packet path. This is the slice that turns an impressive engine into a playable game.

**Story 1.1 — Complete Navigator (verified ~64%, core done as of 2026-07-05)**
*As a* player, *I want* to browse, search, and enter rooms, *so I can* enter the game.
- [x] Categories, search, favorites, my rooms, room creation return real data through grains
      (`NewNavigatorInitMessageHandler`, `NewNavigatorSearchMessageHandler`, `CreateFlatMessageHandler`,
      `GetGuestRoomMessageHandler`, favorites handlers — all implemented).
- [ ] Remaining 13 stub handlers are peripheral, not core-blocking: room-events (`CancelEvent`/
      `EditEvent`/`GetUserEventCats`), room-ads (ties to Catalog's still-stub room-ads subsystem),
      staff-pick/tags/popular-tags/home-room/rate-flat secondary UX.

**Story 1.2 — Enter / exit / initial room state fully — done 2026-07-05**
- [x] Entry (`OpenFlatConnectionMessageHandler`), exit (`QuitMessageHandler`), full initial state
      (heightmap, floor/wall items, avatars, rights) all implemented and verified.
- [x] Fixed real bug: entry rights were computed via `isOwner ? Owner : None` instead of the real
      resolver — a rights-holder (not owner) was told on entry they had no rights. Now uses
      `IRoomGrain.GetControllerLevelAsync`.
- [x] Implemented the three access rejections left as "(for now)" comments: room-full, locked,
      password-mismatch. Owners/rights-holders bypass all three.
- [ ] Full doorbell/queue UX (request-to-enter flow for locked rooms, `EnterQueue`) — explicitly out
      of scope for now, matches original "(for now)" framing; only the flat reject ships.

**Story 1.3 — Furniture: place / move / pickup / use — core verified done 2026-07-05**
*As a* player with rights, *I want* to manipulate furniture, *so I can* decorate/interact.
- [x] Place (`PlaceObjectMessageHandler`), move (`MoveObjectMessageHandler`), pickup
      (`PickupObjectMessageHandler`), use (`UseFurnitureMessageHandler`/`UseWallItemMessageHandler`)
      all wired through `RoomActionModule` → `RoomSecurityModule`.
- [x] Checks flow through `CanManipulateFurniAsync` / `CanPlaceFurniAsync` / `CanUseFurniAsync` —
      confirmed actually invoked, not bypassed.
- [x] Stuff data updated and broadcast (`SetStateAsync` → `RefreshStuffDataAsync`); fireworks/dice/
      wheel-of-fortune interactions added 2026-07-05 on the same path.
- [ ] Remaining stub handlers here are all peripheral furniture-type extras, not the basic verbs:
      gift/mystery-trophy/pet-package consumables, post-its, dimmer, mannequin, youtube display,
      rent/buyout (separate from RentableSpace), bot placement, pet mounting, `SetRandomState`
      (semantics unverified, deliberately not guessed at).

**Story 1.4 — Room settings — done for the story's literal scope, verified 2026-07-05**
- [x] Name/description/rights/access/max-users (`SaveRoomSettingsMessageHandler`,
      `GetRoomSettingsMessageHandler`, `GetFlatControllersMessageHandler`,
      `GetBannedUsersFromRoomMessageHandler`, `DeleteRoomMessageHandler`) all implemented.
- [ ] Per-room custom word-filter (`GetCustomRoomFilterMessageHandler`/`UpdateRoomFilterMessageHandler`)
      is the only remaining stub — not part of this story's listed scope.

**Story 1.5 — Finish `RoomSecurityModule` — done, no remaining TODOs found (verified 2026-07-05)**
- [x] No `isGroupRoom = false` hardcode, no missing `canGroupDecorate`/GroupRights/GroupAdmin
      branches, no `// TODO placement rules?` anywhere in the codebase — resolved by earlier work,
      this file just never got updated to reflect it.

**Story 1.6 — Room audio (Sound 0%)** *(lowest priority in epic, unchanged)*
- [ ] Trax/jukebox handlers if V1 includes this feature, otherwise call out deprecation explicitly.

**Epic 1 DoD:** test account can do login → navigator → enter → walk → chat → place/move/pickup a
furniture → set room options → leave, end-to-end, with no stub on this path. Rights are checked at
every step. **Met as of 2026-07-05** — the literal path has no blocking stub; remaining stub handlers
across all stories are peripheral extras, not on this path.

---

### EPIC 2 — Permissions & moderation (near complete, needs finishing)

**Goal:** close remaining gaps in an already wired rights system.

**Story 2.1 — Login reads and assigns roles from DB**
- [ ] Verify `account → player_account_roles` path (seeder `DefaultRoles` exists).
- [ ] New account gets appropriate default role (not Administrator).
- [ ] Client session rank is consistent with capabilities, but client rank remains UI hint only; server decision remains `IPermissionService`.

**Story 2.2 — Group room rights**
- [ ] Real `isGroupRoom` + `canGroupDecorate` + GroupMember/GroupRights/GroupAdmin levels in `RoomSecurityModule`.

**Story 2.3 — Staff moderation tool — Done (2026-07-05)**
*As staff, I want* moderation tooling (CFH/tickets, alerts, sanctions),
*so I can* operate hotel moderation.
- [x] `Turbo.PacketHandlers/Moderator` handlers implemented, gated by capabilities
  `Capabilities.Moderation.*` (added `Chatlogs`/`Cfh`, matching the WIN63 client's own distinct
  tool-permission flags rather than reusing Kick/Mute/Ban/Alert).
- [x] Every action emits moderation audit event (category Moderation), both success and denial.
- [x] Relative-rank check: a staff member can't sanction someone of equal-or-higher rank.
- [x] Room-visit log (enter+exit) and room/user chatlog lookups.
- [x] Full CFH ticket system: category/topic catalog (each topic optionally linked to a
  `SanctionPresetEntity` default action), report submission, pick/close/release/default-sanction
  lifecycle, reporter notification on close, `ModeratorInit`/`CfhTopicsInit` pushed at login.
- [x] Sanction durations are a real, admin-manageable `SanctionPresetEntity` (seeded defaults,
  never overwritten once an admin edits them) instead of hardcoded config.
- Not done: `ModToolPreferencesMessageHandler` (per-staff window geometry), `ModeratorRoomInfo`/
  `ModeratorUserInfo` lookups, `ModerateRoomMessageHandler`/`ModeratorActionMessageHandler`/
  `ModMessageMessageHandler` (room-wide tools) — none block the core CFH+sanction loop.

**Story 2.4 — Policy tests** *(overlaps Story 0.1, do not duplicate)*
- [ ] Complete coverage of `RoomSecurityPolicy` and `ModerationPolicy`.

**Epic 2 DoD:** no sensitive action reaches game logic without tested capability checks; staff can operate
throughout; group rooms handled; moderation tool is functional and audited.

---

### EPIC 3 — WebApi ASP.NET Core migration — done

**Goal:** one hardened HTTP public surface under ASP.NET, fully tested.

**Story 3.1 — Merge feature branch**
- [x] `feat/webapi-aspnetcore-migration` reviewed and merged into `main` (tip `43a924e` is an
      ancestor of `main`).
- [x] 16 integration tests run in gate — `Turbo.WebApi.Tests`, 16/16 green.

**Story 3.2 — Finish and harden**
- [x] All endpoints migrated (parity with old HttpListener) — `Turbo.WebApi/Hosting/WebApiEndpoints.cs`.
- [x] Strict rate limiting verified on `/login`, `/registration/new`, `/ssotoken` — `WebApiAppConfigurator.cs`
      wires per-route `FixedWindowLimiter` policies; `WebApiEndpointsTests.Login_ExceedingRateLimit_Returns429`
      asserts the 429.
- [x] Explicit CORS + HTTPS/HSTS toggled via config — `WebApiAppConfigurator.AddCors`/`AddHttpsRedirection`,
      `config.HstsEnabled` gate.

**Story 3.3 — Remove legacy**
- [x] Remove `WebApiService.cs` (HttpListener) and `WebApiResponseWriter` — neither exists in the repo;
      only doc-comments in `Turbo.WebApi/Hosting/*.cs` reference the old `HttpListener` behavior for
      migration-parity context.

**Epic 3 DoD:** no `HttpListener` remains; public surface runs on ASP.NET, tested and hardened. ✅ **Met.**

---

### EPIC 4 — Economy & inventory (currency is sensitive)

**Goal:** buy, receive, sell/retrieve — every currency movement goes through tested ledger.

**Story 4.1 — Complete Inventory — Done (2026-07-05)**
- [x] Furni/Badges(core)/Pets/Purse were already fully implemented (roadmap's "19%" was stale).
- [x] `RequestRoomPropertySetMessageHandler` — real floor/wallpaper/landscape values from
  `RoomEntity`, now exposed through `RoomInfoSnapshot`.
- [x] `GetBotInventoryMessageHandler` — empty inventory (no Bot feature exists yet; a truthful
  answer, not a stub).
- [x] `GetBadgePointLimitsMessageHandler` — static per-category thresholds (`BadgeConfig`).
- [x] `RequestABadgeMessageHandler` / `GetIsBadgeRequestFulfilledMessageHandler` — truthfully report
  "not fulfilled" (no achievement/progress system exists to ever fulfill one).
- Deliberately deferred (separate stories, not part of this pass): Achievements and Avatar Effects
  both need a full subsystem (DB entity + grain + progress logic) built from zero, not just wiring.

**Story 4.2 — End-to-end catalog purchase — mostly done (2026-07-05)**
*As a* player, *I want* to buy from catalog, *so I can* get furniture.
- [x] Full purchase flow: page → offer → debit (via ledger) → inventory add — was already correct
  (`PurchaseFromCatalogMessageHandler` → `CatalogPurchaseGrain` → `ExecutePurchaseAsync`).
- [x] Club (HC) and member pricing management — already applied at purchase time
  (`CatalogOfferEntity.ClubLevel`/`DiscountPercent`); still missing a dashboard UI to edit it.
- [x] 6 landing-page/misc handlers wired (offer giftability, next/expiring limited offers, bonus
  rare promo, room-ad room list, new-additions no-op).
- [x] Builders Club: subscription tier (`BuildersClubTierEntity`, real furni-count reporting) —
  direct-to-room placement (`BuildersClubPlaceRoomItem`/`PlaceWallItem`) deferred, needs new
  room/furniture orchestration.
- [x] Room ads: full purchase flow (`RoomAdvertisementEntity`, `ExecutePurchaseAsync`-based).
- Deliberately deferred (own story, needs a real per-player campaign entity): Targeted offers
  (`GetTargetedOffer`/`GetNextTargetedOffer`/`SetTargetedOfferState`/`ShopTargetedOfferViewed`/
  `PurchaseTargetedOffer`) and the seasonal calendar daily offer.

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
