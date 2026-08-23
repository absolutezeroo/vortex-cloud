using Vortex.Primitives.Networking;
using Vortex.Primitives.Packets;
using Vortex.Primitives.Rooms.Object;
using Vortex.Protocol.Messages.Incoming.Room.Engine;

namespace Vortex.Revisions.Revision20260701.Parsers.Room.Engine;

internal class ClickFurniMessageParser : IParser
{
    public IMessageEvent Parse(IClientPacket packet) =>
        new ClickFurniMessage
        {
            ObjectId = new RoomObjectId(packet.PopInt()),
            Param = packet.PopInt(),
        };
}
