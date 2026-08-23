using Vortex.Protocol.Messages.Incoming.Marketplace;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Parsers.Marketplace;

internal class BuyMarketplaceOfferMessageParser : IParser
{
    public IMessageEvent Parse(IClientPacket packet) =>
        new BuyMarketplaceOfferMessage { OfferId = packet.PopInt() };
}
