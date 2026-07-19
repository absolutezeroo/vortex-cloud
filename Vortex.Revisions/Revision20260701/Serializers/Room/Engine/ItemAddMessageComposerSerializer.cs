using Vortex.Primitives.Messages.Outgoing.Room.Engine;
using Vortex.Primitives.Packets;
using Vortex.Revisions.Revision20260701.Serializers.Room.Engine.Data;

namespace Vortex.Revisions.Revision20260701.Serializers.Room.Engine;

internal class ItemAddMessageComposerSerializer(int header)
    : AbstractSerializer<ItemAddMessageComposer>(header)
{
    protected override void Serialize(IServerPacket packet, ItemAddMessageComposer message)
    {
        WallItemSerializer.Serialize(packet, message.WallItem);

        packet.WriteString(message.WallItem.OwnerName);
    }
}
