using Vortex.Primitives.Networking;
using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Incoming.Catalog;

namespace Vortex.Revisions.Revision20260701.Parsers.Catalog;

internal class ChargeFireworkMessageParser : IParser
{
    public IMessageEvent Parse(IClientPacket packet) =>
        new ChargeFireworkMessage { SpriteId = packet.PopInt(), Type = packet.PopInt() };
}
