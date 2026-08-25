# What a crash costs, per domain

Vortex runs rooms memory-first: a sofa being dragged writes to the room's own state and queues a
snapshot, and a background flush turns that into an `UPDATE`. That is a deliberate trade — a database
write per drag would put the hotel's furniture on the hot path of every build session — and the price
is a window in which an ungraceful stop loses whatever happened inside it.

This is that window, per domain, read off the code rather than off the intent. `OnDeactivate` closes
it on a clean shutdown and is **not** guaranteed on a crash, a `kill -9`, or a silo the cluster
evicts, so every number below is what a hard stop costs.

| Domain | Window | Knob | Where |
|---|---|---|---|
| Furniture position, rotation, extra data | ≤ `DirtyItemsTickMs` (**2 s**), plus the backlog tail below | `RoomConfig.DirtyItemsTickMs`, `RoomConfig.MaxDirtyItemsPerFlush` | `RoomPersistenceGrain` |
| Pet stats and position | ≤ `StatFlushIntervalMs` (**60 s**) | `PetConfig.StatFlushIntervalMs` | `RoomPetSystem.FlushDirtyPetsAsync` |
| Permanent Wired variables | **zero** — written through | — | `RoomGrain.Wired.PermanentVariables` |
| Tile heights | **not persisted at all** | — | `RoomGrain.Map.FlushDirtyTilesAsync` |
| Commerce (post-pivot) | **zero** | — | `CommerceJournal`, receipts |

## The backlog tail

A flush writes at most `MaxDirtyItemsPerFlush` (100) items. A room with more dirty items than that
does not lose the overflow — it writes it on the next tick — but it means the window is not flatly
two seconds:

> a builder who has just moved N items is exposed for roughly `ceil(N / 100) × 2 s`.

Five hundred placements is a ten-second tail, not a two-second one. The cap is still right: it is what
stops one busy room holding a database connection through an entire build session.

## Tile heights are not a loss domain

`FlushDirtyTilesAsync` reads like a persistence flush and is not one — it broadcasts
`HeightMapUpdate` to the clients in the room and writes nothing. Tile heights are derived from the
item stack, so a room that reloads recomputes them from the furniture rows. There is nothing to lose
and nothing to reconcile.

## Permanent Wired variables trade the other way

Every `set` on a permanent variable opens a `DbContext` and saves inside the room's turn. Nothing is
lost, and the cost lands on the tick instead: a wired chain writing a permanent variable pays a
database round trip before the room can do anything else. That is the correct trade for a value whose
whole point is surviving the room, and it is worth knowing before anyone puts one in a chain that
fires on every step.

## Idempotence on reload

Reload is a plain read, not a merge — a reactivating room reads its furniture and pets from the
database and believes them. That is what makes a lost window safe rather than merely small:

- **No partial rows.** A furniture snapshot is one row's worth of columns written in a single
  `UPDATE`, so a lost flush means the sofa is where it was two seconds ago, never half-moved.
- **No compounding.** A pet's decay clocks (`LastNutritionDecayAtMs` and its siblings) live in
  `PetMotionState`, which is in-memory and re-seeded to *now* when the room activates. Nothing decays
  while a room is asleep, so a lost flush costs the stats it was carrying and never a second helping
  of decay on top. Worth knowing on its own terms: a pet in a room nobody visits does not get hungry.
- **No reconciliation.** Nothing reads room state from two places, so there is no case where memory
  and the table disagree and something has to decide.

The one thing reload cannot fix is an operation with consequences outside the room, which is why
commerce does not live in this table at all: past its pivot it is journalled, receipted and replayed
by `OperationId`. See `decisions/ADR-001-catalog-pivot.md`.

## Two things that were not part of the trade

Both were fixed rather than documented, because "bounded window" was not true of either.

**A failed flush used to drop its batch.** `RoomPersistenceGrain` took the batch off the queue before
attempting the save, so a connection blip lost those positions *permanently* — not for two seconds,
but until somebody moved the furniture again. Same shape in `RoomPetSystem`, which cleared
`IsStatsDirty` before `SaveChangesAsync`: a throw there marked the pets clean while their stats were
still only in memory. Both now clear after the save succeeds.

**Deactivation used to write one batch.** `OnDeactivateAsync` called the flush once, so a room going
to sleep with more than `MaxDirtyItemsPerFlush` pending moves abandoned the overflow every time. It
drains now, bounded by progress rather than by a count so a database that is refusing writes costs one
extra attempt instead of spinning through deactivation.

`Vortex.Rooms.Tests/Grains/RoomPersistenceLossWindowTests.cs` holds all three as tests.
