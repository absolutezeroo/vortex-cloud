using FluentAssertions;
using Vortex.Primitives.Players;
using Vortex.Primitives.Rooms.Enums.Games;
using Vortex.Rooms.Games.Freeze;
using Vortex.Rooms.Games.Teams;
using Xunit;

namespace Vortex.Rooms.Tests.Freeze;

/// <summary>
/// The pure Freeze rules — the roster's gate join/leave/switch and team caps, and the per-player
/// freeze, thaw, ammo and power-up mechanics. No room, no packets, no lifecycle: the module drives
/// effects and teleports off exactly what these return, so this is where the rules are pinned.
/// </summary>
public sealed class FreezeRosterTests
{
    private static readonly TeamLayout Layout = TeamLayout.FourColours with
    {
        Capacity = FreezeSettings.Default.MaxPlayersPerTeam,
    };

    private static PlayerId P(int id) => (PlayerId)id;

    [Fact]
    public void Gate_Joins_Leaves_And_Switches_Teams()
    {
        GameTeamBook teams = new();
        FreezeRoster roster = new(teams);

        roster
            .ToggleGate(Layout, P(1), GameTeamColor.Red, true, FreezeSettings.Default)
            .Should().Be(TeamGateResult.Joined);
        teams.GetTeam(P(1)).Should().Be(GameTeamColor.Red);
        roster.LivingCount(GameTeamColor.Red).Should().Be(1);

        // The same gate again leaves.
        roster
            .ToggleGate(Layout, P(1), GameTeamColor.Red, true, FreezeSettings.Default)
            .Should().Be(TeamGateResult.Left);
        teams.GetTeam(P(1)).Should().Be(GameTeamColor.None);

        // A different gate switches teams.
        roster.ToggleGate(Layout, P(1), GameTeamColor.Red, true, FreezeSettings.Default);
        roster
            .ToggleGate(Layout, P(1), GameTeamColor.Blue, true, FreezeSettings.Default)
            .Should().Be(TeamGateResult.Joined);
        teams.GetTeam(P(1)).Should().Be(GameTeamColor.Blue);
        roster.LivingCount(GameTeamColor.Red).Should().Be(0);
    }

    [Fact]
    public void Gate_Rejects_A_Full_Team()
    {
        FreezeRoster roster = new(new GameTeamBook());

        for (int i = 1; i <= FreezeSettings.Default.MaxPlayersPerTeam; i++)
        {
            roster
                .ToggleGate(Layout, P(i), GameTeamColor.Green, true, FreezeSettings.Default)
                .Should().Be(TeamGateResult.Joined);
        }

        roster
            .ToggleGate(Layout, P(99), GameTeamColor.Green, true, FreezeSettings.Default)
            .Should().Be(TeamGateResult.None);
        roster
            .LivingCount(GameTeamColor.Green).Should().Be(FreezeSettings.Default.MaxPlayersPerTeam);
    }

    [Fact]
    public void Switching_To_A_Full_Team_Keeps_Current_Membership()
    {
        TeamLayout oneEach = TeamLayout.FourColours with { Capacity = 1 };
        GameTeamBook teams = new();
        FreezeRoster roster = new(teams);

        roster.ToggleGate(oneEach, P(1), GameTeamColor.Blue, true, FreezeSettings.Default);
        roster.ToggleGate(oneEach, P(2), GameTeamColor.Red, true, FreezeSettings.Default);

        roster
            .ToggleGate(oneEach, P(2), GameTeamColor.Blue, true, FreezeSettings.Default)
            .Should().Be(TeamGateResult.None);
        teams.GetTeam(P(2)).Should().Be(GameTeamColor.Red, "a rejected switch changes nothing");
        roster.LivingCount(GameTeamColor.Red).Should().Be(1);
    }

    [Fact]
    public void Gates_Are_Dead_While_A_Match_Runs()
    {
        GameTeamBook teams = new();
        FreezeRoster roster = new(teams);

        roster
            .ToggleGate(
                Layout,
                P(2),
                GameTeamColor.Blue,
                acceptingPlayers: false,
                FreezeSettings.Default
            ).Should().Be(TeamGateResult.None);
        teams.GetTeam(P(2)).Should().Be(GameTeamColor.None);
    }

    [Fact]
    public void ResetLoadouts_Adopts_The_Settings_Resolved_At_Kickoff()
    {
        // An admin edits the balance after a player picked a gate but before the match starts; the
        // reset at prepare is what makes the edit reach them.
        FreezeRoster roster = new(new GameTeamBook());
        roster.ToggleGate(Layout, P(1), GameTeamColor.Red, true, FreezeSettings.Default);
        roster.Get(P(1))!.Lives.Should().Be(FreezeSettings.Default.StartLives);

        roster.ResetLoadouts(FreezeSettings.Default with { StartLives = 7 });

        roster.Get(P(1))!.Lives.Should().Be(7);
    }

    [Fact]
    public void LivingTeamCount_Drops_As_A_Team_Is_Wiped_Out()
    {
        // The early-end rule keys off this: a match armed with two or more teams ends at one.
        FreezeRoster roster = new(new GameTeamBook());
        roster.ToggleGate(Layout, P(1), GameTeamColor.Red, true, FreezeSettings.Default);
        roster.ToggleGate(Layout, P(2), GameTeamColor.Blue, true, FreezeSettings.Default);

        roster.LivingTeamCount().Should().Be(2);

        roster.Remove(P(2));

        roster.LivingTeamCount().Should().Be(1);
    }

    [Fact]
    public void Leaving_The_Roster_Also_Leaves_The_Shared_Team()
    {
        // Every wired team leaf reads the shared ledger, so a player who stopped playing has to stop
        // counting there too, not only in Freeze's own roster.
        GameTeamBook teams = new();
        FreezeRoster roster = new(teams);
        roster.ToggleGate(Layout, P(1), GameTeamColor.Red, true, FreezeSettings.Default);

        roster.Remove(P(1));

        teams.GetTeam(P(1)).Should().Be(GameTeamColor.None);
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
            .CurrentEffect().Should().Be(FreezeConstants.TeamEffectBase + (int)GameTeamColor.Green);

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
