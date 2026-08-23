using Orleans;
using Vortex.Primitives.Networking;

namespace Vortex.Protocol.Messages.Outgoing.Navigator;

[GenerateSerializer, Immutable]
public sealed record CanCreateRoomEventMessageComposer : IComposer
{
    [Id(0)]
    public bool CanCreateEvent { get; init; }

    [Id(1)]
    public int ErrorCode { get; init; }
}
