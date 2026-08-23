using Vortex.Protocol.Messages.Incoming.Room.Furniture;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Packets;
using Vortex.Primitives.Rooms.Object;

namespace Vortex.Revisions.Revision20260701.Parsers.Room.Furniture;

internal class RoomDimmerChangeStateMessageParser : IParser
{
    public IMessageEvent Parse(IClientPacket packet) =>
        new RoomDimmerChangeStateMessage { ObjectId = new RoomObjectId(packet.PopInt()) };
}
