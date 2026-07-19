using Vortex.Primitives.Messages.Incoming.Catalog;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Parsers.Catalog;

internal class PurchaseVipMembershipExtensionMessageParser : IParser
{
    public IMessageEvent Parse(IClientPacket packet) =>
        new PurchaseVipMembershipExtensionMessage { OfferId = packet.PopInt() };
}
