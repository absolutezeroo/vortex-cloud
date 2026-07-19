using Vortex.Primitives.Messages.Outgoing.Notifications;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Serializers.Notifications;

internal class ActivityPointsMessageComposerSerializer(int header)
    : AbstractSerializer<ActivityPointsMessageComposer>(header)
{
    protected override void Serialize(IServerPacket packet, ActivityPointsMessageComposer message)
    {
        packet.WriteInteger(message.PointsByCategoryId.Count);

        foreach ((int categoryId, int points) in message.PointsByCategoryId)
        {
            packet.WriteInteger(categoryId).WriteInteger(points);
        }
    }
}
