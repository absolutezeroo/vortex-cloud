using Vortex.Primitives.Habbicons.Snapshots;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Serializers.Habbicons;

/// <summary>
/// The two repeated Habbicon blocks, in one place because three messages carry them and the client
/// parses all three with the same two helper classes.
/// </summary>
internal static class HabbiconWriter
{
    /// <summary>
    /// One shop row. The client's <c>_SafeCls_4487.parse</c>: habbiconId, name, collectionId, state,
    /// priceCredits, priceActivityPoints, activityPointType — seven fields, all integers except the
    /// name.
    /// </summary>
    public static void WriteShopItem(IServerPacket packet, HabbiconShopItemSnapshot item) =>
        packet
            .WriteInteger(item.HabbiconId)
            .WriteString(item.Code)
            .WriteInteger(item.CollectionId)
            .WriteInteger((int)item.State)
            .WriteInteger(item.PriceCredits)
            .WriteInteger(item.PriceActivityPoints)
            .WriteInteger(item.ActivityPointType);

    /// <summary>
    /// One collection block. The client's <c>_SafeCls_4498.parse</c>: collectionId, name, completed,
    /// rewardHabbiconId, rewardState, priceCredits, priceActivityPoints, activityPointType, then a
    /// count and that many shop rows.
    /// </summary>
    public static void WriteCollection(
        IServerPacket packet,
        HabbiconShopCollectionSnapshot collection
    )
    {
        packet
            .WriteInteger(collection.CollectionId)
            .WriteString(collection.Code)
            .WriteBoolean(collection.Completed)
            .WriteInteger(collection.RewardHabbiconId)
            .WriteInteger((int)collection.RewardState)
            .WriteInteger(collection.PriceCredits)
            .WriteInteger(collection.PriceActivityPoints)
            .WriteInteger(collection.ActivityPointType)
            .WriteInteger(collection.Habbicons.Length);

        foreach (HabbiconShopItemSnapshot item in collection.Habbicons)
        {
            WriteShopItem(packet, item);
        }
    }
}
