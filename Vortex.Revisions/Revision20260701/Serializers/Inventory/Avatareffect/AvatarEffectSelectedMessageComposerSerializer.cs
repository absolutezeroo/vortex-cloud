using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Outgoing.Inventory.Avatareffect;

namespace Vortex.Revisions.Revision20260701.Serializers.Inventory.Avatareffect;

internal class AvatarEffectSelectedMessageComposerSerializer(int header)
    : AbstractSerializer<AvatarEffectSelectedMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        AvatarEffectSelectedMessageComposer message
    )
    {
        packet.WriteInteger(message.Type);
    }
}
