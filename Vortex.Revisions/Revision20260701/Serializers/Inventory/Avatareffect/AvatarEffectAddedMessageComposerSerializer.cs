using Vortex.Primitives.Messages.Outgoing.Inventory.Avatareffect;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Serializers.Inventory.Avatareffect;

internal class AvatarEffectAddedMessageComposerSerializer(int header)
    : AbstractSerializer<AvatarEffectAddedMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        AvatarEffectAddedMessageComposer message
    )
    {
        //
    }
}
