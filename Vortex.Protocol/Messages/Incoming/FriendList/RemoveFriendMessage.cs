using System.Collections.Generic;
using Vortex.Primitives.Networking;

namespace Vortex.Protocol.Messages.Incoming.FriendList;

public record RemoveFriendMessage : IMessageEvent
{
    public required List<int> FriendIds { get; init; }
}
