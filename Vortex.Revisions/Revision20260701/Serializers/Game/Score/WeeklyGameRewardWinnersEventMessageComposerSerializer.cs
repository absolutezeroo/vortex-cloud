using Vortex.Primitives.Messages.Outgoing.Game.Score;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Serializers.Game.Score;

internal class WeeklyGameRewardWinnersEventMessageComposerSerializer(int header)
    : AbstractSerializer<WeeklyGameRewardWinnersEventMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        WeeklyGameRewardWinnersEventMessageComposer message
    )
    {
        //
    }
}
