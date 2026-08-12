using FluentAssertions;
using Vortex.Players.Quests;
using Vortex.Primitives.Quests;
using Xunit;

namespace Vortex.Players.Tests.Quests;

/// <summary>
///     The four states of the landing-view "players online" goal. The widget polls every 5 seconds,
///     so a wrong state is not a one-off glitch — it flickers in front of the player.
/// </summary>
public sealed class ConcurrentUsersGoalRuleTests
{
    [Fact]
    public void Resolve_StaysRewarded_WhenTheHotelDipsBackBelowTheGoal()
    {
        // The regression that matters: without rewarded outranking the live count, a player who
        // claimed would be offered the button again every time the hotel crossed the target.
        ConcurrentUsersGoalRule
            .Resolve(enabled: true, userCountGoal: 100, onlineCount: 12, alreadyRewarded: true)
            .Should()
            .Be(ConcurrentUsersGoalState.Rewarded);
    }

    [Fact]
    public void Resolve_IsDisabled_WhenNoTargetIsConfigured()
    {
        // A zero target would otherwise read as "already reached" and hand a reward to anyone who
        // logs in.
        ConcurrentUsersGoalRule
            .Resolve(enabled: true, userCountGoal: 0, onlineCount: 0, alreadyRewarded: false)
            .Should()
            .Be(ConcurrentUsersGoalState.Disabled);
    }

    [Fact]
    public void Resolve_IsDisabled_WhenTheGoalIsOff()
    {
        ConcurrentUsersGoalRule
            .Resolve(enabled: false, userCountGoal: 100, onlineCount: 200, alreadyRewarded: false)
            .Should()
            .Be(ConcurrentUsersGoalState.Disabled);
    }

    [Theory]
    [InlineData(99, ConcurrentUsersGoalState.Active)]
    [InlineData(100, ConcurrentUsersGoalState.Redeem)] // exactly on target counts as reached
    [InlineData(101, ConcurrentUsersGoalState.Redeem)]
    public void Resolve_FlipsToRedeemOnceTheTargetIsReached(
        int onlineCount,
        ConcurrentUsersGoalState expected
    )
    {
        ConcurrentUsersGoalRule
            .Resolve(enabled: true, userCountGoal: 100, onlineCount, alreadyRewarded: false)
            .Should()
            .Be(expected);
    }

    [Fact]
    public void CanClaim_IsFalse_WhenTheHotelEmptiedBetweenTheRefreshAndTheClick()
    {
        // The widget refreshes on a 5s timer; the button can be pressed against a stale view.
        ConcurrentUsersGoalRule
            .CanClaim(enabled: true, userCountGoal: 100, onlineCount: 40, alreadyRewarded: false)
            .Should()
            .BeFalse();
    }

    [Fact]
    public void CanClaim_IsFalse_OnASecondClick()
    {
        ConcurrentUsersGoalRule
            .CanClaim(enabled: true, userCountGoal: 100, onlineCount: 150, alreadyRewarded: true)
            .Should()
            .BeFalse();
    }

    [Fact]
    public void CanClaim_IsTrue_WhenTheGoalIsMetAndUnclaimed()
    {
        ConcurrentUsersGoalRule
            .CanClaim(enabled: true, userCountGoal: 100, onlineCount: 150, alreadyRewarded: false)
            .Should()
            .BeTrue();
    }
}
