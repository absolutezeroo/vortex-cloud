using Vortex.Primitives.Messages.Incoming.Room.Furniture;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Parsers.Room.Furniture;

internal class ThrowDiceMessageParser : IParser
{
    public IMessageEvent Parse(IClientPacket packet) =>
        new ThrowDiceMessage { ObjectId = packet.PopInt() };
}
