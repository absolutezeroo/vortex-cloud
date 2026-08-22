using System.Collections.Immutable;
using Vortex.Primitives.Inventory.Snapshots;
using Vortex.Primitives.Messages.Outgoing.Userdefinedroomevents.Wiredtrading;
using Vortex.Primitives.Packets;
using Vortex.Revisions.Revision20260701.Serializers.Inventory.Trading.Data;

namespace Vortex.Revisions.Revision20260701.Serializers.UserDefinedRoomEvents.Wiredtrading;

/// <summary>
/// The two sides are written exactly as the player-to-player trade writes them — the client reads
/// them back with that same parser — and only then the two fields this message adds.
/// </summary>
internal class WiredTradeItemsUpdateMessageComposerSerializer(int header)
    : AbstractSerializer<WiredTradeItemsUpdateMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        WiredTradeItemsUpdateMessageComposer message
    )
    {
        WriteSide(packet, message.FirstUserId, message.FirstUserItems, message.FirstUserCredits);
        WriteSide(packet, message.SecondUserId, message.SecondUserItems, message.SecondUserCredits);

        packet.WriteBoolean(message.CanAccept).WriteInteger(message.Extra);
    }

    private static void WriteSide(
        IServerPacket packet,
        int userId,
        ImmutableArray<FurnitureItemSnapshot> items,
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
