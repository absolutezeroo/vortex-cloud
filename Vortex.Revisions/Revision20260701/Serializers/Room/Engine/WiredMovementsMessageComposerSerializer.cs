using Vortex.Primitives.Messages.Outgoing.Room.Engine;
using Vortex.Primitives.Packets;
using Vortex.Primitives.Rooms.Enums.Wired;
using Vortex.Primitives.Rooms.Snapshots.Wired;
using Vortex.Revisions.Revision20260701.Serializers.Room.Engine.Data;

namespace Vortex.Revisions.Revision20260701.Serializers.Room.Engine;

internal class WiredMovementsMessageComposerSerializer(int header)
    : AbstractSerializer<WiredMovementsMessageComposer>(header)
{
    protected override void Serialize(IServerPacket packet, WiredMovementsMessageComposer message)
    {
        int updatesCount =
            message.Users.Count
            + message.FloorItems.Count
            + message.WallItems.Count
            + message.UserDirections.Count;

        packet.WriteInteger(updatesCount);

        foreach (WiredUserMovementSnapshot user in message.Users)
        {
            packet.WriteInteger((int)WiredMovementType.User);

            WiredMovementSerializer.SerializeUserMovement(packet, user);
        }

        foreach (WiredFloorItemMovementSnapshot floorItem in message.FloorItems)
        {
            packet.WriteInteger((int)WiredMovementType.FloorItem);

            WiredMovementSerializer.SerializeFloorItemMovement(packet, floorItem);
        }

        foreach (WiredWallItemMovementSnapshot wallItem in message.WallItems)
        {
            packet.WriteInteger((int)WiredMovementType.WallItem);

            WiredMovementSerializer.SerializeWallItemMovement(packet, wallItem);
        }

        foreach (WiredUserDirectionSnapshot userDirection in message.UserDirections)
        {
            packet.WriteInteger((int)WiredMovementType.UserDirection);

            WiredMovementSerializer.SerializeUserDirection(packet, userDirection);
        }
    }
}
