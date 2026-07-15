using Turbo.Primitives.Messages.Outgoing.Userdefinedroomevents.Wiredmenu;
using Turbo.Primitives.Packets;
using Turbo.Primitives.Rooms.Enums.Wired;
using Turbo.Primitives.Rooms.Snapshots.Wired.Variables;

namespace Turbo.Revisions.Revision20260701.Serializers.UserDefinedRoomEvents.Wiredmenu;

internal class WiredUserPermanentVariablesComposerSerializer(int header)
    : AbstractSerializer<WiredUserPermanentVariablesComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        WiredUserPermanentVariablesComposer message
    )
    {
        WiredPermanentVariablesSnapshot snapshot = message.Snapshot;

        packet
            .WriteInteger((int)snapshot.EntityType)
            .WriteInteger(snapshot.EntityId)
            .WriteString(snapshot.EntityName)
            .WriteString(snapshot.EntityFigure);

        if (snapshot.EntityType != WiredVariableTargetType.User)
        {
            packet
                .WriteInteger(snapshot.OwnerId ?? 0)
                .WriteString(snapshot.OwnerName ?? string.Empty)
                .WriteString(snapshot.OwnerFigure ?? string.Empty);
        }

        packet.WriteInteger(snapshot.Variables.Count);

        foreach ((string variableId, int value) in snapshot.Variables)
        {
            packet.WriteString(variableId).WriteInteger(value);
        }
    }
}
