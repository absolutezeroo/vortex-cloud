using Vortex.Primitives.Messages.Outgoing.Quest;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Serializers.Quest;

internal class ConcurrentUsersGoalProgressMessageComposerSerializer(int header)
    : AbstractSerializer<ConcurrentUsersGoalProgressMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        ConcurrentUsersGoalProgressMessageComposer message
    )
    {
        //
    }
}
