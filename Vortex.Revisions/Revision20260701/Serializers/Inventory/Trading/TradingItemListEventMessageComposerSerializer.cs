using Vortex.Primitives.Inventory.Snapshots;
using Vortex.Primitives.Messages.Outgoing.Inventory.Trading;
using Vortex.Primitives.Packets;
using Vortex.Revisions.Revision20260701.Serializers.Inventory.Trading.Data;

namespace Vortex.Revisions.Revision20260701.Serializers.Inventory.Trading;

internal class TradingItemListEventMessageComposerSerializer(int header)
    : AbstractSerializer<TradingItemListEventMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        TradingItemListEventMessageComposer message
    )
    {
        WriteSide(packet, message.FirstUserId, message.FirstUserItems, message.FirstUserCredits);
        WriteSide(packet, message.SecondUserId, message.SecondUserItems, message.SecondUserCredits);
    }

    private static void WriteSide(
        IServerPacket packet,
        int userId,
        System.Collections.Immutable.ImmutableArray<FurnitureItemSnapshot> items,
        int credits
    )
    {
        packet.WriteInteger(userId).WriteInteger(items.Length);

        foreach (FurnitureItemSnapshot item in items)
        {
            TradeItemSerializer.Serialize(packet, item);
        }

        packet.WriteInteger(items.Length).WriteInteger(credits);
    }
}
