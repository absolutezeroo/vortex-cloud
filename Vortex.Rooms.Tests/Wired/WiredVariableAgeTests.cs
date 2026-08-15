using FluentAssertions;
using Vortex.Primitives.Rooms.Enums.Wired;
using Vortex.Rooms.Wired;
using Xunit;

namespace Vortex.Rooms.Tests.Wired;

/// <summary>
/// The duration arithmetic behind the "variable age" condition: a number and a unit dropdown, and
/// only two of the six comparisons.
/// </summary>
public sealed class WiredVariableAgeTests
{
    [Theory]
    [InlineData(1, WiredTimeUnit.Milliseconds, 1L)]
    [InlineData(1, WiredTimeUnit.Seconds, 1_000L)]
    [InlineData(2, WiredTimeUnit.Minutes, 120_000L)]
    [InlineData(1, WiredTimeUnit.Hours, 3_600_000L)]
    [InlineData(1, WiredTimeUnit.Days, 86_400_000L)]
    [InlineData(1, WiredTimeUnit.Weeks, 604_800_000L)]
    // No calendar on this form, so a month is 30 days and a year 365.
    [InlineData(1, WiredTimeUnit.Months, 2_592_000_000L)]
    [InlineData(1, WiredTimeUnit.Years, 31_536_000_000L)]
    public void ConvertsTheUnit(int duration, WiredTimeUnit unit, long expected) =>
        WiredVariableAge.ToMilliseconds(duration, unit).Should().Be(expected);

    [Fact]
    public void AnAbsurdDuration_Saturates_RatherThanOverflowing()
    {
        // The form accepts the whole int range against a unit as large as years.
        WiredVariableAge
            .ToMilliseconds(int.MaxValue, WiredTimeUnit.Years)
            .Should()
            .Be(long.MaxValue);

        WiredVariableAge
            .ToMilliseconds(int.MinValue, WiredTimeUnit.Years)
            .Should()
            .Be(long.MinValue);
    }

    [Theory]
    [InlineData(WiredComparisonType.GreaterThan, 5_000L, 1_000L, true)]
    [InlineData(WiredComparisonType.GreaterThan, 500L, 1_000L, false)]
    [InlineData(WiredComparisonType.LessThan, 500L, 1_000L, true)]
    [InlineData(WiredComparisonType.LessThan, 5_000L, 1_000L, false)]
    public void ComparesTheAge(
        WiredComparisonType comparison,
        long ageMs,
        long durationMs,
        bool expected
    ) => WiredVariableAge.Matches(ageMs, comparison, durationMs).Should().Be(expected);

    [Fact]
    public void AComparisonTheFormCannotProduce_FailsClosed()
    {
        // The form only draws "Lower than" and "Higher than"; equality on a millisecond age would
        // never be true anyway, and firing on it would be worse.
        WiredVariableAge.Matches(1_000, WiredComparisonType.Equals, 1_000).Should().BeFalse();
    }

    [Fact]
    public void ANegativeAge_IsReadAsZero()
    {
        // A value stamped in the future (a clock change) is not "very young".
        WiredVariableAge.Matches(-5_000, WiredComparisonType.LessThan, 1_000).Should().BeTrue();
        WiredVariableAge.Matches(-5_000, WiredComparisonType.GreaterThan, 1_000).Should().BeFalse();
    }
}
