using Vortex.Primitives.Messages.Outgoing.Handshake;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Serializers.Handshake;

internal class IsFirstLoginOfDayMessageSerializer(int header)
    : AbstractSerializer<IsFirstLoginOfDayMessage>(header)
{
    protected override void Serialize(IServerPacket packet, IsFirstLoginOfDayMessage message)
    {
        packet.WriteBoolean(message.IsFirstLoginOfDay);
    }
}
