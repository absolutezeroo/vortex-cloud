using Vortex.Primitives.Messages.Outgoing.Userdefinedroomevents.Wiredmenu;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Serializers.UserDefinedRoomEvents.Wiredmenu;

internal class WiredMenuErrorEventMessageComposerSerializer(int header)
    : AbstractSerializer<WiredMenuErrorEventMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        WiredMenuErrorEventMessageComposer message
    )
    {
        // A short, not an int. The client reads two bytes here and would decode the next
        // message from the wrong offset if this were widened.
        packet.WriteShort(message.ErrorCode);
    }
}
