using Vortex.Protocol.Messages.Outgoing.Moderation;
using Vortex.Primitives.Moderation;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Serializers.Moderation;

internal class RoomVisitsEventMessageComposerSerializer(int header)
    : AbstractSerializer<RoomVisitsEventMessageComposer>(header)
{
    protected override void Serialize(IServerPacket packet, RoomVisitsEventMessageComposer message)
    {
        packet
            .WriteInteger(message.UserId)
            .WriteString(message.UserName)
            .WriteInteger(message.Visits.Length);

        foreach (RoomVisitSnapshot visit in message.Visits)
        {
            packet
                .WriteInteger(visit.RoomId)
                .WriteString(visit.RoomName)
                .WriteInteger(visit.EnterHour)
                .WriteInteger(visit.EnterMinute);
        }
    }
}
