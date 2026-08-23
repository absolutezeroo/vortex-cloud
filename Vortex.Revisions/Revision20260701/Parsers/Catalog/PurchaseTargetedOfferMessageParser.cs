using Vortex.Primitives.Networking;
using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Incoming.Catalog;

namespace Vortex.Revisions.Revision20260701.Parsers.Catalog;

internal class PurchaseTargetedOfferMessageParser : IParser
{
    public IMessageEvent Parse(IClientPacket packet) =>
        new PurchaseTargetedOfferMessage { OfferId = packet.PopInt(), Quantity = packet.PopInt() };
}
