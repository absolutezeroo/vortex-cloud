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
    public void Switching_To_A_Full_Team_Keeps_Current_Membership()
    {
        RoomFreezeGame game = new()
        {
            Settings = FreezeSettings.Default with { MaxPlayersPerTeam = 1 },
        };
        game.ToggleGate(P(1), GameTeamColor.Blue); // Blue is now full (1/1)
        game.ToggleGate(P(2), GameTeamColor.Red);

        // P2 tries to switch to full Blue -> rejected, and must stay on Red (not stripped to no team).
        game.ToggleGate(P(2), GameTeamColor.Blue).Should().Be(FreezeGateResult.None);
        game.GetTeam(P(2)).Should().Be(GameTeamColor.Red);
        game.GetTeamCount(GameTeamColor.Red).Should().Be(1);
    }

    [Fact]
    public void Start_Adopts_Live_Settings_For_Already_Joined_Players()
    {
        RoomFreezeGame game = new(); // Settings = Default (StartLives 3)
        game.ToggleGate(P(1), GameTeamColor.Red); // player joins under the current settings
        game.GetPlayer(P(1))!.Lives.Should().Be(FreezeSettings.Default.StartLives);

        // Admin edits the config after the player joined but before the round starts.
        game.Settings = FreezeSettings.Default with
        {
            StartLives = 7,
        };
        game.Start();

        game.GetPlayer(P(1))!.Lives.Should().Be(7);
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
    public void LivingTeamCount_Drops_As_A_Team_Is_Wiped_Out()
    {
        // The early-end rule keys off this: a round armed with two+ teams ends when it falls to one.
        RoomFreezeGame game = new();
        game.ToggleGate(P(1), GameTeamColor.Red);
        game.ToggleGate(P(2), GameTeamColor.Blue);
        game.Start();

        game.LivingTeamCount().Should().Be(2);

        // Blue's only player is eliminated (removed from the game).
        game.Remove(P(2));

        game.LivingTeamCount().Should().Be(1);
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
    public void Snowballs_Regenerate_Over_Ticks_Up_To_Max()
    {
        FreezeSettings settings = FreezeSettings.Default with
        {
            StartSnowballs = 0,
            MaxSnowballs = 2,
            SnowballRegenTicks = 2,
        };
        FreezePlayerState player = new(P(1), GameTeamColor.Red, settings);

        player.Snowballs.Should().Be(0);
        player.Tick(); // 1st tick — not yet
        player.Snowballs.Should().Be(0);
        player.Tick(); // 2nd — +1
        player.Snowballs.Should().Be(1);
        player.Tick();
        player.Tick(); // +1 — now at max
        player.Snowballs.Should().Be(2);
        player.Tick();
        player.Tick(); // stays capped
        player.Snowballs.Should().Be(2);
    }

    [Fact]
    public void Frozen_Player_Does_Not_Regenerate_Ammo()
    {
        FreezeSettings settings = FreezeSettings.Default with
        {
            StartSnowballs = 0,
            MaxSnowballs = 5,
            SnowballRegenTicks = 1,
            FrozenTicks = 3,
        };
        FreezePlayerState player = new(P(1), GameTeamColor.Red, settings);
        player.Freeze();

        player.Tick(); // frozen — no regen
        player.IsFrozen.Should().BeTrue();
        player.Snowballs.Should().Be(0);
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
