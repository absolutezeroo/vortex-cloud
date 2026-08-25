# Slice PR-W1 — the wired engine reads the room through an interface

## Goal
`RoomWiredSystem` stops reaching into `RoomGrain`'s fields, so the pipeline can eventually be
exercised without building most of a room.

## behavior_change
`none`. Every member of `IWiredRoomHost` is a read the engine was already performing; the 949 room
tests pass unchanged, which is the assertion.

## What landed
- `IWiredRoomHost` = `IWiredRoomView` (identity, clocks, budgets, items, tiles, avatars) +
  `IWiredDiagnostics` (logger, stop-reason counters, the room's wired log, error counters) + the
  room's internal variables.
- `RoomGrainWiredHost` implements it over the grain.
- Zero direct `_roomGrain._state` / `._logger` / `._metrics` / `._roomConfig` / `._wiredLogChannel`
  / `.MapModule` / `.NowMs` reads left in either partial.
- No mutable collection crosses: `EnumerateTileFloorStack` materialises inside the turn and returns
  floor items already ordered by object id; `TryGetItem` returns one object.

## What is deliberately still open
Three references to `RoomGrain` remain, and all three construct `WiredProcessingContext` /
`WiredExecutionContext` — the box-facing capability surface, which is the `IWiredRoomActions` half of
the host. Moving it touches the contract 171 box classes implement against, so it belongs to the
component extraction (PR-W2 onward) rather than to a slice that promised no semantic change.

Until it moves, `RoomWiredSystem` still takes a `RoomGrain` in its constructor, so the pipeline is
not yet testable on a fake host. That is what PR-W2/W3 buy.

## Required tests
The existing room suite, unchanged and green (949). The parity matrix the V4 note asks for arrives
with the fake host, i.e. after the actions half moves.
