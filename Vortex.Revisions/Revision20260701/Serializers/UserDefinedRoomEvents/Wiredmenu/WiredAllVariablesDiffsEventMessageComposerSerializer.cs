using Vortex.Primitives.Messages.Outgoing.Userdefinedroomevents.Wiredmenu;
using Vortex.Primitives.Packets;
using Vortex.Primitives.Rooms.Snapshots.Wired.Variables;
using Vortex.Primitives.Rooms.Wired.Variable;
using Vortex.Revisions.Revision20260701.Serializers.UserDefinedRoomEvents.Data;

namespace Vortex.Revisions.Revision20260701.Serializers.UserDefinedRoomEvents.Wiredmenu;

internal class WiredAllVariablesDiffsEventMessageComposerSerializer(int header)
    : AbstractSerializer<WiredAllVariablesDiffsEventMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        WiredAllVariablesDiffsEventMessageComposer message
    )
    {
        packet
            .WriteInteger(message.AllVariablesHash.Value)
            .WriteBoolean(message.IsLastChunk)
            .WriteInteger(message.RemovedVariableIds.Count);

        foreach (WiredVariableId removedVariable in message.RemovedVariableIds)
        {
            packet.WriteString(removedVariable.ToString());
        }

        packet.WriteInteger(message.AddedOrUpdated.Count);

        foreach (WiredVariableSnapshot snapshot in message.AddedOrUpdated)
        {
            packet.WriteInteger(snapshot.VariableHash.Value);

            WiredVariableSerializer.Serialize(packet, snapshot);
        }
    }
}
