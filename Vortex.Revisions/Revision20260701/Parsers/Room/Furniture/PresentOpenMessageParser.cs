using Vortex.Primitives.Messages.Incoming.Room.Furniture;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Packets;
using Vortex.Primitives.Rooms.Object;

namespace Vortex.Revisions.Revision20260701.Parsers.Room.Furniture;

internal class PresentOpenMessageParser : IParser
{
    public IMessageEvent Parse(IClientPacket packet) =>
        new PresentOpenMessage { ObjectId = new RoomObjectId(packet.PopInt()) };
}
