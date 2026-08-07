using FluentAssertions;
using Vortex.Primitives.Furniture;
using Xunit;

namespace Vortex.Rooms.Tests.Furniture;

/// <summary>
/// A credit furni is worth whatever its classname says, because nothing else says it: all 135
/// definitions in this catalogue carry a null extra_data and furnidata has no field for the value.
/// That makes the parse the source of truth for real money, so it is pinned rather than trusted.
/// </summary>
public sealed class CreditFurniValueTests
{
    [Theory]
    [InlineData("CF_1_coin_bronze", 1)]
    [InlineData("CF_10_coin_gold", 10)]
    [InlineData("CF_1000_goldenkey", 1000)]
    [InlineData("CF_250_goldenchessboard", 250)]
    public void TryParse_ReadsTheValueBetweenTheFirstTwoUnderscores(string name, int expected)
    {
        CreditFurniValue.TryParse(name, out int credits).Should().BeTrue();
        credits.Should().Be(expected);
    }

    [Theory]
    [InlineData("")]
    [InlineData("chair_norja")]
    [InlineData("CF_coin_gold")]
    [InlineData("CF__coin")]
    [InlineData("CF_0_coin")]
    [InlineData("CF_-50_coin")]
    [InlineData("CF_50")]
    public void TryParse_RefusesAnythingThatDoesNotNameAPositiveValue(string name)
    {
        // Redeeming has to be all-or-nothing: a mislabelled definition must pay nothing rather than
        // pay zero, because the room consumes the furniture either way and only a false here stops
        // it.
        CreditFurniValue.TryParse(name, out int credits).Should().BeFalse();
        credits.Should().Be(0);
    }

    [Fact]
    public void TryParse_DoesNotAcceptASignedOrPaddedNumber()
    {
        // NumberStyles.None: "+50" and " 50" would otherwise parse, and a classname is not a place
        // to be lenient about what a number looks like.
        CreditFurniValue.TryParse("CF_+50_coin", out _).Should().BeFalse();
    }
}
