using Vortex.Primitives.Messages.Outgoing.Room.Engine;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Serializers.Room.Engine;

internal class ItemStateUpdateMessageComposerSerializer(int header)
    : AbstractSerializer<ItemStateUpdateMessageComposer>(header)
{
    protected override void Serialize(IServerPacket packet, ItemStateUpdateMessageComposer message)
    {
        packet.WriteInteger((int)message.ObjectId).WriteString(message.State);
    }
}
