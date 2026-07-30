using FluentAssertions;
using Vortex.Primitives.MysteryBox;
using Xunit;

namespace Vortex.Players.Tests.MysteryBox;

/// <summary>
/// Locks the colour-to-state mapping, which is the only thing that makes a box render in colour: the
/// sprite carries no recolour data, so a wrong state paints the wrong box or the colourless state 0.
/// The expected values were read out of <c>mystery_box.nitro</c> by sampling each frame's dominant
/// colour — state 0 neutral, then eight groups of three in KEY_COLORS order.
/// </summary>
public sealed class MysteryBoxSpriteTests
{
    [Theory]
    [InlineData("purple", 1)]
    [InlineData("blue", 4)]
    [InlineData("green", 7)]
    [InlineData("yellow", 10)]
    [InlineData("lilac", 13)]
    [InlineData("orange", 16)]
    [InlineData("turquoise", 19)]
    [InlineData("red", 22)]
    public void ClosedState_MatchesTheAssetGroups(string color, int expected)
    {
        MysteryBoxSprite.ClosedState(color).Should().Be(expected);
        MysteryBoxSprite.WaitingState(color).Should().Be(expected + 1);
        MysteryBoxSprite.OpenState(color).Should().Be(expected + 2);
    }

    [Theory]
    [InlineData(1, "purple")]
    [InlineData(2, "purple")]
    [InlineData(3, "purple")]
    [InlineData(4, "blue")]
    [InlineData(12, "yellow")]
    [InlineData(24, "red")]
    public void ColorFromState_ReadsBackEveryFrameOfAGroup(int state, string expected)
    {
        MysteryBoxSprite.ColorFromState(state).Should().Be(expected);
    }

    [Fact]
    public void EveryColourRoundTripsThroughItsState()
    {
        foreach (string color in MysteryBoxColors.All)
        {
            MysteryBoxSprite.ColorFromState(MysteryBoxSprite.ClosedState(color)).Should().Be(color);
            MysteryBoxSprite
                .ColorFromState(MysteryBoxSprite.WaitingState(color))
                .Should()
                .Be(color);
            MysteryBoxSprite.ColorFromState(MysteryBoxSprite.OpenState(color)).Should().Be(color);
        }
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(25)]
    [InlineData(999)]
    public void StatesOutsideTheColourGroups_HaveNoColour(int state)
    {
        MysteryBoxSprite.ColorFromState(state).Should().BeEmpty();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("pink")]
    public void UnrenderableColours_FallBackToTheNeutralState(string? color)
    {
        MysteryBoxSprite.ClosedState(color).Should().Be(MysteryBoxSprite.NeutralState);
        MysteryBoxSprite.WaitingState(color).Should().Be(MysteryBoxSprite.NeutralState);
    }

    [Fact]
    public void TheEightGroupsFillTheAssetsTwentyFiveStates()
    {
        // 1 neutral + 8 colours x 3 frames. If a colour were added or removed without the asset
        // changing, this is the assertion that catches it.
        MysteryBoxColors.All.Length.Should().Be(8);
        MysteryBoxSprite.OpenState(MysteryBoxColors.All[^1]).Should().Be(24);
    }
}
