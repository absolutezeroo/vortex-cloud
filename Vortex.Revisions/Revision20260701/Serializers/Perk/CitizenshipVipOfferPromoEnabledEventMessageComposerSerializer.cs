using Vortex.Primitives.Messages.Outgoing.Perk;
using Vortex.Primitives.Packets;

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
