using Vortex.Primitives.Messages.Outgoing.Room.Session;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Serializers.Room.Session;

internal class RoomQueueStatusMessageComposerSerializer(int header)
    : AbstractSerializer<RoomQueueStatusMessageComposer>(header)
{
    protected override void Serialize(IServerPacket packet, RoomQueueStatusMessageComposer message)
    {
        packet.WriteInteger(message.FlatId).WriteInteger(message.QueueSets.Length);

        foreach (RoomQueueSet set in message.QueueSets)
        {
            packet.WriteString(set.Name).WriteInteger(set.Target).WriteInteger(set.Queues.Length);

            foreach (RoomQueueEntry queue in set.Queues)
            {
                packet.WriteString(queue.Name).WriteInteger(queue.Count);
            }
        }
    }
}
