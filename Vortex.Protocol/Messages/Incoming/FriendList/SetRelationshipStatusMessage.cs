using Vortex.Primitives.FriendList.Enums;
using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Incoming.FriendList;

public record SetRelationshipStatusMessage : IMessageEvent
{
    public required int FriendId { get; init; }
    public required MessengerFriendRelationType RelationType { get; init; }
}
