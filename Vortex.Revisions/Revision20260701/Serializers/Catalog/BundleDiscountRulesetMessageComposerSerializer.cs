using Vortex.Primitives.Messages.Outgoing.Catalog;
using Vortex.Primitives.Packets;
using Vortex.Revisions.Revision20260701.Serializers.Catalog.Data;

namespace Vortex.Revisions.Revision20260701.Serializers.Catalog;

internal class BundleDiscountRulesetMessageComposerSerializer(int header)
    : AbstractSerializer<BundleDiscountRulesetMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        BundleDiscountRulesetMessageComposer message
    )
    {
        BundleDiscountRulesetSnapshotSerializer.Serialize(packet, message.BundleDiscountRuleset);
    }
}
