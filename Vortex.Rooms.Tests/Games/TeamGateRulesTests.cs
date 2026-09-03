using FluentAssertions;
using Vortex.Primitives.Players;
using Vortex.Primitives.Rooms.Enums.Games;
using Vortex.Rooms.Games.Teams;
using Xunit;

namespace Vortex.Rooms.Tests.Games;

/// <summary>
/// The one copy of the team-gate rules, shared by Banzai, Freeze and football. Every one of these
/// used to exist three times, and the copies had already drifted: the capacity check has to run
/// BEFORE the player leaves their current team or a rejected switch strips the membership they had.
/// </summary>
public sealed class TeamGateRulesTests
{
    private static readonly PlayerId Alice = new(1);
    private static readonly PlayerId Bob = new(2);

    [Fact]
    public void AGateTouch_JoinsTheSharedTeam()
    {
        GameTeamBook teams = new();

        TeamGateRules
            .Toggle(teams, TeamLayout.FourColours, Alice, GameTeamColor.Red, true)
            .Should().Be(TeamGateResult.Joined);
        teams.GetTeam(Alice).Should().Be(GameTeamColor.Red);
    }

    [Fact]
    public void YourOwnGate_LeavesTheTeam()
    {
        GameTeamBook teams = new();
        TeamGateRules.Toggle(teams, TeamLayout.FourColours, Alice, GameTeamColor.Red, true);

        TeamGateRules
            .Toggle(teams, TeamLayout.FourColours, Alice, GameTeamColor.Red, true)
            .Should().Be(TeamGateResult.Left);
        teams.GetTeam(Alice).Should().Be(GameTeamColor.None);
    }

    [Fact]
    public void AnotherGate_SwitchesTeams()
    {
        GameTeamBook teams = new();
        TeamGateRules.Toggle(teams, TeamLayout.FourColours, Alice, GameTeamColor.Red, true);

        TeamGateRules
            .Toggle(teams, TeamLayout.FourColours, Alice, GameTeamColor.Blue, true)
            .Should().Be(TeamGateResult.Joined);
        teams.GetTeam(Alice).Should().Be(GameTeamColor.Blue);
        teams.GetTeamMemberCount(GameTeamColor.Red).Should().Be(0);
    }

    [Fact]
    public void AFullTeam_RejectsTheJoin_WithoutStrippingMembership()
    {
        TeamLayout oneEach = TeamLayout.FourColours with { Capacity = 1 };
        GameTeamBook teams = new();
        TeamGateRules.Toggle(teams, oneEach, Alice, GameTeamColor.Red, true);
        TeamGateRules.Toggle(teams, oneEach, Bob, GameTeamColor.Blue, true);

        TeamGateRules
            .Toggle(teams, oneEach, Bob, GameTeamColor.Red, true).Should().Be(TeamGateResult.None);
        teams.GetTeam(Bob).Should().Be(GameTeamColor.Blue, "a rejected switch changes nothing");
    }

    [Fact]
    public void ZeroCapacity_MeansUnlimited()
    {
        TeamLayout unlimited = TeamLayout.FourColours with { Capacity = 0 };
        GameTeamBook teams = new();

        for (int i = 1; i <= 30; i++)
        {
            TeamGateRules
                .Toggle(teams, unlimited, new PlayerId(i), GameTeamColor.Green, true)
                .Should().Be(TeamGateResult.Joined);
        }

        teams.GetTeamMemberCount(GameTeamColor.Green).Should().Be(30);
    }

    [Fact]
    public void GatesAreDead_WhileAMatchRuns()
    {
        GameTeamBook teams = new();

        TeamGateRules
            .Toggle(
                teams,
                TeamLayout.FourColours,
                Alice,
                GameTeamColor.Red,
                acceptingPlayers: false
            ).Should().Be(TeamGateResult.None);
        teams.GetTeam(Alice).Should().Be(GameTeamColor.None);
    }

    [Fact]
    public void AColourTheLayoutDoesNotUse_IsRefused()
    {
        TeamLayout twoTeams = TeamLayout.FourColours with
        {
            Colours = [GameTeamColor.Red, GameTeamColor.Blue],
        };
        GameTeamBook teams = new();

        TeamGateRules
            .Toggle(teams, twoTeams, Alice, GameTeamColor.Yellow, true)
            .Should().Be(TeamGateResult.None);
    }
}
