using Vortex.Primitives.Messages.Incoming.Room.Engine;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Packets;
using Vortex.Primitives.Rooms.Object;

namespace Vortex.Revisions.Revision20260701.Parsers.Room.Engine;

internal class PickupObjectMessageParser : IParser
{
    public IMessageEvent Parse(IClientPacket packet)
    {
        int categoryId = packet.PopInt();
        int objectId = packet.PopInt();
        bool confirm = !packet.End && packet.PopBoolean();
        return new PickupObjectMessage
        {
            CategoryId = categoryId,
            ObjectId = new RoomObjectId(objectId),
            Confirm = confirm,
        };
    }
}
