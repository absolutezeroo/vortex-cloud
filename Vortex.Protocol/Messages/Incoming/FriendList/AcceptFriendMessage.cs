using System.Collections.Generic;
using Vortex.Primitives.Networking;

namespace Vortex.Protocol.Messages.Incoming.FriendList;

public record AcceptFriendMessage : IMessageEvent
{
    public required List<int> Friends { get; init; }
}
