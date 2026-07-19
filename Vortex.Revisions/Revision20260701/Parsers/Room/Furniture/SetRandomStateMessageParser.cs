using Vortex.Primitives.Messages.Incoming.Room.Furniture;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Parsers.Room.Furniture;

internal class SetRandomStateMessageParser : IParser
{
    public IMessageEvent Parse(IClientPacket packet) =>
        new SetRandomStateMessage { ObjectId = packet.PopInt(), Param = packet.PopInt() };
}
