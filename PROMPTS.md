# Vortex Cloud — Prompts (ROADMAP execution layer)

This document is the **bridge** between ROADMAP (what and why) and code. Each prompt is an
executable *story* modeled after BMAD: you pass it directly to Claude Code. Design:
- ROADMAP.md: what to do, in which order, and its Definition of Done
- DATA-MODEL.md: exact tables/entities (source of record)
- docs/walkthroughs: practical end-to-end wiring pattern in this repo

> **Why prompts are concise.** DATA-MODEL and walkthroughs already contain details. A prompt’s
> job is to point the agent to authoritative documentation and enforce roadmap Definition of Done — not to
> duplicate schema details. A prompt that restates the full schema is more likely to drift from schema.

---

## Mapping matrix (Roadmap ↔ Prompt ↔ Docs ↔ Status)

| Epic / Story (ROADMAP) | Prompt | Read first | Status |
|---|---|---|---|
| 0 — error strategy + 2.x wired | **P-WIRED** | DATA-MODEL §6 | ⬛ highest priority |
| 0.1 / 2.4 — test harness | **P-TEST** | DATA-MODEL, vertical-slice.md | ⬛ to do |
| 2 — permissions/moderation | *(already executed prompt)* | — | ✅ in `main` |
| 3 — WebApi ASP.NET migration | *(prompt already provided)* | — | 🟧 `feat/webapi-aspnetcore-migration` branch |
| 5 — groups | **P-GROUPS** | DATA-MODEL §2 | ⬛ to do |
| 4 — rentable space | **P-RENTABLE** | DATA-MODEL §3 | ⬛ to do |
| (gap) — pets | **P-PETS** | DATA-MODEL §4 | ⬛ to do |
| (gap) — bots | **P-BOTS** | DATA-MODEL §5 | ⬛ to do |
| 1 / 4 / 5 — fill handlers | **P-FILL** (template) | request-lifecycle.md, add-a-feature.md | ⬛ repeatable |
| 8 — metrics tiering | **P-METRICS** | DATA-MODEL §8 | ⬛ to do |

**Execution order recommended:** P-WIRED → P-TEST → P-GROUPS → P-RENTABLE → P-PETS/P-BOTS →
P-FILL (domain by domain) → P-METRICS. (P-WIRED first: it is the only task fixing currently persistent
player data loss.)

**Common “read first” contract for all prompts:** `CONTEXT.md` (boundaries),
`AGENTS.md` (repo DoD), and `docs/walkthroughs/request-lifecycle.md` (handler→grain→stream).
The prompts below mention this once each and should not duplicate it.

---

## P-WIRED — Fix wired persistence (PRIORITY #1)

```
Implement: ROADMAP Epic 0 (error strategy) + wired diagnosis.
Read first: DATA-MODEL.md §6, then CONTEXT.md and docs/walkthroughs/request-lifecycle.md.

Problem (already diagnosed, see §6): FurnitureWiredLogic uses
StuffPersistanceType.RoomActive, so wired config is NOT written to DB and is lost on room unload/reboot.
extra_data exists and WiredData is already JSON-serializable. This is not a schema change; it is a persistence
mode fix.

Actions:
1. Persist wired config: wired furniture serializes its WiredData in extra_data and reloads it
   when room activates.
2. Separate persisted config (persist: IntParams, StuffIds, StuffIds2, StringParam, VariableIds,
   sources) from ephemeral runtime state (counters like “already triggered”) that remains in memory.
3. Choose correct persistence mode: durable player data must be Persistent, not RoomActive.

Definition of Done: one test proves round-trip — place a wired item, configure it, unload the room,
reload the room -> config is identical. No other wired furniture is impacted.
```

---

## P-TEST — Test harness for pure policies

```
Implement: ROADMAP Epic 0.1 / Story 2.4.
Read first: docs/patterns/vertical-slice.md, existing Turbo.WebApi.Tests project (for pattern),
then CONTEXT.md.

Actions:
1. New project Turbo.Permissions.Tests (xunit + FluentAssertions, mirroring Turbo.WebApi.Tests).
2. Cover RoomSecurityPolicy.ResolveControllerLevel: System, superuser, ModerateAny, BuildAny,
   explicit owner, rights, none.
3. Cover ModerationPolicy.IsAllowed: each action × specific capability × ModerateAny × wildcard × deny.
4. Tests run in quality gate.

Definition of Done: all decision branches of both policies covered; gate executes the tests.
Any failing case must fail the build.
```

---

## P-GROUPS — Group entities + migration

```
Implement: ROADMAP Epic 5 / Story 5.2.
Read first: DATA-MODEL.md §2 (authoritative table source), then CONTEXT.md.

Actions — generate EXACTLY the entities defined in §2, using repo conventions:
TurboEntity, [Table]/[Column] snake_case, FK + required, enums in
Turbo.Primitives.Groups.Enums:
- GroupEntity, GroupMemberEntity, GroupMembershipRequestEntity, GroupForumSettingsEntity,
  GroupForumThreadEntity, GroupForumPostEntity.
- Add nullable group_id to RoomEntity.
- Enums in §2.8.
- Corresponding EF migration.
- Fluent API: non-cascade OnDelete for circular relation groups.room_id ↔ rooms.group_id
  (see warning in §2.7).
- On group creation, create one `group_forum_settings` row with defaults
  (invariant: one row per group).

Do not recreate messenger tables (friends already exist, §1). Do not place forum permissions
on GroupEntity (they belong in GroupForumSettingsEntity, as decided in §2.4).

Definition of Done: entities compile, migration applies, non-cascade OnDelete configured, test creates
a group and verifies its settings row.
```

---

## P-RENTABLE — Rentable space (entities + behavior)

```
Implement: ROADMAP Epic 4 (economy).
Read first: DATA-MODEL.md §3 (entities + packet mapping + WIN63 source behavior), then CONTEXT.md and
docs/walkthroughs/request-lifecycle.md.

Actions:
- Entities §3: RentableSpaceTermsEntity (terms on furniture type), RoomRentableSpaceEntity
  (state, one row per instance, in-place updates), RentableSpaceFurnitureId tag on FurnitureEntity.
  Create migration + FK.
- Handlers rentSpace / cancelRent / getRentableSpaceStatus (furni id) wired through handler→grain flow.
  Status returns fields from mapping §3.2.
- Rules (source §3.4): one renter per space; one rentable space per player (enforced via index
  renter_player_id); cancellation by furniture owner or staff with level ≥ threshold.
- On rent: debit via ledger (`economy_ledger`) — this is also history; no dedicated table (decision §3).
- On expiry: return all items where rentable_space_furniture_id = X to inventory for the renter,
  then set renter / rented_until to null. Placement bounds validated in grain.

Definition of Done: rent/cancel/expire works, each rent creates a ledger entry, one player cannot rent more
than one space, furniture returns on expiry. Tests on these invariants.
```

---

## P-PETS — Pets entities

```
Implement: ROADMAP (pets gap).
Read first: DATA-MODEL.md §4 (pet + food + sub-systems), then CONTEXT.md.

Actions:
- PetEntity (§4) in repo conventions + migration. Defaults: level 1, xp 0, respect 0.
  Nullable room_id. Gender via AvatarGenderType.
- PetFoodEntity (§4.1): food/product config (furniture_definition_id, pet_type, nutrition).
- Feeding logic (grain): pet moves to food -> nutrition += pet_food.nutrition -> food item consumed.
- Optional breeding: parent_one_id / parent_two_id if scoped.

Do not model commands/levels/palettes now (§4.2) — these are separate follow-up slices, food first.

Definition of Done: a pet can be created, placed, moved back to inventory, and fed (nutrition increases,
food is consumed). Include feed test.
```

---

## P-BOTS — Bot entities

```
Implement: ROADMAP (bots gap).
Read first: DATA-MODEL.md §5, then CONTEXT.md.

Actions: generate BotEntity + BotMessageEntity (§5) + BotMessageMode enum, with repo conventions + migration.
Start with identity + position + messages (random/keyword); do NOT implement serving items unless explicitly
in scope (optional table bot_serve_items, §5.2).

Definition of Done: entities compile, migration applies, a bot can be placed with messages.
```

---

## P-FILL — Fill handlers in one domain (reusable template)

```
Implement: ROADMAP Epic 1 (playable core) / 4 / 5, domain = <DOMAIN> (e.g. Navigator, Room,
Inventory, RoomSettings…).
Read first: docs/walkthroughs/request-lifecycle.md AND add-a-feature.md (flow and placement), docs/glossary.md as needed,
then CONTEXT.md.

Actions for domain <DOMAIN>:
1. List all stub handlers in Turbo.PacketHandlers/<DOMAIN> (those ending with
   `await ValueTask.CompletedTask`).
2. Implement one by one following walkthrough flow: handler = orchestration only, resolves grain, delegates;
   business logic lives in grain/module; broadcast via SendComposerToRoomAsync; never DB or socket work in handler.
3. Gate every sensitive action by capability (IPermissionService / RoomSecurityModule), never by trusted client flag.
4. Audit sensitive actions with observability events.

Definition of Done (ROADMAP §5): no new stubs; stub count in domain decreased; sensitive actions are gated
and audited; full end-to-end playable path for that domain.

⚠ Execute this prompt for ONE domain at a time. Start with Navigator (8% — entry point),
then Room, then RoomSettings, then Inventory.
```

---

## P-METRICS — Observability storage tiering

```
Implement: ROADMAP Epic 8.
Read first: DATA-MODEL.md §8, then existing observability architecture (Turbo.Observability).

Actions (decision §8: DO NOT merge tables, tier by pattern):
1. Keep in MySQL: economy_ledger, item_events, audit_events (transactional truth).
2. Route high-volume telemetry (perf + server metrics) to `System.Diagnostics.Metrics` meter and export
   to OTel/Prometheus.
3. Evaluate whether `performance_logs` should be removed (legacy client logging, flash_version) and switch to
   real observability.
4. Apply RGPD retention/purge for audit_events (Epic 7 / Story 7.1).

Definition of Done: high-volume telemetry no longer hits transactional DB, audit retention is implemented,
performance_logs is resolved (kept with justification or removed).
```

---

## Maintenance note

When a table changes in DATA-MODEL.md or a story changes in ROADMAP.md, **the prompt itself must not change** —
it points to authoritative documentation. This is the point: one source of truth per topic.
Update the mapping matrix above (Status column) as work progresses.
