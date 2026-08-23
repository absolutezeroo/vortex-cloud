using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Outgoing.Notifications;

namespace Vortex.Revisions.Revision20260701.Serializers.Notifications;

internal class NotificationDialogMessageComposerSerializer(int header)
    : AbstractSerializer<NotificationDialogMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        NotificationDialogMessageComposer message
    )
    {
        packet.WriteString(message.Type).WriteInteger(message.Parameters.Length);

        foreach (NotificationDialogParameter parameter in message.Parameters)
        {
            packet.WriteString(parameter.Key).WriteString(parameter.Value);
        }
    }
}
