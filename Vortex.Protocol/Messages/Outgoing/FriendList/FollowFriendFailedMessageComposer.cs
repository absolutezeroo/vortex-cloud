using Orleans;
using Vortex.Primitives.FriendList.Enums;
using Vortex.Primitives.Networking;

namespace Vortex.Protocol.Messages.Outgoing.FriendList;

[GenerateSerializer, Immutable]
public sealed record FollowFriendFailedMessageComposer : IComposer
{
    [Id(0)]
    public required FollowFriendErrorCodeType ErrorCode { get; init; }
}
