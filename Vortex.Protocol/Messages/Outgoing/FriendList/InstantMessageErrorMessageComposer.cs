using Orleans;
using Vortex.Primitives.FriendList.Enums;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Players;

namespace Vortex.Primitives.Messages.Outgoing.FriendList;

[GenerateSerializer, Immutable]
public sealed record InstantMessageErrorMessageComposer : IComposer
{
    [Id(0)]
    public required InstantMessageErrorCodeType ErrorCode { get; init; }

    [Id(1)]
    public required PlayerId PlayerId { get; init; }

    [Id(2)]
    public required string Message { get; init; }
}
