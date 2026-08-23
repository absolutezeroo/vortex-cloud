using Vortex.Protocol.Messages.Outgoing.Room.Engine;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Serializers.Room.Engine;

internal class BuildersClubPlacementWarningMessageComposerSerializer(int header)
    : AbstractSerializer<BuildersClubPlacementWarningMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        BuildersClubPlacementWarningMessageComposer message
    )
    {
        packet
            .WriteInteger(message.TypeCode)
            .WriteInteger(message.PageId)
            .WriteInteger(message.OfferId)
            .WriteString(message.ExtraParam);

        // The tail depends on the type code and the two branches are different lengths, so the
        // code written above decides how many bytes the client reads next.
        if (message.TypeCode == FloorTypeCode)
        {
            packet.WriteInteger(message.X).WriteInteger(message.Y).WriteInteger(message.Direction);
        }
        else
        {
            packet.WriteString(message.WallLocation);
        }
    }

    /// <summary>The one type code that selects the floor tail; anything else is a wall.</summary>
    private const int FloorTypeCode = 0;
}
