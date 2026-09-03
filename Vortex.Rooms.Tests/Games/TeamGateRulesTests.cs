using FluentAssertions;
using Vortex.Primitives.Players;
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
    // The four Habbo colours as this game's teams: ordinals 1-4, exactly as HabboTeamPalette.Standard
    // maps them. Named here so the tests read like the domain and not like a colour enum.
    private static readonly TeamSet Teams = TeamSet.HabboColours;
    private static readonly TeamId Red = new(1);
    private static readonly TeamId Green = new(2);
    private static readonly TeamId Blue = new(3);
    private static readonly TeamId Yellow = new(4);

    private static readonly PlayerId Alice = new(1);
    private static readonly PlayerId Bob = new(2);

    [Fact]
    public void AGateTouch_JoinsTheSharedTeam()
    {
        TeamBook teams = new(Teams);

        TeamGateRules.Toggle(teams, Teams, Alice, Red, true).Should().Be(TeamGateResult.Joined);
        teams.GetTeam(Alice).Should().Be(Red);
    }

    [Fact]
    public void YourOwnGate_LeavesTheTeam()
    {
        TeamBook teams = new(Teams);
        TeamGateRules.Toggle(teams, Teams, Alice, Red, true);

        TeamGateRules.Toggle(teams, Teams, Alice, Red, true).Should().Be(TeamGateResult.Left);
        teams.GetTeam(Alice).Should().Be(TeamId.None);
    }

    [Fact]
    public void AnotherGate_SwitchesTeams()
    {
        TeamBook teams = new(Teams);
        TeamGateRules.Toggle(teams, Teams, Alice, Red, true);

        TeamGateRules.Toggle(teams, Teams, Alice, Blue, true).Should().Be(TeamGateResult.Joined);
        teams.GetTeam(Alice).Should().Be(Blue);
        teams.GetTeamMemberCount(Red).Should().Be(0);
    }

    [Fact]
    public void AFullTeam_RejectsTheJoin_WithoutStrippingMembership()
    {
        TeamSet oneEach = Teams.WithCapacity(1);
        TeamBook teams = new(Teams);
        TeamGateRules.Toggle(teams, oneEach, Alice, Red, true);
        TeamGateRules.Toggle(teams, oneEach, Bob, Blue, true);

        TeamGateRules.Toggle(teams, oneEach, Bob, Red, true).Should().Be(TeamGateResult.None);
        teams.GetTeam(Bob).Should().Be(Blue, "a rejected switch changes nothing");
    }

    [Fact]
    public void ZeroCapacity_MeansUnlimited()
    {
        TeamSet unlimited = Teams.WithCapacity(0);
        TeamBook teams = new(Teams);

        for (int i = 1; i <= 30; i++)
        {
            TeamGateRules
                .Toggle(teams, unlimited, new PlayerId(i), Green, true)
                .Should()
                .Be(TeamGateResult.Joined);
        }

        teams.GetTeamMemberCount(Green).Should().Be(30);
    }

    [Fact]
    public void GatesAreDead_WhileAMatchRuns()
    {
        TeamBook teams = new(Teams);

        TeamGateRules
            .Toggle(teams, Teams, Alice, Red, acceptingPlayers: false)
            .Should()
            .Be(TeamGateResult.None);
        teams.GetTeam(Alice).Should().Be(TeamId.None);
    }

    [Fact]
    public void ATeamTheGameDoesNotPlay_IsRefused()
    {
        // A two-team game in a room whose furniture offers four gates: the two it does not play are
        // inert, and that is the SET's answer rather than a colour check.
        TeamSet twoTeams = TeamSet.Of("red", "blue");
        TeamBook teams = new(twoTeams);
        TeamId third = new(3);
        TeamId second = new(2);

        TeamGateRules.Toggle(teams, twoTeams, Alice, third, true).Should().Be(TeamGateResult.None);
        TeamGateRules
            .Toggle(teams, twoTeams, Alice, second, true)
            .Should()
            .Be(TeamGateResult.Joined);
    }
}
