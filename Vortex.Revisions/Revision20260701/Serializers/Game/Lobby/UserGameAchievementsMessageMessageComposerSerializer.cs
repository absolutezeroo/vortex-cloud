using Vortex.Primitives.Messages.Outgoing.Game.Lobby;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Serializers.Game.Lobby;

internal class UserGameAchievementsMessageMessageComposerSerializer(int header)
    : AbstractSerializer<UserGameAchievementsMessageMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        UserGameAchievementsMessageMessageComposer message
    )
    {
        //
    }
}
