using Vortex.Primitives.Networking;
using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Incoming.Navigator;

namespace Vortex.Revisions.Revision20260701.Parsers.Navigator;

internal class UpdateHomeRoomMessageParser : IParser
{
    public IMessageEvent Parse(IClientPacket packet) =>
        new UpdateHomeRoomMessage { RoomId = packet.PopInt() };
}
