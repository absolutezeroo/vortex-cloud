using Vortex.Primitives.Messages.Outgoing.Room.Engine;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Serializers.Room.Engine;

internal class RoomVisualizationSettingsMessageComposerSerializer(int header)
    : AbstractSerializer<RoomVisualizationSettingsMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        RoomVisualizationSettingsMessageComposer message
    )
    {
        packet
            .WriteBoolean(message.WallsHidden)
            .WriteInteger((int)message.WallThickness)
            .WriteInteger((int)message.FloorThickness);
    }
}
