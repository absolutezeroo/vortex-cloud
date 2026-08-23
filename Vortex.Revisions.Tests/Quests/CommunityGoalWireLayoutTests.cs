using System;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Packets;
using Vortex.Primitives.Quests;
using Vortex.Protocol.Messages.Outgoing.Quest;
using Vortex.Revisions.Configuration;
using Xunit;
using Rev = Vortex.Revisions.Revision20260701.Revision20260701;

namespace Vortex.Revisions.Tests.Quests;

/// <summary>
///     The two landing-view goal composers, pinned against the client's own DTO constructors:
///     <c>unknowns/_SafePkg_1976/_SafeCls_4497.as</c> for the community goal and
///     <c>_SafeCls_4165.as</c> for the concurrent-users goal.
///
///     Both were registered in the map and serialized nothing at all — the widget got a header and
///     an empty body, which reads on the client as a goal that exists and has no progress.
/// </summary>
public sealed class CommunityGoalWireLayoutTests
{
    private static readonly Rev Revision = new(Options.Create(new ProtocolLimitsConfig()));

    [Fact]
    public void CommunityGoalProgress_WritesTheTenFieldsInTheClientsReadOrder()
    {
        ClientPacket packet = Serialize(
            new CommunityGoalProgressMessageComposer
            {
                HasGoalExpired = false,
                PersonalContributionScore = 42,
                PersonalContributionRank = 7,
                CommunityTotalScore = 1234,
                CommunityHighestAchievedLevel = 2,
                ScoreRemainingUntilNextLevel = 766,
                PercentCompletionTowardsNextLevel = 61,
                GoalCode = "summer_build",
                TimeRemainingInSeconds = 3600,
                RewardUserLimits = [50, 20, 5],
            }
        );

        // The expired flag leads: the client reads a boolean first, so writing the code or a number
        // here would shift every remaining field.
        packet.PopBoolean().Should().BeFalse();
        packet.PopInt().Should().Be(42);
        packet.PopInt().Should().Be(7);
        packet.PopInt().Should().Be(1234);
        packet.PopInt().Should().Be(2);
        packet.PopInt().Should().Be(766);
        packet.PopInt().Should().Be(61);
        packet.PopString().Should().Be("summer_build");
        packet.PopInt().Should().Be(3600);

        packet.PopInt().Should().Be(3);
        packet.PopInt().Should().Be(50);
        packet.PopInt().Should().Be(20);
        packet.PopInt().Should().Be(5);
        packet.Remaining.Should().Be(0);
    }

    [Fact]
    public void CommunityGoalProgress_WritesAnEmptyRewardArrayAsAZeroCount()
    {
        ClientPacket packet = Serialize(
            new CommunityGoalProgressMessageComposer
            {
                HasGoalExpired = true,
                PersonalContributionScore = 0,
                PersonalContributionRank = 0,
                CommunityTotalScore = 0,
                CommunityHighestAchievedLevel = 0,
                ScoreRemainingUntilNextLevel = 0,
                PercentCompletionTowardsNextLevel = 0,
                GoalCode = "over",
                TimeRemainingInSeconds = 0,
                RewardUserLimits = [],
            }
        );

        packet.PopBoolean().Should().BeTrue();

        for (int i = 0; i < 6; i++)
        {
            packet.PopInt().Should().Be(0);
        }

        packet.PopString().Should().Be("over");
        packet.PopInt().Should().Be(0);
        packet.PopInt().Should().Be(0); // the count still has to be written
        packet.Remaining.Should().Be(0);
    }

    [Fact]
    public void ConcurrentUsersGoal_WritesStateCountGoal()
    {
        ClientPacket packet = Serialize(
            new ConcurrentUsersGoalProgressMessageComposer
            {
                State = ConcurrentUsersGoalState.Redeem,
                UserCount = 150,
                UserCountGoal = 100,
            }
        );

        packet.PopInt().Should().Be(2); // Redeem, the client's own STATE_REDEEM
        packet.PopInt().Should().Be(150);
        packet.PopInt().Should().Be(100);
        packet.Remaining.Should().Be(0);
    }

    [Fact]
    public void CommunityGoalHallOfFame_WritesTheCodeThenCountedEntries()
    {
        ClientPacket packet = Serialize(
            new CommunityGoalHallOfFameMessageComposer
            {
                GoalCode = "summer_build",
                Entries =
                [
                    new CommunityGoalHallOfFameEntry
                    {
                        UserId = 3,
                        UserName = "Frank",
                        Figure = "hd-180-1",
                        Rank = 1,
                        CurrentScore = 99,
                    },
                ],
            }
        );

        packet.PopString().Should().Be("summer_build");
        packet.PopInt().Should().Be(1);
        packet.PopInt().Should().Be(3);
        packet.PopString().Should().Be("Frank");
        packet.PopString().Should().Be("hd-180-1");
        packet.PopInt().Should().Be(1);
        packet.PopInt().Should().Be(99);
        packet.Remaining.Should().Be(0);
    }

    private static ClientPacket Serialize<T>(T composer)
        where T : IComposer
    {
        byte[] bytes = Revision.Serializers[typeof(T)].Serialize(composer).ToArray();

        byte[] payload = new byte[bytes.Length - 6];
        Array.Copy(bytes, 6, payload, 0, payload.Length);

        return new ClientPacket(0, payload);
    }
}
