using Vortex.Primitives.Networking;
using Vortex.Primitives.Packets;
using Vortex.Primitives.Rooms.Object;
using Vortex.Protocol.Messages.Incoming.Room.Engine;

namespace Vortex.Revisions.Revision20260701.Parsers.Room.Engine;

internal class RemoveItemMessageParser : IParser
{
    public IMessageEvent Parse(IClientPacket packet) =>
        new RemoveItemMessage { ObjectId = new RoomObjectId(packet.PopInt()) };
}
