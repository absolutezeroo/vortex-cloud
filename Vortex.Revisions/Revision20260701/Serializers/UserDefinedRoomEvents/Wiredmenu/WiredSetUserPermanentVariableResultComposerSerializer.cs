using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Outgoing.Userdefinedroomevents.Wiredmenu;

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
