using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Outgoing.Users;

namespace Vortex.Revisions.Revision20260701.Serializers.Users;

internal class PetSupplementedNotificationEventMessageComposerSerializer(int header)
    : AbstractSerializer<PetSupplementedNotificationEventMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        PetSupplementedNotificationEventMessageComposer message
    )
    {
        packet
            .WriteInteger(message.PetId)
            .WriteInteger(message.UserId)
            .WriteInteger(message.SupplementType);
    }
}
