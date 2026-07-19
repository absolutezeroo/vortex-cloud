using Orleans;

namespace Vortex.Primitives.Orleans.Snapshots.Room;

[GenerateSerializer, Immutable]
public readonly record struct RoomOccupantSnapshot
{
    [Id(0)]
    public required int PlayerId { get; init; }

    [Id(1)]
    public required string Name { get; init; }
}
