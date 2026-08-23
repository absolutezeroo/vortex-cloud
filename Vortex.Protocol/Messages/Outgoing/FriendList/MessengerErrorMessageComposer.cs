using Orleans;
using Vortex.Primitives.FriendList.Enums;
using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Outgoing.FriendList;

[GenerateSerializer, Immutable]
public sealed record MessengerErrorMessageComposer : IComposer
{
    [Id(0)]
    public required int ClientMessageId { get; init; }

    [Id(1)]
    public required FriendListErrorCodeType ErrorCode { get; init; }
}
