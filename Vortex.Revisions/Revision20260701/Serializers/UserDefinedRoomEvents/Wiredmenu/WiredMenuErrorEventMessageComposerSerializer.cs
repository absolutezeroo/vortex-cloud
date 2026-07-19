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
        //
    }
}
