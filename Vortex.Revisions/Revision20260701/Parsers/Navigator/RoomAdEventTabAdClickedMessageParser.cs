using Vortex.Primitives.Messages.Incoming.Navigator;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Parsers.Navigator;

internal class RoomAdEventTabAdClickedMessageParser : IParser
{
    public IMessageEvent Parse(IClientPacket packet) =>
        new RoomAdEventTabAdClickedMessage
        {
            FlatId = packet.PopInt(),
            RoomAdName = packet.PopString(),
            RoomAdExpiresInMin = packet.PopInt(),
        };
}
