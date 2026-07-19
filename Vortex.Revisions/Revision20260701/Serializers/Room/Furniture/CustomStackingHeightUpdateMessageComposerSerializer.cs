using Vortex.Primitives.Messages.Outgoing.Room.Furniture;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Serializers.Room.Furniture;

internal class CustomStackingHeightUpdateMessageComposerSerializer(int header)
    : AbstractSerializer<CustomStackingHeightUpdateMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        CustomStackingHeightUpdateMessageComposer message
    )
    {
        packet.WriteInteger(message.FurniId).WriteInteger(message.Height);
    }
}
