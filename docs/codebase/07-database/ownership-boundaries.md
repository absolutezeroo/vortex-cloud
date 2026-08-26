# Database ownership boundaries

## Purpose

The standing rule for anyone about to write SQL against this schema.

> **A table in MySQL is not the owner of the value in it.**

The full state→owner table, the loss windows and the safe-fix conventions live on
[State ownership](../03-orleans/persistence.md), which is the canonical page. This page adds the
database-side view: what the schema itself guarantees, and where the guarantee stops.

## Why the usual Orleans warning does not apply

The standard hazard is "a `[PersistentState]` grain hydrates from its store, so direct DB edits get
overwritten by stale store data."

**No grain here uses `[PersistentState]`.** The hazard is the inverse and worse: grains hydrate from
MySQL, hold authoritative mutable state in plain in-memory fields, and write it back. There is no
store to inspect, and a raw `UPDATE` is overwritten by a grain's in-memory snapshot — often within
2 seconds.

`Vortex:Orleans:GrainCollectionAge` is **2 minutes**. That is the window in which a player who "just
logged off" still has a grain holding their row.

## The three red zones

Writes here are silently discarded while the owner is active:

| Table | Owner | Overwrite mechanism |
|---|---|---|
| `furniture` (position, rotation, z, owner, `extra_data`, wall offset) | `RoomGrain` → `RoomPersistenceGrain` | force-marks 8 columns modified every ≤2 s and on deactivate |
| `players` (name, motto, figure, gender, achievement score, respect counters) | `PlayerGrain` | `ExecuteUpdate`s all 8 columns from memory on every mutation and on deactivate |
| `pets` | `RoomPetSystem` | overwrites 15 columns every ≤60 s |

Two more are red for a different reason — the table *is* the state machine, and editing it breaks
correctness rather than being lost:

| Table | Why |
|---|---|
| `commerce_operations` / `commerce_receipts` | `(operation_id, step_key)` uniqueness **is** the replay guard |
| `marketplace_offers` | the conditional `ExecuteUpdate` on `state` is the concurrency pivot; flipping it by hand can double-sell or strand an item |

## Constraints that are business logic

About fifteen indexes encode a rule rather than a performance decision. Changing one changes
behaviour.

| Constraint | Encodes |
|---|---|
| `commerce_receipts (operation_id, step_key)` unique | **the replay guard.** `CommerceJournal.TryRecordStepAsync` deliberately lets the insert fail and reads that failure as "this step already ran" |
| `nft_assets (product_code, serial_number)` unique | **provenance** — what replaces a chain's uniqueness |
| `rooms (group_id)` unique | at most one guild per room, enforced on the **nullable** side because MySQL permits repeated NULLs, so dissolving a guild frees the room. A unique index on `groups.room_id` would be permanently blocked by the soft-deleted guild row — `VortexDbContext` explicitly re-declares that one as `IsUnique(false)` to undo EF's convention |
| `catalog_voucher_redemptions (voucher_id, player_id)` unique | one redemption per player per code, as a database guarantee rather than a read-then-write |
| `player_daily_tasks (player_id, task_id, assigned_on)` unique | the **date in the key** is what makes a daily task re-assignable |
| `security_tickets` unique on **both** `player_id` and `ticket` | one live SSO handoff per player, and no ticket collision |
| `player_currencies (player_id, currency_type_id)` unique | the wallet invariant |
| `account_bans (player_account_id)` unique | at most one *live* ban row — which is why unbanning soft-deletes and re-banning revives with `IgnoreQueryFilters()` |
| `sanction_presets (kind, preset_index)` unique | the ban dialog indexes presets **positionally** — a wire-shape constraint |
| `wired_permanent_variables (target_type, target_id, variable_id)` unique | set/create/delete semantics |
| `furniture_definitions (sprite_id, product_type, furni_category)` unique | note `name` is deliberately **not** unique |
| `messenger_*` on the ordered pair `(player_id, other_id)` | friendships, requests, blocks and ignores are directional rows; the uniqueness is what makes "add if absent" safe |

## Delete behaviour as policy

One rule runs through the whole progression schema, spelled out in `VortexDbContext.OnModelCreating`:

> **A definition owns its children (`Cascade`); player progress cascades with the player and is
> `Restrict` against the definition.**

So editing or retiring content never silently destroys what people already earned.

Two deliberate exceptions, both documented in place:

- `prize_pool_entries` and `prize_pool_bindings` carry a plain definition id with **no FK** — effect
  and club prizes legitimately leave it 0, and a hotel may bind an id its furnidata has not shipped.
- `player_prize_claims` cascades on **both** sides, so retiring a pool clears "already taken" and the
  event can be re-run.

Other shapes worth knowing:

- `cfh_tickets` has three FKs to `players` (reporter, reported, picker), **all `Restrict`** — a ticket
  outlives whichever party it names, and MySQL never has to choose a cascade path.
- `groups.room_id ↔ rooms.group_id` is modelled as **two independent one-directional relationships**,
  both `Restrict`, so MySQL never builds a cascade cycle. Dissolving a guild detaches and then
  soft-deletes.
- `furniture.rentable_space_furniture_id` is self-referencing with `OnDelete(SetNull)`, so deleting a
  space furni does not cascade-delete the items standing in it.
- `poll_questions.parent_question_id` is a self-FK forced to `Restrict` — MySQL rejects a cascading
  self-FK.
- `PluginDbContextBase` forces **every** FK to `Restrict`.

## Soft delete exists and is not used

`VortexEntity` carries `DeletedAt`, and `ApplySoftDeleteQueryFilter` attaches a global
`DeletedAt == null` filter to **every** mapped entity — so the runtime *would* honour a soft delete.

Sanctioned opt-outs are few and explicit: `PlayerGrain` (`AccountBans.IgnoreQueryFilters()`) and
`RoomModerationStore` ×2 (ban/mute revival on re-application).

**Nevertheless the admin surface is essentially hard-delete** — 45 removal call sites and 35 delete
routes, against a single soft delete (`ContentAdminService.NftAvatars.cs` revoking an avatar copy) and
five `DeletedAt = null` resurrections. → [Dashboard operations](../08-dashboard/operations.md)

> **Do not "fix" this by switching the admin surface to soft delete.** The catalogue runtime does not
> filter `DeletedAt` on the paths that matter; a half-migration would leave deleted content live.

## Safe data fixes

`scripts/sql/` holds six one-off scripts. The convention, readable from the files themselves:

- a comment header stating intent
- idempotent and additive — `NOT EXISTS` guards, `INSERT … SELECT`, temporary tables
- **reference tables only**: `catalog_pages`, `catalog_offers`, `furniture_definitions`, `cfh_topics`,
  `role_permissions`

That last constraint is what makes them safe: those are exactly the tables no grain holds live state
for, only singleton snapshots.

**A script against them still needs its `ReloadAsync`.** Otherwise the row changes and the hotel does
not. Which reload depends on the table — see the provider list on
[Orleans overview](../03-orleans/overview.md).

Never point a script at anything in the red zones above.

> `CLAUDE.md` advertises a `/sql-fix` command; `.claude/commands/` contains only `web-guidelines.md`,
> so that checklist is not in this repository.

## The one raw-SQL site in production

`Vortex.Database/Migrations/MigrationHelper.cs` — `UninstallAsync<TContext>` uses `ExecuteSqlRawAsync`
to drop a plugin's prefixed tables. It **refuses an empty prefix** (which would drop the whole schema)
and sanitises `\ ' % _` before the `LIKE`. Called only from `PluginManager` via `IPluginDbModule`.

## Known unknowns

- **Unknown:** whether any deployed database has the orphan migration `20260205185839_AddedModels` in
  its `__EFMigrationsHistory`. → [Migrations](migrations.md)
- **Unknown:** whether `rooms.users_now` and `players.status` should be maintained or dropped. Both are
  written once at creation, never updated, and still read.
  → [State ownership](../03-orleans/persistence.md)

## Sources

- `Vortex.Database/Context/{VortexDbContext,DbContextBase,PluginDbContextBase}.cs`
- `Vortex.Database/Extensions/ModelBuilderExtensions.cs` — `ApplySoftDeleteQueryFilter`
- `Vortex.Database/Entities/VortexEntity.cs`
- `Vortex.Database/Entities/**` — the `[Index]` attributes cited above
- `Vortex.Database/Commerce/CommerceJournal.cs` — `TryRecordStepAsync`
- `Vortex.Database/Migrations/MigrationHelper.cs`
- `Vortex.Rooms/RoomModerationStore.cs`, `Vortex.Players/Grains/PlayerGrain.cs`
- `scripts/sql/*.sql`
