using Vortex.Primitives.Messages.Outgoing.Inventory.Avatareffect;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Serializers.Inventory.Avatareffect;

internal class AvatarEffectActivatedMessageComposerSerializer(int header)
    : AbstractSerializer<AvatarEffectActivatedMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        AvatarEffectActivatedMessageComposer message
    )
    {
        //
    }
}
