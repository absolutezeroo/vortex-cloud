using System.Collections.Generic;
using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Incoming.FriendList;

public record RemoveFriendMessage : IMessageEvent
{
    public required List<int> FriendIds { get; init; }
}
