using Vortex.Primitives.Networking;
using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Incoming.Marketplace;

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
            // Fifth and last: `_SafeCls_1953` pushes `param5:Boolean = true`, which
            // MarketPlaceLogic.as:166 names `_combineUniques`. It was never read.
            CombineUniques = packet.PopBoolean(),
        };
}
