using Vortex.Primitives.Messages.Incoming.Room.Engine;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Packets;
using Vortex.Primitives.Rooms.Enums;
using Vortex.Primitives.Rooms.Object;

namespace Vortex.Revisions.Revision20260701.Parsers.Room.Engine;

internal class MoveObjectMessageParser : IParser
{
    public IMessageEvent Parse(IClientPacket packet) =>
        new MoveObjectMessage
        {
            ObjectId = new RoomObjectId(packet.PopInt()),
            X = packet.PopInt(),
            Y = packet.PopInt(),
            Rotation = (Rotation)packet.PopInt(),
        };
}
