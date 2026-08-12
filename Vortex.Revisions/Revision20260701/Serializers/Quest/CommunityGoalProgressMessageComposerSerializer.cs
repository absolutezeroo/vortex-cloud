using Vortex.Primitives.Messages.Outgoing.Quest;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Serializers.Quest;

/// <summary>
/// The ten fields of the client's community-goal DTO, in its constructor's read order: the expired
/// flag first, then the personal and community numbers, the code, the countdown, and finally the
/// per-level reward limits as a counted int array.
/// </summary>
internal class CommunityGoalProgressMessageComposerSerializer(int header)
    : AbstractSerializer<CommunityGoalProgressMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        CommunityGoalProgressMessageComposer message
    )
    {
        packet
            .WriteBoolean(message.HasGoalExpired)
            .WriteInteger(message.PersonalContributionScore)
            .WriteInteger(message.PersonalContributionRank)
            .WriteInteger(message.CommunityTotalScore)
            .WriteInteger(message.CommunityHighestAchievedLevel)
            .WriteInteger(message.ScoreRemainingUntilNextLevel)
            .WriteInteger(message.PercentCompletionTowardsNextLevel)
            .WriteString(message.GoalCode)
            .WriteInteger(message.TimeRemainingInSeconds)
            .WriteInteger(message.RewardUserLimits.Length);

        foreach (int limit in message.RewardUserLimits)
        {
            packet.WriteInteger(limit);
        }
    }
}
