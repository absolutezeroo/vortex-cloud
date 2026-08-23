using Vortex.Primitives.Networking;
using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Incoming.Marketplace;

namespace Vortex.Revisions.Revision20260701.Parsers.Marketplace;

internal class GetMarketplaceItemStatsMessageParser : IParser
{
    public IMessageEvent Parse(IClientPacket packet) =>
        new GetMarketplaceItemStatsMessage
        {
            CategoryId = packet.PopInt(),
            TypeId = packet.PopInt(),
        };
}
