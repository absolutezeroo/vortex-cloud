using Vortex.Primitives.Messages.Incoming.Room.Engine;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Parsers.Room.Engine;

internal class UseFurnitureMessageParser : IParser
{
    public IMessageEvent Parse(IClientPacket packet) =>
        new UseFurnitureMessage { ObjectId = packet.PopInt(), Param = packet.PopInt() };
}
