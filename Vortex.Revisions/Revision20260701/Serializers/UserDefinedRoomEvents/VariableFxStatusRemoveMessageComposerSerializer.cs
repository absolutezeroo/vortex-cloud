using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Outgoing.Userdefinedroomevents;

namespace Vortex.Revisions.Revision20260701.Serializers.UserDefinedRoomEvents;

internal class VariableFxStatusRemoveMessageComposerSerializer(int header)
    : AbstractSerializer<VariableFxStatusRemoveMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        VariableFxStatusRemoveMessageComposer message
    )
    {
        packet.WriteInteger(message.StatusKeys.Length);

        foreach (string key in message.StatusKeys)
        {
            packet.WriteString(key);
        }
    }
}
