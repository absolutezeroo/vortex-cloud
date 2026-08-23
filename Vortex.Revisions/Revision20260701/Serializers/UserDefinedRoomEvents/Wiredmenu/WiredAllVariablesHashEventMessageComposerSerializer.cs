using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Outgoing.Userdefinedroomevents.Wiredmenu;

namespace Vortex.Revisions.Revision20260701.Serializers.UserDefinedRoomEvents.Wiredmenu;

internal class WiredAllVariablesHashEventMessageComposerSerializer(int header)
    : AbstractSerializer<WiredAllVariablesHashEventMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        WiredAllVariablesHashEventMessageComposer message
    ) => packet.WriteInteger(message.AllVariablesHash.Value);
}
