using Vortex.Primitives.Messages.Outgoing.Room.Layout;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Serializers.Room.Layout;

internal class RoomOccupiedTilesMessageComposerSerializer(int header)
    : AbstractSerializer<RoomOccupiedTilesMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        RoomOccupiedTilesMessageComposer message
    )
    {
        // Ints, not the bytes the height map uses for the same coordinates: this parser calls
        // readInteger twice per tile, and the room is capped at 64 tiles a side either way.
        packet.WriteInteger(message.Tiles.Length);

        foreach ((int X, int Y) tile in message.Tiles)
        {
            packet.WriteInteger(tile.X).WriteInteger(tile.Y);
        }
    }
}
