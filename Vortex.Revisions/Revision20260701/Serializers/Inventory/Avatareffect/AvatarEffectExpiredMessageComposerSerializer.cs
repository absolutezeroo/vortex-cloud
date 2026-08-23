using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Outgoing.Inventory.Avatareffect;

namespace Vortex.Revisions.Revision20260701.Serializers.Inventory.Avatareffect;

internal class AvatarEffectExpiredMessageComposerSerializer(int header)
    : AbstractSerializer<AvatarEffectExpiredMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        AvatarEffectExpiredMessageComposer message
    )
    {
        packet.WriteInteger(message.Type);
    }
}
