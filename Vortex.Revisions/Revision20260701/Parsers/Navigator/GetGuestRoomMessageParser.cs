using Vortex.Primitives.Messages.Incoming.Navigator;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Parsers.Navigator;

internal class GetGuestRoomMessageParser : IParser
{
    public IMessageEvent Parse(IClientPacket packet) =>
        new GetGuestRoomMessage
        {
            RoomId = packet.PopInt(),
            EnterRoom = packet.PopInt() == 1,
            RoomForward = packet.PopInt() == 1,
        };
}
