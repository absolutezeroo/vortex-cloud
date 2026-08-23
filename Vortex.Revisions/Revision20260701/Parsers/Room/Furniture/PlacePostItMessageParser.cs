using Vortex.Protocol.Messages.Incoming.Room.Furniture;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Packets;
using Vortex.Primitives.Rooms.Object;

namespace Vortex.Revisions.Revision20260701.Parsers.Room.Furniture;

internal class PlacePostItMessageParser : IParser
{
    public IMessageEvent Parse(IClientPacket packet)
    {
        int objectId = packet.PopInt();
        string location = packet.PopString();

        return new PlacePostItMessage
        {
            ObjectId = new RoomObjectId(objectId),
            Location = location,
        };
    }
}
