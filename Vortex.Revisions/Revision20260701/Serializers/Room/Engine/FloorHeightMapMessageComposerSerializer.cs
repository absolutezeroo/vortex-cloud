using Vortex.Primitives.Messages.Outgoing.Room.Engine;
using Vortex.Primitives.Orleans.Snapshots.Room.Furniture;
using Vortex.Primitives.Packets;
using Vortex.Primitives.Rooms.Enums;
using Vortex.Revisions.Revision20260701.Serializers.Room.Engine.Data;

namespace Vortex.Revisions.Revision20260701.Serializers.Room.Engine;

internal class FloorHeightMapMessageComposerSerializer(int header)
    : AbstractSerializer<FloorHeightMapMessageComposer>(header)
{
    protected override void Serialize(IServerPacket packet, FloorHeightMapMessageComposer message)
    {
        packet
            .WriteBoolean(message.ScaleType is RoomScaleType.Small)
            .WriteInteger(message.FixedWallsHeight)
            .WriteString(message.ModelData)
            .WriteInteger(message.AreaHideData.Count);

        foreach (AreaHideDataSnapshot area in message.AreaHideData)
        {
            AreaHideDataSerializer.Serialize(packet, area);
        }

        packet
            .WriteInteger(message.CameraInitX)
            .WriteInteger(message.CameraInitY)
            .WriteFloat(message.CameraInitZ);
    }
}
