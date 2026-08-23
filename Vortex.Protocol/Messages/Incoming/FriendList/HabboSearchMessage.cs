using Vortex.Primitives.Networking;

namespace Vortex.Protocol.Messages.Incoming.FriendList;

public record HabboSearchMessage : IMessageEvent
{
    public required string SearchQuery { get; init; }
}
