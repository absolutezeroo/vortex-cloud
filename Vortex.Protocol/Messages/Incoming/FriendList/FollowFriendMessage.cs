using Vortex.Primitives.Networking;
using Vortex.Primitives.Players;

namespace Vortex.Protocol.Messages.Incoming.FriendList;

public record FollowFriendMessage : IMessageEvent
{
    public required PlayerId PlayerId { get; init; }
}
