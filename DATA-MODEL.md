# Vortex Cloud — Data Model (final)

Consolidated and final specification for missing tables and design rules, aligned with real
repo conventions and the Habbo source (WIN63) as ground truth. All decisions are locked so no
developer or agent starts from scratch and ships only “works” implementations.

Summary: 0) conventions and naming · 1) friends (existing) · 2) groups · 3) rentable space ·
4) pets · 5) bots · 6) wired (persistence bug) · 7) trading · 8) metrics (tiering) · 9) checklist.

---

## 0. Conventions & naming rule

### 0.1 Base entity (inherited from `TurboEntity`, DO NOT redeclare)

| Column | Type | Role |
|---|---|---|
| `id` | int, PK identity | primary key |
| `created_at` | datetime | created (auto) |
| `updated_at` | datetime | modified (auto, computed) |
| `deleted_at` | datetime? | **soft delete** |

### 0.2 Style (to reproduce)

- `[Table("snake_case_plural")]`, `[Column("snake_case")]`.
- FK: `[Column("x_id")] public required int XEntityId;` + `[ForeignKey(nameof(XEntityId))]
  public required XEntity XEntity;` (nullable nav if FK nullable).
- Composite uniqueness: `[Index(nameof(A), nameof(B), IsUnique = true)]`.
- Required fields: `required`; optional nullable where appropriate. Enums: direct type, `[DefaultValue]` +
  `DatabaseGeneratedOption.None`, located in `Vortex.Primitives.<Domain>.Enums`.
- Soft delete everywhere; never hard delete.

### 0.3 Prefix rule (resolves `ltd_` inconsistency)

**Prefix = owning bounded context. Shared reference tables stay neutral (no prefix).**

State verified: `catalog_offers/pages/products/club_offers/club_gifts` are aligned, but
`ltd_series` / `ltd_raffle_entries` are not — LTD is a catalog concept.

- To fix: `ltd_series` -> **`catalog_ltd_series`**, `ltd_raffle_entries` ->
  **`catalog_ltd_raffle_entries`**.
- Keep neutral: `currency_types`. Currency does not belong to catalog; it is referenced by
  `economy_ledger`, `marketplace_offers`, and catalog. It is transversal → no catalog prefix
  (do not force `catalog_currency_types`).

> Practical rule: lock this now, apply to **all new** tables, and rename `ltd_*` in a
> dedicated migration when convenient — do not do a big-bang refactor mid-development.

---

## 1. Friends & messaging — EXISTING (do not recreate)

| Table | Entity |
|---|---|
| `messenger_friends` | `MessengerFriendEntity` (`player_id`, `requested_id`, `category_id?`, `relation`) |
| `messenger_requests` | `MessengerRequestEntity` |
| `messenger_blocked` | `MessengerBlockedEntity` |
| `messenger_ignored` | `MessengerIgnoredEntity` |
| `messenger_categories` | `MessengerCategoryEntity` |
| `messenger_messages` | `MessengerMessageEntity` |

Epic 5 for friends = handler wiring only, zero schema.

---

## 2. Groups / Guilds — IMPLEMENTED (see `Vortex.Database/Migrations/20260619035829_AddGroups.cs` onward)

Habbo-style group model. **Decision**: forum config is separated from identity (1:1 table), not columns on
`groups`.

### 2.1 `groups` — `GroupEntity` (identity, hot table)

| Column | Type | Null | Index |
|---|---|---|---|
| `name` | string(50) | no | — |
| `description` | string(255) | yes | — |
| `badge` | string(100) | no | — |
| `room_id` | int FK rooms | no | unique |
| `player_id` | int FK players | no | index |
| `type` | `GroupType` | no | — |
| `color_one` / `color_two` | string(12) | no | — |
| `admin_only_decoration` | bool | no | — |

```csharp
[Table("groups")]
[Index(nameof(RoomEntityId), IsUnique = true)]
[Index(nameof(OwnerPlayerEntityId))]
public class GroupEntity : TurboEntity
{
    [Column("name")] [MaxLength(50)] public required string Name { get; set; }
    [Column("description")] [MaxLength(255)] public string? Description { get; set; }
    [Column("badge")] [MaxLength(100)] public required string Badge { get; set; }
    [Column("room_id")] public required int RoomEntityId { get; set; }
    [Column("player_id")] public required int OwnerPlayerEntityId { get; set; }
    [Column("type")] [DefaultValue(GroupType.Open)] [DatabaseGenerated(DatabaseGeneratedOption.None)] public required GroupType Type { get; set; }
    [Column("color_one")] [MaxLength(12)] public required string ColorOne { get; set; }
    [Column("color_two")] [MaxLength(12)] public required string ColorTwo { get; set; }
    [Column("admin_only_decoration")] [DefaultValue(false)] public required bool AdminOnlyDecoration { get; set; }

    [ForeignKey(nameof(RoomEntityId))] public required RoomEntity RoomEntity { get; set; }
    [ForeignKey(nameof(OwnerPlayerEntityId))] public required PlayerEntity OwnerPlayerEntity { get; set; }
    [InverseProperty(nameof(GroupMemberEntity.GroupEntity))] public List<GroupMemberEntity>? Members { get; set; }
    [InverseProperty(nameof(GroupForumSettingsEntity.GroupEntity))] public GroupForumSettingsEntity? ForumSettings { get; set; }
}
```

### 2.2 `group_members` — `GroupMemberEntity`

| Column | Type | Null | Index |
|---|---|---|---|
| `group_id` | int FK groups | no | unique(group_id, player_id) |
| `player_id` | int FK players | no | ↑ + index |
| `rank` | `GroupMemberRank` | no | — |

### 2.3 `group_membership_requests` — `GroupMembershipRequestEntity`

`group_id`, `player_id` — unique(group_id, player_id). For `Exclusive` groups.

### 2.4 `group_forum_settings` — `GroupForumSettingsEntity` (1:1 config)

Created with defaults at group creation (invariant: one row per group).

| Column | Type | Null | Index |
|---|---|---|---|
| `group_id` | int FK groups | no | unique |
| `enabled` | bool | no | — |
| `read_permission` / `post_permission` / `thread_permission` / `mod_permission` | `GroupForumPermission` | no | — |

### 2.5 / 2.6 Forum: `group_forum_threads` / `group_forum_posts`

- **threads**: `group_id`(idx), `player_id`, `subject`, `state` (`GroupForumThreadState`),
  `is_pinned`, `post_count`, `last_post_at`(idx), `last_post_player_id?`.
- **posts**: `thread_id`(idx), `group_id`(idx), `player_id`, `message`(text), `state`
  (`GroupForumPostState`). Moderation is done through state changes only, never hard-delete.

### 2.7 Room → group link (mod `RoomEntity`)

```csharp
[Column("group_id")] public int? GroupEntityId { get; set; }
[ForeignKey(nameof(GroupEntityId))] public GroupEntity? GroupEntity { get; set; }
```

> **Circular relation** `groups.room_id` ↔ `rooms.group_id` must use non-cascade `OnDelete`
> (Fluent API). Deleting/detaching a group: set `rooms.group_id` to null first, then soft-delete.

### 2.8 Enums (`Vortex.Primitives.Groups.Enums`)

```csharp
public enum GroupType { Open = 0, Exclusive = 1, Private = 2 }
public enum GroupMemberRank { Member = 0, Admin = 1 }
public enum GroupForumThreadState { Open = 0, Locked = 1, Hidden = 2 }
public enum GroupForumPostState { Visible = 0, Hidden = 1, HiddenByAdmin = 2 }
public enum GroupForumPermission { Members = 0, Admins = 1, Owner = 2, Everyone = 3 }
```

---

## 3. Rentable Space — IMPLEMENTED (migration `20260620191212`, Epic 4)

Furniture type `furniture_rentable_space`. The space **is a furniture item**. Status packet:
`rented, canRent, canRentErrorCode, renterId, renterName, timeRemaining, price`.
Packets: `rentSpace/cancelRent/getRentableSpaceStatus(furniId)`.

**Locked decisions:** terms (price/duration/currency/HC) live on the **placed instance**
(`RentableSpaceTermsEntity`, 1:1 with `FurnitureEntity`, not `FurnitureDefinitionEntity`) — the room
owner reconfigures price/duration per placed space, so terms cannot be shared across all instances
of the same furniture type · state = **one row per instance, in-place updates** (MySQL does not
support filtered uniqueness) · history is **free via `economy_ledger`** (rental itself is a ledger entry).

### 3.1 `room_rentable_space_terms` — `RentableSpaceTermsEntity` (1:1 with placed instance)

| Column | Type | Null | Index |
|---|---|---|---|
| `furniture_id` | int FK furniture | no | unique |
| `price` | int | no | — |
| `currency_type_id` | int FK currency_types | no | — |
| `rent_duration_seconds` | int | no | — |
| `requires_hc` | bool | no | — |

### 3.2 `room_rentable_spaces` — `RoomRentableSpaceEntity` (state, one per instance)

| Column | Type | Null | Index |
|---|---|---|---|
| `furniture_id` | int FK furniture | no | unique |
| `renter_player_id` | int? FK players | yes | index |
| `rented_until` | datetime? | yes | — |

```csharp
[Table("room_rentable_spaces")]
[Index(nameof(FurnitureEntityId), IsUnique = true)]
[Index(nameof(RenterPlayerEntityId))]
public class RoomRentableSpaceEntity : TurboEntity
{
    [Column("furniture_id")] public required int FurnitureEntityId { get; set; }
    [Column("renter_player_id")] public int? RenterPlayerEntityId { get; set; }
    [Column("rented_until")] public DateTime? RentedUntil { get; set; }
    [ForeignKey(nameof(FurnitureEntityId))] public required FurnitureEntity FurnitureEntity { get; set; }
    [ForeignKey(nameof(RenterPlayerEntityId))] public PlayerEntity? RenterPlayerEntity { get; set; }
}
```

### 3.3 Placed furniture tag (modify `FurnitureEntity`) — atomic cleanup on expiry

```csharp
[Column("rentable_space_furniture_id")] public int? RentableSpaceFurnitureEntityId { get; set; }
[ForeignKey(nameof(RentableSpaceFurnitureEntityId))] public FurnitureEntity? RentableSpaceFurnitureEntity { get; set; }
```

> Expiry: return all furniture where `rentable_space_furniture_id = X` to inventory, then clear
> `renter` / `rented_until`. Bounds are validated in grain at placement.

### 3.4 Rules (source): one renter per space · **one space per player** (`can_rent_only_one_space`,
via `renter_player_id` index) · cancellation by furniture owner **or** staff (`hasSecurity(5)`).

---

## 4. Pets — IMPLEMENTED (see `Vortex.Database/Migrations/20260620231004_AddPets.cs` onward)

Standard Habbo pet model. A pet belongs to a player, lives in a room or in inventory, carries stats.

| Column | Type | Null | Index | Meaning |
|---|---|---|---|---|
| `player_id` | int FK players | no | index | owner |
| `room_id` | int? FK rooms | yes | index | current room, `null` = inventory |
| `name` | string(40) | no | — | name |
| `type` | int | no | — | species (dog/cat/…) |
| `race` | int | no | — | variant/breed |
| `color` | string(12) | no | — | color |
| `gender` | `AvatarGenderType` | no | — | sex |
| `level` | int | no | — | level |
| `experience` | int | no | — | xp |
| `energy` | int | no | — | energy |
| `nutrition` | int | no | — | hunger |
| `respect` | int | no | — | "scratches"/received respect |
| `x` / `y` | int | no | — | room position |
| `z` | double(10,3) | no | — | height |
| `direction` | int | no | — | facing |

```csharp
[Table("pets")]
[Index(nameof(OwnerPlayerEntityId))]
[Index(nameof(RoomEntityId))]
public class PetEntity : TurboEntity
{
    [Column("player_id")] public required int OwnerPlayerEntityId { get; set; }
    [Column("room_id")] public int? RoomEntityId { get; set; }
    [Column("name")] [MaxLength(40)] public required string Name { get; set; }
    [Column("type")] public required int Type { get; set; }
    [Column("race")] public required int Race { get; set; }
    [Column("color")] [MaxLength(12)] public required string Color { get; set; }
    [Column("gender")] [DefaultValue(AvatarGenderType.Male)] [DatabaseGenerated(DatabaseGeneratedOption.None)] public required AvatarGenderType Gender { get; set; }
    [Column("level")] [DefaultValue(1)] public required int Level { get; set; }
    [Column("experience")] [DefaultValue(0)] public required int Experience { get; set; }
    [Column("energy")] public required int Energy { get; set; }
    [Column("nutrition")] public required int Nutrition { get; set; }
    [Column("respect")] [DefaultValue(0)] public required int Respect { get; set; }
    [Column("x")] public required int X { get; set; }
    [Column("y")] public required int Y { get; set; }
    [Column("z", TypeName = "double(10,3)")] public required double Z { get; set; }
    [Column("direction")] public required int Direction { get; set; }

    [ForeignKey(nameof(OwnerPlayerEntityId))] public required PlayerEntity OwnerPlayerEntity { get; set; }
    [ForeignKey(nameof(RoomEntityId))] public RoomEntity? RoomEntity { get; set; }
}
```

> Breeding is confirmed in source (`ConfirmPetBreedingPetData`). No extra table required:
> a bred pet is a new `PetEntity`; traceability (if desired) is via nullable columns
> `parent_one_id` / `parent_two_id`.

### 4.1 `pet_food` — `PetFoodEntity` (food item config)

Verified on WIN63: food/drinks/toys are **furnitures** with `FurniturePetProductLogic`
(`useObject()` is how the pet uses them). Item → pet-type → effect mapping is
**pet domain config** (therefore `pet_` prefix, not `catalog_`, even if sold in catalog).
Effect values are server-owned (obfuscated client does not expose them, same as rental price).

| Column | Type | Null | Index | Meaning |
|---|---|---|---|---|
| `furniture_definition_id` | int FK definitions | no | unique with `pet_type` | food item |
| `pet_type` | int | no | unique with `furniture_definition_id` | species that can consume |
| `nutrition` | int | no | — | nutrition restored on consume |
| `energy` | int | no | — | energy restored on consume |
| `max_uses` | int | no | — | uses before the food item is spent |

```csharp
[Table("pet_food")]
[Index(nameof(FurnitureDefinitionEntityId), nameof(PetType), IsUnique = true)]
public class PetFoodEntity : TurboEntity
{
    [Column("furniture_definition_id")] public required int FurnitureDefinitionEntityId { get; set; }
    [Column("pet_type")] public required int PetType { get; set; }
    [Column("nutrition")] public required int Nutrition { get; set; }
    [Column("energy")] public required int Energy { get; set; }
    [Column("max_uses")] public required int MaxUses { get; set; }

    [ForeignKey(nameof(FurnitureDefinitionEntityId))] public required FurnitureDefinitionEntity FurnitureDefinitionEntity { get; set; }
}
```

> The unique index is on `(furniture_definition_id, pet_type)`, not on `furniture_definition_id`
> alone: the same furniture item can be configured as valid food for more than one pet type, each
> with its own nutrition/energy/max-uses tuning.
>
> Feeding flow (grain): pet moves to food, eats, `pet.nutrition += pet_food.nutrition`,
> `pet.energy += pet_food.energy`, and the food item is **consumed** (decrement `max_uses` via
> `extra_data` or remove the furni once exhausted).

### 4.2 Levels & commands (implemented via `PETS-DESIGN.md`)

Pet design (learning loop) implies two config tables. See `PETS-DESIGN.md` for runtime behavior.

**`pet_levels` — `PetLevelEntity`** (level threshold config):

| Column | Type | Null | Index | Meaning |
|---|---|---|---|---|
| `pet_type` | int | no | unique(pet_type, level) | species |
| `level` | int | no | ↑ | level |
| `experience_required` | int | no | — | cumulative XP required for this level |
| `energy_cap` | int | no | — | max energy at this level |
| `nutrition_cap` | int | no | — | max nutrition at this level |

**`pet_commands` — `PetCommandEntity`** (learnable command config):

| Column | Type | Null | Index | Meaning |
|---|---|---|---|---|
| `pet_type` | int | no | index | species |
| `command` | int | no | — | command identifier (gesture/trick) |
| `level_required` | int | no | — | minimum level to execute |

> A pet executes a command only if `pet.level >= level_required`. Same conventions as rest of
> model (`TurboEntity`, enums in `Vortex.Primitives.Pets.Enums`).

**Out of immediate scope:**
- **Color palettes** (`SellablePetPalettesEvent`) → `pet_palettes` (`pet_type`, palette, colors) —
  when catalog pet sales are implemented.

> Suggested order: food → levels → commands (see `PETS-DESIGN.md`). Each subsystem is one config table
> plus one runtime effect, same pattern as food.

---

## 5. Bots — TO CREATE (server bots / “rentable bot”, WIN63 confirmed)

A bot is a placeable NPC: avatar identity + position + a set of messages (speech, keyword
responses). Two tables.

### 5.1 `bots` — `BotEntity`

| Column | Type | Null | Index | Meaning |
|---|---|---|---|---|
| `player_id` | int FK players | no | index | owner |
| `room_id` | int? FK rooms | yes | index | room, `null` = inventory |
| `name` | string(40) | no | — | name |
| `motto` | string(255) | yes | — | mission |
| `figure` | string(100) | no | — | appearance |
| `gender` | `AvatarGenderType` | no | — | sex |
| `x` / `y` | int | no | — | position |
| `z` | double(10,3) | no | — | height |
| `direction` | int | no | — | orientation |

```csharp
[Table("bots")]
[Index(nameof(OwnerPlayerEntityId))]
[Index(nameof(RoomEntityId))]
public class BotEntity : TurboEntity
{
    [Column("player_id")] public required int OwnerPlayerEntityId { get; set; }
    [Column("room_id")] public int? RoomEntityId { get; set; }
    [Column("name")] [MaxLength(40)] public required string Name { get; set; }
    [Column("motto")] [MaxLength(255)] public string? Motto { get; set; }
    [Column("figure")] [MaxLength(100)] public required string Figure { get; set; }
    [Column("gender")] [DefaultValue(AvatarGenderType.Male)] [DatabaseGenerated(DatabaseGeneratedOption.None)] public required AvatarGenderType Gender { get; set; }
    [Column("x")] public required int X { get; set; }
    [Column("y")] public required int Y { get; set; }
    [Column("z", TypeName = "double(10,3)")] public required double Z { get; set; }
    [Column("direction")] public required int Direction { get; set; }

    [ForeignKey(nameof(OwnerPlayerEntityId))] public required PlayerEntity OwnerPlayerEntity { get; set; }
    [ForeignKey(nameof(RoomEntityId))] public RoomEntity? RoomEntity { get; set; }
    [InverseProperty(nameof(BotMessageEntity.BotEntity))] public List<BotMessageEntity>? Messages { get; set; }
}
```

### 5.2 `bot_messages` — `BotMessageEntity`

| Column | Type | Null | Index | Meaning |
|---|---|---|---|---|
| `bot_id` | int FK bots | no | index | bot |
| `mode` | `BotMessageMode` | no | — | RandomSpeech / KeywordResponse |
| `keyword` | string(100) | yes | — | trigger for keyword mode |
| `text` | string(255) | no | — | bot text |

```csharp
public enum BotMessageMode { RandomSpeech = 0, KeywordResponse = 1 }
```

> “Serve beverages/items” is a configurable bot action. If in scope, model it with `bot_serve_items`
> (`bot_id`, `item_definition_id`), but start with identity + messages first; that is the core.

---

## 6. Wired — not a schema gap, a persistence bug (priority #1)

**Observed in code.** `StuffPersistanceType` currently has two values:
`RoomActive = 0` and `Persistent = 1`. `FurnitureWiredLogic` sets `_stuffPersistanceType =>
StuffPersistanceType.RoomActive`. Wired furniture already persists state via `extra_data`
(`ExtraData.GetJsonString()`), and `WiredData` is JSON-serializable.

**Conclusion.** With `RoomActive`, data lives while room is active but is **not** written to DB:
wired config (selected items, delays, conditions) does not survive room unload/reboot. This is
silent data loss, not missing schema.

**Fix (no schema migration):**
- Wired furniture must use `StuffPersistanceType.Persistent` and serialize `WiredData` (already JSON-serializable)
  into `extra_data`, which already exists.
- Separate **config** (persisted fields: `IntParams`, `StuffIds`, `StringParam`, sources) from
  ephemeral runtime state (e.g., “already triggered” counters), which remain in memory.
- Add round-trip coverage: place wired item, configure, unload room, reload → same config preserved.

> This is the only item on this list that can still cause immediate player data loss today. Finish it
> before pets/bots/groups.

---

## 7. Trading — ledger-backed logging (no persistent state)

Transient exchange flow (offer grain, lock, confirmation). No dedicated state table. Anti-duplication
is handled with existing `item_events` / `economy_ledger`. The real topic is **atomic commit** and
server-side possession validation (Epic 6).

---

## 8. Metrics / observability — keep tables; tier storage by access pattern

**Verdict:** table count is justified; merging would be a regression. The forms are truly different:
`economy_ledger` (typed financial data, `delta`/`balance_after`, no blobs) ≠
`performance_logs` (legacy client telemetry: `flash_version`, `frame_rate`, `gc`) ≠
`error_groups` + `error_occurrences` (Sentry-like pattern/fingerprint) ≠ `audit_events` /
`item_events` (`data` text payload). Merging creates God-tables/EAV-like structures with sparse columns,
loss of typed columns/indexes, and impossible retention by type.

**Better approach = tiering by access pattern, not table reduction:**
- **Keep in MySQL (transactional truth):** `economy_ledger`, `item_events`, `audit_events`.
- **Move to time-series/analytics store** (ClickHouse or Prometheus/Grafana via existing
  `System.Diagnostics.Metrics`): high-volume telemetry — `performance_logs`, and later server performance metrics.
- **Evaluate/remove:** `performance_logs` contains `flash_version` and is legacy client logging; with
  2026 observability, this is a table candidate for removal, not for merging.

---

## 9. Anti-error checklist (review before adding a table)

- [ ] Inherits `TurboEntity`; do not redeclare id/created_at/updated_at/deleted_at.
- [ ] “Delete” means `deleted_at`, never hard delete.
- [ ] FK uses `[Column("x_id")] int` + `[ForeignKey]` nav pair, nullable configured correctly.
- [ ] Relational pairs use `[Index(..., IsUnique = true)]` composite constraints.
- [ ] Enums in `Vortex.Primitives.<Domain>.Enums`, `[DefaultValue]` + `DatabaseGeneratedOption.None`.
- [ ] **Naming:** prefix = owning bounded context; shared reference = neutral (e.g.
  `catalog_ltd_*`, but `currency_types` stays neutral).
- [ ] Circular relations use non-cascade `OnDelete` (Fluent API).
- [ ] Separate cold/optional/evolving config → one 1:1 table (forum, rent terms), but one-field config should
  not be over-expanded.
- [ ] Instance state ≠ config (type): rent terms are config, room rent state is runtime.
- [ ] No extra history table when `ledger` already provides history.
- [ ] Player-stored durable data must use `Persistent`, not `RoomActive` (see wired section).
- [ ] Do not recreate existing tables (friends/messaging already implemented).
