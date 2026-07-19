using Vortex.Primitives.Messages.Outgoing.Userdefinedroomevents.Wiredmenu;
using Vortex.Primitives.Packets;
using Vortex.Primitives.Rooms.Enums.Wired;
using Vortex.Primitives.Rooms.Wired.Variable;

namespace Vortex.Revisions.Revision20260701.Serializers.UserDefinedRoomEvents.Wiredmenu;

internal class WiredVariablesForObjectEventMessageComposerSerializer(int header)
    : AbstractSerializer<WiredVariablesForObjectEventMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        WiredVariablesForObjectEventMessageComposer message
    )
    {
        packet
            .WriteInteger((int)message.TargetType)
            .WriteInteger(message.TargetId)
            .WriteInteger(message.VariableValues.Count);

        foreach ((WiredVariableId id, WiredVariableValue value) in message.VariableValues)
        {
            packet.WriteString(id.ToString()).WriteInteger(value);
        }

        if (message.TargetType == WiredVariableTargetType.Furni)
        {
            packet.WriteInteger(message.ConfiguredInWireds.Count);

            foreach (int id in message.ConfiguredInWireds)
            {
                packet.WriteInteger(id);
            }
        }
    }
}
