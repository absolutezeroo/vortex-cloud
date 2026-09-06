using Vortex.Primitives.Packets;
using Vortex.Primitives.Rooms.Enums.Wired;
using Vortex.Primitives.Rooms.Wired.Variable;
using Vortex.Protocol.Messages.Outgoing.Userdefinedroomevents.Wiredmenu;

namespace Vortex.Revisions.Revision20260701.Serializers.UserDefinedRoomEvents.Wiredmenu;

internal class WiredVariablesForObjectEventMessageComposerSerializer(int header)
    : AbstractSerializer<WiredVariablesForObjectEventMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        WiredVariablesForObjectEventMessageComposer message
    )
    {
        packet.WriteInteger((int)message.TargetType);

        // The client reads the id only for Furni and User (WiredObjectInspectionData: `type == 0`
        // and `type == 1`); for Global (-10) and the rest it reads none. TargetType is echoed
        // straight back from the client's own request, so writing it unconditionally shifted every
        // variable in the list whenever the menu asked about globals.
        if (message.TargetType is WiredVariableTargetType.Furni or WiredVariableTargetType.User)
        {
            packet.WriteInteger(message.TargetId);
        }

        packet.WriteInteger(message.VariableValues.Count);

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
