using System.Collections.Immutable;
using Vortex.Primitives.Messages.Outgoing.Userdefinedroomevents.Wiredtrading;
using Vortex.Primitives.Packets;
using Vortex.Primitives.Rooms.Snapshots.Wired;

namespace Vortex.Revisions.Revision20260701.Serializers.UserDefinedRoomEvents.Wiredtrading;

/// <summary>
/// One transaction's full breakdown.
/// </summary>
/// <remarks>
/// Deposits first, withdrawals second. The two blocks are identical in shape, so sending them the
/// wrong way round does not throw — it labels every item backwards, which is worse.
/// </remarks>
internal class WiredTransactionDetailsMessageComposerSerializer(int header)
    : AbstractSerializer<WiredTransactionDetailsMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        WiredTransactionDetailsMessageComposer message
    )
    {
        WiredTransactionDetailsSnapshot details = message.Details;

        packet.WriteTransaction(details.Info).WriteInteger(details.ChestIds.Length);

        foreach (int chestId in details.ChestIds)
        {
            packet.WriteInteger(chestId);
        }

        WriteItems(packet, details.Deposited);
        WriteItems(packet, details.Withdrawn);

        packet.WriteBoolean(details.IsIncompleteData);
    }

    private static void WriteItems(
        IServerPacket packet,
        ImmutableArray<WiredTransactionItemCount> items
    )
    {
        packet.WriteInteger(items.Length);

        foreach (WiredTransactionItemCount item in items)
        {
            packet
                .WriteBoolean(item.IsWallItem)
                .WriteInteger(item.SpriteId)
                .WriteString(item.LegacyPosterId)
                .WriteInteger(item.Count);
        }
    }
}
