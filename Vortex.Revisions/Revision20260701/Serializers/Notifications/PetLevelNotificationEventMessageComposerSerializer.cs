using Vortex.Primitives.Messages.Outgoing.Notifications;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Serializers.Notifications;

internal class PetLevelNotificationEventMessageComposerSerializer(int header)
    : AbstractSerializer<PetLevelNotificationEventMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        PetLevelNotificationEventMessageComposer message
    )
    {
        packet
            .WriteInteger(message.PetId)
            .WriteInteger(message.NewLevel)
            .WriteString(message.PetName);
    }
}
