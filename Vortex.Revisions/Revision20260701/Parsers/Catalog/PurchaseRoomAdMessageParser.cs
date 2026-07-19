using Vortex.Primitives.Messages.Incoming.Catalog;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Parsers.Catalog;

internal class PurchaseRoomAdMessageParser : IParser
{
    public IMessageEvent Parse(IClientPacket packet) =>
        new PurchaseRoomAdMessageMessage
        {
            PageId = packet.PopInt(),
            OfferId = packet.PopInt(),
            FlatId = packet.PopInt(),
            Name = packet.PopString(),
            Extended = packet.PopBoolean(),
            Description = packet.PopString(),
            CategoryId = packet.PopInt(),
        };
}
