using Vortex.Primitives.Networking;
using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Incoming.Navigator;

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
