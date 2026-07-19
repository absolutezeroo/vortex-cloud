using Vortex.Primitives.Messages.Outgoing.Userdefinedroomevents;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Serializers.UserDefinedRoomEvents;

internal class WiredRewardResultMessageComposerSerializer(int header)
    : AbstractSerializer<WiredRewardResultMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        WiredRewardResultMessageComposer message
    )
    {
        //
    }
}
