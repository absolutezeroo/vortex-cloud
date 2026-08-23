using Vortex.Primitives.Packets;
using Vortex.Primitives.Rooms.Object;
using Vortex.Protocol.Messages.Outgoing.Room.Engine;

namespace Vortex.Revisions.Revision20260701.Serializers.Room.Engine;

internal class ItemsStateUpdateMessageComposerSerializer(int header)
    : AbstractSerializer<ItemsStateUpdateMessageComposer>(header)
{
    protected override void Serialize(IServerPacket packet, ItemsStateUpdateMessageComposer message)
    {
        packet.WriteInteger(message.ObjectStates.Count);

        foreach ((RoomObjectId objectId, string state) in message.ObjectStates)
        {
            packet.WriteInteger(objectId).WriteString(state);
        }
    }
}
