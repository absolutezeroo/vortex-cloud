using Vortex.Primitives.Networking;
using Vortex.Primitives.Players;

namespace Vortex.Primitives.Messages.Incoming.FriendList;

public record FollowFriendMessage : IMessageEvent
{
    public required PlayerId PlayerId { get; init; }
}
