using Vortex.Primitives.Networking;
using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Incoming.Catalog;

namespace Vortex.Revisions.Revision20260701.Parsers.Catalog;

internal class ShopTargetedOfferViewedMessageParser : IParser
{
    public IMessageEvent Parse(IClientPacket packet) =>
        new ShopTargetedOfferViewedMessage
        {
            TargetedOfferId = packet.PopInt(),
            TrackingState = packet.PopInt(),
        };
}
