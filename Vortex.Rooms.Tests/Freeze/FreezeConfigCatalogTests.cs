using System.Collections.Generic;
using System.Globalization;
using FluentAssertions;
using Vortex.Primitives.Server;
using Vortex.Rooms.Grains.Systems.Freeze;
using Xunit;

namespace Vortex.Rooms.Tests.Freeze;

/// <summary>
/// Guards the deliberate duplication between the feature-side <see cref="FreezeConfig"/> keys /
/// <see cref="FreezeSettings"/> defaults and the dashboard-facing <see cref="ConfigKeyCatalog"/>: every
/// Freeze key must be a known, editable config key, and its catalog default must equal the compiled
/// default the game falls back to. Without this the two can silently drift.
/// </summary>
public sealed class FreezeConfigCatalogTests
{
    private static string I(int v) => v.ToString(CultureInfo.InvariantCulture);

    public static IEnumerable<object[]> ExpectedDefaults()
    {
        FreezeSettings d = FreezeSettings.Default;

        yield return [FreezeConfig.StartLivesKey, I(d.StartLives)];
        yield return [FreezeConfig.MaxLivesKey, I(d.MaxLives)];
        yield return [FreezeConfig.StartSnowballsKey, I(d.StartSnowballs)];
        yield return [FreezeConfig.MaxSnowballsKey, I(d.MaxSnowballs)];
        yield return [FreezeConfig.MaxBoostKey, I(d.MaxExplosionBoost)];
        yield return [FreezeConfig.FrozenTicksKey, I(d.FrozenTicks)];
        yield return [FreezeConfig.ProtectionTicksKey, I(d.ProtectionTicks)];
        yield return [FreezeConfig.LoseSnowballsKey, I(d.FreezeLoseSnowballs)];
        yield return [FreezeConfig.LoseBoostKey, I(d.FreezeLoseBoost)];
        yield return [FreezeConfig.PowerUpChanceKey, I(d.PowerUpChancePercent)];
        yield return [FreezeConfig.ProtectionStacksKey, d.ProtectionStacks ? "true" : "false"];
        yield return [FreezeConfig.FreezePointsKey, I(d.FreezePlayerPoints)];
        yield return [FreezeConfig.BlockPointsKey, I(d.DestroyBlockPoints)];
        yield return [FreezeConfig.PowerUpPointsKey, I(d.PowerUpPoints)];
        yield return [FreezeConfig.MaxPlayersPerTeamKey, I(d.MaxPlayersPerTeam)];
    }

    [Theory]
    [MemberData(nameof(ExpectedDefaults))]
    public void Every_Freeze_Key_Is_In_The_Catalog_With_The_Matching_Default(
        string key,
        string expectedDefault
    )
    {
        ConfigKeyDescriptor? descriptor = ConfigKeyCatalog.Find(key);

        descriptor.Should().NotBeNull($"'{key}' must be a dashboard-editable config key");
        descriptor!.DefaultValue.Should().Be(expectedDefault);
        descriptor.Group.Should().Be("Freeze");
    }
}
