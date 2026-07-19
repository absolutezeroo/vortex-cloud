using Vortex.Primitives.Messages.Outgoing.Notifications;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Serializers.Notifications;

internal class OfferRewardDeliveredMessageComposerSerializer(int header)
    : AbstractSerializer<OfferRewardDeliveredMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        OfferRewardDeliveredMessageComposer message
    )
    {
        //
    }
}
