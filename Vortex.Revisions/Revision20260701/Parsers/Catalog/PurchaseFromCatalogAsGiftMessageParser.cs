using Vortex.Primitives.Messages.Incoming.Catalog;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Parsers.Catalog;

internal class PurchaseFromCatalogAsGiftMessageParser : IParser
{
    public IMessageEvent Parse(IClientPacket packet) =>
        new PurchaseFromCatalogAsGiftMessage
        {
            PageId = packet.PopInt(),
            OfferCode = packet.PopInt(),
            ExtraParam = packet.PopString(),
            RecieverName = packet.PopString(),
            Message = packet.PopString(),
            BoxStuffTypeId = packet.PopInt(),
            BoxTypeId = packet.PopInt(),
            RibbonTypeId = packet.PopInt(),
            ShowPurchaserName = packet.PopBoolean(),
        };
}
