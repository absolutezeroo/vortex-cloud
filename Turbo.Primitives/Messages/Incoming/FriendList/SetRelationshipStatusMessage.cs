using Turbo.Primitives.FriendList.Enums;
using Turbo.Primitives.Networking;

namespace Turbo.Primitives.Messages.Incoming.FriendList;

public record SetRelationshipStatusMessage : IMessageEvent
{
    public required int FriendId { get; init; }
    public required MessengerFriendRelationType RelationType { get; init; }
}
