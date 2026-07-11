using Turbo.Primitives.Messages.Outgoing.Users;
using Turbo.Primitives.Packets;

namespace Turbo.Revisions.Revision20260701.Serializers.Users;

internal class PetRespectNotificationEventMessageComposerSerializer(int header)
    : AbstractSerializer<PetRespectNotificationEventMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        PetRespectNotificationEventMessageComposer message
    )
    {
        packet
            .WriteInteger(message.PetRespect)
            .WriteInteger(message.PetOwnerId)
            .WriteInteger(message.PetId)
            .WriteString(message.PetName)
            .WriteInteger(message.PetType)
            .WriteInteger(message.PetPaletteId)
            .WriteString(message.PetColor)
            .WriteInteger(message.PetRace)
            .WriteInteger(0) // customPartCount
            .WriteInteger(message.PetLevel);
    }
}
