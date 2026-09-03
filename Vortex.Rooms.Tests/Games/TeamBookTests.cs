using FluentAssertions;
using Vortex.Primitives.Players;
using Vortex.Primitives.Rooms.Object;
using Vortex.Rooms.Games.Teams;
using Xunit;

namespace Vortex.Rooms.Tests.Games;

/// <summary>Locks the pure team/score ledger the room grain relies on: single-team membership, team
/// switching, derived member counts, balanced-team selection, score accumulation with a floor at
/// zero, the per-game score caps, and leak-proof cleanup. These are the invariants that keep teams
/// from going stale and scores from desyncing.
/// <para>
/// Nothing here is a colour. The book is keyed by the game's own <see cref="TeamId"/>s over its own
/// <see cref="TeamSet"/>, which is what lets a game have two teams, or seven, or teams the Habbo
/// furniture cannot show — see <c>GenericTeamModelTests</c> for those.
/// </para></summary>
public sealed class TeamBookTests
{
    // The four Habbo colours as this game's teams: ordinals 1-4, exactly as HabboTeamPalette.Standard
    // maps them. Named here so the tests read like the domain and not like a colour enum.
    private static readonly TeamSet Teams = TeamSet.HabboColours;
    private static readonly TeamId Red = new(1);
    private static readonly TeamId Green = new(2);
    private static readonly TeamId Blue = new(3);
    private static readonly TeamId Yellow = new(4);

    private static readonly PlayerId One = new(101);
    private static readonly PlayerId Two = new(202);
    private static readonly PlayerId Three = new(303);
    private static readonly RoomObjectId Box = new(9001);

    [Fact]
    public void JoinTeam_AssignsTeam_AndSignalsChange()
    {
        TeamBook state = new(Teams);

        state.JoinTeam(One, Red).Should().BeTrue();
        state.GetTeam(One).Should().Be(Red);
    }

    [Fact]
    public void JoinTeam_SameTeamAgain_IsNoChange()
    {
        TeamBook state = new(Teams);
        state.JoinTeam(One, Red);

        state.JoinTeam(One, Red).Should().BeFalse();
        state.GetTeam(One).Should().Be(Red);
    }

    [Fact]
    public void JoinTeam_Switching_MovesPlayerAndLeavesNoResidueOnOldTeam()
    {
        TeamBook state = new(Teams);
        state.JoinTeam(One, Red);

        state.JoinTeam(One, Blue).Should().BeTrue();

        state.GetTeam(One).Should().Be(Blue);
        state.GetTeamMemberCount(Red).Should().Be(0);
        state.GetTeamMemberCount(Blue).Should().Be(1);
    }

    [Fact]
    public void JoinTeam_RejectsNoneAndOutOfRange()
    {
        TeamBook state = new(Teams);

        state.JoinTeam(One, TeamId.None).Should().BeFalse();
        state.JoinTeam(One, new TeamId(99)).Should().BeFalse();
        state.GetTeam(One).Should().Be(TeamId.None);
    }

    [Fact]
    public void LeaveTeam_ClearsMembership_AndReportsWhetherOnATeam()
    {
        TeamBook state = new(Teams);
        state.JoinTeam(One, Green);

        state.LeaveTeam(One).Should().BeTrue();
        state.GetTeam(One).Should().Be(TeamId.None);
        state.LeaveTeam(One).Should().BeFalse();
    }

    [Fact]
    public void GetPlayersInTeam_ReturnsOnlyThatTeamsMembers()
    {
        TeamBook state = new(Teams);
        state.JoinTeam(One, Red);
        state.JoinTeam(Two, Red);
        state.JoinTeam(Three, Blue);

        state.GetPlayersInTeam(Red).Should().BeEquivalentTo(new[] { One, Two });
        state.GetPlayersInTeam(Blue).Should().BeEquivalentTo(new[] { Three });
        state.GetPlayersInTeam(Yellow).Should().BeEmpty();
    }

    [Fact]
    public void GetSmallestTeam_PicksFewest_TieBreaksToTheSetsOwnOrder()
    {
        TeamBook state = new(Teams);

        // All empty -> lowest colour.
        state.GetSmallestTeam().Should().Be(Red);

        state.JoinTeam(One, Red);
        // Red now has 1; Green/Blue/Yellow have 0 -> lowest empty colour is Green.
        state.GetSmallestTeam().Should().Be(Green);
    }

    [Fact]
    public void GiveScoreToPlayerTeam_AddsToTheirTeam_AndSkipsTeamless()
    {
        TeamBook state = new(Teams);
        state.JoinTeam(One, Red);

        state.TryGiveScoreToPlayerTeam(Box, One, 5, 0).Should().BeTrue();
        state.TryGiveScoreToPlayerTeam(Box, One, 3, 0).Should().BeTrue();
        state.GetTeamScore(Red).Should().Be(8);

        // Two is on no team -> no score awarded.
        state.TryGiveScoreToPlayerTeam(Box, Two, 5, 0).Should().BeFalse();
    }

    [Fact]
    public void TeamScore_FloorsAtZero()
    {
        TeamBook state = new(Teams);
        state.JoinTeam(One, Red);
        state.TryGiveScoreToPlayerTeam(Box, One, 4, 0);

        state.TryGiveScoreToPlayerTeam(Box, One, -10, 0);

        state.GetTeamScore(Red).Should().Be(0);
    }

    [Fact]
    public void GiveScore_PerBoxPerPlayerCap_IsEnforced_AndZeroMeansUnlimited()
    {
        TeamBook state = new(Teams);
        state.JoinTeam(One, Red);

        // cap of 2 for this box+player.
        state.TryGiveScoreToPlayerTeam(Box, One, 1, 2).Should().BeTrue();
        state.TryGiveScoreToPlayerTeam(Box, One, 1, 2).Should().BeTrue();
        state.TryGiveScoreToPlayerTeam(Box, One, 1, 2).Should().BeFalse();
        state.GetTeamScore(Red).Should().Be(2);
    }

    [Fact]
    public void GiveScoreToTeam_PerBoxCap_IsEnforced()
    {
        TeamBook state = new(Teams);

        state.TryGiveScoreToTeam(Box, Blue, 10, 1).Should().BeTrue();
        state.TryGiveScoreToTeam(Box, Blue, 10, 1).Should().BeFalse();
        state.GetTeamScore(Blue).Should().Be(10);
    }

    [Fact]
    public void OnPlayerLeft_ClearsMembership_AndResetsTheirScoreCap()
    {
        TeamBook state = new(Teams);
        state.JoinTeam(One, Red);
        state.TryGiveScoreToPlayerTeam(Box, One, 1, 1); // exhausts the cap for (Box, One)

        state.OnPlayerLeft(One);

        state.GetTeam(One).Should().Be(TeamId.None);

        // Rejoining and scoring works again — the cap counter for that player was cleared.
        state.JoinTeam(One, Red);
        state.TryGiveScoreToPlayerTeam(Box, One, 1, 1).Should().BeTrue();
    }

    [Fact]
    public void Reset_WipesEverything_AndReturnsFormerMembers()
    {
        TeamBook state = new(Teams);
        state.JoinTeam(One, Red);
        state.JoinTeam(Two, Blue);
        state.TryGiveScoreToPlayerTeam(Box, One, 7, 0);

        state.Reset().Should().BeEquivalentTo(new[] { One, Two });

        state.GetTeam(One).Should().Be(TeamId.None);
        state.GetTeam(Two).Should().Be(TeamId.None);
        state.GetTeamScore(Red).Should().Be(0);
        state.GetTeamMemberCount(Blue).Should().Be(0);
    }

    [Fact]
    public void ResetScores_ClearsScoresAndCaps_ButKeepsMembership()
    {
        TeamBook state = new(Teams);
        state.JoinTeam(One, Red);
        state.TryGiveScoreToPlayerTeam(Box, One, 7, 1); // exhausts the (Box, One) cap

        state.ResetScores();

        // Freeze starts a round with this: the gates already picked the teams.
        state.GetTeam(One).Should().Be(Red);
        state.GetTeamScore(Red).Should().Be(0);

        // The per-game caps are part of "a fresh round", so they reset with the scores.
        state.TryGiveScoreToPlayerTeam(Box, One, 3, 1).Should().BeTrue();
        state.GetTeamScore(Red).Should().Be(3);
    }

    [Fact]
    public void AddScore_IsUncapped_FloorsAtZero_AndIgnoresNoTeam()
    {
        TeamBook state = new(Teams);

        state.AddScore(Blue, 5);
        state.AddScore(Blue, 3);
        state.GetTeamScore(Blue).Should().Be(8);

        // A Freeze friendly-fire penalty can exceed the score; it must not go negative.
        state.AddScore(Blue, -50);
        state.GetTeamScore(Blue).Should().Be(0);

        // A teamless award is dropped rather than aliasing onto index 0.
        state.AddScore(TeamId.None, 10);
        state.GetTeamScore(TeamId.None).Should().Be(0);
    }

    [Fact]
    public void GetLeadingTeam_PicksTheHighest_AndIsNoneWhenNobodyScored()
    {
        TeamBook state = new(Teams);

        state.GetLeadingTeam().Should().Be(TeamId.None);

        state.AddScore(Green, 4);
        state.AddScore(Yellow, 9);
        state.GetLeadingTeam().Should().Be(Yellow);

        // A tie resolves to the lowest colour, the same order the rank condition ranks by.
        state.AddScore(Green, 5);
        state.GetLeadingTeam().Should().Be(Green);
    }
}
