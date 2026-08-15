using FluentAssertions;
using Vortex.Primitives.Rooms.Enums.Wired;
using Vortex.Rooms.Wired;
using Xunit;

namespace Vortex.Rooms.Tests.Wired;

/// <summary>
/// The two numeric readings every variable box depends on: the client's two-int encoding of a
/// signed long, and the comparison operator it sends alongside it.
/// </summary>
public sealed class WiredNumericParamTests
{
    [Theory]
    [InlineData(0, 0, 0L)]
    [InlineData(0, 42, 42L)]
    [InlineData(0, int.MaxValue, 2147483647L)]
    // The client writes -1 as the high word for anything negative (Util.pushIntAsLong).
    [InlineData(-1, -5, -5L)]
    [InlineData(-1, int.MinValue, -2147483648L)]
    public void IntAsLong_RecombinesThePairTheClientPushed(int high, int low, long expected) =>
        WiredIntAsLong.Read(high, low).Should().Be(expected);

    [Fact]
    public void IntAsLong_BeyondTheIntRange_Saturates()
    {
        // A genuine 64-bit value cannot fit the int a wired variable holds; keeping the sign and
        // the magnitude is what makes a ">" comparison still answer sensibly.
        WiredIntAsLong.ReadClamped(1, 0).Should().Be(int.MaxValue);
        WiredIntAsLong.ReadClamped(-2, 0).Should().Be(int.MinValue);
    }

    [Fact]
    public void IntAsLong_ReadingOnlyTheLowHalf_WouldLoseTheSign()
    {
        // Guards the mistake this helper exists to prevent: the value is not intParams[3].
        WiredIntAsLong.Read(-1, -5).Should().NotBe(-1);
    }

    [Theory]
    [InlineData(WiredComparisonType.LessThan, 4, 10, true)]
    [InlineData(WiredComparisonType.LessThan, 10, 10, false)]
    [InlineData(WiredComparisonType.Equals, 10, 10, true)]
    [InlineData(WiredComparisonType.GreaterThan, 11, 10, true)]
    [InlineData(WiredComparisonType.LessThanOrEquals, 10, 10, true)]
    [InlineData(WiredComparisonType.NotEquals, 9, 10, true)]
    [InlineData(WiredComparisonType.GreaterTHanOrEquals, 10, 10, true)]
    [InlineData(WiredComparisonType.GreaterTHanOrEquals, 9, 10, false)]
    public void Comparison_MatchesTheOperatorTheClientSends(
        WiredComparisonType comparison,
        long left,
        long right,
        bool expected
    ) => WiredVariableComparison.Matches(left, comparison, right).Should().Be(expected);

    [Fact]
    public void Comparison_UnknownOperator_FailsClosed()
    {
        // Passing everything would turn a corrupt config into a condition that always fires.
        WiredVariableComparison.Matches(1, (WiredComparisonType)99, 1).Should().BeFalse();
    }
}
