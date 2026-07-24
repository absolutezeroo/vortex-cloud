using FluentAssertions;
using Vortex.Primitives.Players;
using Vortex.Primitives.Rooms.Enums.Games;
using Vortex.Rooms.Grains.Systems.Freeze;
using Xunit;

namespace Vortex.Rooms.Tests.Freeze;

/// <summary>
/// Locks how each of the six ice-block power-ups changes a player's loadout — the rules the grain applies
/// on walk-on.
/// </summary>
public sealed class FreezePowerUpTests
{
    private static FreezePlayerState Player() =>
        new((PlayerId)1, GameTeamColor.Red, FreezeSettings.Default);

    [Fact]
    public void ExtraSnowball_Adds_Ammo()
    {
        FreezePlayerState player = Player();
        int before = player.Snowballs;

        FreezePowerUps.Apply(FreezePowerUp.ExtraSnowball, player);

        player.Snowballs.Should().Be(before + 1);
    }

    [Fact]
    public void LongerRange_Raises_Blast_Radius()
    {
        FreezePlayerState player = Player();

        FreezePowerUps.Apply(FreezePowerUp.LongerRange, player);

        player.ExplosionBoost.Should().Be(1);
    }

    [Fact]
    public void ExtraLife_Grants_A_Life_Up_To_The_Cap()
    {
        FreezePlayerState player = Player();
        player.Freeze(); // drop below the starting lives so a heart can top it up
        int afterHit = player.Lives;

        FreezePowerUps.Apply(FreezePowerUp.ExtraLife, player);

        player.Lives.Should().Be(afterHit + 1);
    }

    [Fact]
    public void Shield_Grants_Protection()
    {
        FreezePlayerState player = Player();

        FreezePowerUps.Apply(FreezePowerUp.Shield, player);

        player.IsProtected.Should().BeTrue();
        player.CanBeFrozen.Should().BeFalse();
    }

    [Fact]
    public void XBlast_Arms_The_Next_Diagonal_Throw()
    {
        FreezePlayerState player = Player();

        FreezePowerUps.Apply(FreezePowerUp.XBlast, player);

        player.NextDiagonal.Should().BeTrue();
    }

    [Fact]
    public void Mega_Forces_The_Next_Throw_To_Max_Radius()
    {
        FreezePlayerState player = Player();

        FreezePowerUps.Apply(FreezePowerUp.Mega, player);

        player.TempMassive.Should().BeTrue();
        player.TakeThrowRadius().Should().Be(FreezeSettings.Default.MaxExplosionBoost);
    }

    [Fact]
    public void Pick_Cycles_Through_All_Six_By_Roll()
    {
        FreezePowerUps.Pick(0).Should().Be(FreezePowerUp.ExtraSnowball);
        FreezePowerUps.Pick(5).Should().Be(FreezePowerUp.Mega);
        FreezePowerUps.Pick(6).Should().Be(FreezePowerUp.ExtraSnowball); // wraps
        FreezePowerUps.Pickable.Should().HaveCount(6);
    }

    [Theory]
    [InlineData(FreezePowerUp.XBlast, 2)]
    [InlineData(FreezePowerUp.ExtraSnowball, 3)]
    [InlineData(FreezePowerUp.LongerRange, 4)]
    [InlineData(FreezePowerUp.Mega, 5)]
    [InlineData(FreezePowerUp.ExtraLife, 6)]
    [InlineData(FreezePowerUp.Shield, 7)]
    public void RevealState_Matches_The_es_box_Icon_Frames(FreezePowerUp powerUp, int state)
    {
        // The es_box furni asset shows a specific icon per state (2..7); keep the binding pinned.
        FreezePowerUps.RevealState(powerUp).Should().Be(state);
        FreezePowerUps.FromRevealState(state).Should().Be(powerUp);
    }

    [Fact]
    public void FromRevealState_Is_None_For_NonReveal_States()
    {
        // 0 intact, 1 empty, 12..17 collected — none carry an uncollectable power-up.
        FreezePowerUps.FromRevealState(0).Should().Be(FreezePowerUp.None);
        FreezePowerUps.FromRevealState(1).Should().Be(FreezePowerUp.None);
        FreezePowerUps.FromRevealState(13).Should().Be(FreezePowerUp.None);
    }
}
