using Vortex.Primitives.Messages.Outgoing.Room.Action;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Serializers.Room.Action;

internal class CarryObjectMessageComposerSerializer(int header)
    : AbstractSerializer<CarryObjectMessageComposer>(header)
{
    protected override void Serialize(IServerPacket packet, CarryObjectMessageComposer message)
    {
        packet.WriteInteger(message.UserId).WriteInteger(message.ItemType);
    }
}
