using Vortex.Primitives.Messages.Incoming.Moderator;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Parsers.Moderator;

internal class ModerateRoomMessageParser : IParser
{
    public IMessageEvent Parse(IClientPacket packet)
    {
        // Ints, not booleans: _SafeCls_2501 pushes `flag ? 1 : 0`, and the client's encoder writes
        // an AS3 int as four bytes. Reading these as single-byte booleans desynchronises the buffer.
        int roomId = packet.PopInt();
        bool lockDoor = packet.PopInt() != 0;
        bool changeName = packet.PopInt() != 0;
        bool kickUsers = packet.PopInt() != 0;

        return new ModerateRoomMessage
        {
            RoomId = roomId,
            LockDoor = lockDoor,
            ChangeName = changeName,
            KickUsers = kickUsers,
        };
    }
}
