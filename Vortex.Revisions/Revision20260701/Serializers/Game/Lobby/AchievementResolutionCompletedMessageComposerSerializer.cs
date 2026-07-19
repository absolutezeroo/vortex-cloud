using Vortex.Primitives.Messages.Outgoing.Game.Lobby;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Serializers.Game.Lobby;

internal class AchievementResolutionCompletedMessageComposerSerializer(int header)
    : AbstractSerializer<AchievementResolutionCompletedMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        AchievementResolutionCompletedMessageComposer message
    )
    {
        //
    }
}
