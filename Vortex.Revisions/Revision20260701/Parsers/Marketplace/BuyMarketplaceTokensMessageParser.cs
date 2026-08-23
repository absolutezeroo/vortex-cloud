using Vortex.Primitives.Networking;
using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Incoming.Marketplace;

namespace Vortex.Revisions.Revision20260701.Parsers.Marketplace;

internal class BuyMarketplaceTokensMessageParser : IParser
{
    public IMessageEvent Parse(IClientPacket packet) => new BuyMarketplaceTokensMessage();
}
