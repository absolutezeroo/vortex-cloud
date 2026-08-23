using Vortex.Primitives.Networking;

namespace Vortex.Protocol.Messages.Incoming.FriendList;

public record RequestFriendMessage : IMessageEvent
{
    public required string PlayerName { get; init; }
}
