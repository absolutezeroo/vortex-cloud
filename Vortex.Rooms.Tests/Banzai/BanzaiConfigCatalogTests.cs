using System.Collections.Generic;
using System.Globalization;
using FluentAssertions;
using Vortex.Primitives.Server;
using Vortex.Rooms.Games.BattleBanzai;
using Xunit;

namespace Vortex.Rooms.Tests.Banzai;

/// <summary>
/// Guards the deliberate duplication between the feature-side <see cref="BanzaiConfig"/> keys /
/// <see cref="BanzaiSettings"/> defaults and the dashboard-facing <see cref="ConfigKeyCatalog"/>:
/// every Banzai key must be a known, editable config key, and its catalog default must equal the
/// compiled default the game falls back to. Without this the two can silently drift.
/// </summary>
public sealed class BanzaiConfigCatalogTests
{
    private static string I(int v) => v.ToString(CultureInfo.InvariantCulture);

    public static IEnumerable<object[]> ExpectedDefaults()
    {
        BanzaiSettings d = BanzaiSettings.Default;

        yield return [BanzaiConfig.PointsLockTileKey, I(d.PointsLockTile)];
        yield return [BanzaiConfig.PointsFillTileKey, I(d.PointsFillTile)];
        yield return [BanzaiConfig.PointsHijackTileKey, I(d.PointsHijackTile)];
        yield return [BanzaiConfig.MaxPlayersPerTeamKey, I(d.MaxPlayersPerTeam)];
        yield return [BanzaiConfig.LockBatchPerTickKey, I(d.LockBatchPerTick)];
    }

    [Theory]
    [MemberData(nameof(ExpectedDefaults))]
    public void Every_Banzai_Key_Is_In_The_Catalog_With_The_Matching_Default(
        string key,
        string expectedDefault
    )
    {
        ConfigKeyDescriptor? descriptor = ConfigKeyCatalog.Find(key);

        descriptor.Should().NotBeNull($"'{key}' must be a dashboard-editable config key");
        descriptor!.DefaultValue.Should().Be(expectedDefault);
        descriptor.Group.Should().Be("Banzai");
    }

    [Fact]
    public void The_Batched_Resolve_Reads_Every_Key()
    {
        // A key someone adds to the settings but forgets in AllKeys silently resolves to its
        // default forever — the batch must cover the whole group.
        BanzaiConfig.AllKeys.Should().HaveCount(5).And.OnlyHaveUniqueItems();
    }
}
