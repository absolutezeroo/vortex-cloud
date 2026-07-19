using Vortex.Primitives.FriendList.Enums;
using Vortex.Primitives.Messages.Incoming.FriendList;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Parsers.FriendList;

internal class SetRelationshipStatusMessageParser : IParser
{
    public IMessageEvent Parse(IClientPacket packet) =>
        new SetRelationshipStatusMessage
        {
            FriendId = packet.PopInt(),
            RelationType = (MessengerFriendRelationType)packet.PopInt(),
        };
}
