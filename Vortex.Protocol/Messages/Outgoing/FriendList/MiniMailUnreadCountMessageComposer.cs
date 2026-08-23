using Orleans;
using Vortex.Primitives.Networking;

namespace Vortex.Protocol.Messages.Outgoing.FriendList;

[GenerateSerializer, Immutable]
public sealed record MiniMailUnreadCountMessageComposer : IComposer
{
    [Id(0)]
    public required int UnreadCount { get; init; }
}
