using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Outgoing.Room.Engine;

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
