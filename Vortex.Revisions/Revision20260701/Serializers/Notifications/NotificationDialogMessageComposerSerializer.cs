using Vortex.Primitives.Messages.Outgoing.Notifications;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Serializers.Notifications;

internal class NotificationDialogMessageComposerSerializer(int header)
    : AbstractSerializer<NotificationDialogMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        NotificationDialogMessageComposer message
    )
    {
        //
    }
}
