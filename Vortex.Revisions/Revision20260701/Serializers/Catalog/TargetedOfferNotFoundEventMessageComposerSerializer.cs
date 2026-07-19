using Vortex.Primitives.Messages.Outgoing.Catalog;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Serializers.Catalog;

internal class TargetedOfferNotFoundEventMessageComposerSerializer(int header)
    : AbstractSerializer<TargetedOfferNotFoundEventMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        TargetedOfferNotFoundEventMessageComposer message
    )
    {
        //
    }
}
