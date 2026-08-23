using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Outgoing.Game.Score;

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
