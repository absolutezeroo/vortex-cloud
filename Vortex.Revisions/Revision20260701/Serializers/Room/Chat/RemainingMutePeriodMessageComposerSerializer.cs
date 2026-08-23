using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Outgoing.Room.Chat;

namespace Vortex.Revisions.Revision20260701.Serializers.Room.Chat;

internal class RemainingMutePeriodMessageComposerSerializer(int header)
    : AbstractSerializer<RemainingMutePeriodMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        RemainingMutePeriodMessageComposer message
    )
    {
        packet.WriteInteger(message.SecondsRemaining);
    }
}
