using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Outgoing.Nux;

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
