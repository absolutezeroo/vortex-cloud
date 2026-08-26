using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Outgoing.Userdefinedroomevents;

namespace Vortex.Revisions.Revision20260701.Serializers.UserDefinedRoomEvents;

internal class WiredClickUserResponseMessageComposerSerializer(int header)
    : AbstractSerializer<WiredClickUserResponseMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        WiredClickUserResponseMessageComposer message
    ) => packet.WriteInteger(message.Index).WriteBoolean(message.OpenMenu);
}
