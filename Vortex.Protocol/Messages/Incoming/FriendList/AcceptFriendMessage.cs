using System.Collections.Generic;
using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Incoming.FriendList;

public record AcceptFriendMessage : IMessageEvent
{
    public required List<int> Friends { get; init; }
}
