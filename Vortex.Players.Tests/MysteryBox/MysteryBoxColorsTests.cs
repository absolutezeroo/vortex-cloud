using FluentAssertions;
using Vortex.Primitives.MysteryBox;
using Xunit;

namespace Vortex.Players.Tests.MysteryBox;

/// <summary>
/// The eight colours are client truth (<c>MysteryBoxToolbarExtension.KEY_COLORS</c> is a hardcoded
/// dictionary, and the tooltips resolve <c>${mysterybox.tracker.box.&lt;colour&gt;}</c>). Anything
/// else must be rejected before it reaches the wire, or the tracker renders untinted with a raw
/// localization key showing.
/// </summary>
public sealed class MysteryBoxColorsTests
{
    [Theory]
    [InlineData("purple")]
    [InlineData("blue")]
    [InlineData("green")]
    [InlineData("yellow")]
    [InlineData("lilac")]
    [InlineData("orange")]
    [InlineData("turquoise")]
    [InlineData("red")]
    public void EveryClientColour_IsAccepted(string color)
    {
        MysteryBoxColors.IsValid(color).Should().BeTrue();
        MysteryBoxColors.Normalize(color).Should().Be(color);
    }

    [Theory]
    [InlineData("PURPLE", "purple")]
    [InlineData("  Blue  ", "blue")]
    [InlineData("ReD", "red")]
    public void CasingAndPadding_AreNormalizedAway(string input, string expected)
    {
        MysteryBoxColors.Normalize(input).Should().Be(expected);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("pink")]
    [InlineData("#ff00ff")]
    public void UnrenderableColours_NormalizeToEmpty(string? input)
    {
        MysteryBoxColors.Normalize(input).Should().BeEmpty();
        MysteryBoxColors.IsValid(input).Should().BeFalse();
    }
}
