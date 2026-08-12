using Vortex.Primitives.Messages.Outgoing.Quest;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Serializers.Quest;

/// <summary>State, current count, target — the three ints the client's parser reads in that order.</summary>
internal class ConcurrentUsersGoalProgressMessageComposerSerializer(int header)
    : AbstractSerializer<ConcurrentUsersGoalProgressMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        ConcurrentUsersGoalProgressMessageComposer message
    )
    {
        packet.WriteInteger((int)message.State);
        packet.WriteInteger(message.UserCount);
        packet.WriteInteger(message.UserCountGoal);
    }
}
