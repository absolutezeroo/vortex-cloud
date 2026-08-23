using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Outgoing.Game.Lobby;

namespace Vortex.Revisions.Revision20260701.Serializers.Game.Lobby;

internal class AchievementResolutionProgressMessageComposerSerializer(int header)
    : AbstractSerializer<AchievementResolutionProgressMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        AchievementResolutionProgressMessageComposer message
    )
    {
        packet
            .WriteInteger(message.StuffId)
            .WriteInteger(message.AchievementId)
            .WriteString(message.RequiredLevelBadgeCode)
            .WriteInteger(message.UserProgress)
            .WriteInteger(message.TotalProgress)
            .WriteInteger(message.SecondsLeft);
    }
}
