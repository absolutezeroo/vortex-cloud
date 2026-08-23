using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Outgoing.Userdefinedroomevents;

namespace Vortex.Revisions.Revision20260701.Serializers.UserDefinedRoomEvents;

internal class WiredRewardResultMessageComposerSerializer(int header)
    : AbstractSerializer<WiredRewardResultMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        WiredRewardResultMessageComposer message
    )
    {
        packet.WriteInteger(message.Reason);
    }
}
