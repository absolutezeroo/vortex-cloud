using Vortex.Primitives.Collectibles;
using Vortex.Primitives.Messages.Outgoing.Collectibles;
using Vortex.Primitives.Packets;
using Vortex.Revisions.Revision20260701.Serializers.Collectibles.Data;

namespace Vortex.Revisions.Revision20260701.Serializers.Collectibles;

internal class NftCollectionsMessageComposerSerializer(int header)
    : AbstractSerializer<NftCollectionsMessageComposer>(header)
{
    protected override void Serialize(IServerPacket packet, NftCollectionsMessageComposer message)
    {
        packet.WriteInteger(message.Collections.Length);

        foreach (NftCollectionSnapshot collection in message.Collections)
        {
            WriteCollection(packet, collection);
        }
    }

    /// <summary>
    /// One collection. The two optional items are each announced by a boolean, and their claims are
    /// written further down under those same booleans — after the status, not beside the items — so
    /// the flags are read once and reused rather than recomputed.
    /// </summary>
    private static void WriteCollection(IServerPacket packet, NftCollectionSnapshot collection)
    {
        packet.WriteInteger(collection.Items.Length);

        foreach (CollectibleProductItemSnapshot item in collection.Items)
        {
            CollectibleSerialization.WriteProductItem(packet, item);
        }

        bool hasBonusItem = collection.BonusItem is not null;
        bool hasRewardItem = collection.RewardItem is not null;

        packet
            .WriteString(collection.CollectionId)
            .WriteString(collection.CollectionName)
            .WriteInteger(collection.CollectionScore)
            .WriteInteger(collection.CollectionTotalScore)
            .WriteInteger(collection.CollectionBoostScore)
            .WriteBoolean(hasBonusItem);

        if (collection.BonusItem is { } bonusItem)
        {
            CollectibleSerialization.WriteProductItem(packet, bonusItem);
        }

        packet.WriteBoolean(hasRewardItem);

        if (collection.RewardItem is { } rewardItem)
        {
            CollectibleSerialization.WriteProductItem(packet, rewardItem);
        }

        packet
            .WriteLong(collection.ReleasedTimeMs)
            .WriteLong(collection.SnapshotTimeMs)
            .WriteShort((short)collection.Status);

        // A collection that announced an item but carries no claim for it still owes the client a
        // claim struct, or every field after this one is read from the wrong place.
        if (hasBonusItem)
        {
            CollectibleSerialization.WriteClaim(
                packet,
                collection.BonusItemClaim ?? EmptyClaim(collection.CollectionId, "bonus")
            );
        }

        if (hasRewardItem)
        {
            CollectibleSerialization.WriteClaim(
                packet,
                collection.RewardItemClaim ?? EmptyClaim(collection.CollectionId, "reward")
            );
        }
    }

    private static CollectibleItemClaimSnapshot EmptyClaim(string collectionId, string kind) =>
        new()
        {
            ClaimId = $"{collectionId}:{kind}",
            ClaimedAmount = 0,
            ClaimLimit = 1,
            Status = CollectibleClaimStatus.NotClaimable,
        };
}
