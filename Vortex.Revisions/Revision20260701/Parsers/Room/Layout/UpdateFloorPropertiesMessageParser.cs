using Vortex.Primitives.Messages.Incoming.Room.Layout;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Parsers.Room.Layout;

internal class UpdateFloorPropertiesMessageParser : IParser
{
    /// <summary>What the client sends for "not set", and what it means here too.</summary>
    private const int Unset = -1;

    public IMessageEvent Parse(IClientPacket packet)
    {
        string model = packet.PopString();

        // Read what is there rather than what a full save would carry: the same composer sends one
        // field, six or seven depending on which arguments were left at -1, so anything fixed-width
        // throws on two of its three forms.
        return new UpdateFloorPropertiesMessage
        {
            Model = model,
            DoorX = PopOrUnset(packet),
            DoorY = PopOrUnset(packet),
            DoorRotation = PopOrUnset(packet),
            WallThickness = PopOrUnset(packet),
            FloorThickness = PopOrUnset(packet),
            WallHeight = PopOrUnset(packet),
        };
    }

    private static int PopOrUnset(IClientPacket packet) =>
        packet.Remaining >= sizeof(int) ? packet.PopInt() : Unset;
}
