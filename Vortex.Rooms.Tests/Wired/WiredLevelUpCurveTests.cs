using FluentAssertions;
using Vortex.Rooms.Wired.LevelUp;
using Xunit;

namespace Vortex.Rooms.Tests.Wired;

/// <summary>
/// The level-up curves, and mainly one question: does the generic derivation in
/// <see cref="WiredLevelUpCurve"/> reproduce the client's linear formulas exactly?
/// </summary>
/// <remarks>
/// It has to, because the linear curve is the only one AIR implements those members for, and it is
/// therefore the only evidence of what they are supposed to mean. If the generalisation agrees with
/// it everywhere, the same code answering for the other two curves is the client's arithmetic
/// applied to a different shape; if it disagrees anywhere, it is a new invention wearing the same
/// names.
/// </remarks>
public sealed class WiredLevelUpCurveTests
{
    [Theory]
    [InlineData(1, 0)]
    [InlineData(2, 100)]
    [InlineData(5, 400)]
    [InlineData(25, 2400)]
    public void Linear_XpForLevel_IsStepTimesLevelMinusOne(int level, int expected)
    {
        new WiredLinearLevelUpCurve(100, 25).XpForLevel(level).Should().Be(expected);
    }

    /// <summary>Every derived member, against the formula AIR's LinearLevelUpper writes by hand.</summary>
    [Fact]
    public void Linear_EveryDerivedMember_MatchesTheClientsOwnFormula()
    {
        const int step = 100;
        const int maxLevel = 25;

        WiredLinearLevelUpCurve curve = new(step, maxLevel);

        for (int xp = -50; xp <= (maxLevel * step) + 200; xp++)
        {
            int bounded = xp < 0 ? 0 : System.Math.Min(xp, maxLevel * step);
            int level = System.Math.Min(maxLevel, 1 + (bounded / step));
            bool maxed = level >= maxLevel;

            curve.MaxXp.Should().Be(maxLevel * step);
            curve.BoundedValue(xp).Should().Be(bounded);
            curve.CurrentLevel(xp).Should().Be(level);
            curve.IsMaxed(xp).Should().Be(maxed);
            curve.TotalXpRequired(xp).Should().Be(maxed ? 0 : step);
            curve.Progress(xp).Should().Be(maxed ? 0 : bounded % step);
            curve.XpRemaining(xp).Should().Be(maxed ? 0 : step - (bounded % step));
            curve.ProgressPercentage(xp).Should().Be(maxed ? 0 : bounded % step * 100 / step);
        }
    }

    /// <summary>
    /// The top of the curve is where "maxed" is easy to get wrong, and AIR is explicit about it.
    /// </summary>
    [Fact]
    public void Linear_IsMaxed_AsksTheLevelAndNotTheXp()
    {
        WiredLinearLevelUpCurve curve = new(100, 25);

        curve.CurrentLevel(2400).Should().Be(25);
        curve.IsMaxed(2400).Should().BeTrue("level 25 is reached at 2,400 XP");
        curve.MaxXp.Should().Be(2500, "the whole of the last level is still spendable");
    }

    [Fact]
    public void Exponential_CostsMoreEachLevel_AndStillLevelsCorrectly()
    {
        WiredExponentialLevelUpCurve curve = new(
            firstLevelXp: 100,
            increaseFactor: 20,
            maxLevel: 10
        );

        curve.XpForLevel(1).Should().Be(0);
        curve.XpForLevel(2).Should().Be(100);
        curve.XpForLevel(3).Should().Be(220);
        curve.XpForLevel(4).Should().Be(364);

        curve.CurrentLevel(99).Should().Be(1);
        curve.CurrentLevel(100).Should().Be(2);
        curve.CurrentLevel(219).Should().Be(2);
        curve.CurrentLevel(220).Should().Be(3);

        curve.TotalXpRequired(100).Should().Be(120);
        curve.Progress(150).Should().Be(50);
        curve.XpRemaining(150).Should().Be(70);
        curve.ProgressPercentage(160).Should().Be(50);
    }

    [Fact]
    public void Manual_InterpolatesBetweenTheAnchorsItWasGiven()
    {
        WiredInterpolatedLevelUpCurve? curve = WiredInterpolatedLevelUpCurve.TryParse(
            "5=100\n10=500\n20=4000"
        );

        curve.Should().NotBeNull();
        curve!.MaxLevel.Should().Be(20);
        curve.XpForLevel(5).Should().Be(100);
        curve.XpForLevel(10).Should().Be(500);
        curve.XpForLevel(3).Should().Be(50, "halfway from level 1 at 0 XP to level 5 at 100");
        curve.CurrentLevel(100).Should().Be(5);
        curve.CurrentLevel(499).Should().Be(9);
        curve.CurrentLevel(4000).Should().Be(20);
        curve.IsMaxed(4000).Should().BeTrue();
    }

    /// <summary>
    /// One bad line voids the curve, exactly as VariableLevelUp.parseLevelToXpMap returns null and
    /// blanks the preview. Skipping the line instead would level players on a shape nobody saw.
    /// </summary>
    [Theory]
    [InlineData("5=100\n4=500")]
    [InlineData("5=100\n10=50")]
    [InlineData("5=100\n5=500")]
    [InlineData("")]
    [InlineData("nonsense")]
    public void Manual_ARefusedCurve_IsNoCurveAtAll(string text)
    {
        WiredInterpolatedLevelUpCurve.TryParse(text).Should().BeNull();
    }
}
