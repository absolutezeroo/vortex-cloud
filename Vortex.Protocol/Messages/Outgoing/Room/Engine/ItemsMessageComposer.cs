using System.Collections.Immutable;
using Orleans;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Players;
using Vortex.Primitives.Rooms.Snapshots.Furniture;

namespace Vortex.Primitives.Messages.Outgoing.Room.Engine;

[GenerateSerializer, Immutable]
public sealed record ItemsMessageComposer : IComposer
{
    [Id(0)]
    public required ImmutableDictionary<PlayerId, string> OwnerNames { get; init; }

    [Id(1)]
    public required ImmutableArray<RoomWallItemSnapshot> WallItems { get; init; }
}
