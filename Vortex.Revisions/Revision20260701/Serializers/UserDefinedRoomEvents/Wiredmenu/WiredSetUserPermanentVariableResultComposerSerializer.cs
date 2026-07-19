using Vortex.Primitives.Messages.Outgoing.Userdefinedroomevents.Wiredmenu;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Serializers.UserDefinedRoomEvents.Wiredmenu;

internal class WiredSetUserPermanentVariableResultComposerSerializer(int header)
    : AbstractSerializer<WiredSetUserPermanentVariableResultComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        WiredSetUserPermanentVariableResultComposer message
    )
    {
        packet.WriteBoolean(message.Success);
    }
}
