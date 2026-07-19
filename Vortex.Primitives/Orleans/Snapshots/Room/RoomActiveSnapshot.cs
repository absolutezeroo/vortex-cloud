using Orleans;

namespace Vortex.Primitives.Orleans.Snapshots.Room;

[GenerateSerializer, Immutable]
public sealed record RoomActiveSnapshot : RoomSummarySnapshot { }
