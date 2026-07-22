using Vortex.Primitives.Messages.Incoming.Room.Engine;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Packets;
using Vortex.Primitives.Rooms.Object;

namespace Vortex.Revisions.Revision20260701.Parsers.Room.Engine;

internal class MoveWallItemMessageParser : IParser
{
    public IMessageEvent Parse(IClientPacket packet) =>
        new MoveWallItemMessage
        {
            ObjectId = new RoomObjectId(packet.PopInt()),
            WallPosition = packet.PopString(),
        };
}
