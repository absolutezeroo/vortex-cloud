using Turbo.Primitives.Messages.Outgoing.Userdefinedroomevents.Wiredmenu;
using Turbo.Primitives.Packets;

namespace Turbo.Revisions.Revision20260701.Serializers.UserDefinedRoomEvents.Wiredmenu;

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
