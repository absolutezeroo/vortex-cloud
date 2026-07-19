using Orleans;

namespace Vortex.Primitives.Rooms.Snapshots.Furniture;

[GenerateSerializer, Immutable]
public sealed record RoomFloorItemSnapshot : RoomItemSnapshot { }
