using System.Collections.Immutable;
using Orleans;
using Turbo.Primitives.Networking;
using Turbo.Primitives.Orleans.Snapshots.Room;
using Turbo.Primitives.Rooms;

namespace Turbo.Primitives.Messages.Outgoing.Roomsettings;

[GenerateSerializer, Immutable]
public sealed record FlatControllersEventMessageComposer : IComposer
{
    [Id(0)]
    public required RoomId RoomId { get; init; }

    [Id(1)]
    public required ImmutableArray<RoomControllerSnapshot> Controllers { get; init; }
}
