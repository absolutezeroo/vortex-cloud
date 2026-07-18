using FluentAssertions;
using Turbo.Primitives.Players;
using Turbo.Primitives.Rooms.Enums.Games;
using Turbo.Primitives.Rooms.Object;
using Turbo.Rooms.Grains.Systems;
using Xunit;

namespace Turbo.Rooms.Tests.Games;

/// <summary>Locks the pure wired-game team/score state the room grain relies on: single-team
/// membership, team switching, derived member counts, balanced-team selection, score accumulation with
/// a floor at zero, the per-game score caps, and leak-proof cleanup. These are the invariants that keep
/// teams from going stale and scores from desyncing.</summary>
public sealed class GameTeamStateTests
{
    private static readonly PlayerId One = new(101);
    private static readonly PlayerId Two = new(202);
    private static readonly PlayerId Three = new(303);
    private static readonly RoomObjectId Box = new(9001);

    [Fact]
    public void JoinTeam_AssignsTeam_AndSignalsChange()
    {
        GameTeamState state = new();

        state.JoinTeam(One, GameTeamColor.Red).Should().BeTrue();
        state.GetTeam(One).Should().Be(GameTeamColor.Red);
    }

    [Fact]
    public void JoinTeam_SameTeamAgain_IsNoChange()
    {
        GameTeamState state = new();
        state.JoinTeam(One, GameTeamColor.Red);

        state.JoinTeam(One, GameTeamColor.Red).Should().BeFalse();
        state.GetTeam(One).Should().Be(GameTeamColor.Red);
    }

    [Fact]
    public void JoinTeam_Switching_MovesPlayerAndLeavesNoResidueOnOldTeam()
    {
        GameTeamState state = new();
        state.JoinTeam(One, GameTeamColor.Red);

        state.JoinTeam(One, GameTeamColor.Blue).Should().BeTrue();

        state.GetTeam(One).Should().Be(GameTeamColor.Blue);
        state.GetTeamMemberCount(GameTeamColor.Red).Should().Be(0);
        state.GetTeamMemberCount(GameTeamColor.Blue).Should().Be(1);
    }

    [Fact]
    public void JoinTeam_RejectsNoneAndOutOfRange()
    {
        GameTeamState state = new();

        state.JoinTeam(One, GameTeamColor.None).Should().BeFalse();
        state.JoinTeam(One, (GameTeamColor)99).Should().BeFalse();
        state.GetTeam(One).Should().Be(GameTeamColor.None);
    }

    [Fact]
    public void LeaveTeam_ClearsMembership_AndReportsWhetherOnATeam()
    {
        GameTeamState state = new();
        state.JoinTeam(One, GameTeamColor.Green);

        state.LeaveTeam(One).Should().BeTrue();
        state.GetTeam(One).Should().Be(GameTeamColor.None);
        state.LeaveTeam(One).Should().BeFalse();
    }

    [Fact]
    public void GetPlayersInTeam_ReturnsOnlyThatTeamsMembers()
    {
        GameTeamState state = new();
        state.JoinTeam(One, GameTeamColor.Red);
        state.JoinTeam(Two, GameTeamColor.Red);
        state.JoinTeam(Three, GameTeamColor.Blue);

        state.GetPlayersInTeam(GameTeamColor.Red).Should().BeEquivalentTo(new[] { One, Two });
        state.GetPlayersInTeam(GameTeamColor.Blue).Should().BeEquivalentTo(new[] { Three });
        state.GetPlayersInTeam(GameTeamColor.Yellow).Should().BeEmpty();
    }

    [Fact]
    public void GetSmallestTeam_PicksFewest_TieBreaksToLowestColour()
    {
        GameTeamState state = new();

        // All empty -> lowest colour.
        state.GetSmallestTeam().Should().Be(GameTeamColor.Red);

        state.JoinTeam(One, GameTeamColor.Red);
        // Red now has 1; Green/Blue/Yellow have 0 -> lowest empty colour is Green.
        state.GetSmallestTeam().Should().Be(GameTeamColor.Green);
    }

    [Fact]
    public void GiveScoreToPlayerTeam_AddsToTheirTeam_AndSkipsTeamless()
    {
        GameTeamState state = new();
        state.JoinTeam(One, GameTeamColor.Red);

        state.TryGiveScoreToPlayerTeam(Box, One, 5, 0).Should().BeTrue();
        state.TryGiveScoreToPlayerTeam(Box, One, 3, 0).Should().BeTrue();
        state.GetTeamScore(GameTeamColor.Red).Should().Be(8);

        // Two is on no team -> no score awarded.
        state.TryGiveScoreToPlayerTeam(Box, Two, 5, 0).Should().BeFalse();
    }

    [Fact]
    public void TeamScore_FloorsAtZero()
    {
        GameTeamState state = new();
        state.JoinTeam(One, GameTeamColor.Red);
        state.TryGiveScoreToPlayerTeam(Box, One, 4, 0);

        state.TryGiveScoreToPlayerTeam(Box, One, -10, 0);

        state.GetTeamScore(GameTeamColor.Red).Should().Be(0);
    }

    [Fact]
    public void GiveScore_PerBoxPerPlayerCap_IsEnforced_AndZeroMeansUnlimited()
    {
        GameTeamState state = new();
        state.JoinTeam(One, GameTeamColor.Red);

        // cap of 2 for this box+player.
        state.TryGiveScoreToPlayerTeam(Box, One, 1, 2).Should().BeTrue();
        state.TryGiveScoreToPlayerTeam(Box, One, 1, 2).Should().BeTrue();
        state.TryGiveScoreToPlayerTeam(Box, One, 1, 2).Should().BeFalse();
        state.GetTeamScore(GameTeamColor.Red).Should().Be(2);
    }

    [Fact]
    public void GiveScoreToTeam_PerBoxCap_IsEnforced()
    {
        GameTeamState state = new();

        state.TryGiveScoreToTeam(Box, GameTeamColor.Blue, 10, 1).Should().BeTrue();
        state.TryGiveScoreToTeam(Box, GameTeamColor.Blue, 10, 1).Should().BeFalse();
        state.GetTeamScore(GameTeamColor.Blue).Should().Be(10);
    }

    [Fact]
    public void OnPlayerLeft_ClearsMembership_AndResetsTheirScoreCap()
    {
        GameTeamState state = new();
        state.JoinTeam(One, GameTeamColor.Red);
        state.TryGiveScoreToPlayerTeam(Box, One, 1, 1); // exhausts the cap for (Box, One)

        state.OnPlayerLeft(One);

        state.GetTeam(One).Should().Be(GameTeamColor.None);

        // Rejoining and scoring works again — the cap counter for that player was cleared.
        state.JoinTeam(One, GameTeamColor.Red);
        state.TryGiveScoreToPlayerTeam(Box, One, 1, 1).Should().BeTrue();
    }

    [Fact]
    public void Reset_WipesEverything_AndReturnsFormerMembers()
    {
        GameTeamState state = new();
        state.JoinTeam(One, GameTeamColor.Red);
        state.JoinTeam(Two, GameTeamColor.Blue);
        state.TryGiveScoreToPlayerTeam(Box, One, 7, 0);

        state.Reset().Should().BeEquivalentTo(new[] { One, Two });

        state.GetTeam(One).Should().Be(GameTeamColor.None);
        state.GetTeam(Two).Should().Be(GameTeamColor.None);
        state.GetTeamScore(GameTeamColor.Red).Should().Be(0);
        state.GetTeamMemberCount(GameTeamColor.Blue).Should().Be(0);
    }
}
