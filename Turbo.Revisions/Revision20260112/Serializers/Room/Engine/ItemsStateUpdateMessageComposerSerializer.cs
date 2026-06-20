using Turbo.Primitives.Messages.Outgoing.Room.Engine;
using Turbo.Primitives.Packets;
using Turbo.Primitives.Rooms.Object;

namespace Turbo.Revisions.Revision20260112.Serializers.Room.Engine;

internal class ItemsStateUpdateMessageComposerSerializer(int header)
    : AbstractSerializer<ItemsStateUpdateMessageComposer>(header)
{
    protected override void Serialize(IServerPacket packet, ItemsStateUpdateMessageComposer message)
    {
        packet.WriteInteger(message.ObjectStates.Count);

        foreach ((RoomObjectId objectId, string state) in message.ObjectStates)
            packet.WriteInteger((int)objectId).WriteString(state);
    }
}
