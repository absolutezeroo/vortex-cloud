using FluentAssertions;
using Vortex.Dashboard.API.Operations;
using Vortex.Primitives.Players.Enums.Wallet;
using Xunit;

namespace Vortex.Dashboard.Tests.Hosting;

/// <summary>
/// The endpoint decides which currencies this operation will touch, and it has to refuse the two it
/// must not: credits and activity points reach the same enum, but each has its own endpoint, and
/// routing an activity-point grant through here would drop the point type it needs.
/// </summary>
public sealed class CollectiblesCurrencyParsingTests
{
    [Theory]
    [InlineData("silver", CurrencyType.Silver)]
    [InlineData("emeralds", CurrencyType.Emeralds)]
    [InlineData("  SILVER  ", CurrencyType.Silver)]
    [InlineData("Emeralds", CurrencyType.Emeralds)]
    public void AcceptsTheTwoCollectiblesCurrencies(string value, CurrencyType expected)
    {
        DashboardOperationsService
            .TryParseCollectiblesCurrency(value, out CurrencyType currency)
            .Should()
            .BeTrue();

        currency.Should().Be(expected);
    }

    [Theory]
    [InlineData("credits")]
    [InlineData("activitypoints")]
    [InlineData("duckets")]
    [InlineData("")]
    [InlineData(null)]
    public void RefusesEverythingElse(string? value)
    {
        DashboardOperationsService.TryParseCollectiblesCurrency(value, out _).Should().BeFalse();
    }
}
