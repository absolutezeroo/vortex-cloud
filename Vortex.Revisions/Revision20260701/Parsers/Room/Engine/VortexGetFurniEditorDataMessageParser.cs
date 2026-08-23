using Vortex.Protocol.Messages.Incoming.Room.Engine;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Packets;
using Vortex.Primitives.Rooms.Object;

namespace Vortex.Revisions.Revision20260701.Parsers.Room.Engine;

internal class VortexGetFurniEditorDataMessageParser : IParser
{
    public IMessageEvent Parse(IClientPacket packet) =>
        new VortexGetFurniEditorDataMessage { ObjectId = new RoomObjectId(packet.PopInt()) };
}
