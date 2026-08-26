# Movement and the room tick

## Purpose

The clock that drives a live room: what runs per tick, how failures are contained, and how avatars
actually move.

## The driver

One `RegisterGrainTimer` in `RoomGrain.OnActivateAsync`, period `RoomConfig.RoomTickMs` = **50 ms**.

> The property's XML doc records the measurement: on Windows the 15.625 ms timer quantum makes this
> ~62.5 ms / 16 Hz in practice, measured at 63.2 ms. Do not assume 20 Hz.

## The eleven steps

Every step runs through `RunTickStepAsync(name, fn)`.

| Step | Period | Entry point |
|---|---|---|
| `avatars` | `AvatarTickMs` 500 | `RoomAvatarTickSystem.ProcessAvatarsAsync` |
| `pets` | `PetConfig.TickMs` 500 | `RoomPetSystem.ProcessPetsAsync` |
| `bots` | 1000 (**private const**) | `RoomBotSystem.ProcessBotsAsync` |
| `wired` | `WiredTickMs` 50 | `RoomWiredSystem.ProcessWiredAsync` |
| `rollers` | `RollerTickMs` 2000 | `RoomRollerSystem.ProcessRollersAsync` |
| `game-timer` | every tick | `RoomGameTimerSystem.ProcessAsync` |
| one per registered minigame | every tick | `IRoomMinigame.TickAsync` |
| `doorbell` | every tick, threshold 20 000 ms | `RoomGrain.ProcessDoorbellTimeoutsAsync` |
| `mystery-box` | every tick, threshold 120 000 ms | `RoomGrain.ProcessMysteryBoxTimeoutsAsync` |
| `flush-tiles` | every tick | `RoomGrain.FlushDirtyTilesAsync` |
| `flush-items` | every tick | `RoomGrain.FlushDirtyItemsAsync` |

## Failure isolation

`RunTickStepAsync`:

- rethrows `OperationCanceledException`
- catches everything else, counts **consecutive** failures per step, logs the 1st and every 200th
- times the step into `_metrics.RoomTickStepCompleted` (tagged by step name)

The comment records exactly why this exists: **one malformed wired item used to abort the whole
tick** — and because the two flushes run last, the room also stopped persisting. Isolation plus
ordering is the fix; either alone is not.

## Boundary maths

Per-subsystem boundaries live in `RoomLiveState` (`NextAvatarBoundaryMs`, `NextPetBoundaryMs`, …) and
are advanced by `AlignToNextBoundary` / `AdvanceBoundaryPast`.

The replaced design is documented: a `while (now >= next) next += tick` stepping loop, which became
**millions of iterations inside one grain turn** after a pause, a sleep, or a clock jump.

Pinned by `Vortex.Rooms.Tests/Grains/TickBoundaryTests.cs` — the fast maths lands exactly where the
old loop would have (theory-driven), a boundary always moves past the instant it fired, and a
day-long gap closes without walking every tick.

## Avatar movement

### Batched status updates

`RoomAvatarTickSystem.ProcessAvatarsAsync` walks `AvatarsByObjectId`, calls
`ProcessNextAvatarStepAsync`, collects `avatar.GetSnapshot()` for every avatar where `IsDirty` into a
**reused** `_dirtySnapshots` list (one instance per room grain, cleared per tick — avoiding a per-tick
allocation proportional to room count × tick rate), and emits **exactly one**
`UserUpdateMessageComposer{Avatars = [..]}` per tick.

Also per tick, per avatar: a one-shot `Sign` status is dropped, and `ExpireHandItem` clears a hand
item past `CarryItemUntilMs` with an explicit `CarryObjectMessageComposer{ItemType = 0}` — because the
item is not carried in the avatar block.

### Per-step validation

`ValidateAvatarStepAsync`:

1. max step height (`MaxStepHeight` = 200 hundredths)
2. `CanAvatarWalkBetween`
3. re-path towards the goal **once** if the next tile became blocked mid-walk
4. `OnWalkOffAsync` / `OnWalkOnAsync` on the highest floor item of the previous / next tile
5. `AddStatus(Move, "x,y,z")` and rotation

### Pathfinding

`Vortex.Rooms/Grains/Systems/RoomPathingSystem.cs` — **A\*** on an 8-direction grid.

| | |
|---|---|
| Open set | `PriorityQueue<Node,int>` |
| Closed set | `HashSet<(int,int)>` |
| Costs | `CARDINAL_COST = 10`, `DIAGONAL_COST = 14` |
| Heuristic | octile |
| Node budget | `RoomConfig.MaxPathNodes` = 4096 |

Walkability is injected as two delegates (`canOccupyTile`, `canMoveBetween`), so bots and pets reuse
the same search rather than forking it.

`RoomAvatarModule.WalkAvatarToAsync` skips the first node (`path.Skip(1)`) into `avatar.TilePath`, and
refuses outright when `avatar.IsMovementLocked` — which is how wired freeze works.

## The two flushes, last and isolated

- `FlushDirtyTilesAsync` is **not persistence** despite the name. It batches height-map deltas into
  `HeightMapUpdateMessageComposer`s at `MaxTileHeightsPerFlush` (200) and sends them with
  `LogAndForget`. Tile heights are derived and never stored.
- `FlushDirtyItemsAsync` materialises snapshots from `DirtyItemIds`, **clears the set before the
  cross-grain call**, and hands the batch to `RoomPersistenceGrain`.
  → [Persistence](../03-orleans/persistence.md)

## Instrumentation

`Vortex.room.tick.duration` and `Vortex.room.tick.step.duration` (tagged `step`).
`RoomPerformanceAggregator` reads them back out of the meter with a `MeterListener`, so the dashboard
and a Prometheus scrape are literally the same measurements — there is no second write path at 20 Hz
per room. → [Observability](../10-operations/observability.md)

## Cost

`Vortex.Rooms.Tests/Wired/WiredEngineCostTests.cs` asserts a steady tick costs the same regardless of
room size, that the only full scan is the wired index rebuild and there is one, and that a wave of
stack changes collapses into a single rebuild.

## Sources

- `Vortex.Rooms/Grains/RoomGrain.cs` — `OnActivateAsync`, `RunTickStepAsync`, `AlignToNextBoundary`, `AdvanceBoundaryPast`
- `Vortex.Rooms/Grains/RoomGrain.Map.cs` — `FlushDirtyTilesAsync`
- `Vortex.Rooms/Grains/RoomGrain.Furni.cs` — `FlushDirtyItemsAsync`
- `Vortex.Rooms/Grains/Systems/{RoomAvatarTickSystem,RoomPathingSystem,RoomBotSystem}.cs`
- `Vortex.Rooms/Grains/Modules/RoomAvatarModule.cs` — `WalkAvatarToAsync`
- `Vortex.Rooms/Grains/Systems/RoomAvatarTickSystem.cs` — `ProcessAvatarsAsync`, `ValidateAvatarStepAsync` (private)
- `Vortex.Rooms/Configuration/RoomConfig.cs`
- `Vortex.Rooms.Tests/Grains/TickBoundaryTests.cs`, `Wired/WiredEngineCostTests.cs`
