using Vortex.Primitives.Messages.Incoming.Marketplace;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Parsers.Marketplace;

internal class GetMarketplaceOffersMessageParser : IParser
{
    public IMessageEvent Parse(IClientPacket packet) =>
        new GetMarketplaceOffersMessage
        {
            MinPrice = packet.PopInt(),
            MaxPrice = packet.PopInt(),
            SearchQuery = packet.PopString(),
            SortOrder = packet.PopInt(),
        };
}
