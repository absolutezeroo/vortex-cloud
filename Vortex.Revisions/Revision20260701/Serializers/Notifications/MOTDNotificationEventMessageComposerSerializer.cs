using Vortex.Primitives.Messages.Outgoing.Notifications;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Serializers.Notifications;

internal class MOTDNotificationEventMessageComposerSerializer(int header)
    : AbstractSerializer<MOTDNotificationEventMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        MOTDNotificationEventMessageComposer message
    )
    {
        packet.WriteInteger(message.Messages.Count);

        foreach (string motd in message.Messages)
        {
            packet.WriteString(motd);
        }
    }
}
