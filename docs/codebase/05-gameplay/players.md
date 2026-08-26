# Players and permissions

## Purpose

Three grains share the word "player" and own completely different things. Getting them straight
prevents most ownership mistakes.

## Three grains, one word

| Grain | Key | Owns | Does **not** own |
|---|---|---|---|
| `PlayerGrain` | player id | `PlayerLiveState` — name, motto, figure, gender, chat style, achievement score, respect counters, mutes, favourite group, the whole club + kickback block | the wallet, the inventory, presence |
| `PlayerPresenceGrain` | player id | one session observer, one outbound queue, a 4-field room pointer | **any player data at all** |
| `PlayerDirectoryGrain` | `"global"`, `[KeepAlive]` | a read-through id↔name cache | the names themselves |

`PlayerPresenceGrain`'s name is about **routing**. `PlayerPresenceState` is `ActiveRoomId`,
`PendingRoomId`, `PendingRoomApproved`, `ActiveRoomSinceUtc` — nothing else.
→ [Presence routing](../03-orleans/presence-routing.md)

Money is `PlayerWalletGrain`'s. → [Economy overview](../06-economy/overview.md)

## PlayerGrain

Hydrate-on-activate plus write-through, and **two disjoint write sets** — which is the detail worth
knowing:

| Write | Covers |
|---|---|
| `WriteToDatabaseAsync` | 8 columns, from memory, on every mutation **and** on `OnDeactivateAsync` |
| targeted single-column `ExecuteUpdateAsync` | `RoomChatStyleId`, `FavouriteGroupId`, `NuxCompletedAt` — each on its own |

The comment explains the split: the targeted updates exist **to avoid clobbering**, because
`WriteToDatabaseAsync` writes all 8 from the in-memory snapshot.

> Those 8 columns are a red zone. A raw SQL edit to a player's name, motto, figure, gender,
> achievement score or respect counters while the grain is activated is **silently overwritten**.
> → [State ownership](../03-orleans/persistence.md)

### The live-authorization pattern, done right

`ApplyHotelMuteAsync` updates `_state.MutedUntil` **in the same method** that persists it, with a
comment naming the bug it prevents. That is the `AGENTS.md` "keep live authorization state synced with
DB writes" rule, honoured.

The room-side equivalent — and its second half — is on
[Room architecture](../04-rooms/room-architecture.md).

### Club and kickback

`PurchaseClubAsync` → `ExecutePurchaseAsync` **without a journal** → `ApplyClubMonthsAsync`: streak and
grace resolved through `IServerConfigGrain` (`club.gift_cycle_days` 31, `club.streak_grace_days` 7),
`PlayerSubscriptionEntity` upserted, club badges granted, kickback row upserted, `ClubPurchasedEvent`
published.

A maintenance timer runs at `ClubConfig.MaintenanceIntervalMs` (1 h).

Two known risks live here: club payday can pay twice across a restart, and `TrackCreditSpendAsync` is
never compensated. → [Transactions](../06-economy/transactions.md)

## PlayerDirectoryGrain

A cache, not an owner. The write order matters:

```
PlayerGrain.SetNameAsync
  ├─ DB write
  └─ then notify the directory
```

`SetPlayerNameAsync` only touches the cache. And `SetNameCache` **removes the stale reverse entry
before inserting** — that is the forward/reverse coherence rule from `AGENTS.md`, and skipping it
leaves a name resolving to two ids.

Lookup is case-insensitive: `_nameToId` uses `StringComparer.OrdinalIgnoreCase`, and the DB fallback
is `x.Name.ToLower().Equals(name.ToLower())`.

> **Two unbounded caches.** `_idToName` and `_nameToId` are `[KeepAlive]` with no eviction, so they
> grow with the distinct-player count. Same shape in `PermissionService._playerToAccount`, which is
> never invalidated (only `_byAccount` has a 60 s TTL).
> → [Performance](../10-operations/performance.md)

## Permissions

Capability-based, **not rank-based**. `SecurityLevelType` exists but is client-UI only —
`SecurityLevelPolicy`'s doc says so.

`PermissionSet.Has` / `HasAny` short-circuit on the wildcard `"*"`.

`IPermissionService` is deliberately **not a grain** — its class doc: *"read-mostly service (not a
grain): no per-subject serialization is needed"*. `ResolveForAccountAsync` / `ResolveForPlayerAsync` /
`InvalidateAccount` / `InvalidatePlayer`, with a 60 s TTL on `_byAccount`.

The same `Capabilities` file holds both the `dashboard.*` set and the in-client set (`Capabilities.Room.*`,
`Capabilities.Moderation.*`, `Capabilities.Economy.*`, `Capabilities.Navigator.StaffPick`).
→ [Capabilities](../08-dashboard/capabilities.md)

## The per-player long tail

About twenty grains are keyed by player id and **own no state** — they exist for per-key
serialization, opening a short-lived `DbContext` per call:

`PlayerBadgeGrain` · `PlayerClothingGrain` · `PlayerEffectGrain` (a timer handle only) ·
`PlayerMysteryBoxGrain` · `PlayerNavigatorGrain` (this one *does* cache) · `PlayerQuestGrain` ·
`PlayerDailyTaskGrain` · `PlayerPollGrain` · `PlayerPrizeGrain` · `PlayerQuizGrain` ·
`PlayerAchievementResolutionGrain` · `PlayerMintGrain` · `PlayerNftClaimsGrain` ·
`PlayerNftWardrobeGrain` · `PlayerVaultGrain`

That is a legitimate reason to be a grain here. → [Grain map](../03-orleans/grain-map.md)

## Sanctions

`account_bans (player_account_id)` unique means at most one **live** ban row — which is why unbanning
soft-deletes and re-banning **revives** the row with `IgnoreQueryFilters()`. One of only three
sanctioned opt-outs from the global soft-delete filter.

Room-scoped moderation is separate and lives on the room grain, with room mutes and *hotel* mutes
deliberately in different fields so a room owner cannot lift a staff sanction.
→ [Room architecture](../04-rooms/room-architecture.md)

## Known gaps

Two adjacent gaps, both one grain call away from being right:

- **`PlayerGrain.GetExtendedProfileSnapshotAsync` hardcodes `IsOnline = true`** rather than asking
  `PlayerPresenceGrain.IsOnlineAsync`. Also `IsHidden = false`, `StarGemCount = 0`, `Guilds = []`.
  Adjacent comments say the *other* constants were fixed; these were left.
- **`PlayerDirectoryGrain.SearchPlayersAsync` returns `Online = false`** for every result.

Whether either is deliberate is **Unverified** — nothing in either file says.

Separately, `players.status` is a **dead column** — never written after creation, and read verbatim by
the dashboard, so an admin profile always says "Offline".

## Configuration

| Key | Default |
|---|---|
| `Vortex:Club:MaintenanceIntervalMs` | 3 600 000 |
| `Vortex:PlayerPresence:MaxOutgoingQueueSize` | 500 |
| `messenger.max_friends`, `club.*` (via `IServerConfigGrain`) | → [Configuration](../01-runtime/configuration.md) |

**Undeclared, defaults-only:** `Vortex:Players:{NameMinLength, NameMaxLength, NameSuggestionCount}`
and `Vortex:Rooms:DailyRespectLimit` are read raw from `IConfiguration` in handlers and appear in no
config class.

## Sources

- `Vortex.Players/Grains/PlayerGrain.cs` + 5 partials — `WriteToDatabaseAsync`, `ApplyHotelMuteAsync`, `PurchaseClubAsync`, `SetNameAsync`
- `Vortex.Players/Grains/{PlayerPresenceGrain,PlayerDirectoryGrain,PlayerWalletGrain}.cs`
- `Vortex.Players/Grains/PlayerLiveState.cs`, `PlayerPresenceState.cs`
- `Vortex.Primitives/Permissions/{PermissionSet,IPermissionService,SecurityLevelPolicy,Capabilities}.cs`
- `Vortex.Authentication/Permissions/PermissionService.cs`
- `Vortex.Players/Configuration/{ClubConfig,PlayerPresenceConfig}.cs`
