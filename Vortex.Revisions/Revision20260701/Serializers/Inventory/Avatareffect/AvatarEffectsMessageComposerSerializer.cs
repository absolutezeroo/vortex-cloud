using Vortex.Primitives.Inventory.Snapshots;
using Vortex.Primitives.Messages.Outgoing.Inventory.Avatareffect;
using Vortex.Primitives.Packets;
using Vortex.Revisions.Revision20260701.Serializers.Inventory.Avatareffect.Data;

namespace Vortex.Revisions.Revision20260701.Serializers.Inventory.Avatareffect;

internal class AvatarEffectsMessageComposerSerializer(int header)
    : AbstractSerializer<AvatarEffectsMessageComposer>(header)
{
    protected override void Serialize(IServerPacket packet, AvatarEffectsMessageComposer message)
    {
        packet.WriteInteger(message.Effects.Length);

        foreach (AvatarEffectSnapshot effect in message.Effects)
        {
            AvatarEffectSerializer.Serialize(packet, effect);
        }
    }
}
