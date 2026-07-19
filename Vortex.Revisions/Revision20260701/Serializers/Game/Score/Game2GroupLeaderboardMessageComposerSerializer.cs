using Vortex.Primitives.Messages.Outgoing.Game.Score;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Serializers.Game.Score;

internal class Game2GroupLeaderboardMessageComposerSerializer(int header)
    : AbstractSerializer<Game2GroupLeaderboardMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        Game2GroupLeaderboardMessageComposer message
    )
    {
        //
    }
}
