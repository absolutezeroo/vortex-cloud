using Vortex.Primitives.Messages.Outgoing.Room.Engine;
using Vortex.Primitives.Packets;
using Vortex.Revisions.Revision20260701.Serializers.Room.Engine.Data;

namespace Vortex.Revisions.Revision20260701.Serializers.Room.Engine;

internal class ItemUpdateMessageComposerSerializer(int header)
    : AbstractSerializer<ItemUpdateMessageComposer>(header)
{
    protected override void Serialize(IServerPacket packet, ItemUpdateMessageComposer message)
    {
        WallItemSerializer.Serialize(packet, message.WallItem);
    }
}
