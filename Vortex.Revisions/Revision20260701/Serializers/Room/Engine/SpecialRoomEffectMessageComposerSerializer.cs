using Vortex.Primitives.Messages.Outgoing.Room.Engine;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Serializers.Room.Engine;

internal class SpecialRoomEffectMessageComposerSerializer(int header)
    : AbstractSerializer<SpecialRoomEffectMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        SpecialRoomEffectMessageComposer message
    )
    {
        packet.WriteInteger(message.EffectId);
    }
}
