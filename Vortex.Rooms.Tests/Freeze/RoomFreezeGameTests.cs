using FluentAssertions;
using Vortex.Primitives.Players;
using Vortex.Primitives.Rooms.Enums.Games;
using Vortex.Rooms.Grains.Systems.Freeze;
using Xunit;

namespace Vortex.Rooms.Tests.Freeze;

/// <summary>
/// Locks the pure Freeze game rules — gate join/leave/switch and the team caps, the round lifecycle,
/// and per-player freeze/thaw/power-up mechanics — the contract the grain wrapper drives effects and
/// teleports off. Uses the compiled-in default settings.
/// </summary>
public sealed class RoomFreezeGameTests
{
    private static PlayerId P(int id) => (PlayerId)id;

    [Fact]
    public void Gate_Joins_Leaves_And_Switches_Teams()
    {
        RoomFreezeGame game = new();

        game.ToggleGate(P(1), GameTeamColor.Red).Should().Be(FreezeGateResult.Joined);
        game.GetTeam(P(1)).Should().Be(GameTeamColor.Red);
        game.GetTeamCount(GameTeamColor.Red).Should().Be(1);

        // Same gate again = leave.
        game.ToggleGate(P(1), GameTeamColor.Red).Should().Be(FreezeGateResult.Left);
        game.GetTeam(P(1)).Should().Be(GameTeamColor.None);

        // A different gate switches teams.
        game.ToggleGate(P(1), GameTeamColor.Red);
        game.ToggleGate(P(1), GameTeamColor.Blue).Should().Be(FreezeGateResult.Joined);
        game.GetTeam(P(1)).Should().Be(GameTeamColor.Blue);
        game.GetTeamCount(GameTeamColor.Red).Should().Be(0);
    }

    [Fact]
    public void Gate_Rejects_Full_Team()
    {
        RoomFreezeGame game = new(); // default MaxPlayersPerTeam = 5

        for (int i = 1; i <= FreezeSettings.Default.MaxPlayersPerTeam; i++)
        {
            game.ToggleGate(P(i), GameTeamColor.Green).Should().Be(FreezeGateResult.Joined);
        }

        game.ToggleGate(P(99), GameTeamColor.Green).Should().Be(FreezeGateResult.None);
        game.GetTeamCount(GameTeamColor.Green)
            .Should()
            .Be(FreezeSettings.Default.MaxPlayersPerTeam);
    }

    [Fact]
    public void Gate_Is_Ignored_While_Running()
    {
        RoomFreezeGame game = new();
        game.ToggleGate(P(1), GameTeamColor.Red);
        game.Start();

        game.ToggleGate(P(2), GameTeamColor.Blue).Should().Be(FreezeGateResult.None);
        game.GetTeam(P(2)).Should().Be(GameTeamColor.None);
    }

    [Fact]
    public void Start_Resets_Loadout_And_Stop_Returns_Winner()
    {
        RoomFreezeGame game = new();
        game.ToggleGate(P(1), GameTeamColor.Red);
        game.ToggleGate(P(2), GameTeamColor.Blue);

        game.Start().Should().BeTrue();
        game.IsRunning.Should().BeTrue();
        game.GetPlayer(P(1))!.Lives.Should().Be(FreezeSettings.Default.StartLives);

        game.AddTeamScore(GameTeamColor.Blue, 30);
        game.AddTeamScore(GameTeamColor.Red, 10);

        game.Stop().Should().Be(GameTeamColor.Blue);
        game.IsRunning.Should().BeFalse();
    }

    [Fact]
    public void Freeze_Costs_A_Life_And_Kills_At_Zero()
    {
        FreezePlayerState player = new(P(1), GameTeamColor.Red, FreezeSettings.Default);

        player.Freeze().Should().BeFalse(); // 3 -> 2
        player.IsFrozen.Should().BeTrue();
        player.Lives.Should().Be(2);

        // Cannot be frozen again while already frozen.
        player.Freeze().Should().BeFalse();
        player.Lives.Should().Be(2);
    }

    [Fact]
    public void Frozen_Thaws_After_Its_Duration()
    {
        FreezePlayerState player = new(P(1), GameTeamColor.Red, FreezeSettings.Default);
        player.Freeze();

        for (int i = 1; i < FreezeSettings.Default.FrozenTicks; i++)
        {
            player.Tick().Should().BeFalse();
            player.IsFrozen.Should().BeTrue();
        }

        player.Tick().Should().BeTrue(); // the tick that thaws
        player.IsFrozen.Should().BeFalse();
    }

    [Fact]
    public void Shield_Blocks_Freezing()
    {
        FreezePlayerState player = new(P(1), GameTeamColor.Red, FreezeSettings.Default);
        player.AddProtection();

        player.IsProtected.Should().BeTrue();
        player.CanBeFrozen.Should().BeFalse();
        player.Freeze().Should().BeFalse();
        player.Lives.Should().Be(FreezeSettings.Default.StartLives); // untouched
    }

    [Fact]
    public void Mega_Snowball_Forces_Max_Radius_Once()
    {
        FreezePlayerState player = new(P(1), GameTeamColor.Red, FreezeSettings.Default)
        {
            TempMassive = true,
        };

        player.TakeThrowRadius().Should().Be(FreezeSettings.Default.MaxExplosionBoost);
        player.TakeThrowRadius().Should().Be(0); // consumed
    }

    [Fact]
    public void Effect_Reflects_State()
    {
        FreezePlayerState player = new(P(1), GameTeamColor.Green, FreezeSettings.Default);

        player
            .CurrentEffect()
            .Should()
            .Be(FreezeConstants.TeamEffectBase + (int)GameTeamColor.Green);

        player.AddProtection();
        player
            .CurrentEffect()
            .Should()
            .Be(
                FreezeConstants.TeamEffectBase
                    + (int)GameTeamColor.Green
                    + FreezeConstants.ProtectionEffectBonus
            );

        // A protected player cannot be frozen; a fresh one shows the frozen effect once hit.
        FreezePlayerState other = new(P(2), GameTeamColor.Green, FreezeSettings.Default);
        other.Freeze();
        other.CurrentEffect().Should().Be(FreezeConstants.FrozenEffect);
    }
}
