using Vortex.Primitives.Messages.Outgoing.Userdefinedroomevents;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Serializers.UserDefinedRoomEvents;

internal class OpenEventMessageComposerSerializer(int header)
    : AbstractSerializer<OpenEventMessageComposer>(header)
{
    protected override void Serialize(IServerPacket packet, OpenEventMessageComposer message)
    {
        packet.WriteInteger(message.ItemId);
    }
}
