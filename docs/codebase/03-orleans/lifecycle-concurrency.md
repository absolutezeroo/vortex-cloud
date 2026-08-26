# Lifecycle and concurrency

## Purpose

What Orleans' execution model buys this codebase, where it is deliberately relied on instead of a
lock, and the patterns that exist because the alternative already caused a bug.

## Single-threading is the concurrency model

Orleans runs one turn per activation at a time. This codebase treats that as the primary concurrency
primitive rather than as an implementation detail.

**Verified: zero manual locks in grains.** Searched `lock (`, `lock(`, `SemaphoreSlim`, `Monitor.`,
`Interlocked.`, `ConcurrentDictionary` under every `Grains/` path — the only matches are the
substring `lock` inside identifiers like `FreezeBlock`, `movement lock` and `ResetClock`.

The one `SemaphoreSlim` on the session path is `SessionGateway._mappingGate`, which is in a
**service**, not a grain, and its outbound grain calls are deliberately made after release.

### The alternative to a lock

```csharp
// LtdRaffleGrain — a plain bool, and a comment explaining why that is enough
// "A plain field is enough (and is not a lock): Orleans runs one grain turn at a time,
//  so this only has to survive across await points within the activation."
private bool _drawInProgress;
```

Same shape in `PlayerPresenceGrain._isProcessingQueue`.

### What it is used to serialize

| Concern | Grain, keyed by | Why |
|---|---|---|
| Catalog purchase | `CatalogPurchaseGrain`, player id | one purchase per player at a time, debit and grant in one turn |
| Wallet mutation | `PlayerWalletGrain`, player id | balance changes cannot interleave |
| Furni-limit check-then-create | `InventoryGrain`, player id | `GrantSingleFurnitureIfUnderLimitAsync`'s doc says this outright |
| Limited-edition draw | `LtdRaffleGrain`, series id | concurrent buyers serialize per series |
| Voucher redemption | `VoucherGrain`, `code.Trim().ToUpperInvariant()` | one redemption attempt per code at a time |
| Room mutation | `RoomGrain`, room id | the entire live room is one turn |
| CFH queue | `ModerationQueueGrain`, `"global"` | the in-client mod tool and the dashboard **contend** rather than overwrite |

`Vortex.Rooms.Tests/Grains/GrainTurnIsolationTests.cs` runs 250 concurrent `AddPlayerToRoomAsync`
calls against a real `RoomDirectoryGrain` activation and asserts the exact population. Its header
notes the claim was *"load-bearing and, until now, asserted nowhere"*.

### One place the guarantee is thinner than it looks

`ExecutePurchaseAsync` is **not** a grain method. It is a caller-side extension in
`Vortex.Primitives/Players/Wallet/WalletPurchaseExtensions.cs` taking a
`Func<CancellationToken, Task<TReward>>`. It makes two separate grain calls and runs the grant in the
**caller's** turn.

So "each player gets a purchase grain so buys are serialized with no locks" is true only because
`CatalogPurchaseGrain` is player-keyed and holds its turn across both halves. Nothing structurally
prevents a future caller from invoking the executor from a non-player-keyed grain and losing the
guarantee. → [Transactions](../06-economy/transactions.md)

## Concurrency annotations

Only two kinds appear, and sparingly. **No `[Reentrant]`, no `[StatelessWorker]` anywhere.**

| Annotation | Sites | Why |
|---|---|---|
| `[AlwaysInterleave]` | 5 — 2 on `IPlayerPresenceGrain.SendComposerAsync`, 3 on `IRoomCore` | prevents the room↔presence deadlock; lets pure reads skip the queue |
| `[ReadOnly]` | 4 — 3 on `IRoomCore`, 1 on `IRoomMap` | a snapshot read must not queue behind a 50 ms room tick |

`IRoomCore.GetSnapshotAsync`, `GetSummaryAsync` and `GetRoomPropertiesAsync` carry both.
`IRoomAvatars.GetAllAvatarSnapshotsAsync` deliberately carries **neither** — it reads mutable
per-tick state.

## Activation and deactivation

| Grain | On activate | On deactivate |
|---|---|---|
| `RoomGrain` | seed tick clocks, hydrate room + rights + group ranks + mutes, register in `RoomDirectoryGrain`, bind the outbound stream, start the 50 ms timer | flush dirty items and pets, remove from the directory |
| `PlayerGrain` | hydrate `PlayerLiveState` | `WriteToDatabaseAsync` |
| `MessengerGrain` | hydrate friends/requests/blocks/ignores, `Task.WhenAll` `IsOnlineAsync` across the whole friend list | flush delivered flags |
| `PlayerAchievementGrain` | — | flush dirty progress |
| `RoomPersistenceGrain` | start the 2 s timer | **drain** (loop until no progress) |
| `PlayerPresenceGrain` | — | clear queue, unregister, unsubscribe stream |
| Reference-data singletons | load definitions, set `_loaded` | — |

Deactivation is Orleans-managed at `GrainCollectionAge` (2 min) unless `[KeepAlive]`.

`RoomGrain` additionally extends its own life: `DelayRoomDeactivationAsync` calls
`DelayDeactivation(RoomDeactivationDelayMs)` = **30 minutes**, and `RoomDirectoryGrain.CheckRoomsAsync`
sweeps every 5 minutes, extending populated rooms and calling `DeactivateRoomAsync()` on empty ones.

### `[KeepAlive]` grains

14, all singletons or directories: `PlayerDirectoryGrain`, `RoomDirectoryGrain`,
`ModerationQueueGrain`, `GuideDirectoryGrain`, and the 10 reference-data caches.

`ServerConfigGrain` is **not** `[KeepAlive]` while every other reference-data singleton is. With
`GrainCollectionAge` at 2 minutes it will be collected and reload the whole `server_config` table on
the next read. Whether that is deliberate (config is small, reload is cheap) or an omission is
**Unverified** — nothing in the file says.

> `[KeepAlive]` makes leaks permanent. `RoomDirectoryGrain.RemoveActiveRoomAsync` drops all three of
> its maps together, with a comment naming exactly that: anything left behind is held for the silo's
> lifetime. If a `RoomGrain` dies without running `OnDeactivateAsync` (a silo kill), the directory
> entry survives and no reaper exists.

## Timers

10 `RegisterGrainTimer` sites. **Zero reminders** — no `IRemindable`, no `RegisterOrUpdateReminder`.
Everything is in-activation, so nothing survives a restart by itself.

| Grain | Interval | Work |
|---|---|---|
| `RoomGrain` | `RoomTickMs` = 50 ms | the 11-step room tick |
| `RoomPersistenceGrain` | `DirtyItemsTickMs` = 2000 ms | furniture flush |
| `RoomDirectoryGrain` | `RoomCheckMs` = 300 000 ms | lifecycle sweep |
| `PlayerAchievementGrain` | `ProgressFlushIntervalMs` = 5000 ms | dirty progress flush |
| `MessengerGrain` | `DeliveredFlushIntervalMs` = 10 000 ms | delivered-flag flush |
| `PlayerGrain` | `ClubConfig.MaintenanceIntervalMs` = 1 h | club maintenance |
| `GuideDirectoryGrain` | 5 s | duty-roster sweep |
| `PlayerEffectGrain`, `LtdRaffleGrain`, `RentableSpaceGrain` | one-shot | expiry / draw |

## Cross-grain call patterns

### Parallelize independent calls

25 `Task.WhenAll` sites in grain-owning modules. The best ones carry the bug they fixed:

- `MessengerGrain.HydrateAsync` — online flags for every friend at once. The comment records that
  presence only ever arrived via a *change* notification, so already-connected friends stayed offline
  forever.
- `RoomDirectoryGrain.CheckRoomsAsync` — fan-out over every active room.
- `LtdRaffleGrain.RefundEntriesAsync` — parallel wallet refunds, outcomes persisted in one save.

### Never `.Ignore()`

```csharp
// Vortex.Logging/Extensions/TaskLoggingExtensions.cs
public static void LogAndForget(this Task task, ILogger logger, string message, params object?[] args)
```

Implemented as `_ = AwaitAndLogAsync(...)` with a `try/catch → LogError`, **not** `ContinueWith` — the
doc explains that `ContinueWith(..., TaskScheduler.Current)` semantics inside a grain turn are subtle.

**52 call sites** outside `Vortex.Logging`. **`.Ignore()` appears zero times repo-wide** — the rule is
fully applied. `PlayerPresenceGrain` and `MessengerGrain` each wrap it in a private overload that
pre-binds the player id.

### Sequential-await-in-loop, where it survives

Four provable cases, all low severity, all recorded rather than hidden:

| Site | Shape |
|---|---|
| `RoomGrain.Doorbell.cs` | per expired ringer: a name lookup and two presence calls. The *inner* notify fan-out is parallel; the outer loop is not |
| `WiredActionKickUser`, `WiredActionMuteUser` | awaited presence call per selected player, bounded by `WiredSelectedItemsLimit` (20), inside the 50 ms tick |
| `RoomGrain.Settings.cs` ×2 | `_events.PublishAsync` per target (in-process bus) and `RemoveAvatarFromPlayerAsync` per occupant (local module) — neither is a grain call |

### Never swallow

Every grain doing cross-grain or DB work injects `ILogger<T>`. A bare `catch { }` is forbidden because
a silently failed cross-grain notification leaves state asymmetric with nobody knowing.

Where a catch *is* deliberate it says so — `PlayerPresenceGrain.ClearActiveRoomAsync` swallows the
room call's failure on purpose so the player still stops being an occupant.

## Isolate heavy I/O

One grain per responsibility. When a grain needs heavy I/O, it delegates to a dedicated secondary
grain so the primary stays responsive.

```
RoomGrain (50 ms tick)  ──EnqueueDirtyItemsAsync──►  RoomPersistenceGrain (2 s flush)
   stays responsive                                     absorbs the DB latency
```

The enqueue is synchronous by design. → [Persistence](persistence.md)

## Tick isolation

`RoomGrain.RunTickStepAsync(name, fn)` wraps every one of the 11 tick steps:

- rethrows `OperationCanceledException`
- counts consecutive failures per step, logs the 1st and every 200th
- times the step into `_metrics.RoomTickStepCompleted`

The comment records why: one malformed wired item used to abort the whole tick — and because the two
flushes run last, the room also stopped persisting. → [Movement and tick](../04-rooms/movement-and-tick.md)

## Testing grains

Two harnesses, for two different questions.

**Real cluster** — `Vortex.Rooms.Tests/Grains/VortexClusterFixture.cs` builds an in-process
`TestCluster(1)` with an in-memory EF store. Its doc is explicit that it exists to test what a
hand-constructed grain cannot: one activation per key, turn-based execution, and by-value argument
serialization.

**Outside a silo** — `Vortex.Tests.Support/GrainActivationContext.cs` builds a grain with `new`, using
reflection over `RuntimeContext.SetExecutionContext` to set the ambient activation context **around
the constructor call** (because `RoomGrain` reads its primary key in its own constructor — patching
afterwards is too late). The context is a `FakeProxy` answering `get_GrainId`, `get_ActivationServices`
and, for any interface-returning member, a **recursive** stub — so arming a timer succeeds and does
nothing. Both reflection lookups throw a named error if Orleans moves them.

## Known unknowns

- **Unknown:** whether `ServerConfigGrain` lacking `[KeepAlive]` is deliberate.
  - Inspected: the grain, all 14 `[KeepAlive]` sites, `GrainCollectionAge`.
  - What would resolve it: a comment, or a measurement of the reload cost.
- **Unknown:** whether `RoomDirectoryGrain` needs a reaper for entries orphaned by a silo kill.
  - Inspected: `RemoveActiveRoomAsync` and its comment, which acknowledges the risk; no reaper exists.
  - What would resolve it: a decision on whether an unclean shutdown is in scope for a single-silo
    deployment.

## Sources

- `Vortex.Rooms/Grains/RoomGrain.cs` — `OnActivateAsync`, `RunTickStepAsync`, `DelayRoomDeactivationAsync`
- `Vortex.Rooms/Grains/RoomDirectoryGrain.cs` — `CheckRoomsAsync`, `RemoveActiveRoomAsync`
- `Vortex.Rooms/Grains/RoomPersistenceGrain.cs`
- `Vortex.Catalog/Grains/LtdRaffleGrain.cs` — the `_drawInProgress` comment
- `Vortex.Primitives/Players/Wallet/WalletPurchaseExtensions.cs`
- `Vortex.Primitives/Inventory/Grains/IInventoryGrain.Furni.cs`
- `Vortex.Logging/Extensions/TaskLoggingExtensions.cs`
- `Vortex.Rooms.Tests/Grains/{GrainTurnIsolationTests,VortexClusterFixture}.cs`
- `Vortex.Tests.Support/{GrainActivationContext,FakeProxy}.cs`
- `AGENTS.md` — "Orleans grain development rules"
