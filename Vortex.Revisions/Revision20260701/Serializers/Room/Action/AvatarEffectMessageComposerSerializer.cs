using Vortex.Primitives.Messages.Outgoing.Room.Action;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Serializers.Room.Action;

internal class AvatarEffectMessageComposerSerializer(int header)
    : AbstractSerializer<AvatarEffectMessageComposer>(header)
{
    protected override void Serialize(IServerPacket packet, AvatarEffectMessageComposer message)
    {
        packet
            .WriteInteger(message.UserId)
            .WriteInteger(message.EffectId)
            .WriteInteger(message.DelayMilliseconds);
    }
}
