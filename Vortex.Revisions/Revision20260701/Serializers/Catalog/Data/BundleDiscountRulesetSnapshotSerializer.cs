using Vortex.Primitives.Packets;
using Vortex.Primitives.Snapshots.Catalog;

namespace Vortex.Revisions.Revision20260701.Serializers.Catalog.Data;

internal class BundleDiscountRulesetSnapshotSerializer
{
    public static void Serialize(IServerPacket packet, BundleDiscountRulesetSnapshot message)
    {
        packet
            .WriteInteger(message.MaxPurchaseSize)
            .WriteInteger(message.BundleSize)
            .WriteInteger(message.BundleDiscountSize)
            .WriteInteger(message.BonusThreshold)
            .WriteInteger(message.AdditionalBonusDiscountThresholdQuantities.Length);

        foreach (int quantity in message.AdditionalBonusDiscountThresholdQuantities)
        {
            packet.WriteInteger(quantity);
        }
    }
}
