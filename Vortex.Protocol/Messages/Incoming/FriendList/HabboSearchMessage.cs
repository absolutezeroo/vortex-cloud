using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Incoming.FriendList;

public record HabboSearchMessage : IMessageEvent
{
    public required string SearchQuery { get; init; }
}
