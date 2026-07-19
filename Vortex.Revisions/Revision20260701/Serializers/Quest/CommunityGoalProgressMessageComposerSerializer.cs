using Vortex.Primitives.Messages.Outgoing.Quest;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Serializers.Quest;

internal class CommunityGoalProgressMessageComposerSerializer(int header)
    : AbstractSerializer<CommunityGoalProgressMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        CommunityGoalProgressMessageComposer message
    )
    {
        //
    }
}
