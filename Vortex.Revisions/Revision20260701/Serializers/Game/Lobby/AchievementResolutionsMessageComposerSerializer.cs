using Vortex.Primitives.Messages.Outgoing.Game.Lobby;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Serializers.Game.Lobby;

internal class AchievementResolutionsMessageComposerSerializer(int header)
    : AbstractSerializer<AchievementResolutionsMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        AchievementResolutionsMessageComposer message
    )
    {
        //
    }
}
