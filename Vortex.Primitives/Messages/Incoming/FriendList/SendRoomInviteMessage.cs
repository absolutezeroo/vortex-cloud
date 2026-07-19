using System.Collections.Generic;
using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Incoming.FriendList;

public record SendRoomInviteMessage : IMessageEvent
{
    public required string Message { get; init; }
    public required List<int> FriendIds { get; init; }
}
