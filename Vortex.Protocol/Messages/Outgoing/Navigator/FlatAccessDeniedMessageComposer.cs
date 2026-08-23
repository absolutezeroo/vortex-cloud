using Orleans;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Rooms;

namespace Vortex.Protocol.Messages.Outgoing.Navigator;

[GenerateSerializer, Immutable]
public sealed record FlatAccessDeniedMessageComposer : IComposer
{
    [Id(0)]
    public RoomId RoomId { get; init; }

    [Id(1)]
    public string? Username { get; init; }
}
