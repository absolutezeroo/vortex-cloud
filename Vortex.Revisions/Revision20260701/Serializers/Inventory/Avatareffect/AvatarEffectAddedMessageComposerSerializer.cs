using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Outgoing.Inventory.Avatareffect;

namespace Vortex.Revisions.Revision20260701.Serializers.Inventory.Avatareffect;

internal class AvatarEffectAddedMessageComposerSerializer(int header)
    : AbstractSerializer<AvatarEffectAddedMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        AvatarEffectAddedMessageComposer message
    )
    {
        packet
            .WriteInteger(message.Type)
            .WriteInteger(message.SubType)
            .WriteInteger(message.Duration)
            .WriteBoolean(message.IsPermanent);
    }
}
