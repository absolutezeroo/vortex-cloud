using Vortex.Primitives.Messages.Outgoing.Room.Engine;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Serializers.Room.Engine;

internal class ObjectRemoveMultipleMessageComposerSerializer(int header)
    : AbstractSerializer<ObjectRemoveMultipleMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        ObjectRemoveMultipleMessageComposer message
    )
    {
        packet.WriteInteger(message.ObjectIdsToRemove.Length);

        foreach (long objectId in message.ObjectIdsToRemove)
        {
            packet.WriteInteger((int)objectId);
        }

        packet.WriteInteger(message.PickerId);
    }
}
