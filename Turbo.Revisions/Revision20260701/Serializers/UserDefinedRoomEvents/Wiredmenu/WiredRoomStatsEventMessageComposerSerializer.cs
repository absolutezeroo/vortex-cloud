using Turbo.Primitives.Messages.Outgoing.Userdefinedroomevents.Wiredmenu;
using Turbo.Primitives.Packets;

namespace Turbo.Revisions.Revision20260701.Serializers.UserDefinedRoomEvents.Wiredmenu;

internal class WiredRoomStatsEventMessageComposerSerializer(int header)
    : AbstractSerializer<WiredRoomStatsEventMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        WiredRoomStatsEventMessageComposer message
    )
    {
        packet
            .WriteDouble(message.ExecutionCost)
            .WriteDouble(message.ExecutionCostCap)
            .WriteBoolean(message.IsHeavy)
            .WriteInteger(message.FloorItemCount)
            .WriteInteger(message.FloorItemCap)
            .WriteInteger(message.WallItemCount)
            .WriteInteger(message.WallItemCap)
            .WriteInteger(message.PermanentFurniVariables)
            .WriteInteger(message.MaxPermanentFurniVariables)
            .WriteInteger(message.PermanentUserVariables)
            .WriteInteger(message.MaxPermanentUserVariables)
            .WriteInteger(message.PermanentGlobalVariables)
            .WriteInteger(message.MaxPermanentGlobalVariables);
    }
}
