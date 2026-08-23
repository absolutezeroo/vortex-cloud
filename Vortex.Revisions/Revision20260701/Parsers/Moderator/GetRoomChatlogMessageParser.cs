using Vortex.Primitives.Networking;
using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Incoming.Moderator;

namespace Vortex.Revisions.Revision20260701.Parsers.Moderator;

internal class GetRoomChatlogMessageParser : IParser
{
    public IMessageEvent Parse(IClientPacket packet)
    {
        // Order matters: _SafeCls_2601(roomType, roomId) puts the type first. Reading the id first
        // made every room-chatlog lookup query room 0 or 1 instead of the room being moderated.
        int roomType = packet.PopInt();
        int roomId = packet.PopInt();

        return new GetRoomChatlogMessage { RoomType = roomType, RoomId = roomId };
    }
}
