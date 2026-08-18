using FluentAssertions;
using Vortex.Primitives.Players;
using Vortex.Primitives.Rooms.Enums.Games;
using Vortex.Rooms.Grains.Systems;
using Vortex.Rooms.Grains.Systems.Banzai;
using Xunit;

namespace Vortex.Rooms.Tests.Banzai;

/// <summary>
/// The Banzai roster rules over the SHARED team store — Banzai keeps no player state of its own, so
/// the gate toggle IS the membership write every wired team leaf reads. Gates only work while idle
/// (teams are picked before kick-off), your own gate leaves, a full team rejects without stripping
/// the player's current membership, and the winner comes from the shared scores.
/// </summary>
public sealed class RoomBanzaiGameTests
{
    private static readonly PlayerId Alice = new(1);
    private static readonly PlayerId Bob = new(2);

    [Fact]
    public void AGateTouch_JoinsTheSharedTeam()
    {
        RoomBanzaiGame game = new();

        game.ToggleGate(Alice, GameTeamColor.Red).Should().Be(BanzaiGateResult.Joined);
        game.Teams.GetTeam(Alice).Should().Be(GameTeamColor.Red);
    }

    [Fact]
    public void YourOwnGate_LeavesTheTeam()
    {
        RoomBanzaiGame game = new();
        game.ToggleGate(Alice, GameTeamColor.Red);

        game.ToggleGate(Alice, GameTeamColor.Red).Should().Be(BanzaiGateResult.Left);
        game.Teams.GetTeam(Alice).Should().Be(GameTeamColor.None);
    }

    [Fact]
    public void AnotherGate_SwitchesTeams()
    {
        RoomBanzaiGame game = new();
        game.ToggleGate(Alice, GameTeamColor.Red);

        game.ToggleGate(Alice, GameTeamColor.Blue).Should().Be(BanzaiGateResult.Joined);
        game.Teams.GetTeam(Alice).Should().Be(GameTeamColor.Blue);
        game.Teams.GetTeamMemberCount(GameTeamColor.Red).Should().Be(0);
    }

    [Fact]
    public void AFullTeam_RejectsTheJoin_WithoutStrippingMembership()
    {
        RoomBanzaiGame game = new() { Settings = new BanzaiSettings { MaxPlayersPerTeam = 1 } };
        game.ToggleGate(Alice, GameTeamColor.Red);
        game.ToggleGate(Bob, GameTeamColor.Blue);

        game.ToggleGate(Bob, GameTeamColor.Red).Should().Be(BanzaiGateResult.None);
        game.Teams.GetTeam(Bob)
            .Should()
            .Be(GameTeamColor.Blue, "a rejected switch changes nothing");
    }

    [Fact]
    public void GatesAreDead_WhileTheRoundRuns()
    {
        RoomBanzaiGame game = new();
        game.Start();

        game.ToggleGate(Alice, GameTeamColor.Red).Should().Be(BanzaiGateResult.None);
        game.Teams.GetTeam(Alice).Should().Be(GameTeamColor.None);
    }

    [Fact]
    public void StartAndStop_GateThePhase_AndStopNamesTheLeader()
    {
        RoomBanzaiGame game = new();

        game.Start().Should().BeTrue();
        game.Start().Should().BeFalse("already running");
        game.IsRunning.Should().BeTrue();

        game.Teams.JoinTeam(Alice, GameTeamColor.Green);
        game.Teams.AddScore(GameTeamColor.Green, 7);

        game.Stop().Should().Be(GameTeamColor.Green);
        game.IsRunning.Should().BeFalse();
    }

    [Fact]
    public void AWiredJoinedPlayer_IsAPlayerLikeAnyOther()
    {
        // Membership lives ONLY in the shared store, so a wired join-team box and a gate produce
        // indistinguishable players — the board asks what team the walker is on, nothing else.
        RoomBanzaiGame game = new();
        game.Teams.JoinTeam(Alice, GameTeamColor.Yellow);
        game.Start();
        game.Board.Activate([5], 10);

        game.Board.Mark(game.Teams.GetTeam(Alice), 5).Kind.Should().Be(BanzaiMarkKind.Hijack);
    }
}
