using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Outgoing.Perk;

namespace Vortex.Revisions.Revision20260701.Serializers.Perk;

internal class CitizenshipVipOfferPromoEnabledEventMessageComposerSerializer(int header)
    : AbstractSerializer<CitizenshipVipOfferPromoEnabledEventMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        CitizenshipVipOfferPromoEnabledEventMessageComposer message
    )
    {
        //
    }
}
