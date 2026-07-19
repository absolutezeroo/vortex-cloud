using Vortex.Primitives.Messages.Outgoing.Userdefinedroomevents;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Serializers.UserDefinedRoomEvents;

internal class WiredSaveSuccessEventMessageComposerSerializer(int header)
    : AbstractSerializer<WiredSaveSuccessEventMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        WiredSaveSuccessEventMessageComposer message
    )
    {
        //
    }
}
