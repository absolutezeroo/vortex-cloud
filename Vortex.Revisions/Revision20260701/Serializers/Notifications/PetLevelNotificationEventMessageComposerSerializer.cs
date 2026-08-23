using Vortex.Protocol.Messages.Outgoing.Notifications;
using Vortex.Primitives.Packets;
using Vortex.Revisions.Revision20260701.Serializers.Room.Pets.Snapshots;

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
            .WriteString(message.PetName)
            .WriteInteger(message.NewLevel);

        PetFigureDataSerializer.Serialize(packet, message.Pet);
    }
}
