using System.Collections.Generic;
using Vortex.Primitives.Networking;

namespace Vortex.Protocol.Messages.Incoming.FriendList;

public record DeclineFriendMessage : IMessageEvent
{
    public bool DeclineAll { get; init; }
    public List<int>? Friends { get; init; }
}
