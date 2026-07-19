using Orleans;
using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Outgoing.Navigator;

[GenerateSerializer, Immutable]
public sealed record CanCreateRoomMessageComposer : IComposer
{
    [Id(0)]
    public int ResultCode { get; init; }

    [Id(1)]
    public int RoomLimit { get; init; }
}
