using Vortex.Primitives.Messages.Incoming.Marketplace;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Parsers.Marketplace;

internal class CancelMarketplaceOfferMessageParser : IParser
{
    public IMessageEvent Parse(IClientPacket packet) =>
        new CancelMarketplaceOfferMessage { OfferId = packet.PopInt() };
}
