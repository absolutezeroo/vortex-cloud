using FluentAssertions;
using Vortex.Rooms.Wired;
using Xunit;

namespace Vortex.Rooms.Tests.Wired;

/// <summary>
/// Locks the wired SCORE_ACHIEVED fire condition (ScoreAchieved.ts): fires once as a team crosses up
/// onto the threshold, honours the any-team (0) vs specific-team (1-4) selector, and never re-fires
/// while already above.
/// </summary>
public sealed class WiredScoreAchievedMatcherTests
{
    // configTeam 0 = any; team ids: Red=1, Green=2, Blue=3, Yellow=4.

    [Fact]
    public void Fires_OnTheCrossing_UpOntoThreshold()
    {
        // Threshold 10, any team: 8 → 11 crosses 10.
        WiredScoreAchievedMatcher.Matches(0, 10, 1, 11, 8).Should().BeTrue();
        // Exactly reaching the threshold counts.
        WiredScoreAchievedMatcher.Matches(0, 10, 1, 10, 9).Should().BeTrue();
    }

    [Fact]
    public void DoesNotFire_BeforeReaching()
    {
        WiredScoreAchievedMatcher.Matches(0, 10, 1, 9, 5).Should().BeFalse();
    }

    [Fact]
    public void DoesNotReFire_WhenAlreadyAbove()
    {
        // Was already at/above 10, gains more — must not fire again.
        WiredScoreAchievedMatcher.Matches(0, 10, 1, 15, 10).Should().BeFalse();
        WiredScoreAchievedMatcher.Matches(0, 10, 1, 20, 11).Should().BeFalse();
    }

    [Fact]
    public void SpecificTeam_OnlyMatchesThatTeam()
    {
        // Config = Blue (3), threshold 5.
        WiredScoreAchievedMatcher.Matches(3, 5, 3, 5, 4).Should().BeTrue(); // Blue crosses
        WiredScoreAchievedMatcher.Matches(3, 5, 1, 5, 4).Should().BeFalse(); // Red crosses — ignored
    }

    [Fact]
    public void AnyTeam_MatchesAnyTeamCrossing()
    {
        WiredScoreAchievedMatcher.Matches(0, 5, 2, 6, 4).Should().BeTrue(); // Green
        WiredScoreAchievedMatcher.Matches(0, 5, 4, 6, 4).Should().BeTrue(); // Yellow
    }
}
