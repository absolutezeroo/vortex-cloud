using Vortex.Primitives.Messages.Outgoing.Room.Engine;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Serializers.Room.Engine;

internal class ItemRemoveMessageComposerSerializer(int header)
    : AbstractSerializer<ItemRemoveMessageComposer>(header)
{
    protected override void Serialize(IServerPacket packet, ItemRemoveMessageComposer message)
    {
        packet.WriteString(message.ObjectId.ToString()).WriteInteger(message.PickerId);
    }
}
