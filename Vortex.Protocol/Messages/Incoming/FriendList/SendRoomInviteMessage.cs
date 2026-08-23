using System.Collections.Generic;
using Vortex.Primitives.Networking;

namespace Vortex.Protocol.Messages.Incoming.FriendList;

public record SendRoomInviteMessage : IMessageEvent
{
    public required string Message { get; init; }
    public required List<int> FriendIds { get; init; }
}
