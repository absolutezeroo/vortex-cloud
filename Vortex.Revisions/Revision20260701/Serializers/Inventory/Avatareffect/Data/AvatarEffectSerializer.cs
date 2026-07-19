using Vortex.Primitives.Inventory.Snapshots;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Serializers.Inventory.Avatareffect.Data;

internal class AvatarEffectSerializer
{
    public static void Serialize(IServerPacket packet, AvatarEffectSnapshot effect)
    {
        packet
            .WriteInteger(effect.Type)
            .WriteInteger(effect.SubType)
            .WriteInteger(effect.Duration)
            .WriteInteger(effect.InactiveEffectsInInventory)
            .WriteInteger(effect.SecondsLeftIfActive)
            .WriteBoolean(effect.IsPermanent);
    }
}
