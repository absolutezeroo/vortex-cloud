using Vortex.Primitives.Packets;
using Vortex.Primitives.Players.Snapshots;
using Vortex.Protocol.Messages.Outgoing.Game.Lobby;

namespace Vortex.Revisions.Revision20260701.Serializers.Game.Lobby;

internal class AchievementResolutionsMessageComposerSerializer(int header)
    : AbstractSerializer<AchievementResolutionsMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        AchievementResolutionsMessageComposer message
    )
    {
        packet.WriteInteger(message.StuffId).WriteInteger(message.Achievements.Length);

        foreach (AchievementResolutionSnapshot achievement in message.Achievements)
        {
            packet
                .WriteInteger(achievement.AchievementId)
                .WriteInteger(achievement.Level)
                .WriteString(achievement.BadgeId)
                .WriteInteger(achievement.RequiredLevel)
                .WriteInteger((int)achievement.State);
        }

        // After the list, not before it: the client reads the whole vector and only then the
        // countdown.
        packet.WriteInteger(message.SecondsLeft);
    }
}
