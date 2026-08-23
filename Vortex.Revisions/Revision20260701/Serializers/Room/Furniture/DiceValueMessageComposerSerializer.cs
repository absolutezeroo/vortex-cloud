using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Outgoing.Room.Furniture;

namespace Vortex.Revisions.Revision20260701.Serializers.Room.Furniture;

internal class DiceValueMessageComposerSerializer(int header)
    : AbstractSerializer<DiceValueMessageComposer>(header)
{
    protected override void Serialize(IServerPacket packet, DiceValueMessageComposer message)
    {
        packet.WriteInteger(message.FurniId).WriteInteger(message.Value);
    }
}
