using Vortex.Primitives.Messages.Incoming.Inventory.Trading;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Parsers.Inventory.Trading;

internal class RemoveItemFromTradeMessageParser : IParser
{
    public IMessageEvent Parse(IClientPacket packet) =>
        new RemoveItemFromTradeMessage { ItemId = packet.PopInt() };
}
