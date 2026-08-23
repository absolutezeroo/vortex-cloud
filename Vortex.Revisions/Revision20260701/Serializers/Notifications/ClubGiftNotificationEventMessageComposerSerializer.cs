using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Outgoing.Notifications;

namespace Vortex.Revisions.Revision20260701.Serializers.Notifications;

internal class ClubGiftNotificationEventMessageComposerSerializer(int header)
    : AbstractSerializer<ClubGiftNotificationEventMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        ClubGiftNotificationEventMessageComposer message
    )
    {
        packet.WriteInteger(message.GiftsAvailable);
    }
}
