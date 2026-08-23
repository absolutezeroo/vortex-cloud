using Orleans;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Rooms;

namespace Vortex.Primitives.Messages.Outgoing.Navigator;

[GenerateSerializer, Immutable]
public sealed record FavouriteChangedMessageComposer : IComposer
{
    [Id(0)]
    public RoomId RoomId { get; init; }

    [Id(1)]
    public bool Added { get; init; }
}
