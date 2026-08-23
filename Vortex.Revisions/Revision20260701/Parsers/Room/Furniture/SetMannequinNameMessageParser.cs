using Vortex.Primitives.Networking;
using Vortex.Primitives.Packets;
using Vortex.Primitives.Rooms.Object;
using Vortex.Protocol.Messages.Incoming.Room.Furniture;

namespace Vortex.Revisions.Revision20260701.Parsers.Room.Furniture;

internal class SetMannequinNameMessageParser : IParser
{
    public IMessageEvent Parse(IClientPacket packet)
    {
        int objectId = packet.PopInt();
        string name = packet.PopString();

        return new SetMannequinNameMessage { ObjectId = new RoomObjectId(objectId), Name = name };
    }
}
