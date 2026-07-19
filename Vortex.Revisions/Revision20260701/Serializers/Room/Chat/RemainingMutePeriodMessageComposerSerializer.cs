using Vortex.Primitives.Messages.Outgoing.Room.Chat;
using Vortex.Primitives.Packets;

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
