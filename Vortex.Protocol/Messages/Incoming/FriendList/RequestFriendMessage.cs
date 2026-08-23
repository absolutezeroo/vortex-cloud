using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Incoming.FriendList;

public record RequestFriendMessage : IMessageEvent
{
    public required string PlayerName { get; init; }
}
