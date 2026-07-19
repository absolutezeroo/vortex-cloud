using Vortex.Primitives.Messages.Outgoing.Nux;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Serializers.Nux;

internal class NewUserExperienceGiftOfferEventMessageComposerSerializer(int header)
    : AbstractSerializer<NewUserExperienceGiftOfferEventMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        NewUserExperienceGiftOfferEventMessageComposer message
    )
    {
        //
    }
}
