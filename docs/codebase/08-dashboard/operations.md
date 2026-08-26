# Dashboard operations and live-state coherence

## Purpose

An admin write that persists successfully is not necessarily a write that *took effect*. This page
classifies every admin operation by what it actually reaches, and names the cases where the database
and the live hotel can disagree.

## The four classes

| Class | Shape | Coherent? |
|---|---|---|
| **(a)** | pure DB write; the reader queries per request | ✅ if the per-request claim is true |
| **(b)** | grain-method call — the grain is the single writer | ✅ by construction |
| **(c)** | DB write **+** reload of the cache it feeds | ✅ if the reload succeeds |
| **(d)** | DB write to grain-owned state with **no live path** | ❌ divergence |

**No class-(d) operation was found in the dashboard write surface.** Where a live owner exists, the
write is *refused* rather than allowed — see below.

## Enforcement

`DashboardOperationsService` contains **zero** direct DB access — no `SaveChangesAsync`, no
`ExecuteUpdateAsync`, no `ExecuteDeleteAsync`. Every write is a grain call or an
`I<Domain>AdminService` call. `IDbContextFactory` appears in exactly two files, both read services.

`scripts/hooks/check-architecture-walls.mjs` Wall 2 is one line:

```js
const dashboardWrites = hits('Vortex.Dashboard.API', /\bSaveChangesAsync\b/);
```

What it does **not** catch, all verified as currently unused but unguarded: `ExecuteUpdateAsync` /
`ExecuteDeleteAsync` (the exact APIs `ForensicsPurgeService` uses), synchronous `SaveChanges()`,
`ExecuteSqlRawAsync` / `FromSqlRaw`, and anything in `Vortex.WebApi` — which does call
`SaveChangesAsync` and is not walled.

> **Structurally, the wall is about the file, not the effect.** An `I<Domain>AdminService` that writes
> grain-owned state with no live path satisfies it perfectly, because the `SaveChangesAsync` is in
> another project. Class (d) is invisible to it by construction. The table below is the check the hook
> cannot be.

## (b) — grain-method calls

| Operation | Path |
|---|---|
| `ops.currency.credits.grant` / `.activitypoints` / `.collectibles` | `GetPlayerWalletGrain(...).GrantCreditsAsync` / `GrantActivityPointsAsync` / `GrantCurrencyAsync` |
| `ops.item.grant` | `GetInventoryGrain(...).GrantFurnitureDefinitionAsync` |
| `ops.player.ban` / `unban` / `trading_lock` / `trading_unlock` | `GetPlayerGrain(...).ApplyAccountBanAsync` / `ApplyTradingLockAsync` |
| `ops.player.kick` | `ISessionGateway.RemoveSessionFromPlayerAsync` |
| `ops.player.mute` | `GetPlayerPresenceGrain().GetActiveRoomAsync()` **first**, then `GetRoomModeration(roomId).MuteUserAsync` — refuses `target_not_in_room` up front rather than letting the grain no-op |
| `ops.room.close` / `ops.room.kick` | `GetRoomCore(...).DeactivateRoomAsync` / `GetRoomModeration(...).KickUserAsync` |
| `ops.cfh.pick` / `close` / `release` | `GetModerationQueueGrain()` — the doc names the reason: the in-client mod tool uses the same grain, so the two **contend** rather than overwrite |
| `ops.vouchers.create` / `deactivate` | `GetVoucherGrain(code)` |
| `ops.config.set` | `GetServerConfigGrain().SetValueAsync` (write-through DB + live cache); key validated against `ConfigKeyCatalog`, value against the declared `ConfigValueKind` |
| `ops.content.effect.grant` | `GetPlayerEffectGrain(...).AddEffectAsync` — routed through the grain **specifically** so `AvatarEffectAdded` reaches the client |

### The staff actor

Room-scoped grain calls need a real `PlayerId` and reject `ActionContext.System`. So
`ResolveStaffActorPlayerIdAsync` resolves the reserved `__dashboard_staff__` player — seeded by the
`SeedDashboardStaffActor` migration — once, behind a `SemaphoreSlim`.

**If that migration has not run, every room-scoped operation fails with
`dashboard_staff_actor_missing`.**

## (c) — write then reload

Verified reload-after-every-write in all of: `CatalogAdminService` (reloads **both** the
`NormalCatalog` and `BuildersClubCatalog` providers unconditionally — the class doc says this is
deliberate over-reloading rather than inferring which tree an edit touched), `TargetedOfferAdminService`,
`FurnitureAdminService`, `NavigatorAdminService` (12 write sites, 12 reloads), `PollAdminService`,
`PrizePoolAdminService`, `QuestAdminService`, `MysteryBoxAdminService`, `ContentAdminService`
(achievements, collections, store, minting, currencies), `QuestContentAdminService`.

The reload wrappers **rethrow**: `QuestContentAdminService.ReloadGoalAsync`,
`FurnitureAdminService.ReloadAsync` and `ContentAdminService.ReloadCurrenciesAsync` each log
*"…is now stale until the next reload or restart"* and `throw`, so a failed reload surfaces as a
failed operation.

That is the right shape — but note that the write **has already committed**, so the operator sees a
red result over a durable change. See caveat 3.

## (a) — pure DB, read per request

Each of these claims "the reader queries per request". Where that was independently traced, it is
marked ✅.

| Operation | Claim | Verified |
|---|---|---|
| `ops.daily_task.*` | read per request by the player grain | ✅ `PlayerDailyTaskGrain` reads `DailyTasks.AsNoTracking()` per call |
| `ops.content.badge.grant/revoke` | the badge grain re-reads per request | ✅ `PlayerBadgeGrain` is a pure DB gateway with no fields |
| `ops.content.nftavatar.*` | the wardrobe grain reads per request | ✅ `PlayerNftWardrobeGrain.GetWardrobeAsync` queries `AsNoTracking()` per call |
| `ops.staff.preset.*` | no caching needed | ✅ `SanctionPresetService` opens a context per lookup — **but** the client's preset dropdown is pushed once per tool-open, so an edit is invisible in an already-open mod tool |
| `ops.staff.role.*` / `assignments` | invalidation is explicit | ✅ `StaffAdminService` calls `IPermissionService.InvalidateAccount` for every affected account. `CreateRoleAsync` needs none; `DeleteRoleAsync` refuses `role_still_assigned` |
| `ops.content.builders_club.*` | "read straight from the table on every check" | ⚠️ **Unverified** — not traced to its consumer |
| `ops.content.rentable_terms.*` | no claim in the file | ⚠️ **Unverified** |
| `ops.content.hand_item.*` | "read from the database every time a pet is fed" | ⚠️ **Unverified** |

## (d) — refused, not allowed

The one place a live owner exists and the write would diverge, it is **rejected**:

```csharp
// ContentAdminService.UpdateBotAsync / DeleteBotAsync
if (entity.RoomEntityId is not null) return "bot_is_placed";
// "A placed bot is owned by its room's grain… Writing the row underneath would leave the two disagreeing"
```

That is the pattern to copy for any future admin surface over grain-owned state.

## Six coherence caveats

Each is real, and none is currently warned about in the code.

### 1 · A furniture-definition edit does not reach active rooms 🔴

`POST /api/v1/operations/furniture/definitions/update` is class (c) — `FurnitureAdminService.UpdateAsync`
reloads `IFurnitureDefinitionProvider`, which publishes a new immutable dictionary via
`Volatile.Write`.

But `Vortex.Rooms/Providers/RoomItemsProvider.cs` copies the `FurnitureDefinitionSnapshot` **into each
`RoomItem` at materialization**, and `RoomGrain` holds the provider. So an active room keeps the old
width, length, stack height and logic for items it already built. The change lands on new placements
and on rooms activated after the write.

Nobody's bug — a documented-nowhere consequence of snapshot-at-materialization. Strongly supported;
see the *Known unknowns* on [Furniture](../04-rooms/furniture.md) for the limit of that reading.

### 2 · `ops.content.effect.revoke` is silent to a connected client 🟠

`ContentAdminService.RevokeEffectAsync` deletes the `player_effects` row directly — no `grainFactory`
call, no composer — while its sibling `GrantEffectAsync` routes through `PlayerEffectGrain.AddEffectAsync`
**precisely so** `AvatarEffectAddedMessageComposer` fires.

`PlayerEffectGrain` caches nothing, so the *data* is coherent. The client's effect list is not
corrected until it re-reads. Asymmetric by omission.

### 3 · A reload that throws leaves a committed write behind 🟠

All three rethrowing reload wrappers report failure *after* `SaveChangesAsync`. The operator sees
`operation_failed`; the row is written; the live snapshot is stale until the next successful write or
a restart.

### 4 · The audit is best-effort under load 🟠

`ChannelAuditSink.Emit` writes to a bounded channel and, on `TryWrite` failure, **drops the record**
with `_metrics.AuditWriteFailed("enqueue")` and a warning. "Every operation emits a durable audit
record" is true only while the writer keeps up.

### 5 · Bulk statements leave no before-image 🟠

`ForensicsPurgeService` uses `ExecuteDeleteAsync` / `ExecuteUpdateAsync`, which bypass the change
tracker. So `ops.player.forensics_purge` audits the action and its counts but records **no `changes`**.
`EntityChangeInterceptor`'s own doc calls this out rather than pretending otherwise.

That operation is also irreversible and step-up gated.

### 6 · Catalog and navigator reloads are hotel-wide and immediate 🟡

`CatalogAdminService` reloads both catalog trees on every write; `NavigatorAdminService` reloads on all
12. Correct — but it means a **half-finished admin edit is live to every player the moment it is
saved**. There is no draft state.

## Auditing

`ExecuteAsync` emits one `AuditEvent` per operation, on every exit path:

| Field | Contents |
|---|---|
| Category | `Auth`, `Staff`, `Moderation`, `Economy`, `Item`, `Room`, `Security`, `Social`, `System`, `RentableSpace`, `Progression` — stored as a **string**, so appending needs no migration |
| `Data` | `{ actor, reason, detail, actorAccountId, actorMismatch, changes }` |

Dashboard operations default to `Staff`; moderation and room ops pass `Moderation`; forensics purge
passes `Security`; the HTTP access emitter always writes `Security` / `audit.viewed`.

**`changes` is omitted when empty, deliberately, so `changes: []` never reads as "nothing changed".**

Two attribution details worth knowing:

- **The actor string is not trusted as security context.** `Emit` writes both the caller-supplied
  `actor` and `ActorSecurityContext.Current?.AccountId`, plus `actorMismatch` — null when they agree
  or when there is no request, `true` when they disagree.
- **Every write carries a mandatory audited reason.** `IReasonedRequest` +
  `DashboardRequestValidationFilter`, attached once in `MapPost`. Floor is 3 trimmed characters,
  matching the client's `reasonOk`. `DashboardOperationReasonTests` drives real requests through
  `TestServer` to prove it, because a filter is not endpoint metadata and the matrix test cannot see
  it.

## Reversibility

**Admin deletes are hard, with one exception.** 45 `.Remove(...)` / `.RemoveRange(...)` call sites
across 12 admin service files, exposed as **35 `/delete` routes**.

A global soft-delete query filter *does* exist — `ApplySoftDeleteQueryFilter` attaches
`DeletedAt == null` to every mapped `VortexEntity` — so the runtime would honour a soft delete. The
admin surface uses it in exactly two ways:

- **one soft delete**: `ContentAdminService.NftAvatars.cs` — revoking an NFT avatar copy sets
  `copy.DeletedAt = DateTime.UtcNow`
- **five resurrections**: `ContentAdminService.Hotel.cs` ×4 and `.NftAvatars.cs` ×1 set
  `DeletedAt = null` to revive a previously removed row rather than inserting a duplicate

Everything else removes the row.

Reversibility therefore rests on two things:

1. `EntityChangeInterceptor` records the **full row** on a delete (*"on a delete the whole row is the
   thing being lost"*), minus 14 redacted column names (`Password`, `PasswordHash`, `Token`, `Secret`,
   `Email`, `IpHash`, …) and truncated at 512 chars per value.
2. `IDatabaseBackupService`.

**Restore is deliberately absent from the API.** `DashboardEndpoints.Backup.cs`'s header: *"rolling
the whole database back to a dump discards every change since, which is a decision for a shell and a
maintenance window."*

## Known unknowns

- **Unknown:** whether builders-club tiers, rentable-space terms and hand-item definitions really are
  read per request. Three class-(a) claims not traced to their consumers.
- **Unknown:** whether NFT claims have a cached owner. `ops.content.claim.create` / `.delete` write
  with no reload and carry no comment explaining why.
- **Unknown:** whether `ISessionGateway.RemoveSessionFromPlayerAsync` (the dashboard kick) also tears
  down room presence or only the socket.
- **Unknown:** how `AuditSeverity` feeds retention. `ForensicsRetentionService` **does** prune
  `audit_events` (`dbCtx.AuditEvents.Where(a => a.OccurredAt < cutoff)`, bounded batches, on
  `RetentionSweepIntervalHours`), so the sweep exists — but the link from `AuditSeverity`'s documented
  "retention policy" role to that cutoff was not traced.

## Sources

- `Vortex.Dashboard.API/Operations/DashboardOperationsService.cs` + 19 partials
- `Vortex.Catalog/CatalogAdminService.cs`, `Vortex.Furniture/FurnitureAdminService.cs`
- `Vortex.Players/Content/ContentAdminService.cs`, `.Hotel.cs`, `.NftAvatars.cs`
- `Vortex.Progression/Quests/QuestContentAdminService.cs`
- `Vortex.Authentication/Permissions/{StaffAdminService,PermissionService,SanctionPresetService}.cs`
- `Vortex.Rooms/Providers/RoomItemsProvider.cs`
- `Vortex.Database/Auditing/{EntityChangeInterceptor,EntityChangeCapture}.cs`
- `Vortex.Observability/Audit/ChannelAuditSink.cs`, `Runtime/ForensicsPurgeService.cs`
- `Vortex.Primitives/Observability/{AuditEvent,AuditEnums}.cs`
- `scripts/hooks/check-architecture-walls.mjs`
- `Vortex.Dashboard.Tests/Hosting/DashboardOperationReasonTests.cs`
