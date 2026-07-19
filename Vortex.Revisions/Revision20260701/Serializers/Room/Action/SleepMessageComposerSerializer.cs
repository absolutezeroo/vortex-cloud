using Vortex.Primitives.Messages.Outgoing.Room.Action;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Serializers.Room.Action;

internal class SleepMessageComposerSerializer(int header)
    : AbstractSerializer<SleepMessageComposer>(header)
{
    protected override void Serialize(IServerPacket packet, SleepMessageComposer message)
    {
        packet.WriteInteger(message.UserId).WriteBoolean(message.IsSleeping);
    }
}
