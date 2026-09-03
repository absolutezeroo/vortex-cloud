using System.Collections.Generic;
using System.Globalization;
using FluentAssertions;
using Vortex.Primitives.Server;
using Vortex.Rooms.Games.Football;
using Xunit;

namespace Vortex.Rooms.Tests.Football;

/// <summary>
/// Guards the deliberate duplication between the feature-side <see cref="FootballConfig"/> keys /
/// <see cref="FootballSettings"/> defaults and the dashboard-facing <see cref="ConfigKeyCatalog"/>.
/// It matters more for football than for the others: every one of its numbers is an assumption
/// rather than a Habbo behaviour, so an operator has to be able to change all of them, and a key
/// missing from the catalogue is a knob that exists and can never be turned.
/// </summary>
public sealed class FootballConfigCatalogTests
{
    private static string I(int v) => v.ToString(CultureInfo.InvariantCulture);

    public static IEnumerable<object[]> ExpectedDefaults()
    {
        FootballSettings d = FootballSettings.Default;

        yield return [FootballConfig.KickDistanceKey, I(d.KickDistance)];
        yield return [FootballConfig.BallStepMsKey, I(d.BallStepMs)];
        yield return [FootballConfig.GoalPointsKey, I(d.GoalPoints)];
        yield return [FootballConfig.GoalResetMsKey, I(d.GoalResetMs)];
        yield return [FootballConfig.MaxPlayersPerTeamKey, I(d.MaxPlayersPerTeam)];
    }

    [Theory]
    [MemberData(nameof(ExpectedDefaults))]
    public void Every_Football_Key_Is_In_The_Catalog_With_The_Matching_Default(
        string key,
        string expectedDefault
    )
    {
        ConfigKeyDescriptor? descriptor = ConfigKeyCatalog.Find(key);

        descriptor.Should().NotBeNull($"'{key}' must be a dashboard-editable config key");
        descriptor!.DefaultValue.Should().Be(expectedDefault);
        descriptor.Group.Should().Be("Football");
    }

    [Fact]
    public void The_Batched_Resolve_Reads_Every_Key()
    {
        // A key someone adds to the settings but forgets in AllKeys silently resolves to its
        // default forever — the batch must cover the whole group.
        FootballConfig.AllKeys.Should().HaveCount(5).And.OnlyHaveUniqueItems();
    }
}
