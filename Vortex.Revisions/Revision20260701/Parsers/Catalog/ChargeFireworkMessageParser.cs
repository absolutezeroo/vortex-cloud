using Vortex.Primitives.Messages.Incoming.Catalog;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Parsers.Catalog;

internal class ChargeFireworkMessageParser : IParser
{
    public IMessageEvent Parse(IClientPacket packet) =>
        new ChargeFireworkMessage { SpriteId = packet.PopInt(), Type = packet.PopInt() };
}
